using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Creates a runtime PostProcess profile with an animated Vignette effect.
    /// The vignette intensity is driven by <see cref="intensityCurve"/> over
    /// <see cref="animationDuration"/> seconds, looping if <see cref="loop"/> is true.
    /// Works in both Edit mode (Timeline preview) and Play mode.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(PostProcessVolume))]
    public class IntroPostProcessSetup : MonoBehaviour
    {
        [Header("Vignette Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float maxIntensity = 0.5f;

        [Range(0.01f, 1f)]
        [SerializeField] private float smoothness = 0.4f;

        [Range(0f, 1f)]
        [SerializeField] private float roundness = 1f;

        [SerializeField] private Color vignetteColor = Color.black;

        [Header("Animation")]
        [Tooltip("Curve that controls vignette intensity over time (Y axis is multiplied by Max Intensity).")]
        [SerializeField] private AnimationCurve intensityCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(0.3f, 1f, 0f, 0f),
            new Keyframe(0.7f, 1f, 0f, 0f),
            new Keyframe(1f, 0.6f, -1f, 0f)
        );

        [Tooltip("Total duration of one curve cycle in seconds.")]
        [SerializeField] private float animationDuration = 15f;

        [SerializeField] private bool loop = false;

        private PostProcessVolume _volume;
        private PostProcessProfile _runtimeProfile;
        private Vignette _vignette;
        private float _elapsed;

        private void OnEnable()
        {
            _volume = GetComponent<PostProcessVolume>();

            _runtimeProfile = _volume.sharedProfile != null
                ? Instantiate(_volume.sharedProfile)
                : ScriptableObject.CreateInstance<PostProcessProfile>();

            _runtimeProfile.hideFlags = HideFlags.HideAndDontSave;
            _volume.profile = _runtimeProfile;

            if (!_runtimeProfile.TryGetSettings(out _vignette))
            {
                _vignette = _runtimeProfile.AddSettings<Vignette>();
            }

            _vignette.enabled.Override(true);
            _vignette.mode.Override(VignetteMode.Classic);
            _vignette.smoothness.Override(smoothness);
            _vignette.roundness.Override(roundness);
            _vignette.color.Override(vignetteColor);
            _vignette.intensity.Override(0f);

            _elapsed = 0f;
        }

        private void Update()
        {
            if (_vignette == null) return;

            float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.016f;
            _elapsed += dt;

            float t;
            if (loop)
            {
                t = animationDuration > 0f
                    ? Mathf.Repeat(_elapsed, animationDuration) / animationDuration
                    : 1f;
            }
            else
            {
                t = animationDuration > 0f
                    ? Mathf.Clamp01(_elapsed / animationDuration)
                    : 1f;
            }

            float curveValue = Mathf.Clamp01(intensityCurve.Evaluate(t));
            _vignette.intensity.Override(curveValue * maxIntensity);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_vignette != null)
            {
                _vignette.smoothness.Override(smoothness);
                _vignette.roundness.Override(roundness);
                _vignette.color.Override(vignetteColor);
            }
        }
#endif

        private void OnDisable()
        {
            if (_runtimeProfile != null)
            {
                if (Application.isPlaying)
                    Destroy(_runtimeProfile);
                else
                    DestroyImmediate(_runtimeProfile);

                _runtimeProfile = null;
                _vignette = null;
            }
        }
    }
}
