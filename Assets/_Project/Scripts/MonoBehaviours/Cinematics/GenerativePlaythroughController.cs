using System;
using System.Collections;
using FarmSimVR.Core;
using FarmSimVR.MonoBehaviours.Tutorial;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public sealed class GenerativePlaythroughController : MonoBehaviour
    {
        private const string ControllerObjectName = "GenerativePlaythroughController";
        private const string SessionIdPrefKey = "FarmSimVR.GenerativeRuntime.SessionId";
        private const string BaseUrlPrefKey = "FarmSimVR.GenerativeRuntime.BaseUrl";
        private const string JobIdPrefKey = "FarmSimVR.GenerativeRuntime.JobId";
        private const float JobPollIntervalSeconds = 0.5f;

        [SerializeField] private string orchestratorBaseUrl = TownVoiceTokenServiceEndpointResolver.ProductionBaseUrl;

        private string _activeSessionId = string.Empty;
        private string _activeBaseUrl = string.Empty;
        private string _activeJobId = string.Empty;
        private string _lastError = string.Empty;
        private string _preparedEntrySceneName = string.Empty;
        private bool _requestInFlight;
        private bool _initialized;
        private bool _resumeAttempted;
        private bool _resumeInFlight;
        private int _operationVersion;

        public static GenerativePlaythroughController Instance { get; private set; }

        public string ActiveBaseUrl => _activeBaseUrl ?? string.Empty;
        public string ActiveSessionId => _activeSessionId ?? string.Empty;
        public string ActiveJobId => _activeJobId ?? string.Empty;
        public string LastError => _lastError ?? string.Empty;
        public string PreparedEntrySceneName => _preparedEntrySceneName ?? string.Empty;
        public bool HasActiveSession => !string.IsNullOrWhiteSpace(ActiveSessionId);
        public bool HasPendingOperation => _requestInFlight || _resumeInFlight;
        public bool HasPreparedSequence => !HasPendingOperation
            && HasActiveSession
            && !string.IsNullOrWhiteSpace(PreparedEntrySceneName)
            && GenerativeTurnRuntimeState.HasPreparedTurn;

        public static GenerativePlaythroughController GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            var runtimeObject = new GameObject(ControllerObjectName);
            var controller = runtimeObject.AddComponent<GenerativePlaythroughController>();
            controller.InitializeInstance();
            return controller;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeInstance();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void EnsureLocalOrchestratorRunningInBackground()
        {
            if (!_resumeAttempted && !_resumeInFlight)
                StartCoroutine(ResumePreparedTurnIfAvailable());
        }

        public bool BeginSequencePreparation(Action<string> onPrepared, Action<string> onUnavailable = null)
        {
            if (_requestInFlight)
                return false;

            var operationVersion = BeginOperation();
            _requestInFlight = true;
            StartCoroutine(BeginSequencePreparationRoutine(operationVersion, onPrepared, onUnavailable));
            return true;
        }

        public bool PrepareExistingReadyTurn(
            string baseUrl,
            string sessionId,
            string turnId,
            Action<string> onPrepared,
            Action<string> onUnavailable = null)
        {
            if (_requestInFlight ||
                string.IsNullOrWhiteSpace(baseUrl) ||
                string.IsNullOrWhiteSpace(sessionId) ||
                string.IsNullOrWhiteSpace(turnId))
            {
                return false;
            }

            var operationVersion = BeginOperation();
            _activeBaseUrl = baseUrl;
            _activeSessionId = sessionId;
            _activeJobId = string.Empty;
            _preparedEntrySceneName = string.Empty;
            _lastError = string.Empty;
            _requestInFlight = true;
            GenerativeTurnRuntimeState.Clear();
            PersistState();
            StartCoroutine(PrepareSpecificTurn(operationVersion, turnId, onPrepared, onUnavailable));
            return true;
        }

        public bool TryContinueGeneratedSequence(TutorialFlowController flowController, string currentSceneName)
        {
            if (_requestInFlight || !HasPreparedSequence)
                return false;

            var envelope = GenerativeTurnRuntimeState.PreparedTurn;
            if (envelope?.minigame == null || !string.Equals(envelope.minigame.scene_name, currentSceneName, StringComparison.Ordinal))
                return false;

            var operationVersion = BeginOperation();
            _requestInFlight = true;
            StartCoroutine(SubmitOutcomeAndPrepareNextRoutine(operationVersion, flowController, true));
            return true;
        }

        public void ClearSequenceState()
        {
            _operationVersion++;
            _activeSessionId = string.Empty;
            _activeBaseUrl = string.Empty;
            _activeJobId = string.Empty;
            _lastError = string.Empty;
            _preparedEntrySceneName = string.Empty;
            _requestInFlight = false;
            _resumeInFlight = false;
            GenerativeTurnRuntimeState.Clear();
            ClearPersistedState();
        }

        private IEnumerator BeginSequencePreparationRoutine(
            int operationVersion,
            Action<string> onPrepared,
            Action<string> onUnavailable)
        {
            GenerativeTurnRuntimeState.Clear();
            _preparedEntrySceneName = string.Empty;
            _lastError = string.Empty;

            LocalStoryOrchestratorReadyResult readyResult = null;
            yield return LocalStoryOrchestratorLauncher.EnsureReady(orchestratorBaseUrl, result => readyResult = result);
            if (!IsOperationCurrent(operationVersion))
                yield break;

            if (readyResult == null || !readyResult.Success)
            {
                _requestInFlight = false;
                _lastError = readyResult?.ErrorMessage ?? "Story-orchestrator service is unavailable.";
                onUnavailable?.Invoke(_lastError);
                yield break;
            }

            GenerativeRuntimeCreateSessionPayload createPayload = null;
            yield return GenerativeRuntimeClient.CreateSession(
                readyResult.BaseUrl,
                result => createPayload = result);
            if (!IsOperationCurrent(operationVersion))
                yield break;

            if (createPayload == null || !createPayload.Success)
            {
                _requestInFlight = false;
                _lastError = createPayload?.ErrorMessage ?? "Runtime session create failed.";
                onUnavailable?.Invoke(_lastError);
                yield break;
            }

            _activeBaseUrl = createPayload.BaseUrl;
            _activeSessionId = createPayload.Response.session_id;
            _activeJobId = createPayload.Response.job_id;
            PersistState();
            yield return PrepareReadyTurnFromJob(operationVersion, _activeJobId, onPrepared, onUnavailable);
        }

        private IEnumerator SubmitOutcomeAndPrepareNextRoutine(
            int operationVersion,
            TutorialFlowController flowController,
            bool success)
        {
            var envelope = GenerativeTurnRuntimeState.PreparedTurn;
            if (envelope == null)
            {
                _requestInFlight = false;
                flowController?.NotifyGeneratedSequenceContinuationUnavailable();
                yield break;
            }

            GenerativeRuntimeOutcomePayload outcomePayload = null;
            yield return GenerativeRuntimeClient.SubmitOutcome(
                _activeBaseUrl,
                _activeSessionId,
                envelope.turn_id,
                success,
                result => outcomePayload = result);
            if (!IsOperationCurrent(operationVersion))
                yield break;

            if (outcomePayload == null || !outcomePayload.Success || outcomePayload.Response == null)
            {
                _requestInFlight = false;
                _lastError = outcomePayload?.ErrorMessage ?? "Runtime outcome submit failed.";
                flowController?.NotifyGeneratedSequenceContinuationUnavailable();
                yield break;
            }

            var sessionState = outcomePayload.Response.session_state;
            if (sessionState != null &&
                string.Equals(sessionState.status, "completed", StringComparison.Ordinal))
            {
                CompleteFiniteSequence();
                flowController?.NotifyGeneratedSequenceContinuationUnavailable();
                yield break;
            }

            _activeJobId = outcomePayload.Response.next_job_id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_activeJobId))
            {
                CompleteFiniteSequence();
                flowController?.NotifyGeneratedSequenceContinuationUnavailable();
                yield break;
            }

            PersistState();
            yield return PrepareReadyTurnFromJob(
                operationVersion,
                _activeJobId,
                sceneName => LoadPreparedScene(sceneName),
                _ =>
                {
                    if (!IsOperationCurrent(operationVersion))
                        return;
                    _requestInFlight = false;
                    flowController?.NotifyGeneratedSequenceContinuationUnavailable();
                });
        }

        private IEnumerator PrepareReadyTurnFromJob(
            int operationVersion,
            string jobId,
            Action<string> onPrepared,
            Action<string> onUnavailable)
        {
            GenerativeRuntimeJobPayload jobPayload = null;
            while (true)
            {
                if (!IsOperationCurrent(operationVersion))
                    yield break;

                yield return GenerativeRuntimeClient.GetJob(_activeBaseUrl, jobId, result => jobPayload = result);
                if (!IsOperationCurrent(operationVersion))
                    yield break;

                if (jobPayload == null || !jobPayload.Success || jobPayload.Job == null)
                {
                    _requestInFlight = false;
                    _lastError = jobPayload?.ErrorMessage ?? "Runtime job polling failed.";
                    onUnavailable?.Invoke(_lastError);
                    yield break;
                }

                var status = jobPayload.Job.status ?? string.Empty;
                if (string.Equals(status, "ready", StringComparison.Ordinal))
                    break;

                if (string.Equals(status, "failed", StringComparison.Ordinal) || string.Equals(status, "cancelled", StringComparison.Ordinal))
                {
                    _requestInFlight = false;
                    _lastError = string.IsNullOrWhiteSpace(jobPayload.Job.error_message)
                        ? "Generated runtime job failed."
                        : jobPayload.Job.error_message;
                    onUnavailable?.Invoke(_lastError);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(JobPollIntervalSeconds);
            }

            if (string.IsNullOrWhiteSpace(jobPayload.Job.turn_id))
            {
                _requestInFlight = false;
                _lastError = "Generated runtime job completed without a turn identifier.";
                onUnavailable?.Invoke(_lastError);
                yield break;
            }

            yield return PrepareSpecificTurn(operationVersion, jobPayload.Job.turn_id, onPrepared, onUnavailable);
        }

        private IEnumerator PrepareSpecificTurn(
            int operationVersion,
            string turnId,
            Action<string> onPrepared,
            Action<string> onUnavailable)
        {
            if (string.IsNullOrWhiteSpace(turnId))
            {
                _requestInFlight = false;
                _lastError = "Generated runtime turn identifier is missing.";
                onUnavailable?.Invoke(_lastError);
                yield break;
            }

            GenerativeRuntimeTurnPayload turnPayload = null;
            yield return GenerativeRuntimeClient.GetTurn(
                _activeBaseUrl,
                _activeSessionId,
                turnId,
                result => turnPayload = result);
            if (!IsOperationCurrent(operationVersion))
                yield break;

            if (turnPayload == null || !turnPayload.Success || turnPayload.Envelope == null)
            {
                _requestInFlight = false;
                _lastError = turnPayload?.ErrorMessage ?? "Runtime turn fetch failed.";
                onUnavailable?.Invoke(_lastError);
                yield break;
            }

            if (!GenerativeTurnRuntimeState.IsPlayableEnvelope(turnPayload.Envelope, out var envelopeError))
            {
                _requestInFlight = false;
                _lastError = string.IsNullOrWhiteSpace(envelopeError)
                    ? "Runtime turn envelope was invalid."
                    : envelopeError;
                _activeSessionId = string.Empty;
                _activeBaseUrl = string.Empty;
                _activeJobId = string.Empty;
                _preparedEntrySceneName = string.Empty;
                GenerativeTurnRuntimeState.Clear();
                ClearPersistedState();
                onUnavailable?.Invoke(_lastError);
                yield break;
            }

            PreloadedGenerativeTurnAssets preloadedAssets = null;
            string preloadError = string.Empty;
            yield return ArtifactPreloader.PreloadTurn(
                _activeBaseUrl,
                turnPayload.Envelope,
                (assets, error) =>
                {
                    preloadedAssets = assets;
                    preloadError = error;
                });
            if (!IsOperationCurrent(operationVersion))
                yield break;

            if (preloadedAssets == null)
            {
                _requestInFlight = false;
                _lastError = string.IsNullOrWhiteSpace(preloadError)
                    ? "Runtime artifact preload failed."
                    : preloadError;
                onUnavailable?.Invoke(_lastError);
                yield break;
            }

            GenerativeTurnRuntimeState.SetPreparedTurn(turnPayload.Envelope, preloadedAssets);
            if (!GenerativeTurnRuntimeState.HasPreparedTurn)
            {
                _requestInFlight = false;
                _lastError = "Runtime turn preparation failed validation.";
                _activeSessionId = string.Empty;
                _activeBaseUrl = string.Empty;
                _activeJobId = string.Empty;
                _preparedEntrySceneName = string.Empty;
                ClearPersistedState();
                onUnavailable?.Invoke(_lastError);
                yield break;
            }

            _preparedEntrySceneName = turnPayload.Envelope.entry_scene_name ?? string.Empty;
            _lastError = string.Empty;
            _requestInFlight = false;
            PersistState();
            onPrepared?.Invoke(_preparedEntrySceneName);
        }

        private IEnumerator ResumePreparedTurnIfAvailable()
        {
            _resumeAttempted = true;
            if (string.IsNullOrWhiteSpace(_activeSessionId))
                yield break;

            _resumeInFlight = true;
            var operationVersion = _operationVersion;
            LocalStoryOrchestratorReadyResult readyResult = null;
            yield return LocalStoryOrchestratorLauncher.EnsureReady(orchestratorBaseUrl, result => readyResult = result);
            if (!IsOperationCurrent(operationVersion))
            {
                _resumeInFlight = false;
                yield break;
            }
            if (readyResult == null || !readyResult.Success)
            {
                _resumeInFlight = false;
                yield break;
            }

            if (string.IsNullOrWhiteSpace(_activeBaseUrl))
                _activeBaseUrl = readyResult.BaseUrl;

            GenerativeRuntimeSessionPayload sessionPayload = null;
            yield return GenerativeRuntimeClient.GetSession(_activeBaseUrl, _activeSessionId, result => sessionPayload = result);
            if (!IsOperationCurrent(operationVersion))
            {
                _resumeInFlight = false;
                yield break;
            }
            if (sessionPayload == null || !sessionPayload.Success || sessionPayload.Detail == null)
            {
                _resumeInFlight = false;
                yield break;
            }

            if (string.Equals(sessionPayload.Detail.status, "completed", StringComparison.Ordinal) ||
                string.Equals(sessionPayload.Detail.status, "failed", StringComparison.Ordinal))
            {
                ClearSequenceState();
                _resumeInFlight = false;
                yield break;
            }

            if (sessionPayload.Detail.last_ready_turn != null &&
                !string.IsNullOrWhiteSpace(sessionPayload.Detail.last_ready_turn.turn_id))
            {
                yield return PrepareSpecificTurn(
                    operationVersion,
                    sessionPayload.Detail.last_ready_turn.turn_id,
                    _ => { },
                    _ => { });
                _resumeInFlight = false;
                yield break;
            }

            if (sessionPayload.Detail.active_job != null &&
                !string.IsNullOrWhiteSpace(sessionPayload.Detail.active_job.job_id))
            {
                _activeJobId = sessionPayload.Detail.active_job.job_id;
                yield return PrepareReadyTurnFromJob(operationVersion, _activeJobId, _ => { }, _ => { });
            }

            _resumeInFlight = false;
        }

        private void CompleteFiniteSequence()
        {
            _requestInFlight = false;
            ClearSequenceState();
        }

        private void LoadPreparedScene(string sceneName)
        {
            _requestInFlight = false;
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            UnityEngine.SceneManagement.SceneManager.LoadScene(FarmSimVR.Core.Tutorial.SceneWorkCatalog.GetLoadableSceneName(sceneName));
        }

        private void LoadPersistedState()
        {
            _activeSessionId = PlayerPrefs.GetString(SessionIdPrefKey, string.Empty);
            _activeBaseUrl = PlayerPrefs.GetString(BaseUrlPrefKey, string.Empty);
            _activeJobId = PlayerPrefs.GetString(JobIdPrefKey, string.Empty);
        }

        private void PersistState()
        {
            PlayerPrefs.SetString(SessionIdPrefKey, _activeSessionId ?? string.Empty);
            PlayerPrefs.SetString(BaseUrlPrefKey, _activeBaseUrl ?? string.Empty);
            PlayerPrefs.SetString(JobIdPrefKey, _activeJobId ?? string.Empty);
            PlayerPrefs.Save();
        }

        private void ClearPersistedState()
        {
            PlayerPrefs.DeleteKey(SessionIdPrefKey);
            PlayerPrefs.DeleteKey(BaseUrlPrefKey);
            PlayerPrefs.DeleteKey(JobIdPrefKey);
            PlayerPrefs.Save();
        }

        private void InitializeInstance()
        {
            if (_initialized)
                return;

            Instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
            LoadPersistedState();
            _initialized = true;
        }

        private int BeginOperation()
        {
            _operationVersion++;
            return _operationVersion;
        }

        private bool IsOperationCurrent(int operationVersion) => operationVersion == _operationVersion;
    }
}
