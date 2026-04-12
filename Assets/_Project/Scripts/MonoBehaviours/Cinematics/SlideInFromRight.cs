using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Slides a UI RectTransform in from the right when enabled, then
    /// applies a short rumble at the landing position. Resets on disable
    /// so Timeline Activation Tracks can re-trigger cleanly.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SlideInFromRight : MonoBehaviour
    {
        [Header("Slide-In")]
        [SerializeField] private float slideOffset = 800f;
        [SerializeField] private float slideDuration = 0.6f;
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Rumble")]
        [SerializeField] private float rumbleIntensity = 12f;
        [SerializeField] private float rumbleDuration = 0.4f;
        [SerializeField] private float rumbleFrequency = 30f;

        private RectTransform _rect;
        private Vector2 _restPosition;
        private Coroutine _routine;

        private void OnEnable()
        {
            _rect = GetComponent<RectTransform>();
            _routine = StartCoroutine(SlideAndRumble());
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_rect != null)
                _rect.anchoredPosition = _restPosition;
        }

        private IEnumerator SlideAndRumble()
        {
            // Parent layout (e.g. CanvasScaler, slideshow letterbox) must run first
            // or anchoredPosition rest is wrong for edge-anchored graphics.
            yield return null;
            Canvas.ForceUpdateCanvases();
            _restPosition = _rect.anchoredPosition;
            _rect.anchoredPosition = _restPosition + new Vector2(slideOffset, 0f);

            Vector2 startPos = _rect.anchoredPosition;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float curved = slideCurve.Evaluate(t);
                _rect.anchoredPosition = Vector2.LerpUnclamped(startPos, _restPosition, curved);
                yield return null;
            }

            _rect.anchoredPosition = _restPosition;

            elapsed = 0f;
            float interval = 1f / rumbleFrequency;
            float timer = 0f;

            while (elapsed < rumbleDuration)
            {
                elapsed += Time.deltaTime;
                timer += Time.deltaTime;

                if (timer >= interval)
                {
                    timer -= interval;
                    float decay = 1f - (elapsed / rumbleDuration);
                    Vector2 offset = Random.insideUnitCircle * rumbleIntensity * decay;
                    _rect.anchoredPosition = _restPosition + offset;
                }

                yield return null;
            }

            _rect.anchoredPosition = _restPosition;
            _routine = null;
        }
    }
}
