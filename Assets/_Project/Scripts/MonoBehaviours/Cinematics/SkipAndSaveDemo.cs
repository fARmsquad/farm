using UnityEngine;
using UnityEngine.InputSystem;

using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Debug overlay for testing SkipPrompt and AutoSave.
    /// Toggle with Shift+K (sKip). Panel appears top-center.
    /// </summary>
    public class SkipAndSaveDemo : MonoBehaviour
    {
        private SkipPrompt skipPrompt;
        private static readonly Key Panel = Key.K;

        private void Start()
        {
            skipPrompt = FindAnyObjectByType<SkipPrompt>();

            if (skipPrompt == null)
            {
                var go = new GameObject("[SkipPrompt]");
                skipPrompt = go.AddComponent<SkipPrompt>();
                Debug.Log("[SkipAndSaveDemo] Created SkipPrompt (none found in scene).");
            }
            else
            {
                Debug.Log("[SkipAndSaveDemo] Found existing SkipPrompt.");
            }
        }

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1))
            {
                Debug.Log("[SkipAndSaveDemo] Activate Skip Prompt");
                skipPrompt?.Activate();
            }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2))
            {
                Debug.Log("[SkipAndSaveDemo] Deactivate Skip Prompt");
                skipPrompt?.Deactivate();
            }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3))
            {
                Debug.Log("[SkipAndSaveDemo] Save Intro Complete");
                AutoSave.SaveIntroComplete();
            }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4))
            {
                bool completed = AutoSave.HasCompletedIntro();
                Debug.Log($"[SkipAndSaveDemo] HasCompletedIntro = {completed}");
            }
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5))
            {
                Debug.Log("[SkipAndSaveDemo] Clear Save");
                AutoSave.ClearSave();
            }
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 300f;
            float h = 260f;
            float x = (Screen.width - w) * 0.5f;
            float y = 10f;
            float btnH = 28f;
            float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Skip & Save (Shift+K)");
            float cy = y + 22f;

            // ── Status ───────────────────────────────────────
            bool hasCompleted = AutoSave.HasCompletedIntro();
            bool isActive = false;

            if (skipPrompt != null)
            {
                isActive = skipPrompt.IsActive;
                // holdProgress is private; reflect the visual via the prompt itself
            }

            GUI.Label(new Rect(x + 4, cy, w - 8, 20f),
                $"introComplete: {hasCompleted}  |  skipActive: {isActive}");
            cy += 24f;

            // ── Buttons ──────────────────────────────────────
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[1] Activate Skip Prompt"))
                skipPrompt?.Activate();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[2] Deactivate Skip Prompt"))
                skipPrompt?.Deactivate();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[3] Save Intro Complete"))
                AutoSave.SaveIntroComplete();
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[4] Check Save Status"))
            {
                bool completed = AutoSave.HasCompletedIntro();
                Debug.Log($"[SkipAndSaveDemo] HasCompletedIntro = {completed}");
            }
            cy += btnH + pad;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[5] Clear Save"))
                AutoSave.ClearSave();
        }
    }
}
