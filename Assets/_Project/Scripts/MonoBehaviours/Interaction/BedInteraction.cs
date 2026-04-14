using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Farming;

namespace FarmSimVR.MonoBehaviours.Interaction
{
    /// <summary>
    /// Subclass of InteractableObject attached to the bed.
    /// Fades to black, advances the day clock to the next morning,
    /// then fades back in.
    /// </summary>
    public class BedInteraction : InteractableObject
    {
        private const float FADE_DURATION = 0.5f;
        private const float SLEEP_DURATION = 1f;

        /// <summary>Normalised time to wake up at (0.35 = early morning).</summary>
        private const float MORNING_TIME = 0.35f;

        private bool _isSleeping;

        /// <summary>
        /// Starts the sleep coroutine when the player interacts with the bed.
        /// </summary>
        public override void OnInteract()
        {
            if (_isSleeping) return;
            StartCoroutine(SleepCoroutine());
        }

        private IEnumerator SleepCoroutine()
        {
            _isSleeping = true;

            // Disable player controller during sleep
            var playerController = FindAnyObjectByType<TownPlayerController>();
            if (playerController != null)
                playerController.enabled = false;

            // Fade to black
            bool fadeDone = false;
            if (ScreenEffects.Instance != null)
            {
                ScreenEffects.Instance.FadeToBlack(FADE_DURATION, () => fadeDone = true);
                while (!fadeDone)
                    yield return null;
            }

            // Advance the clock to next morning
            if (FarmDayClockDriver.Instance != null)
            {
                FarmDayClockDriver.Instance.Clock.SkipTo(MORNING_TIME);
                Debug.Log("[BedInteraction] Clock advanced to next morning.");
            }

            // Simulate passage of time
            yield return new WaitForSeconds(SLEEP_DURATION);

            // Fade from black
            fadeDone = false;
            if (ScreenEffects.Instance != null)
            {
                ScreenEffects.Instance.FadeFromBlack(FADE_DURATION, () => fadeDone = true);
                while (!fadeDone)
                    yield return null;
            }

            // Re-enable player controller
            if (playerController != null)
                playerController.enabled = true;

            Debug.Log("[BedInteraction] Player rested.");
            _isSleeping = false;
        }
    }
}
