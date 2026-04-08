using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Debug overlay for testing LightingTransition and WindowCascade.
    /// Toggle with Shift+L. Number keys 1-5 trigger actions when the panel is active.
    /// </summary>
    public class LightingDemo : MonoBehaviour
    {
        private LightingTransition lightingTransition;
        private WindowCascade windowCascade;

        private LightingPreset originalPreset;
        private LightingPreset nightPreset;
        private LightingPreset dawnPreset;

        private static readonly Key Panel = Key.L;

        private void Start()
        {
            lightingTransition = FindAnyObjectByType<LightingTransition>();
            windowCascade = FindAnyObjectByType<WindowCascade>();

            // Capture original lighting so we can restore it later
            if (lightingTransition != null)
                originalPreset = lightingTransition.CaptureCurrentState();

            // Build runtime presets
            nightPreset = CreateNightPreset();
            dawnPreset = CreateDawnPreset();

            Debug.Log($"[LightingDemo] Start — transition={(lightingTransition != null ? "found" : "NULL")}, cascade={(windowCascade != null ? "found" : "NULL")}");
        }

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;
            if (lightingTransition == null) lightingTransition = FindAnyObjectByType<LightingTransition>();

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1))
            {
                Debug.Log("[Lighting] Apply Night Preset");
                lightingTransition?.ApplyPreset(nightPreset);
            }

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2))
            {
                Debug.Log("[Lighting] Apply Dawn Preset");
                lightingTransition?.ApplyPreset(dawnPreset);
            }

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3))
            {
                Debug.Log("[Lighting] Transition Night -> Dawn (5s)");
                lightingTransition?.Play(nightPreset, dawnPreset, 5f);
            }

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4))
            {
                Debug.Log("[Lighting] Trigger Window Cascade");
                if (windowCascade != null)
                    windowCascade.Trigger(Vector3.zero);
            }

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5))
            {
                Debug.Log("[Lighting] Reset Lighting");
                lightingTransition?.Stop();
                if (originalPreset != null)
                    lightingTransition?.ApplyPreset(originalPreset);
                if (windowCascade != null)
                    windowCascade.ResetAll();
            }
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 300f;
            float h = 220f;
            float x = (Screen.width - w) / 2f;
            float y = 10f;
            float btnH = 28f;
            float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Lighting Presets (Shift+L)");
            float cy = y + 22f;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[1] Apply Night Preset"))
            {
                lightingTransition?.ApplyPreset(nightPreset);
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[2] Apply Dawn Preset"))
            {
                lightingTransition?.ApplyPreset(dawnPreset);
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[3] Transition Night->Dawn (5s)"))
            {
                lightingTransition?.Play(nightPreset, dawnPreset, 5f);
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[4] Trigger Window Cascade"))
            {
                if (windowCascade != null)
                    windowCascade.Trigger(Vector3.zero);
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[5] Reset Lighting"))
            {
                lightingTransition?.Stop();
                if (originalPreset != null)
                    lightingTransition?.ApplyPreset(originalPreset);
                if (windowCascade != null)
                    windowCascade.ResetAll();
            }
        }

        #region Runtime Preset Factories

        private static LightingPreset CreateNightPreset()
        {
            var preset = ScriptableObject.CreateInstance<LightingPreset>();
            preset.name = "Night";
            preset.ambientColor = new Color(0.05f, 0.05f, 0.15f);
            preset.ambientIntensity = 0.1f;
            preset.directionalColor = new Color(0.3f, 0.3f, 0.5f);
            preset.directionalIntensity = 0.2f;
            preset.directionalRotation = new Vector3(30f, -150f, 0f);
            preset.fogColor = new Color(0.05f, 0.05f, 0.15f);
            preset.fogDensity = 0.03f;
            preset.skyboxTint = new Color(0.05f, 0.05f, 0.1f);
            return preset;
        }

        private static LightingPreset CreateDawnPreset()
        {
            var preset = ScriptableObject.CreateInstance<LightingPreset>();
            preset.name = "Dawn";
            preset.ambientColor = new Color(0.8f, 0.6f, 0.3f);
            preset.ambientIntensity = 0.6f;
            preset.directionalColor = new Color(1f, 0.95f, 0.8f);
            preset.directionalIntensity = 1.2f;
            preset.directionalRotation = new Vector3(50f, -30f, 0f);
            preset.fogColor = new Color(0.8f, 0.6f, 0.3f);
            preset.fogDensity = 0.01f;
            preset.skyboxTint = new Color(0.8f, 0.6f, 0.4f);
            return preset;
        }

        #endregion
    }
}
