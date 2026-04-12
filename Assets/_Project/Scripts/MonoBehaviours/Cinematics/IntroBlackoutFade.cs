using FarmSimVR.Core.Cinematics;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Drives a CanvasGroup alpha from the intro <see cref="PlayableDirector"/> clock
    /// so Slide_08 can fade to black in sync with timeline audio (e.g. ecstasy-of-gold).
    /// </summary>
    public sealed class IntroBlackoutFade : MonoBehaviour
    {
        [SerializeField] private PlayableDirector director;
        [SerializeField] private CanvasGroup blackoutGroup;
        [SerializeField] private double fadeStartTime = 51.49369;
        [SerializeField] private double fadeDuration = 4d;

        private void LateUpdate()
        {
            if (director == null || blackoutGroup == null)
                return;

            var clock = director.time;
            var duration = director.duration;
            if (duration > 0.0001d && clock > duration)
                clock = duration;

            if (clock < 0d)
                clock = 0d;

            blackoutGroup.alpha = IntroBlackoutFadeMath.ComputeAlpha(
                clock,
                fadeStartTime,
                fadeDuration);
        }
    }
}
