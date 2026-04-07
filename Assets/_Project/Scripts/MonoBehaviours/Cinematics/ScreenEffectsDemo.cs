using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public class ScreenEffectsDemo : MonoBehaviour
    {
        [SerializeField] public ScreenEffects screenEffects;
        private int objectiveCount = 1;
        private static readonly Key Panel = DebugPanelShortcuts.ScreenEffects;

        private void Start()
        {
            if (screenEffects == null)
                screenEffects = FindAnyObjectByType<ScreenEffects>();
            Debug.Log($"[ScreenEffectsDemo] Start — screenEffects={(screenEffects != null ? "found" : "NULL")}");
        }

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) { Debug.Log("[SFX] Fade To Black"); screenEffects?.FadeToBlack(1f); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) { Debug.Log("[SFX] Fade From Black"); screenEffects?.FadeFromBlack(1f); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) { Debug.Log("[SFX] Screen Shake"); screenEffects?.ScreenShake(0.5f, 0.5f); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) { Debug.Log("[SFX] Show Letterbox"); screenEffects?.ShowLetterbox(1f, 0.5f); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5)) { Debug.Log("[SFX] Hide Letterbox"); screenEffects?.HideLetterbox(0.5f); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit6)) { Debug.Log("[SFX] Show Objective"); screenEffects?.ShowObjective($"OBJECTIVE #{objectiveCount++}"); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit7)) { Debug.Log("[SFX] Mission Passed"); screenEffects?.ShowMissionPassed("MISSION PASSED"); }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit8)) { Debug.Log("[SFX] Reset All"); screenEffects?.ResetAll(); }
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 260f; float h = 300f; float x = 10f; float y = 10f;
            float btnH = 28f; float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Screen Effects (Shift+1)");
            float cy = y + 22f;

            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[1] Fade To Black")) { screenEffects?.FadeToBlack(1f); }       cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[2] Fade From Black")) { screenEffects?.FadeFromBlack(1f); }    cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[3] Screen Shake")) { screenEffects?.ScreenShake(0.5f, 0.5f); } cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[4] Show Letterbox")) { screenEffects?.ShowLetterbox(1f, 0.5f); } cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[5] Hide Letterbox")) { screenEffects?.HideLetterbox(0.5f); }   cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[6] Show Objective")) { screenEffects?.ShowObjective($"OBJECTIVE #{objectiveCount++}"); } cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[7] Mission Passed")) { screenEffects?.ShowMissionPassed("MISSION PASSED"); } cy += btnH+pad;
            if (GUI.Button(new Rect(x+4, cy, w-8, btnH), "[8] Reset All")) { screenEffects?.ResetAll(); }
        }
    }
}
