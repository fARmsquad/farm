using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Demo controller for testing INT-001 Screen Effects.
    /// Each public method is wired to a UI button in the test scene.
    /// </summary>
    public class ScreenEffectsDemo : MonoBehaviour
    {
        [SerializeField] public ScreenEffects screenEffects;

        private int objectiveCount = 1;

        private void Start()
        {
            if (screenEffects == null)
                screenEffects = FindAnyObjectByType<ScreenEffects>();
        }

        public void OnFadeToBlack()
        {
            Debug.Log("[ScreenEffectsDemo] Fade To Black (1s)");
            screenEffects?.FadeToBlack(1f, () => Debug.Log("[ScreenEffectsDemo] Fade To Black complete"));
        }

        public void OnFadeFromBlack()
        {
            Debug.Log("[ScreenEffectsDemo] Fade From Black (1s)");
            screenEffects?.FadeFromBlack(1f, () => Debug.Log("[ScreenEffectsDemo] Fade From Black complete"));
        }

        public void OnScreenShake()
        {
            Debug.Log("[ScreenEffectsDemo] Screen Shake (intensity: 0.5, duration: 0.5s)");
            screenEffects?.ScreenShake(0.5f, 0.5f, () => Debug.Log("[ScreenEffectsDemo] Screen Shake complete"));
        }

        public void OnShowLetterbox()
        {
            Debug.Log("[ScreenEffectsDemo] Show Letterbox (100%, 0.5s)");
            screenEffects?.ShowLetterbox(1f, 0.5f, () => Debug.Log("[ScreenEffectsDemo] Show Letterbox complete"));
        }

        public void OnHideLetterbox()
        {
            Debug.Log("[ScreenEffectsDemo] Hide Letterbox (0.5s)");
            screenEffects?.HideLetterbox(0.5f, () => Debug.Log("[ScreenEffectsDemo] Hide Letterbox complete"));
        }

        public void OnShowObjective()
        {
            string objective = $"OBJECTIVE #{objectiveCount}: Investigate the disturbance";
            Debug.Log($"[ScreenEffectsDemo] Show Objective: {objective}");
            screenEffects?.ShowObjective(objective, () => Debug.Log("[ScreenEffectsDemo] Show Objective complete"));
            objectiveCount++;
        }

        public void OnMissionPassed()
        {
            Debug.Log("[ScreenEffectsDemo] Mission Passed");
            screenEffects?.ShowMissionPassed("MISSION PASSED", () => Debug.Log("[ScreenEffectsDemo] Mission Passed complete"));
        }

        public void OnResetAll()
        {
            Debug.Log("[ScreenEffectsDemo] Reset All Effects");
            screenEffects?.ResetAll();
        }
    }
}
