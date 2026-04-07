using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Bootstraps the WorldMain scene on load: fades in from black
    /// and shows an objective popup. Requires a ScreenEffects instance in the scene.
    /// </summary>
    public class WorldSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private float fadeInDuration = 1.5f;
        [SerializeField] private string objectiveText = "Explore Willowbrook";

        private void Start()
        {
            if (ScreenEffects.Instance == null)
            {
                Debug.LogWarning("[WorldSceneBootstrap] No ScreenEffects found — skipping fade-in.");
                return;
            }

            ScreenEffects.Instance.FadeToBlack(0);
            StartCoroutine(FadeInSequence());
        }

        private IEnumerator FadeInSequence()
        {
            // Brief hold on black to let the scene settle
            yield return new WaitForSecondsRealtime(0.3f);

            ScreenEffects.Instance.FadeFromBlack(fadeInDuration, () =>
            {
                if (!string.IsNullOrEmpty(objectiveText))
                    ScreenEffects.Instance.ShowObjective(objectiveText);
            });
        }
    }
}
