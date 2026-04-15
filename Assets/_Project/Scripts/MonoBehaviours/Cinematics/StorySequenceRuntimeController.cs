using System;
using System.Collections;
using FarmSimVR.Core;
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

        [SerializeField] private string orchestratorBaseUrl = TownVoiceTokenServiceEndpointResolver.ProductionBaseUrl;

        private string _activeSessionId = string.Empty;
        private string _activeBaseUrl = string.Empty;
        private string _lastError = string.Empty;
        private string _lastOrchestratorReadyBaseUrl = string.Empty;
        private string _preparedEntrySceneName = string.Empty;
        private bool _ensureOrchestratorInFlight;
        private bool _requestInFlight;
        private Func<Action<LocalStoryOrchestratorReadyResult>, IEnumerator> _ensureOrchestratorReadyOverride;
        private Func<Action<StorySequenceAdvancePayload>, IEnumerator> _beginSequenceRequestOverride;
        private Func<string, Action<StorySequenceAdvancePayload>, IEnumerator> _advanceSequenceRequestOverride;
        private Action<string> _sceneLoadOverride;
        private Action<string> _beginSequenceUnavailableCallback;

        public static StorySequenceRuntimeController Instance { get; private set; }

        public string ActiveSessionId => _activeSessionId ?? string.Empty;
        public string LastError => _lastError ?? string.Empty;
        public string PreparedEntrySceneName => _preparedEntrySceneName ?? string.Empty;
        public bool HasActiveSession => !string.IsNullOrWhiteSpace(ActiveSessionId);
        public bool HasPreparedSequence => HasActiveSession && !string.IsNullOrWhiteSpace(PreparedEntrySceneName);

        public static StorySequenceRuntimeController GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            var runtimeObject = new GameObject(ControllerObjectName);
            var controller = runtimeObject.AddComponent<StorySequenceRuntimeController>();
            Instance = controller;
            return controller;
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

        public bool BeginSequenceAndLoad(string fallbackSceneName, Action<string> onUnavailable = null)
        {
            if (_requestInFlight)
            {
                GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), "BeginSequenceAndLoad rejected because a request is already in flight.");
                return false;
            }

            _beginSequenceUnavailableCallback = onUnavailable;
            StartCoroutine(BeginSequenceAndLoadRoutine(fallbackSceneName));
            return true;
        }

        public bool BeginSequencePreparation(Action<string> onPrepared, Action<string> onUnavailable = null)
        {
            if (_requestInFlight)
            {
                GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), "BeginSequencePreparation rejected because a request is already in flight.");
                return false;
            }

            _beginSequenceUnavailableCallback = onUnavailable;
            StartCoroutine(BeginSequencePreparationRoutine(onPrepared));
            return true;
        }

        public bool TryContinueGeneratedSequence(TutorialFlowController flowController, string currentSceneName)
        {
            if (_requestInFlight || !HasActiveSession || !IsGeneratedTerminalBeat(currentSceneName))
                return false;

            StartCoroutine(AdvanceSequenceAndLoadRoutine(flowController, currentSceneName));
            return true;
        }

        public void EnsureLocalOrchestratorRunningInBackground()
        {
            if (_requestInFlight || _ensureOrchestratorInFlight)
                return;

            StartCoroutine(EnsureLocalOrchestratorReady(_ => { }));
        }

        public void ClearSequenceState()
        {
            GeneratedStorySliceDiagnostics.Log(nameof(StorySequenceRuntimeController), "Clearing generated sequence runtime state.");
            _activeSessionId = string.Empty;
            _activeBaseUrl = string.Empty;
            _lastOrchestratorReadyBaseUrl = string.Empty;
            _lastError = string.Empty;
            _preparedEntrySceneName = string.Empty;
            _ensureOrchestratorInFlight = false;
            _requestInFlight = false;
            _beginSequenceUnavailableCallback = null;
            StoryPackageRuntimeCatalog.ClearRuntimeOverride();
        }

        private IEnumerator BeginSequenceAndLoadRoutine(string fallbackSceneName)
        {
            _ = fallbackSceneName;
            ResetPreparedSequence();
            yield return ExecuteBeginSequenceRequest(payload =>
                LoadSceneLogicalOrFallback(payload.EntrySceneName));
        }

        private IEnumerator BeginSequencePreparationRoutine(Action<string> onPrepared)
        {
            ResetPreparedSequence();
            yield return ExecuteBeginSequenceRequest(_ =>
                onPrepared?.Invoke(PreparedEntrySceneName));
        }

        private IEnumerator ExecuteBeginSequenceRequest(Action<StorySequenceAdvancePayload> onSuccess)
        {
            GeneratedStorySliceDiagnostics.Log(nameof(StorySequenceRuntimeController), $"Starting generated sequence request against '{orchestratorBaseUrl}'.");
            _requestInFlight = true;

            LocalStoryOrchestratorReadyResult readyResult = null;
            var ensureRoutine = EnsureLocalOrchestratorReady(result => readyResult = result);
            yield return ensureRoutine;

            if (readyResult == null || !readyResult.Success)
            {
                _requestInFlight = false;
                string bootstrapError = readyResult?.ErrorMessage ?? "Local story-orchestrator is unavailable.";
                GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), $"Generated sequence bootstrap failed: {bootstrapError}");
                var onUnavailable = _beginSequenceUnavailableCallback;
                _beginSequenceUnavailableCallback = null;
                ResetActiveSession(bootstrapError);
                onUnavailable?.Invoke(bootstrapError);
                yield break;
            }

            StorySequenceAdvancePayload payload = null;
            string requestBaseUrl = string.IsNullOrWhiteSpace(readyResult.BaseUrl)
                ? orchestratorBaseUrl
                : readyResult.BaseUrl;
            var request = _beginSequenceRequestOverride != null
                ? _beginSequenceRequestOverride(result => payload = result)
                : StorySequenceServiceClient.CreateSessionAndAdvance(
                    requestBaseUrl,
                    result => payload = result);

            yield return request;

            _requestInFlight = false;
            if (!TryApplyPayload(payload, out var errorMessage))
            {
                GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), $"Generated sequence request failed: {errorMessage}");
                var onUnavailable = _beginSequenceUnavailableCallback;
                _beginSequenceUnavailableCallback = null;
                ResetActiveSession(errorMessage);
                onUnavailable?.Invoke(errorMessage);
                yield break;
            }

            GeneratedStorySliceDiagnostics.Log(nameof(StorySequenceRuntimeController), $"Generated sequence prepared for session '{_activeSessionId}' with entry scene '{_preparedEntrySceneName}'.");
            _beginSequenceUnavailableCallback = null;
            onSuccess?.Invoke(payload);
        }

        private IEnumerator AdvanceSequenceAndLoadRoutine(
            TutorialFlowController flowController,
            string currentSceneName)
        {
            GeneratedStorySliceDiagnostics.Log(nameof(StorySequenceRuntimeController), $"Advancing generated sequence from terminal beat scene '{currentSceneName}'.");
            _requestInFlight = true;

            LocalStoryOrchestratorReadyResult readyResult = null;
            var ensureRoutine = EnsureLocalOrchestratorReady(result => readyResult = result);
            yield return ensureRoutine;

            if (readyResult == null || !readyResult.Success)
            {
                _requestInFlight = false;
                string bootstrapError = readyResult?.ErrorMessage ?? "Local story-orchestrator is unavailable.";
                _lastError = bootstrapError;
                GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), $"Generated sequence continuation bootstrap failed: {bootstrapError}");
                flowController?.NotifyGeneratedSequenceContinuationUnavailable();
                yield break;
            }

            StorySequenceAdvancePayload payload = null;
            string requestBaseUrl = string.IsNullOrWhiteSpace(readyResult.BaseUrl)
                ? (string.IsNullOrWhiteSpace(_activeBaseUrl) ? orchestratorBaseUrl : _activeBaseUrl)
                : readyResult.BaseUrl;
            var request = _advanceSequenceRequestOverride != null
                ? _advanceSequenceRequestOverride(
                    ActiveSessionId,
                    result => payload = result)
                : StorySequenceServiceClient.AdvanceSession(
                    requestBaseUrl,
                    ActiveSessionId,
                    result => payload = result);

            yield return request;

            _requestInFlight = false;
            if (!TryApplyPayload(payload, out var errorMessage))
            {
                GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), $"Generated sequence continuation failed: {errorMessage}");
                _lastError = errorMessage;
                flowController?.NotifyGeneratedSequenceContinuationUnavailable();
                yield break;
            }

            GeneratedStorySliceDiagnostics.Log(nameof(StorySequenceRuntimeController), $"Generated sequence continuation resolved entry scene '{payload.EntrySceneName}'.");
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
            {
                GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), $"Rejected generated payload before import. Error: {errorMessage}");
                return false;
            }

            if (!StoryPackageRuntimeCatalog.TrySetRuntimeOverride(payload.RuntimePackage, out errorMessage))
            {
                GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), $"Generated payload package import failed: {errorMessage}");
                return false;
            }

            _activeSessionId = payload.SessionId;
            _activeBaseUrl = payload.BaseUrl;
            _lastError = string.Empty;
            _preparedEntrySceneName = ResolvePreparedEntrySceneName(payload.EntrySceneName);
            GeneratedStorySliceDiagnostics.Log(nameof(StorySequenceRuntimeController), $"Applied generated payload for session '{_activeSessionId}' with prepared scene '{_preparedEntrySceneName}'.");
            return true;
        }

        private IEnumerator EnsureLocalOrchestratorReady(Action<LocalStoryOrchestratorReadyResult> onComplete)
        {
            if (_ensureOrchestratorInFlight)
            {
                while (_ensureOrchestratorInFlight)
                    yield return null;

                onComplete?.Invoke(BuildCachedOrchestratorReadyResult());
                yield break;
            }

            _ensureOrchestratorInFlight = true;

            LocalStoryOrchestratorReadyResult readyResult = null;
            var ensureRoutine = _ensureOrchestratorReadyOverride != null
                ? _ensureOrchestratorReadyOverride(result => readyResult = result)
                : LocalStoryOrchestratorLauncher.EnsureReady(orchestratorBaseUrl, result => readyResult = result);
            yield return ensureRoutine;

            _ensureOrchestratorInFlight = false;

            if (readyResult != null && readyResult.Success && !string.IsNullOrWhiteSpace(readyResult.BaseUrl))
            {
                _lastOrchestratorReadyBaseUrl = readyResult.BaseUrl;
                orchestratorBaseUrl = readyResult.BaseUrl;
            }
            else if (readyResult != null && !readyResult.Success)
            {
                _lastOrchestratorReadyBaseUrl = string.Empty;
            }

            onComplete?.Invoke(readyResult ?? BuildCachedOrchestratorReadyResult());
        }

        private void ResetActiveSession(string errorMessage)
        {
            GeneratedStorySliceDiagnostics.LogWarning(nameof(StorySequenceRuntimeController), $"Resetting generated runtime session state. Error: {errorMessage}");
            StoryPackageRuntimeCatalog.ClearRuntimeOverride();
            _activeSessionId = string.Empty;
            _activeBaseUrl = string.Empty;
            _lastError = errorMessage ?? string.Empty;
            _preparedEntrySceneName = string.Empty;
        }

        private void ResetPreparedSequence()
        {
            _preparedEntrySceneName = string.Empty;
            _lastError = string.Empty;
        }

        private static string ResolvePreparedEntrySceneName(string sceneName)
        {
            return string.IsNullOrWhiteSpace(sceneName)
                ? TutorialSceneCatalog.PostChickenCutsceneSceneName
                : sceneName;
        }

        private LocalStoryOrchestratorReadyResult BuildCachedOrchestratorReadyResult()
        {
            if (!string.IsNullOrWhiteSpace(_lastOrchestratorReadyBaseUrl))
                return new LocalStoryOrchestratorReadyResult(_lastOrchestratorReadyBaseUrl, true, false, string.Empty);

            return new LocalStoryOrchestratorReadyResult(
                orchestratorBaseUrl,
                false,
                false,
                "Local story-orchestrator is unavailable.");
        }

        private void LoadSceneLogicalOrFallback(string sceneName)
        {
            var logicalSceneName = ResolvePreparedEntrySceneName(sceneName);
            var loadableSceneName = SceneWorkCatalog.GetLoadableSceneName(logicalSceneName);
            GeneratedStorySliceDiagnostics.Log(nameof(StorySequenceRuntimeController), $"Loading generated scene '{logicalSceneName}' as build scene '{loadableSceneName}'.");

            if (_sceneLoadOverride != null)
            {
                _sceneLoadOverride(loadableSceneName);
                return;
            }

            SceneManager.LoadScene(loadableSceneName);
        }
    }
}
