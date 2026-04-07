using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Debugging
{
    /// <summary>
    /// Simple debug panel system. Each panel has a Shift+key toggle.
    /// Only one panel open at a time. Number keys route to the active panel only.
    ///
    /// Shift+1 = Screen Effects     Shift+2 = Audio Manager
    /// Shift+3 = Dialogue System    Shift+4 = Cinematic Camera
    /// Shift+N = NPC Controller     Shift+M = Mission Manager
    /// </summary>
    public static class DebugPanelShortcuts
    {
        // ── Panel Keys (the key pressed WITH Shift to toggle) ────
        public static readonly Key ScreenEffects   = Key.Digit1;
        public static readonly Key AudioManager    = Key.Digit2;
        public static readonly Key Dialogue        = Key.Digit3;
        public static readonly Key CinematicCamera = Key.Digit4;
        public static readonly Key NPCController   = Key.N;
        public static readonly Key MissionManager  = Key.M;

        // ── State ────────────────────────────────────────────────
        private static Key activePanel = Key.None;

        /// <summary>
        /// Call in each demo's Update. Returns true if this panel is now active.
        /// Handles toggle logic: Shift+key opens this panel (closing any other).
        /// Same combo again closes it.
        /// </summary>
        public static bool UpdateToggle(Key panelKey)
        {
            var kb = Keyboard.current;
            if (kb == null) return activePanel == panelKey;

            if (kb.leftShiftKey.isPressed && kb[panelKey].wasPressedThisFrame)
            {
                activePanel = (activePanel == panelKey) ? Key.None : panelKey;
            }

            return activePanel == panelKey;
        }

        /// <summary>
        /// Returns true if this panel is currently active.
        /// </summary>
        public static bool IsPanelActive(Key panelKey) => activePanel == panelKey;

        /// <summary>
        /// Returns true when the action digit key is pressed and this panel
        /// is the active one and Shift is NOT held. No collision possible.
        /// </summary>
        public static bool WasActionPressed(Key panelKey, Key actionKey)
        {
            if (activePanel != panelKey) return false;
            var kb = Keyboard.current;
            if (kb == null) return false;
            return !kb.leftShiftKey.isPressed && kb[actionKey].wasPressedThisFrame;
        }
    }
}
