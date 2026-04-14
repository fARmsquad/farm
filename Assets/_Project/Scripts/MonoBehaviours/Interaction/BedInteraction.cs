using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Interaction
{
    /// <summary>
    /// Subclass of InteractableObject attached to the bed.
    /// Fades to black to simulate sleeping/resting, then fades back in.
    /// </summary>
    public class BedInteraction : InteractableObject
    {
        private const float FADE_DURATION = 0.5f;
        private const float SLEEP_DURATION = 1f;

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
