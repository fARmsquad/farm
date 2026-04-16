using System.Collections;
using System;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
namespace FarmSimVR.MonoBehaviours
{
    public class TitleScreenManager : MonoBehaviour
    {
        public const string TutorialSliceLauncherRootName = "TutorialSliceLauncher";
        public const string GenerateUniquePlaythroughLabel = "Generate Unique Playthrough";
        public const string PlayUniquePlaythroughLabel = "Play Unique Playthrough";
        public const string StoryPackageSampleLabel = GenerateUniquePlaythroughLabel;
        public const string GeneratedStorySliceStatusName = "GeneratedStorySliceStatus";
        public const string GeneratedStorySliceLoadingOverlayName = "GeneratedStorySliceLoadingOverlay";
        private const string StoryPackageSampleSceneName = TutorialSceneCatalog.PostChickenCutsceneSceneName;
        private const string GenerateUniquePlaythroughButtonName = "TutorialSlice_GenerateUniquePlaythrough";
        private const string PlayUniquePlaythroughButtonName = "TutorialSlice_PlayUniquePlaythrough";
        private const string GeneratingUniquePlaythroughMessage = "Generating unique playthrough... writing the story, then cutscene images, then narration audio. Check Console logs for live steps.";
        private const string LoadingUniquePlaythroughMessage = "Loading unique playthrough...";
        private const string ReadyUniquePlaythroughMessage = "Unique playthrough ready. Press Play Unique Playthrough.";
        private const string GenerateUniquePlaythroughFirstMessage = "Generate a unique playthrough first.";
        private const string UniquePlaythroughAlreadyStartingMessage = "Unique playthrough generation is already starting. Please wait a moment.";
        private const string DefaultGeneratedStoryDiagnosticsNote = "Open Generative Playthroughs to create, track, or replay a service-backed run.";
        private const string IdleGeneratedStoryState = "Idle";
        private const string GeneratingGeneratedStoryState = "Generating";
        private const string ReadyGeneratedStoryState = "Ready";
        private const string LoadingGeneratedStoryState = "Loading";
        private const string BusyGeneratedStoryState = "Busy";
        private const string FailedGeneratedStoryState = "Failed";
        [FormerlySerializedAs("farmMainSceneName")]
        [SerializeField] private string targetSceneName = TutorialSceneCatalog.IntroSceneName;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private float fadeDuration = 1.2f;
        private Canvas fadeCanvas;
        private Image fadeImage;
        private bool isTransitioning, launchGeneratedStorySlice;
        private Text generatedStoryStatusLabel;
        private GameObject generatedStoryLoadingOverlay;
        private Text generatedStoryLoadingLabel;
        private Button generateUniquePlaythroughButton;
        private Button playUniquePlaythroughButton;
        private string generatedStoryLifecycleState = IdleGeneratedStoryState;
        private string generatedStoryLifecycleNote = DefaultGeneratedStoryDiagnosticsNote;
        private string generatedStoryLifecycleError = string.Empty;
        private string generatedStoryPreparedAtUtc = string.Empty;
        private bool _autoPlayGeneratedSliceWhenReady;
        private void Start()
        {
            Application.runInBackground = true;
            GenerativePlaythroughController.GetOrCreate().EnsureLocalOrchestratorRunningInBackground();
            if (musicSource != null && !musicSource.isPlaying)
                musicSource.Play();
            CreateTutorialSliceLauncher();
            CreateFadeOverlay();
            RefreshGeneratedStoryStatus();
            GeneratedStorySliceDiagnostics.Log(nameof(TitleScreenManager), "Title screen initialized.");
        }
        private void Update() => SyncPreparedGeneratedPlaythroughStateFromRuntimeController();
        public void StartGame() => StartScene(SceneWorkCatalog.FirstTutorialSceneName);
        public void StartGameStorySlice()
        {
            _autoPlayGeneratedSliceWhenReady = true;
            var c = GenerativePlaythroughController.Instance;
            if (c != null && c.HasPreparedSequence) PlayGeneratedStorySlice(); else StartGeneratedStorySlice();
        }
        public void StartScene(string sceneName)
        {
            if (isTransitioning)
                return;
            var resolvedSceneName = ResolveTargetSceneName(sceneName);
            if (!string.Equals(resolvedSceneName, SceneWorkCatalog.GenerativePlaythroughMenuSceneName, StringComparison.Ordinal))
                GenerativePlaythroughController.Instance?.ClearSequenceState();
            HideGeneratedStoryLoading();
            SetGeneratedPlaythroughButtons(true, false);
            SetGeneratedStoryStatus(DefaultGeneratedStoryDiagnosticsNote);
            SetGeneratedStoryLifecycleState(IdleGeneratedStoryState, clearError: true, resetPreparedAt: true);
            launchGeneratedStorySlice = false;
            targetSceneName = resolvedSceneName;
            isTransitioning = true;
            StartCoroutine(TransitionToGame());
        }
        public void StartGeneratedStorySlice()
        {
            if (isTransitioning) { GeneratedStorySliceDiagnostics.LogWarning(nameof(TitleScreenManager), "Generate requested while a transition is already in progress."); return; }
            GeneratedStorySliceDiagnostics.Log(nameof(TitleScreenManager), "Generate Unique Playthrough clicked.");
            GenerativePlaythroughController.GetOrCreate().ClearSequenceState();
            ShowGeneratedStoryLoading(GeneratingUniquePlaythroughMessage);
            SetGeneratedStoryStatus(GeneratingUniquePlaythroughMessage);
            SetGeneratedStoryLifecycleState(GeneratingGeneratedStoryState, clearError: true, resetPreparedAt: true);
            SetGeneratedPlaythroughButtons(false, false);
            targetSceneName = StoryPackageSampleSceneName;
            launchGeneratedStorySlice = true;
            isTransitioning = true;
            StartCoroutine(TransitionToGame());
        }
        public void PlayGeneratedStorySlice()
        {
            if (isTransitioning) { GeneratedStorySliceDiagnostics.LogWarning(nameof(TitleScreenManager), "Play requested while a transition is already in progress."); return; }
            GeneratedStorySliceDiagnostics.Log(nameof(TitleScreenManager), "Play Unique Playthrough clicked.");
            var runtimeController = GenerativePlaythroughController.Instance;
            if (runtimeController == null || !runtimeController.HasPreparedSequence)
            {
                GeneratedStorySliceDiagnostics.LogWarning(nameof(TitleScreenManager), "Play requested without a prepared generated sequence.");
                HideGeneratedStoryLoading();
                SetGeneratedPlaythroughButtons(true, false);
                SetGeneratedStoryStatus(GenerateUniquePlaythroughFirstMessage);
                SetGeneratedStoryLifecycleState(IdleGeneratedStoryState, clearError: true, resetPreparedAt: true);
                return;
            }
            HideGeneratedStoryLoading();
            SetGeneratedPlaythroughButtons(false, false);
            SetGeneratedStoryStatus(LoadingUniquePlaythroughMessage);
            SetGeneratedStoryLifecycleState(LoadingGeneratedStoryState, clearError: true);
            launchGeneratedStorySlice = false;
            targetSceneName = ResolveTargetSceneName(runtimeController.PreparedEntrySceneName);
            isTransitioning = true;
            StartCoroutine(TransitionToGame());
        }
        private IEnumerator TransitionToGame()
        {
            var sceneName = ResolveTargetSceneName(targetSceneName);
            if (launchGeneratedStorySlice)
            {
                GeneratedStorySliceDiagnostics.Log(nameof(TitleScreenManager), "Transition entering generated playthrough preparation.");
                launchGeneratedStorySlice = false;
                var runtimeController = GenerativePlaythroughController.GetOrCreate();
                if (!runtimeController.BeginSequencePreparation(HandleGeneratedStorySlicePrepared, HandleGeneratedStorySliceUnavailable))
                {
                    GeneratedStorySliceDiagnostics.LogWarning(nameof(TitleScreenManager), "Runtime controller refused to start generated sequence preparation because a request is already in flight.");
                    HideGeneratedStoryLoading();
                    SetGeneratedPlaythroughButtons(true, false);
                    isTransitioning = false;
                    SetGeneratedStoryStatus(UniquePlaythroughAlreadyStartingMessage);
                    SetGeneratedStoryLifecycleState(BusyGeneratedStoryState, clearError: true);
                    yield break;
                }
                ShowGeneratedStoryLoading(GeneratingUniquePlaythroughMessage);
                SetGeneratedStoryStatus(GeneratingUniquePlaythroughMessage);
                yield break;
            }
            GeneratedStorySliceDiagnostics.Log(nameof(TitleScreenManager), $"Loading scene '{sceneName}'.");
            HideGeneratedStoryLoading();
            var duration = Mathf.Max(0f, fadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : elapsed / duration;
                fadeImage.color = new Color(0f, 0f, 0f, t);
                if (musicSource != null)
                    musicSource.volume = 1f - t;
                yield return null;
            }
            fadeImage.color = Color.black;
            if (musicSource != null)
                musicSource.Stop();
            SceneManager.LoadScene(SceneWorkCatalog.GetLoadableSceneName(sceneName));
        }
        private void CreateFadeOverlay()
        {
            var go = new GameObject("FadeOverlay");
            go.transform.SetParent(transform);
            fadeCanvas = go.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 999;
            var imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(go.transform, false);
            fadeImage = imgGo.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = false;
            var rect = imgGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            CreateGeneratedStoryLoadingOverlay(go.transform);
        }
        private static string ResolveTargetSceneName(string sceneName)
        {
            return string.IsNullOrWhiteSpace(sceneName)
                ? SceneWorkCatalog.FirstTutorialSceneName
                : sceneName;
        }
        private void SyncPreparedGeneratedPlaythroughStateFromRuntimeController()
        {
            var runtimeController = GenerativePlaythroughController.Instance;
            if (runtimeController == null || !runtimeController.HasPreparedSequence)
                return;
            if (generatedStoryLifecycleState == LoadingGeneratedStoryState)
                return;
            bool playButtonReady = playUniquePlaythroughButton != null && playUniquePlaythroughButton.interactable;
            bool lifecycleReady = generatedStoryLifecycleState == ReadyGeneratedStoryState;
            if (!isTransitioning && playButtonReady && lifecycleReady)
                return;
            bool stampPreparedAt = string.IsNullOrWhiteSpace(generatedStoryPreparedAtUtc);
            isTransitioning = false;
            HideGeneratedStoryLoading();
            RestoreTitlePresentation();
            SetGeneratedPlaythroughButtons(true, true);
            SetGeneratedStoryStatus(ReadyUniquePlaythroughMessage);
            SetGeneratedStoryLifecycleState(ReadyGeneratedStoryState, clearError: true, stampPreparedAt: stampPreparedAt);
            GeneratedStorySliceDiagnostics.Log(nameof(TitleScreenManager), $"Recovered generated playthrough readiness from runtime state for session '{runtimeController.ActiveSessionId}' and scene '{runtimeController.PreparedEntrySceneName}'.");
        }
        private void HandleGeneratedStorySlicePrepared(string sceneName)
        {
            GeneratedStorySliceDiagnostics.Log(nameof(TitleScreenManager), $"Generated playthrough prepared with entry scene '{sceneName}'.");
            targetSceneName = ResolveTargetSceneName(sceneName);
            isTransitioning = false;
            HideGeneratedStoryLoading();
            RestoreTitlePresentation();
            SetGeneratedPlaythroughButtons(true, true);
            SetGeneratedStoryStatus(ReadyUniquePlaythroughMessage);
            SetGeneratedStoryLifecycleState(ReadyGeneratedStoryState, clearError: true, stampPreparedAt: true);
            if (_autoPlayGeneratedSliceWhenReady) { _autoPlayGeneratedSliceWhenReady = false; PlayGeneratedStorySlice(); }
        }
        private void HandleGeneratedStorySliceUnavailable(string errorMessage)
        {
            GeneratedStorySliceDiagnostics.LogWarning(nameof(TitleScreenManager), $"Generated playthrough unavailable: {errorMessage}");
            launchGeneratedStorySlice = false;
            isTransitioning = false;
            HideGeneratedStoryLoading();
            SetGeneratedPlaythroughButtons(true, false);
            RestoreTitlePresentation();
            GenerativePlaythroughController.Instance?.ClearSequenceState();
            SetGeneratedStoryStatus(BuildGeneratedStorySliceStatus(errorMessage));
            SetGeneratedStoryLifecycleState(FailedGeneratedStoryState, errorMessage);
        }
        private void RestoreTitlePresentation()
        {
            if (fadeImage != null)
                fadeImage.color = new Color(0f, 0f, 0f, 0f);
            if (musicSource == null)
                return;
            musicSource.volume = 1f;
            if (!musicSource.isPlaying)
                musicSource.Play();
        }
        private void SetGeneratedStoryStatus(string message)
        {
            generatedStoryLifecycleNote = message ?? string.Empty;
            RefreshGeneratedStoryStatus();
        }
        private void SetGeneratedStoryLifecycleState(
            string lifecycleState,
            string errorMessage = null,
            bool clearError = false,
            bool resetPreparedAt = false,
            bool stampPreparedAt = false)
        {
            generatedStoryLifecycleState = string.IsNullOrWhiteSpace(lifecycleState)
                ? IdleGeneratedStoryState
                : lifecycleState;
            if (clearError)
                generatedStoryLifecycleError = string.Empty;
            else if (errorMessage != null)
                generatedStoryLifecycleError = errorMessage;
            if (resetPreparedAt)
                generatedStoryPreparedAtUtc = string.Empty;
            else if (stampPreparedAt)
                generatedStoryPreparedAtUtc = System.DateTime.UtcNow.ToString("u");
            RefreshGeneratedStoryStatus();
        }
        private void RefreshGeneratedStoryStatus()
        {
            if (generatedStoryStatusLabel == null)
                return;
            generatedStoryStatusLabel.text = GeneratedPlaythroughStatusFormatter.Build(
                generatedStoryLifecycleState,
                generatedStoryLifecycleNote,
                generatedStoryLifecycleError,
                generatedStoryPreparedAtUtc,
                GenerativePlaythroughController.Instance);
        }
        private void ShowGeneratedStoryLoading(string message)
        {
            if (generatedStoryLoadingLabel != null)
                generatedStoryLoadingLabel.text = message ?? string.Empty;
            if (generatedStoryLoadingOverlay != null)
                generatedStoryLoadingOverlay.SetActive(true);
        }
        private void HideGeneratedStoryLoading()
        {
            if (generatedStoryLoadingLabel != null)
                generatedStoryLoadingLabel.text = string.Empty;
            if (generatedStoryLoadingOverlay != null)
                generatedStoryLoadingOverlay.SetActive(false);
        }
        private void SetGeneratedPlaythroughButtons(bool generateEnabled, bool playEnabled)
        {
            if (generateUniquePlaythroughButton != null)
                generateUniquePlaythroughButton.interactable = generateEnabled;
            if (playUniquePlaythroughButton != null)
                playUniquePlaythroughButton.interactable = playEnabled;
        }
        private static string BuildGeneratedStorySliceStatus(string errorMessage)
        {
            const string prefix = "Unique playthrough unavailable. Unity could not reach the story-orchestrator service.";
            if (string.IsNullOrWhiteSpace(errorMessage))
                return prefix;
            var singleLine = errorMessage.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (singleLine.Length > 120)
                singleLine = singleLine.Substring(0, 117) + "...";
            return prefix + " " + singleLine;
        }
        private void CreateGeneratedStoryLoadingOverlay(Transform parent)
        {
            var overlay = new GameObject(GeneratedStorySliceLoadingOverlayName);
            overlay.transform.SetParent(parent, false);
            overlay.SetActive(false);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            overlay.AddComponent<CanvasRenderer>();
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.62f);
            overlayImage.raycastTarget = true;
            var panel = new GameObject("Panel");
            panel.transform.SetParent(overlay.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(440f, 140f);
            panel.AddComponent<CanvasRenderer>();
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.11f, 0.15f, 0.11f, 0.96f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            generatedStoryLoadingLabel = CreateLabel(
                "Message",
                panel.transform,
                font,
                string.Empty,
                20,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Rect(18f, 18f, 404f, 104f),
                Color.white);
            generatedStoryLoadingOverlay = overlay;
        }
        private void CreateTutorialSliceLauncher()
        {
            if (GameObject.Find(TutorialSliceLauncherRootName) != null)
                return;
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject(TutorialSliceLauncherRootName);
            root.transform.SetParent(canvas.transform, false);
            var rootRect = root.AddComponent<RectTransform>();
            root.AddComponent<CanvasRenderer>();
            var rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0.05f, 0.06f, 0.05f, 0.86f);
            var totalButtonCount = SceneWorkCatalog.TitleScreenLaunchableScenes.Count + 2;
            var height = 230f + (totalButtonCount * 46f);
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.sizeDelta = new Vector2(320f, height);
            rootRect.anchoredPosition = new Vector2(-32f, -32f);
            CreateLabel(
                "Header",
                root.transform,
                font,
                "Playable Slices",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Rect(16f, 12f, 288f, 28f),
                Color.white);
            generateUniquePlaythroughButton = CreateSliceButton(
                root.transform,
                font,
                GenerateUniquePlaythroughButtonName,
                GenerateUniquePlaythroughLabel,
                48f,
                StartGeneratedStorySlice);
            playUniquePlaythroughButton = CreateSliceButton(
                root.transform,
                font,
                PlayUniquePlaythroughButtonName,
                PlayUniquePlaythroughLabel,
                94f,
                PlayGeneratedStorySlice);
            SetGeneratedPlaythroughButtons(true, false);
            for (int i = 0; i < SceneWorkCatalog.TitleScreenLaunchableScenes.Count; i++)
            {
                var scene = SceneWorkCatalog.TitleScreenLaunchableScenes[i];
                CreateSliceButton(root.transform, font, scene, 140f + (i * 46f));
            }
            var statusTopOffset = 48f + (totalButtonCount * 46f);
            generatedStoryStatusLabel = CreateLabel(
                GeneratedStorySliceStatusName,
                root.transform,
                font,
                string.Empty,
                12,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Rect(16f, statusTopOffset, 288f, 156f),
                new Color(0.98f, 0.84f, 0.74f));
            RefreshGeneratedStoryStatus();
        }
        private void CreateSliceButton(Transform parent, Font font, SceneWorkDefinition scene, float topOffset)
        {
            CreateSliceButton(
                parent,
                font,
                $"TutorialSlice_{scene.NumberLabel}_{scene.SceneName}",
                $"{scene.NumberLabel} {scene.DisplayName}",
                topOffset,
                () => StartScene(scene.SceneName));
        }
        private Button CreateSliceButton(
            Transform parent,
            Font font,
            string objectName,
            string label,
            float topOffset,
            UnityAction onClick)
        {
            var buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.sizeDelta = new Vector2(-24f, 36f);
            buttonRect.anchoredPosition = new Vector2(0f, -topOffset);
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.17f, 0.25f, 0.17f, 0.95f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(onClick);
            var colors = button.colors;
            colors.normalColor = new Color(0.17f, 0.25f, 0.17f, 0.95f);
            colors.highlightedColor = new Color(0.22f, 0.36f, 0.22f, 0.95f);
            colors.pressedColor = new Color(0.12f, 0.18f, 0.12f, 0.95f);
            button.colors = colors;
            CreateLabel(
                "Label",
                buttonObject.transform,
                font,
                label,
                15,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Rect(0f, 0f, 0f, 0f),
                Color.white,
                stretchToParent: true);
            return button;
        }
        private static Text CreateLabel(
            string name,
            Transform parent,
            Font font,
            string text,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Rect rect,
            Color color,
            bool stretchToParent = false)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            var labelRect = labelObject.AddComponent<RectTransform>();
            if (stretchToParent)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.sizeDelta = Vector2.zero;
                labelRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(0f, 1f);
                labelRect.pivot = new Vector2(0f, 1f);
                labelRect.anchoredPosition = new Vector2(rect.x, -rect.y);
                labelRect.sizeDelta = new Vector2(rect.width, rect.height);
            }
            var label = labelObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
    }
}
