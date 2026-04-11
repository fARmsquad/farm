using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Applies a slow Ken Burns-style zoom to a UI element by animating its
    /// RectTransform localScale from <see cref="startScale"/> to <see cref="endScale"/>
    /// over <see cref="duration"/> seconds. Resets on disable so Timeline Activation
    /// Tracks can re-trigger it cleanly.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SlideZoom : MonoBehaviour
    {
        [Header("Zoom")]
        [Tooltip("Scale at the start of the zoom.")]
        [SerializeField] private float startScale = 1f;

        [Tooltip("Scale at the end of the zoom.")]
        [SerializeField] private float endScale = 1.15f;

        [Header("Timing")]
        [Tooltip("Duration of the zoom in seconds.")]
        [SerializeField] private float duration = 8f;

        [Tooltip("Easing curve (0→1 over normalised time).")]
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private RectTransform _rect;
        private float _elapsed;

        private void OnEnable()
        {
            _rect = GetComponent<RectTransform>();
            _elapsed = 0f;
            ApplyScale(0f);
        }

        private void Update()
        {
            if (_rect == null) return;

            _elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(_elapsed / duration) : 1f;
            ApplyScale(t);
        }

        private void ApplyScale(float t)
        {
            float curvedT = easeCurve.Evaluate(t);
            float scale = Mathf.LerpUnclamped(startScale, endScale, curvedT);
            _rect.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnDisable()
        {
            if (_rect != null)
            {
                _rect.localScale = Vector3.one;
            }
        }
    }
}
