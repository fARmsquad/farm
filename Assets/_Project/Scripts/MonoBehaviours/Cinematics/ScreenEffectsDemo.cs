using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Debug overlay for testing INT-001 Screen Effects in the World scene.
    /// Press F1 to toggle the debug panel. Uses IMGUI so it's self-contained
    /// with no Canvas setup required.
    /// </summary>
    public class ScreenEffectsDemo : MonoBehaviour
    {
        [SerializeField] public ScreenEffects screenEffects;
        private int objectiveCount = 1;
        private bool showPanel;

        private void Start()
        {
            if (screenEffects == null)
                screenEffects = FindAnyObjectByType<ScreenEffects>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.leftAltKey.isPressed && kb.digit1Key.wasPressedThisFrame)
                showPanel = !showPanel;
        }

        private void OnGUI()
        {
            if (!showPanel) return;

            float w = 220f;
            float h = 310f;
            float x = 10f;
            float y = 10f;
            float btnH = 30f;
            float pad = 5f;

            GUI.Box(new Rect(x, y, w, h), "Screen Effects (Alt+1)");
            float cy = y + 25f;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, btnH), "Fade To Black"))
                OnFadeToBlack();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, btnH), "Fade From Black"))
                OnFadeFromBlack();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, btnH), "Screen Shake"))
                OnScreenShake();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, btnH), "Show Letterbox"))
                OnShowLetterbox();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, btnH), "Hide Letterbox"))
                OnHideLetterbox();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, btnH), "Show Objective"))
                OnShowObjective();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, btnH), "Mission Passed"))
                OnMissionPassed();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + pad, cy, w - pad * 2, btnH), "Reset All"))
                OnResetAll();
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
