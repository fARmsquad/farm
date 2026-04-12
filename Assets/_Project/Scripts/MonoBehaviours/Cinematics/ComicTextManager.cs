using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Manages comic-style text overlays: panel text, comic bursts, and speech bubbles.
    /// Singleton — access via ComicTextManager.Instance.
    /// Creates its own screen-space overlay Canvas (sortingOrder 1001) if none is assigned.
    /// </summary>
    public class ComicTextManager : MonoBehaviour
    {
        public static ComicTextManager Instance { get; private set; }

        [Header("Canvas")]
        [SerializeField] private Canvas comicCanvas;

        // ── Internal State ──────────────────────────────────────────
        private readonly List<Coroutine> activeCoroutines = new List<Coroutine>();
        private readonly List<GameObject> activeObjects = new List<GameObject>();

        // Panel text references
        private GameObject panelTextObject;
        private TMP_Text panelTmpText;
        private CanvasGroup panelCanvasGroup;
        private Coroutine panelCoroutine;

        // Burst text references
        private GameObject burstTextObject;
        private TMP_Text burstTmpText;
        private CanvasGroup burstCanvasGroup;
        private Coroutine burstCoroutine;

        // Speech bubble tracking
        private readonly List<GameObject> activeSpeechBubbles = new List<GameObject>();
        private readonly List<Coroutine> speechBubbleCoroutines = new List<Coroutine>();

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureCanvas();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Canvas Setup

        private void EnsureCanvas()
        {
            if (comicCanvas != null) return;

            var canvasGo = new GameObject("ComicTextCanvas");
            canvasGo.transform.SetParent(transform, false);

            comicCanvas = canvasGo.AddComponent<Canvas>();
            comicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            comicCanvas.sortingOrder = 1001;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        #endregion

        #region ShowPanelText

        /// <summary>
        /// Shows bottom-screen panel text with fade in/out.
        /// Replaces any active panel text.
        /// </summary>
        public void ShowPanelText(string text, float holdDuration, float fontSize = 36f,
            Color? color = null, Action onComplete = null)
        {
            if (panelCoroutine != null)
            {
                StopCoroutine(panelCoroutine);
                panelCoroutine = null;
            }

            if (panelTextObject != null)
            {
                Destroy(panelTextObject);
                panelTextObject = null;
            }

            CreatePanelTextObject(fontSize, color ?? Color.white);
            panelTmpText.text = text;

            panelCoroutine = StartCoroutine(PanelTextCoroutine(holdDuration, onComplete));
        }

        private void CreatePanelTextObject(float fontSize, Color color)
        {
            panelTextObject = new GameObject("PanelText");
            panelTextObject.transform.SetParent(comicCanvas.transform, false);

            var rect = panelTextObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.15f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            panelCanvasGroup = panelTextObject.AddComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 0f;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(panelTextObject.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(-20f, -10f);
            bgRect.offsetMax = new Vector2(20f, 10f);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.7f);

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelTextObject.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            panelTmpText = textGo.AddComponent<TextMeshProUGUI>();
            panelTmpText.fontSize = fontSize;
            panelTmpText.color = color;
            panelTmpText.alignment = TextAlignmentOptions.Center;
            panelTmpText.textWrappingMode = TextWrappingModes.Normal;

            activeObjects.Add(panelTextObject);
        }

        private IEnumerator PanelTextCoroutine(float holdDuration, Action onComplete)
        {
            // Fade in
            float fadeInDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;

            // Hold
            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out
            float fadeOutDuration = 0.5f;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 0f;

            if (panelTextObject != null)
            {
                activeObjects.Remove(panelTextObject);
                Destroy(panelTextObject);
                panelTextObject = null;
            }

            panelCoroutine = null;
            onComplete?.Invoke();
        }

        #endregion

        #region ShowComicBurst

        /// <summary>
        /// Shows center-screen large comic burst text with scale overshoot bounce.
        /// Random rotation +/- 5 degrees, then fade out.
        /// </summary>
        public void ShowComicBurst(string text, float holdDuration = 2f, float fontSize = 72f,
            Color? color = null, Color? outlineColor = null, Action onComplete = null)
        {
            if (burstCoroutine != null)
            {
                StopCoroutine(burstCoroutine);
                burstCoroutine = null;
            }

            if (burstTextObject != null)
            {
                Destroy(burstTextObject);
                burstTextObject = null;
            }

            CreateBurstTextObject(fontSize, color ?? Color.white, outlineColor);
            burstTmpText.text = text;

            burstCoroutine = StartCoroutine(BurstCoroutine(holdDuration, onComplete));
        }

        private void CreateBurstTextObject(float fontSize, Color color, Color? outlineColor)
        {
            burstTextObject = new GameObject("BurstText");
            burstTextObject.transform.SetParent(comicCanvas.transform, false);

            var rect = burstTextObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 200f);

            burstCanvasGroup = burstTextObject.AddComponent<CanvasGroup>();
            burstCanvasGroup.alpha = 1f;

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(burstTextObject.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            burstTmpText = textGo.AddComponent<TextMeshProUGUI>();
            burstTmpText.fontSize = fontSize;
            burstTmpText.color = color;
            burstTmpText.alignment = TextAlignmentOptions.Center;
            burstTmpText.fontStyle = FontStyles.Bold;
            burstTmpText.textWrappingMode = TextWrappingModes.NoWrap;

            if (outlineColor.HasValue)
            {
                burstTmpText.outlineWidth = 0.3f;
                burstTmpText.outlineColor = outlineColor.Value;
            }

            // Random rotation
            float randomAngle = UnityEngine.Random.Range(-5f, 5f);
            burstTextObject.transform.localRotation = Quaternion.Euler(0f, 0f, randomAngle);

            // Start at scale 0
            burstTextObject.transform.localScale = Vector3.zero;

            activeObjects.Add(burstTextObject);
        }

        private IEnumerator BurstCoroutine(float holdDuration, Action onComplete)
        {
            // Scale from 0 -> 1.2 -> 1.0 over 0.3s (overshoot bounce)
            float scaleDuration = 0.3f;
            float elapsed = 0f;

            while (elapsed < scaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / scaleDuration);
                float scale = EvaluateOvershootBounce(t);
                burstTextObject.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            burstTextObject.transform.localScale = Vector3.one;

            // Hold
            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out
            float fadeOutDuration = 0.5f;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                burstCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }
            burstCanvasGroup.alpha = 0f;

            if (burstTextObject != null)
            {
                activeObjects.Remove(burstTextObject);
                Destroy(burstTextObject);
                burstTextObject = null;
            }

            burstCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Overshoot bounce: goes from 0 to 1.2 at t=0.7, then settles to 1.0 at t=1.0.
        /// </summary>
        private static float EvaluateOvershootBounce(float t)
        {
            if (t < 0.7f)
            {
                // Scale from 0 to 1.2 in the first 70% of the animation
                float normalized = t / 0.7f;
                return Mathf.Lerp(0f, 1.2f, normalized);
            }
            else
            {
                // Scale from 1.2 to 1.0 in the last 30%
                float normalized = (t - 0.7f) / 0.3f;
                return Mathf.Lerp(1.2f, 1.0f, normalized);
            }
        }

        #endregion

        #region ShowSpeechBubble

        /// <summary>
        /// Shows a world-space speech bubble above the target transform.
        /// Rounded rect background, typewriter effect at 30 chars/sec.
        /// Optional smaller italic translation line below.
        /// </summary>
        public void ShowSpeechBubble(Transform target, string text, string translationText = null,
            float holdDuration = 3f, Action onComplete = null)
        {
            if (target == null)
            {
                Debug.LogWarning("[ComicTextManager] ShowSpeechBubble: target is null.");
                onComplete?.Invoke();
                return;
            }

            var bubble = CreateSpeechBubble(target, text, translationText);
            var coroutine = StartCoroutine(SpeechBubbleCoroutine(bubble, text, translationText, holdDuration, onComplete));

            activeSpeechBubbles.Add(bubble.root);
            speechBubbleCoroutines.Add(coroutine);
        }

        private struct SpeechBubbleRefs
        {
            public GameObject root;
            public TMP_Text mainText;
            public TMP_Text translationTmpText;
            public CanvasGroup canvasGroup;
            public Transform target;
        }

        private SpeechBubbleRefs CreateSpeechBubble(Transform target, string text, string translationText)
        {
            var refs = new SpeechBubbleRefs();
            refs.target = target;

            // Root with world-space Canvas
            var root = new GameObject("SpeechBubble");
            root.transform.position = target.position + Vector3.up * 2.5f;
            refs.root = root;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1002;

            root.AddComponent<CanvasScaler>();

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(400f, 150f);
            rootRect.localScale = Vector3.one * 0.01f;

            refs.canvasGroup = root.AddComponent<CanvasGroup>();
            refs.canvasGroup.alpha = 1f;

            // Background (rounded rect)
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(root.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.8f);

            // Main text
            var textGo = new GameObject("MainText");
            textGo.transform.SetParent(root.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();

            if (string.IsNullOrEmpty(translationText))
            {
                textRect.anchorMin = new Vector2(0.05f, 0.1f);
                textRect.anchorMax = new Vector2(0.95f, 0.9f);
            }
            else
            {
                textRect.anchorMin = new Vector2(0.05f, 0.35f);
                textRect.anchorMax = new Vector2(0.95f, 0.9f);
            }
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            refs.mainText = textGo.AddComponent<TextMeshProUGUI>();
            refs.mainText.text = text;
            refs.mainText.fontSize = 28f;
            refs.mainText.color = Color.black;
            refs.mainText.alignment = TextAlignmentOptions.Center;
            refs.mainText.textWrappingMode = TextWrappingModes.Normal;
            refs.mainText.maxVisibleCharacters = 0;

            // Translation text (optional)
            if (!string.IsNullOrEmpty(translationText))
            {
                var transGo = new GameObject("TranslationText");
                transGo.transform.SetParent(root.transform, false);
                var transRect = transGo.AddComponent<RectTransform>();
                transRect.anchorMin = new Vector2(0.05f, 0.05f);
                transRect.anchorMax = new Vector2(0.95f, 0.35f);
                transRect.offsetMin = Vector2.zero;
                transRect.offsetMax = Vector2.zero;

                refs.translationTmpText = transGo.AddComponent<TextMeshProUGUI>();
                refs.translationTmpText.text = translationText;
                refs.translationTmpText.fontSize = 20f;
                refs.translationTmpText.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                refs.translationTmpText.fontStyle = FontStyles.Italic;
                refs.translationTmpText.alignment = TextAlignmentOptions.Center;
                refs.translationTmpText.textWrappingMode = TextWrappingModes.Normal;
                refs.translationTmpText.maxVisibleCharacters = 0;
            }

            return refs;
        }

        private IEnumerator SpeechBubbleCoroutine(SpeechBubbleRefs refs, string text,
            string translationText, float holdDuration, Action onComplete)
        {
            float charsPerSecond = 30f;

            // Typewriter effect for main text
            int totalMainChars = text.Length;
            float charTimer = 0f;
            int visibleCount = 0;

            while (visibleCount < totalMainChars)
            {
                if (refs.root == null) yield break;

                // Follow target
                if (refs.target != null)
                    refs.root.transform.position = refs.target.position + Vector3.up * 2.5f;

                // Face camera
                FaceBubbleToCamera(refs.root.transform);

                charTimer += Time.unscaledDeltaTime * charsPerSecond;
                int charsToShow = Mathf.FloorToInt(charTimer);
                if (charsToShow > visibleCount)
                {
                    visibleCount = Mathf.Min(charsToShow, totalMainChars);
                    refs.mainText.maxVisibleCharacters = visibleCount;
                }
                yield return null;
            }
            refs.mainText.maxVisibleCharacters = totalMainChars;

            // Typewriter effect for translation text
            if (refs.translationTmpText != null && !string.IsNullOrEmpty(translationText))
            {
                int totalTransChars = translationText.Length;
                charTimer = 0f;
                visibleCount = 0;

                while (visibleCount < totalTransChars)
                {
                    if (refs.root == null) yield break;

                    if (refs.target != null)
                        refs.root.transform.position = refs.target.position + Vector3.up * 2.5f;
                    FaceBubbleToCamera(refs.root.transform);

                    charTimer += Time.unscaledDeltaTime * charsPerSecond;
                    int charsToShow = Mathf.FloorToInt(charTimer);
                    if (charsToShow > visibleCount)
                    {
                        visibleCount = Mathf.Min(charsToShow, totalTransChars);
                        refs.translationTmpText.maxVisibleCharacters = visibleCount;
                    }
                    yield return null;
                }
                refs.translationTmpText.maxVisibleCharacters = totalTransChars;
            }

            // Hold
            float elapsed = 0f;
            while (elapsed < holdDuration)
            {
                if (refs.root == null) yield break;

                if (refs.target != null)
                    refs.root.transform.position = refs.target.position + Vector3.up * 2.5f;
                FaceBubbleToCamera(refs.root.transform);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out
            float fadeOutDuration = 0.5f;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                if (refs.root == null) yield break;

                if (refs.target != null)
                    refs.root.transform.position = refs.target.position + Vector3.up * 2.5f;
                FaceBubbleToCamera(refs.root.transform);

                elapsed += Time.unscaledDeltaTime;
                refs.canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }

            CleanupSpeechBubble(refs.root);
            onComplete?.Invoke();
        }

        private static void FaceBubbleToCamera(Transform bubbleTransform)
        {
            var cam = Camera.main;
            if (cam != null)
                bubbleTransform.forward = cam.transform.forward;
        }

        private void CleanupSpeechBubble(GameObject bubbleRoot)
        {
            if (bubbleRoot != null)
            {
                activeSpeechBubbles.Remove(bubbleRoot);
                Destroy(bubbleRoot);
            }
        }

        #endregion

        #region HideAll

        /// <summary>
        /// Stops all active overlays and destroys their GameObjects.
        /// </summary>
        public void HideAll()
        {
            // Stop panel text
            if (panelCoroutine != null)
            {
                StopCoroutine(panelCoroutine);
                panelCoroutine = null;
            }
            if (panelTextObject != null)
            {
                activeObjects.Remove(panelTextObject);
                Destroy(panelTextObject);
                panelTextObject = null;
            }

            // Stop burst text
            if (burstCoroutine != null)
            {
                StopCoroutine(burstCoroutine);
                burstCoroutine = null;
            }
            if (burstTextObject != null)
            {
                activeObjects.Remove(burstTextObject);
                Destroy(burstTextObject);
                burstTextObject = null;
            }

            // Stop speech bubbles
            for (int i = 0; i < speechBubbleCoroutines.Count; i++)
            {
                if (speechBubbleCoroutines[i] != null)
                    StopCoroutine(speechBubbleCoroutines[i]);
            }
            speechBubbleCoroutines.Clear();

            for (int i = 0; i < activeSpeechBubbles.Count; i++)
            {
                if (activeSpeechBubbles[i] != null)
                    Destroy(activeSpeechBubbles[i]);
            }
            activeSpeechBubbles.Clear();

            // Cleanup any remaining tracked objects
            for (int i = 0; i < activeObjects.Count; i++)
            {
                if (activeObjects[i] != null)
                    Destroy(activeObjects[i]);
            }
            activeObjects.Clear();
        }

        #endregion
    }
}
