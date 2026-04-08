using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// MonoBehaviour wrapper for a ParticleSystem, providing simple Play/Stop/Intensity controls.
    /// Attach to a GameObject that already has a ParticleSystem, or assign one via the Inspector.
    /// </summary>
    public class ParticleController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem targetParticleSystem;

        /// <summary>
        /// True when the wrapped particle system is currently emitting or has live particles.
        /// </summary>
        public bool IsPlaying => targetParticleSystem != null && targetParticleSystem.isPlaying;

        private void Awake()
        {
            if (targetParticleSystem == null)
                targetParticleSystem = GetComponent<ParticleSystem>();
        }

        /// <summary>
        /// Starts particle emission.
        /// </summary>
        public void Play()
        {
            if (targetParticleSystem != null)
                targetParticleSystem.Play();
        }

        /// <summary>
        /// Stops new emission. Existing particles fade out naturally.
        /// </summary>
        public void Stop()
        {
            if (targetParticleSystem != null)
                targetParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>
        /// Sets the emission rate multiplier. Clamped to [0, 1].
        /// 0 = no emission, 1 = full configured rate.
        /// </summary>
        public void SetIntensity(float intensity)
        {
            if (targetParticleSystem == null) return;

            intensity = Mathf.Clamp01(intensity);
            var emission = targetParticleSystem.emission;
            emission.rateOverTimeMultiplier = intensity;
        }
    }
}
