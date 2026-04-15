using System;
using System.Collections;
using System.Collections.Generic;
using FarmSimVR.Core;
using FarmSimVR.Core.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public sealed class GenerativePlaythroughMenuController : MonoBehaviour
    {
        private const string CanvasName = "GenerativePlaythroughMenuCanvas";
        private const string EventSystemName = "GenerativePlaythroughMenuEventSystem";
        private const string RootName = "GenerativePlaythroughMenuRoot";
        internal const string DefaultStatus = "Select a previous playthrough or create a new one.";
        private const float ActiveRefreshIntervalSeconds = 0.75f;
        private const float IdleRefreshIntervalSeconds = 5f;

        [SerializeField] private string orchestratorBaseUrl = FarmSimVR.Core.TownVoiceTokenServiceEndpointResolver.ProductionBaseUrl;
        [SerializeField] private int historyLimit = 8;

        private readonly List<Button> _sessionButtons = new();
        private readonly List<Image> _stageTileImages = new();
        private readonly List<Text> _stageTileLabels = new();

        private Font _font;
        private Text _statusLabel;
        private Text _detailLabel;
        private Text _historyEmptyLabel;
        private RectTransform _historyContent;
        private Button _createButton;
        private Button _refreshButton;
        private Button _playButton;
        private Button _backButton;
        private string _selectedSessionId = string.Empty;
        private string _selectedBaseUrl = string.Empty;
        private string _statusNote = DefaultStatus;
        private float _nextRefreshAt;
        private bool _refreshInFlight;
        private bool _playWhenPrepared;
        private GenerativeRuntimeTrackerSessionSummary[] _sessionSummaries = Array.Empty<GenerativeRuntimeTrackerSessionSummary>();
        private GenerativeRuntimeTrackerSessionDetail _selectedDetail;

        private void Start()
        {
            Application.runInBackground = true;
            EnsureInterfaceCreated();
            PrimeSelectionFromRuntime();
            GenerativePlaythroughController.GetOrCreate().EnsureLocalOrchestratorRunningInBackground();
            RequestRefresh("Loading generated playthroughs...");
        }

        private void Update()
        {
            RefreshInteractiveState();
            if (_refreshInFlight || Time.unscaledTime < _nextRefreshAt)
                return;

            RequestRefresh();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                RequestRefresh("Refreshing generated playthroughs...");
        }

        private void EnsureInterfaceCreated()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = GenerativePlaythroughMenuViewBuilder.EnsureCanvas(CanvasName);
            GenerativePlaythroughMenuViewBuilder.EnsureEventSystem(EventSystemName);

            if (canvas.transform.Find(RootName) != null)
                return;

            var view = GenerativePlaythroughMenuViewBuilder.Build(
                canvas.transform,
                RootName,
                _font,
                HandleCreateClicked,
                HandleRefreshClicked,
                HandlePlayClicked,
                HandleBackClicked);
            _statusLabel = view.StatusLabel;
            _detailLabel = view.DetailLabel;
            _historyEmptyLabel = view.HistoryEmptyLabel;
            _historyContent = view.HistoryContent;
            _createButton = view.CreateButton;
            _refreshButton = view.RefreshButton;
            _playButton = view.PlayButton;
            _backButton = view.BackButton;
            _stageTileImages.AddRange(view.StageTileImages);
            _stageTileLabels.AddRange(view.StageTileLabels);
            RefreshAllLabels();
            RefreshInteractiveState();
        }

        private void PrimeSelectionFromRuntime()
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            _selectedSessionId = controller.ActiveSessionId;
            _selectedBaseUrl = controller.ActiveBaseUrl;
        }

        private void RequestRefresh(string note = null)
        {
            if (_refreshInFlight)
                return;

            if (!string.IsNullOrWhiteSpace(note))
                _statusNote = note;

            _refreshInFlight = true;
            RefreshAllLabels();
            RefreshInteractiveState();
            StartCoroutine(RefreshRoutine());
        }

        private IEnumerator RefreshRoutine()
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            GenerativeRuntimeTrackerSessionsPayload sessionsPayload = null;
            yield return GenerativeRuntimeTrackerClient.ListSessions(orchestratorBaseUrl, historyLimit, result => sessionsPayload = result);

            if (sessionsPayload == null || !sessionsPayload.Success)
            {
                _sessionSummaries = Array.Empty<GenerativeRuntimeTrackerSessionSummary>();
                _selectedDetail = null;
                _statusNote = sessionsPayload?.ErrorMessage ?? "Unable to load generated playthroughs.";
                FinishRefresh();
                yield break;
            }

            _selectedBaseUrl = sessionsPayload.BaseUrl;
            _sessionSummaries = sessionsPayload.Sessions ?? Array.Empty<GenerativeRuntimeTrackerSessionSummary>();
            ResolveSelection(controller);
            if (string.IsNullOrWhiteSpace(_selectedSessionId))
            {
                _selectedDetail = null;
                _statusNote = _sessionSummaries.Length == 0 ? "No generated playthroughs yet. Create a new one." : _statusNote;
                FinishRefresh();
                yield break;
            }

            GenerativeRuntimeTrackerSessionDetailPayload detailPayload = null;
            yield return GenerativeRuntimeTrackerClient.GetSessionDetail(_selectedBaseUrl, _selectedSessionId, result => detailPayload = result);
            if (detailPayload == null || !detailPayload.Success)
            {
                _selectedDetail = null;
                _statusNote = detailPayload?.ErrorMessage ?? "Unable to load selected playthrough.";
                FinishRefresh();
                yield break;
            }

            _selectedDetail = detailPayload.Detail;
            if (string.IsNullOrWhiteSpace(_statusNote) || string.Equals(_statusNote, DefaultStatus, StringComparison.Ordinal))
                _statusNote = "Generated playthrough library refreshed.";
            FinishRefresh();
        }

        private void FinishRefresh()
        {
            _refreshInFlight = false;
            _nextRefreshAt = Time.unscaledTime + ResolveRefreshInterval();
            RefreshAllLabels();
            RefreshInteractiveState();
        }

        private void ResolveSelection(GenerativePlaythroughController controller)
        {
            if (TrySelectSession(controller.ActiveSessionId))
                return;
            if (TrySelectSession(_selectedSessionId))
                return;
            if (_sessionSummaries.Length == 0)
            {
                _selectedSessionId = string.Empty;
                return;
            }

            _selectedSessionId = _sessionSummaries[0].session_id ?? string.Empty;
        }

        private bool TrySelectSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            foreach (var summary in _sessionSummaries)
            {
                if (summary != null && string.Equals(summary.session_id, sessionId, StringComparison.Ordinal))
                {
                    _selectedSessionId = sessionId;
                    return true;
                }
            }

            return false;
        }

        private float ResolveRefreshInterval()
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            if (controller.HasPendingOperation)
                return ActiveRefreshIntervalSeconds;
            if (_selectedDetail?.active_job != null && !string.Equals(_selectedDetail.active_job.status, "ready", StringComparison.Ordinal))
                return ActiveRefreshIntervalSeconds;
            return IdleRefreshIntervalSeconds;
        }

        private void HandleCreateClicked()
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            controller.ClearSequenceState();
            _selectedSessionId = string.Empty;
            _selectedDetail = null;
            _playWhenPrepared = false;
            _statusNote = "Ordering a fresh generated playthrough...";
            if (!controller.BeginSequencePreparation(HandlePrepared, HandleUnavailable))
            {
                _statusNote = "A generation request is already running.";
                RefreshAllLabels();
                return;
            }

            RequestRefresh();
        }

        private void HandleRefreshClicked()
        {
            RequestRefresh("Refreshing generated playthroughs...");
        }

        private void HandlePlayClicked()
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            if (controller.HasPreparedSequence && MatchesPreparedSession(controller.ActiveSessionId))
            {
                LoadScene(controller.PreparedEntrySceneName);
                return;
            }

            var latestTurn = GetLatestReadyTurn();
            if (latestTurn == null)
            {
                _statusNote = "The selected playthrough is not ready yet.";
                RefreshAllLabels();
                return;
            }

            _playWhenPrepared = true;
            _statusNote = "Preparing the selected playthrough for launch...";
            if (!controller.PrepareExistingReadyTurn(_selectedBaseUrl, _selectedSessionId, latestTurn.turn_id, HandlePrepared, HandleUnavailable))
            {
                _playWhenPrepared = false;
                _statusNote = "Unable to prepare the selected playthrough right now.";
                RefreshAllLabels();
                return;
            }

            RefreshAllLabels();
            RefreshInteractiveState();
        }

        private void HandleBackClicked()
        {
            SceneManager.LoadScene(SceneWorkCatalog.TitleScreenSceneName);
        }

        private void HandlePrepared(string sceneName)
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            _selectedSessionId = controller.ActiveSessionId;
            _selectedBaseUrl = controller.ActiveBaseUrl;
            _statusNote = "Playthrough ready. Press Play Ready Playthrough.";
            RequestRefresh();
            if (!_playWhenPrepared)
                return;

            _playWhenPrepared = false;
            LoadScene(sceneName);
        }

        private void HandleUnavailable(string errorMessage)
        {
            _playWhenPrepared = false;
            _statusNote = string.IsNullOrWhiteSpace(errorMessage)
                ? "Generated playthrough unavailable."
                : errorMessage;
            RequestRefresh();
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            SceneManager.LoadScene(SceneWorkCatalog.GetLoadableSceneName(sceneName));
        }

        private bool MatchesPreparedSession(string activeSessionId)
        {
            return string.IsNullOrWhiteSpace(_selectedSessionId) ||
                   string.Equals(_selectedSessionId, activeSessionId, StringComparison.Ordinal);
        }

        private GenerativeRuntimeTrackerTurnDetail GetLatestReadyTurn()
        {
            var turns = _selectedDetail?.turns;
            if (turns == null || turns.Length == 0)
                return null;

            return turns[turns.Length - 1];
        }

        private void RefreshAllLabels()
        {
            RebuildSessionButtons();
            RefreshStageTiles();
            if (_statusLabel != null)
            {
                _statusLabel.text = GenerativePlaythroughMenuFormatter.BuildStatus(
                    _statusNote,
                    GenerativePlaythroughController.Instance,
                    _selectedDetail);
            }

            if (_detailLabel != null)
                _detailLabel.text = GenerativePlaythroughMenuFormatter.BuildDetail(_selectedDetail);
        }

        private void RefreshInteractiveState()
        {
            var controller = GenerativePlaythroughController.GetOrCreate();
            if (_createButton != null)
                _createButton.interactable = !_refreshInFlight && !controller.HasPendingOperation;
            if (_refreshButton != null)
                _refreshButton.interactable = !_refreshInFlight;
            if (_playButton != null)
                _playButton.interactable = !_refreshInFlight && CanPlay(controller);
            if (_backButton != null)
                _backButton.interactable = true;
        }

        private bool CanPlay(GenerativePlaythroughController controller)
        {
            if (controller.HasPendingOperation)
                return false;
            if (controller.HasPreparedSequence && MatchesPreparedSession(controller.ActiveSessionId))
                return true;
            return GetLatestReadyTurn() != null;
        }

        private void RebuildSessionButtons()
        {
            foreach (var button in _sessionButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            _sessionButtons.Clear();
            if (_historyEmptyLabel != null)
                _historyEmptyLabel.gameObject.SetActive(_sessionSummaries.Length == 0);
            if (_historyContent == null)
                return;

            for (int index = 0; index < _sessionSummaries.Length; index++)
            {
                var summary = _sessionSummaries[index];
                var button = CreateSessionButton(_historyContent, summary, index);
                _sessionButtons.Add(button);
            }
        }

        private Button CreateSessionButton(Transform parent, GenerativeRuntimeTrackerSessionSummary summary, int index)
        {
            var buttonObject = new GameObject($"Session_{index + 1}");
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 54f);
            rect.anchoredPosition = new Vector2(0f, -(index * 60f));

            var image = buttonObject.AddComponent<Image>();
            image.color = string.Equals(_selectedSessionId, summary?.session_id, StringComparison.Ordinal)
                ? new Color(0.13f, 0.28f, 0.68f, 0.96f)
                : new Color(0.1f, 0.17f, 0.33f, 0.92f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => HandleSessionSelected(summary));

            GenerativePlaythroughMenuViewBuilder.CreateLabel(_font, "Title", buttonObject.transform, summary?.package_display_name ?? "Generated Playthrough", 15, FontStyle.Bold, TextAnchor.UpperLeft, new Rect(12f, 8f, 300f, 18f), Color.white);
            GenerativePlaythroughMenuViewBuilder.CreateLabel(_font, "Meta", buttonObject.transform, GenerativePlaythroughMenuFormatter.BuildSessionRow(summary, string.Equals(_selectedSessionId, summary?.session_id, StringComparison.Ordinal)), 12, FontStyle.Normal, TextAnchor.UpperLeft, new Rect(12f, 28f, 300f, 18f), new Color(0.92f, 0.95f, 1f));
            return button;
        }

        private void HandleSessionSelected(GenerativeRuntimeTrackerSessionSummary summary)
        {
            _selectedSessionId = summary?.session_id ?? string.Empty;
            _statusNote = "Loading selected playthrough...";
            RequestRefresh();
        }

        private void RefreshStageTiles()
        {
            var stages = GenerativePlaythroughMenuFormatter.BuildStages(_selectedDetail);
            for (int index = 0; index < _stageTileImages.Count && index < stages.Count; index++)
            {
                _stageTileImages[index].color = ResolveStageColor(stages[index].State);
                _stageTileLabels[index].text = stages[index].Label + "\n" + stages[index].State.ToUpperInvariant();
            }
        }

        private static Color ResolveStageColor(string stageState)
        {
            return stageState switch
            {
                "completed" => new Color(0.0f, 0.35f, 0.85f, 0.96f),
                "running" => new Color(0.9f, 0.1f, 0.15f, 0.96f),
                "failed" => new Color(0.55f, 0.08f, 0.08f, 0.96f),
                _ => new Color(0.2f, 0.24f, 0.3f, 0.92f),
            };
        }
    }
}
