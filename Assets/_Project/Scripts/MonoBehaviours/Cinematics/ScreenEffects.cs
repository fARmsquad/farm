using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Orchestrates full-screen visual effects: fade, shake, letterbox, objective popups, mission banners.
    /// Self-contained implementation with no external dependencies beyond Unity + TMPro.
    /// </summary>
    public class ScreenEffects : MonoBehaviour
    {
        public static ScreenEffects Instance { get; private set; }

        [Header("Fade")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Letterbox")]
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private float maxLetterboxHeight = 100f;

        [Header("Objective Popup")]
        [SerializeField] private RectTransform objectiveContainer;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private float objectiveHoldDuration = 2f;
        [SerializeField] private float objectiveSlideDistance = 600f;

        [Header("Mission Passed")]
        [SerializeField] private CanvasGroup missionPassedGroup;
        [SerializeField] private TMP_Text missionPassedText;
        [SerializeField] private float missionPassedDuration = 3f;

        [Header("Camera Shake")]
        [SerializeField] private Camera targetCamera;

        [Header("Events")]
        public UnityEvent OnFadeComplete;
        public UnityEvent OnShakeComplete;
        public UnityEvent OnLetterboxComplete;
        public UnityEvent OnObjectiveComplete;
        public UnityEvent OnMissionPassedComplete;

        // Internal state
        private Coroutine fadeCoroutine;
        private Coroutine shakeCoroutine;
        private Coroutine letterboxCoroutine;
        private Coroutine objectiveCoroutine;
        private Coroutine missionPassedCoroutine;
        private Vector3 originalCameraPosition;
        private float currentFadeAlpha;
        private float currentLetterboxHeight;

        // Public state for testing
        public bool IsFading => fadeCoroutine != null;
        public bool IsShaking => shakeCoroutine != null;
        public bool IsLetterboxAnimating => letterboxCoroutine != null;
        public float CurrentFadeAlpha => currentFadeAlpha;
        public float CurrentLetterboxHeight => currentLetterboxHeight;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera != null)
                originalCameraPosition = targetCamera.transform.localPosition;

            // Initialize states
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0;
                currentFadeAlpha = 0;
            }

            if (topBar != null)
                topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, 0);
            if (bottomBar != null)
                bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, 0);
            currentLetterboxHeight = 0;

            if (objectiveContainer != null)
                objectiveContainer.gameObject.SetActive(false);

            if (missionPassedGroup != null)
            {
                missionPassedGroup.alpha = 0;
                missionPassedGroup.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #region Fade

        /// <summary>
        /// Fades screen to black over the specified duration.
        /// </summary>
        public void FadeToBlack(float duration, Action onComplete = null)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            if (duration <= 0)
            {
                SetFadeAlpha(1f);
                onComplete?.Invoke();
                OnFadeComplete?.Invoke();
                return;
            }

            fadeCoroutine = StartCoroutine(FadeCoroutine(currentFadeAlpha, 1f, duration, onComplete));
        }

        /// <summary>
        /// Fades screen from black to transparent over the specified duration.
        /// </summary>
        public void FadeFromBlack(float duration, Action onComplete = null)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            if (duration <= 0)
            {
                SetFadeAlpha(0f);
                onComplete?.Invoke();
                OnFadeComplete?.Invoke();
                return;
            }

            fadeCoroutine = StartCoroutine(FadeCoroutine(currentFadeAlpha, 0f, duration, onComplete));
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeCurve.Evaluate(elapsed / duration);
                SetFadeAlpha(Mathf.Lerp(from, to, t));
                yield return null;
            }
            SetFadeAlpha(to);
            fadeCoroutine = null;
            onComplete?.Invoke();
            OnFadeComplete?.Invoke();
        }

        private void SetFadeAlpha(float alpha)
        {
            currentFadeAlpha = alpha;
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = alpha;
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0, 0, 0, alpha);
        }

        #endregion

        #region Screen Shake

        /// <summary>
        /// Shakes the camera with the given intensity and duration.
        /// </summary>
        public void ScreenShake(float intensity, float duration, Action onComplete = null)
        {
            if (intensity <= 0 || duration <= 0)
            {
                onComplete?.Invoke();
                OnShakeComplete?.Invoke();
                return;
            }

            if (shakeCoroutine != null)
                StopCoroutine(shakeCoroutine);

            shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration, onComplete));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration, Action onComplete)
        {
            if (targetCamera == null)
            {
                shakeCoroutine = null;
                onComplete?.Invoke();
                OnShakeComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - (elapsed / duration);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * intensity * decay;
                targetCamera.transform.localPosition = originalCameraPosition + new Vector3(offset.x, offset.y, 0);
                yield return null;
            }

            targetCamera.transform.localPosition = originalCameraPosition;
            shakeCoroutine = null;
            onComplete?.Invoke();
            OnShakeComplete?.Invoke();
        }

        #endregion

        #region Letterbox

        /// <summary>
        /// Shows letterbox bars at the specified height percentage (0-1).
        /// </summary>
        public void ShowLetterbox(float heightPercent, float duration, Action onComplete = null)
        {
            float targetHeight = Mathf.Clamp01(heightPercent) * maxLetterboxHeight;

            if (letterboxCoroutine != null)
                StopCoroutine(letterboxCoroutine);

            if (duration <= 0)
            {
                SetLetterboxHeight(targetHeight);
                onComplete?.Invoke();
                OnLetterboxComplete?.Invoke();
                return;
            }

            letterboxCoroutine = StartCoroutine(LetterboxCoroutine(currentLetterboxHeight, targetHeight, duration, onComplete));
        }

        /// <summary>
        /// Hides letterbox bars over the specified duration.
        /// </summary>
        public void HideLetterbox(float duration, Action onComplete = null)
        {
            if (letterboxCoroutine != null)
                StopCoroutine(letterboxCoroutine);

            if (duration <= 0)
            {
                SetLetterboxHeight(0);
                onComplete?.Invoke();
                OnLetterboxComplete?.Invoke();
                return;
            }

            letterboxCoroutine = StartCoroutine(LetterboxCoroutine(currentLetterboxHeight, 0, duration, onComplete));
        }

        private IEnumerator LetterboxCoroutine(float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseInOutQuadratic(elapsed / duration);
                SetLetterboxHeight(Mathf.Lerp(from, to, t));
                yield return null;
            }
            SetLetterboxHeight(to);
            letterboxCoroutine = null;
            onComplete?.Invoke();
            OnLetterboxComplete?.Invoke();
        }

        private void SetLetterboxHeight(float height)
        {
            currentLetterboxHeight = height;
            if (topBar != null)
                topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, height);
            if (bottomBar != null)
                bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, height);
        }

        #endregion

        #region Objective Popup

        /// <summary>
        /// Shows objective text: slides in from left, holds, slides out to right.
        /// </summary>
        public void ShowObjective(string text, Action onComplete = null)
        {
            if (objectiveCoroutine != null)
                StopCoroutine(objectiveCoroutine);

            objectiveCoroutine = StartCoroutine(ObjectiveCoroutine(text, onComplete));
        }

        private IEnumerator ObjectiveCoroutine(string text, Action onComplete)
        {
            if (objectiveText == null || objectiveContainer == null)
            {
                objectiveCoroutine = null;
                onComplete?.Invoke();
                OnObjectiveComplete?.Invoke();
                yield break;
            }

            objectiveText.text = text;
            objectiveContainer.gameObject.SetActive(true);

            float slideDuration = 0.5f;
            Vector2 startPos = new Vector2(-objectiveSlideDistance, 0);
            Vector2 centerPos = Vector2.zero;
            Vector2 endPos = new Vector2(objectiveSlideDistance, 0);

            // Slide in
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutQuadratic(elapsed / slideDuration);
                objectiveContainer.anchoredPosition = Vector2.Lerp(startPos, centerPos, t);
                yield return null;
            }
            objectiveContainer.anchoredPosition = centerPos;

            // Hold
            yield return new WaitForSecondsRealtime(objectiveHoldDuration);

            // Slide out
            elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseInQuadratic(elapsed / slideDuration);
                objectiveContainer.anchoredPosition = Vector2.Lerp(centerPos, endPos, t);
                yield return null;
            }

            objectiveContainer.gameObject.SetActive(false);
            objectiveCoroutine = null;
            onComplete?.Invoke();
            OnObjectiveComplete?.Invoke();
        }

        #endregion

        #region Mission Passed

        /// <summary>
        /// Shows mission passed banner centered, auto-fades after duration.
        /// </summary>
        public void ShowMissionPassed(string text, Action onComplete = null)
        {
            if (missionPassedCoroutine != null)
                StopCoroutine(missionPassedCoroutine);

            missionPassedCoroutine = StartCoroutine(MissionPassedCoroutine(text, onComplete));
        }

        private IEnumerator MissionPassedCoroutine(string text, Action onComplete)
        {
            if (missionPassedText == null || missionPassedGroup == null)
            {
                missionPassedCoroutine = null;
                onComplete?.Invoke();
                OnMissionPassedComplete?.Invoke();
                yield break;
            }

            missionPassedText.text = text;
            missionPassedGroup.gameObject.SetActive(true);

            // Fade in
            float fadeInDuration = 0.3f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                missionPassedGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeInDuration);
                yield return null;
            }
            missionPassedGroup.alpha = 1;

            // Hold
            yield return new WaitForSecondsRealtime(missionPassedDuration);

            // Fade out
            float fadeOutDuration = 0.5f;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                missionPassedGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeOutDuration);
                yield return null;
            }

            missionPassedGroup.alpha = 0;
            missionPassedGroup.gameObject.SetActive(false);
            missionPassedCoroutine = null;
            onComplete?.Invoke();
            OnMissionPassedComplete?.Invoke();
        }

        #endregion

        #region Easing Functions

        private static float EaseInQuadratic(float t) => t * t;
        private static float EaseOutQuadratic(float t) => t * (2 - t);
        private static float EaseInOutQuadratic(float t) => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;

        #endregion

        #region Utility

        /// <summary>
        /// Resets all effects to their default state.
        /// </summary>
        public void ResetAll()
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            if (letterboxCoroutine != null) StopCoroutine(letterboxCoroutine);
            if (objectiveCoroutine != null) StopCoroutine(objectiveCoroutine);
            if (missionPassedCoroutine != null) StopCoroutine(missionPassedCoroutine);

            fadeCoroutine = null;
            shakeCoroutine = null;
            letterboxCoroutine = null;
            objectiveCoroutine = null;
            missionPassedCoroutine = null;

            SetFadeAlpha(0);
            SetLetterboxHeight(0);

            if (targetCamera != null)
                targetCamera.transform.localPosition = originalCameraPosition;

            if (objectiveContainer != null)
                objectiveContainer.gameObject.SetActive(false);

            if (missionPassedGroup != null)
            {
                missionPassedGroup.alpha = 0;
                missionPassedGroup.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
