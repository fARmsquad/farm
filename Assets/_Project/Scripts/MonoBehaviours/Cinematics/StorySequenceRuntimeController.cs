using System;
using System.Collections;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public sealed class StorySequenceRuntimeController : MonoBehaviour
    {
        private const string ControllerObjectName = "StorySequenceRuntimeController";
        private const string GeneratedBeatPrefix = "sequence_turn_";

        [SerializeField] private string orchestratorBaseUrl = "http://127.0.0.1:8012";

        private string _activeSessionId = string.Empty;
        private string _activeBaseUrl = string.Empty;
        private string _lastError = string.Empty;
        private bool _requestInFlight;
        private Func<Action<StorySequenceAdvancePayload>, IEnumerator> _beginSequenceRequestOverride;
        private Func<string, Action<StorySequenceAdvancePayload>, IEnumerator> _advanceSequenceRequestOverride;
        private Action<string> _sceneLoadOverride;

        public static StorySequenceRuntimeController Instance { get; private set; }

        public string ActiveSessionId => _activeSessionId ?? string.Empty;
        public string LastError => _lastError ?? string.Empty;
        public bool HasActiveSession => !string.IsNullOrWhiteSpace(ActiveSessionId);

        public static StorySequenceRuntimeController GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            var runtimeObject = new GameObject(ControllerObjectName);
            return runtimeObject.AddComponent<StorySequenceRuntimeController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BeginSequenceAndLoad(string fallbackSceneName)
        {
            if (_requestInFlight)
                return;

            StartCoroutine(BeginSequenceAndLoadRoutine(fallbackSceneName));
        }

        public bool TryContinueGeneratedSequence(TutorialFlowController flowController, string currentSceneName)
        {
            if (_requestInFlight || !HasActiveSession || !IsGeneratedTerminalBeat(currentSceneName))
                return false;

            StartCoroutine(AdvanceSequenceAndLoadRoutine(flowController, currentSceneName));
            return true;
        }

        public void ClearSequenceState()
        {
            _activeSessionId = string.Empty;
            _activeBaseUrl = string.Empty;
            _lastError = string.Empty;
            _requestInFlight = false;
            StoryPackageRuntimeCatalog.ClearRuntimeOverride();
        }

        private IEnumerator BeginSequenceAndLoadRoutine(string fallbackSceneName)
        {
            _requestInFlight = true;

            StorySequenceAdvancePayload payload = null;
            var request = _beginSequenceRequestOverride != null
                ? _beginSequenceRequestOverride(result => payload = result)
                : StorySequenceServiceClient.CreateSessionAndAdvance(
                    orchestratorBaseUrl,
                    result => payload = result);

            yield return request;

            _requestInFlight = false;
            if (!TryApplyPayload(payload, out var errorMessage))
            {
                ResetActiveSession(errorMessage);
                LoadSceneLogicalOrFallback(fallbackSceneName);
                yield break;
            }

            LoadSceneLogicalOrFallback(payload.EntrySceneName);
        }

        private IEnumerator AdvanceSequenceAndLoadRoutine(
            TutorialFlowController flowController,
            string currentSceneName)
        {
            _requestInFlight = true;

            StorySequenceAdvancePayload payload = null;
            var request = _advanceSequenceRequestOverride != null
                ? _advanceSequenceRequestOverride(
                    ActiveSessionId,
                    result => payload = result)
                : StorySequenceServiceClient.AdvanceSession(
                    string.IsNullOrWhiteSpace(_activeBaseUrl) ? orchestratorBaseUrl : _activeBaseUrl,
                    ActiveSessionId,
                    result => payload = result);

            yield return request;

            _requestInFlight = false;
            if (!TryApplyPayload(payload, out var errorMessage))
            {
                _lastError = errorMessage;
                flowController?.NotifyGeneratedSequenceContinuationUnavailable();
                yield break;
            }

            LoadSceneLogicalOrFallback(payload.EntrySceneName);
        }

        private bool IsGeneratedTerminalBeat(string currentSceneName)
        {
            if (!StoryPackageRuntimeCatalog.TryGetBeat(currentSceneName, out var beat) || beat == null)
                return false;

            return !string.IsNullOrWhiteSpace(beat.BeatId) &&
                   beat.BeatId.StartsWith(GeneratedBeatPrefix, StringComparison.Ordinal) &&
                   string.IsNullOrWhiteSpace(beat.NextSceneName);
        }

        private bool TryApplyPayload(StorySequenceAdvancePayload payload, out string errorMessage)
        {
            errorMessage = payload?.ErrorMessage ?? "Generated story sequence response was empty.";
            if (payload == null || !payload.Success)
                return false;

            if (!StoryPackageRuntimeCatalog.TrySetRuntimeOverride(payload.RuntimePackage, out errorMessage))
                return false;

            _activeSessionId = payload.SessionId;
            _activeBaseUrl = payload.BaseUrl;
            _lastError = string.Empty;
            return true;
        }

        private void ResetActiveSession(string errorMessage)
        {
            StoryPackageRuntimeCatalog.ClearRuntimeOverride();
            _activeSessionId = string.Empty;
            _activeBaseUrl = string.Empty;
            _lastError = errorMessage ?? string.Empty;
        }

        private void LoadSceneLogicalOrFallback(string sceneName)
        {
            var logicalSceneName = string.IsNullOrWhiteSpace(sceneName)
                ? TutorialSceneCatalog.PostChickenCutsceneSceneName
                : sceneName;
            var loadableSceneName = SceneWorkCatalog.GetLoadableSceneName(logicalSceneName);

            if (_sceneLoadOverride != null)
            {
                _sceneLoadOverride(loadableSceneName);
                return;
            }

            SceneManager.LoadScene(loadableSceneName);
        }
    }
}
