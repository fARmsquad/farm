using FarmSimVR.Core.Farming;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed class WorldFarmDevShortcuts : MonoBehaviour
    {
        public const string SaveShortcutLabel = "Shift+P";
        public const string LoadShortcutLabel = "Shift+L";

        [SerializeField] private WorldFarmProgressionController progression;

        private string _statusMessage = string.Empty;
        private float _statusUntil;

        public string StatusMessage => Time.unscaledTime <= _statusUntil ? _statusMessage : string.Empty;

        private void Update()
        {
            if (!TryResolveProgression(out var controller))
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var shift = IsShiftPressed(keyboard);

            if (shift && keyboard.pKey.wasPressedThisFrame)
                SetStatus(controller.SaveNow() ? "Saved farming progression." : controller.StatusMessage);
            else if (shift && keyboard.lKey.wasPressedThisFrame)
                SetStatus(controller.LoadNow() ? "Loaded farming progression." : controller.StatusMessage);
            else if (!shift && keyboard.kKey.wasPressedThisFrame)
                SetStatus(controller.TrySellHarvested(out var message) ? message : message);
            else if (!shift && keyboard.uKey.wasPressedThisFrame)
                SetStatus(controller.TryBuyWateringUpgrade(out var message) ? message : message);
            else if (!shift && keyboard.oKey.wasPressedThisFrame)
                SetStatus(controller.TryUnlockNextExpansion(out var message) ? message : message);
            else if (!shift && keyboard.leftBracketKey.wasPressedThisFrame)
                GrantDebugCoins(controller);
            else if (!shift && keyboard.rightBracketKey.wasPressedThisFrame)
                GrantDebugExperience(controller);
            else if (!shift && keyboard.digit1Key.wasPressedThisFrame)
                SetStatus(controller.TrySpendSkill(FarmSkillType.GreenThumb, out var message) ? message : message);
            else if (!shift && keyboard.digit2Key.wasPressedThisFrame)
                SetStatus(controller.TrySpendSkill(FarmSkillType.Merchant, out var message) ? message : message);
            else if (!shift && keyboard.digit3Key.wasPressedThisFrame)
                SetStatus(controller.TrySpendSkill(FarmSkillType.RainTender, out var message) ? message : message);
        }

        private void GrantDebugCoins(WorldFarmProgressionController controller)
        {
            controller.GrantDebugCoins(100);
            SetStatus("+100 coins");
        }

        private void GrantDebugExperience(WorldFarmProgressionController controller)
        {
            controller.GrantDebugExperience(100);
            SetStatus("+100 XP");
        }

        private bool TryResolveProgression(out WorldFarmProgressionController controller)
        {
            if (progression == null)
                progression = GetComponent<WorldFarmProgressionController>() ?? FindAnyObjectByType<WorldFarmProgressionController>();

            controller = progression;
            return controller != null;
        }

        private void SetStatus(string message)
        {
            _statusMessage = message ?? string.Empty;
            _statusUntil = Time.unscaledTime + 3f;
        }

        private static bool IsShiftPressed(Keyboard keyboard)
        {
            return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }
    }
}
