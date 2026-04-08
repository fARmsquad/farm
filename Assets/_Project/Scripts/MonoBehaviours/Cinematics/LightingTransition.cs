using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Smoothly transitions between LightingPreset states by lerping RenderSettings
    /// and directional light properties over a specified duration.
    /// Uses Time.unscaledDeltaTime so transitions work even when the game is paused.
    /// </summary>
    public class LightingTransition : MonoBehaviour
    {
        [SerializeField] private Light directionalLight;

        [Header("Events")]
        public UnityEvent OnTransitionComplete;

        private Coroutine activeTransition;

        private void Start()
        {
            if (directionalLight == null)
                directionalLight = FindAnyObjectByType<Light>();
        }

        /// <summary>
        /// Plays a transition from one preset to another over the given duration.
        /// </summary>
        /// <param name="from">Starting lighting state.</param>
        /// <param name="to">Target lighting state.</param>
        /// <param name="duration">Transition duration in seconds.</param>
        /// <param name="easing">Optional animation curve. Defaults to EaseInOut if null.</param>
        public void Play(LightingPreset from, LightingPreset to, float duration, AnimationCurve easing = null)
        {
            if (activeTransition != null)
                StopCoroutine(activeTransition);

            if (easing == null)
                easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            if (duration <= 0f)
            {
                ApplyPreset(to);
                OnTransitionComplete?.Invoke();
                return;
            }

            activeTransition = StartCoroutine(TransitionCoroutine(from, to, duration, easing));
        }

        /// <summary>
        /// Captures the current lighting state as "from" and transitions to the target preset.
        /// </summary>
        /// <param name="to">Target lighting state.</param>
        /// <param name="duration">Transition duration in seconds.</param>
        public void Play(LightingPreset to, float duration)
        {
            var from = CaptureCurrentState();
            Play(from, to, duration);
        }

        /// <summary>
        /// Cancels the active transition, snapping to the current interpolated state.
        /// </summary>
        public void Stop()
        {
            if (activeTransition != null)
            {
                StopCoroutine(activeTransition);
                activeTransition = null;
            }
        }

        /// <summary>
        /// Immediately applies a preset without any transition.
        /// </summary>
        public void ApplyPreset(LightingPreset preset)
        {
            if (preset == null) return;

            RenderSettings.ambientLight = preset.ambientColor;
            RenderSettings.ambientIntensity = preset.ambientIntensity;
            RenderSettings.fogColor = preset.fogColor;
            RenderSettings.fogDensity = preset.fogDensity;

            if (RenderSettings.skybox != null)
                RenderSettings.skybox.SetColor("_Tint", preset.skyboxTint);

            if (directionalLight != null)
            {
                directionalLight.color = preset.directionalColor;
                directionalLight.intensity = preset.directionalIntensity;
                directionalLight.transform.eulerAngles = preset.directionalRotation;
            }
        }

        /// <summary>
        /// Captures the current scene lighting into a runtime LightingPreset.
        /// </summary>
        public LightingPreset CaptureCurrentState()
        {
            var preset = ScriptableObject.CreateInstance<LightingPreset>();
            preset.ambientColor = RenderSettings.ambientLight;
            preset.ambientIntensity = RenderSettings.ambientIntensity;
            preset.fogColor = RenderSettings.fogColor;
            preset.fogDensity = RenderSettings.fogDensity;

            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
                preset.skyboxTint = RenderSettings.skybox.GetColor("_Tint");
            else
                preset.skyboxTint = new Color(0.5f, 0.5f, 0.5f);

            if (directionalLight != null)
            {
                preset.directionalColor = directionalLight.color;
                preset.directionalIntensity = directionalLight.intensity;
                preset.directionalRotation = directionalLight.transform.eulerAngles;
            }

            return preset;
        }

        private IEnumerator TransitionCoroutine(LightingPreset from, LightingPreset to, float duration, AnimationCurve easing)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float raw = Mathf.Clamp01(elapsed / duration);
                float t = easing.Evaluate(raw);

                // Ambient
                RenderSettings.ambientLight = Color.Lerp(from.ambientColor, to.ambientColor, t);
                RenderSettings.ambientIntensity = Mathf.Lerp(from.ambientIntensity, to.ambientIntensity, t);

                // Fog
                RenderSettings.fogColor = Color.Lerp(from.fogColor, to.fogColor, t);
                RenderSettings.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);

                // Skybox
                if (RenderSettings.skybox != null)
                    RenderSettings.skybox.SetColor("_Tint", Color.Lerp(from.skyboxTint, to.skyboxTint, t));

                // Directional light
                if (directionalLight != null)
                {
                    directionalLight.color = Color.Lerp(from.directionalColor, to.directionalColor, t);
                    directionalLight.intensity = Mathf.Lerp(from.directionalIntensity, to.directionalIntensity, t);

                    // Lerp rotation via Quaternion to avoid gimbal issues
                    Quaternion fromRot = Quaternion.Euler(from.directionalRotation);
                    Quaternion toRot = Quaternion.Euler(to.directionalRotation);
                    directionalLight.transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
                }

                yield return null;
            }

            // Snap to final state
            ApplyPreset(to);
            activeTransition = null;
            OnTransitionComplete?.Invoke();
        }
    }
}
