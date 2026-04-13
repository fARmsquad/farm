using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FarmSimVR.Core.Tutorial;

namespace FarmSimVR.MonoBehaviours
{
    public class TitleScreenManager : MonoBehaviour
    {
        public const string TutorialSliceLauncherRootName = "TutorialSliceLauncher";
        public const string StoryPackageSampleLabel = "Story Package Sample";

        private const string StoryPackageSampleSceneName = TutorialSceneCatalog.IntroSceneName;

        [FormerlySerializedAs("farmMainSceneName")]
        [SerializeField] private string targetSceneName = TutorialSceneCatalog.IntroSceneName;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private float fadeDuration = 1.2f;

        private Canvas fadeCanvas;
        private Image fadeImage;
        private bool isTransitioning;

        private void Start()
        {
            if (musicSource != null && !musicSource.isPlaying)
                musicSource.Play();

            CreateTutorialSliceLauncher();
            CreateFadeOverlay();
        }

        public void StartGame()
        {
            StartScene(SceneWorkCatalog.FirstTutorialSceneName);
        }

        public void StartScene(string sceneName)
        {
            if (isTransitioning) return;
            targetSceneName = ResolveTargetSceneName(sceneName);
            isTransitioning = true;
            StartCoroutine(TransitionToGame());
        }

        private IEnumerator TransitionToGame()
        {
            // Fade to black
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;
                fadeImage.color = new Color(0, 0, 0, t);
                if (musicSource != null)
                    musicSource.volume = 1f - t;
                yield return null;
            }
            fadeImage.color = Color.black;

            if (musicSource != null)
                musicSource.Stop();

            var sceneName = ResolveTargetSceneName(targetSceneName);
            SceneManager.LoadScene(sceneName);
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
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.raycastTarget = false;

            var rect = imgGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        private static string ResolveTargetSceneName(string sceneName)
        {
            return string.IsNullOrWhiteSpace(sceneName)
                ? SceneWorkCatalog.FirstTutorialSceneName
                : sceneName;
        }

        private void CreateTutorialSliceLauncher()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
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

            var totalButtonCount = SceneWorkCatalog.TitleScreenLaunchableScenes.Count + 1;
            var height = 72f + (totalButtonCount * 46f);
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

            CreateSliceButton(
                root.transform,
                font,
                "TutorialSlice_StoryPackageSample",
                StoryPackageSampleLabel,
                StoryPackageSampleSceneName,
                48f);

            for (int i = 0; i < SceneWorkCatalog.TitleScreenLaunchableScenes.Count; i++)
            {
                var scene = SceneWorkCatalog.TitleScreenLaunchableScenes[i];
                CreateSliceButton(root.transform, font, scene, 94f + (i * 46f));
            }
#endif
        }

        private void CreateSliceButton(Transform parent, Font font, SceneWorkDefinition scene, float topOffset)
        {
            CreateSliceButton(
                parent,
                font,
                $"TutorialSlice_{scene.NumberLabel}_{scene.SceneName}",
                $"{scene.NumberLabel} {scene.DisplayName}",
                scene.SceneName,
                topOffset);
        }

        private void CreateSliceButton(
            Transform parent,
            Font font,
            string objectName,
            string label,
            string sceneName,
            float topOffset)
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

            var colors = button.colors;
            colors.normalColor = new Color(0.17f, 0.25f, 0.17f, 0.95f);
            colors.highlightedColor = new Color(0.22f, 0.36f, 0.22f, 0.95f);
            colors.pressedColor = new Color(0.12f, 0.18f, 0.12f, 0.95f);
            button.colors = colors;

            button.onClick.AddListener(() => StartScene(sceneName));

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
