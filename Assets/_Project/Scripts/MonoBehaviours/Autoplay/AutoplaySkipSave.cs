using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplaySkipSave : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-012";
            specTitle = "Cutscene Skip & Auto-Save";
            totalSteps = 5;
        }

        protected override IEnumerator RunDemo()
        {
            // Find or create a SkipPrompt
            var skipPrompt = FindAnyObjectByType<SkipPrompt>();
            if (skipPrompt == null)
            {
                var go = new GameObject("SkipPrompt_Autoplay");
                skipPrompt = go.AddComponent<SkipPrompt>();
                Debug.Log("[AutoplaySkipSave] Created runtime SkipPrompt.");
            }

            Step("Activating skip prompt");
            skipPrompt.Activate();
            Debug.Log("[AutoplaySkipSave] SkipPrompt activated (appears after 3s delay).");
            yield return Wait(3f);

            Step("Skip prompt visible \u2014 hold Space to test");
            Debug.Log("[AutoplaySkipSave] Skip prompt should now be visible. Hold Space to test skip.");
            yield return Wait(5f);

            Step("Deactivating skip prompt");
            skipPrompt.Deactivate();
            Debug.Log("[AutoplaySkipSave] SkipPrompt deactivated.");
            yield return Wait(2f);

            Step("Saving intro complete");
            AutoSave.SaveIntroComplete();
            Debug.Log($"[AutoplaySkipSave] Save path: {Application.persistentDataPath}/farm_save.json");
            yield return Wait(2f);

            Step("Checking save status");
            bool completed = AutoSave.HasCompletedIntro();
            Debug.Log($"[AutoplaySkipSave] HasCompletedIntro = {completed}");
            AutoSave.ClearSave();
            Debug.Log("[AutoplaySkipSave] Save cleared.");
            yield return Wait(2f);
        }
    }
}
