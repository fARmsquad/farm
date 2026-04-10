using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public sealed class WorldPenDevShortcuts : MonoBehaviour
    {
        public const string ExperienceShortcutLabel = "Shift+J";
        public const string SkillShortcutLabel = "Shift+H";

        [SerializeField] private WorldPenGameController gameController;
        [SerializeField] private WorldPenProgressionController progressionController;

        private string _statusMessage = string.Empty;
        private float _statusUntil;

        public string StatusMessage => Time.unscaledTime <= _statusUntil ? _statusMessage : string.Empty;

        public void Configure(WorldPenGameController controller, WorldPenProgressionController progression)
        {
            gameController = controller;
            progressionController = progression;
        }

        private void Update()
        {
            ResolveDependencies();
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (IsShiftPressed(keyboard) && keyboard.jKey.wasPressedThisFrame)
                GrantPenExperience();
            else if (IsShiftPressed(keyboard) && keyboard.hKey.wasPressedThisFrame)
                SpendHandlingPoint();
        }

        private void GrantPenExperience()
        {
            if (progressionController == null)
            {
                SetStatus("Pen progression missing.");
                return;
            }

            progressionController.GrantDebugExperience(100);
            SetStatus("+100 pen XP");
        }

        private void SpendHandlingPoint()
        {
            if (progressionController == null)
            {
                SetStatus("Pen progression missing.");
                return;
            }

            progressionController.TrySpendHandlingPoint(out var message);
            SetStatus(message);
        }

        private void ResolveDependencies()
        {
            if (gameController == null)
                gameController = GetComponent<WorldPenGameController>() ?? FindAnyObjectByType<WorldPenGameController>();

            if (progressionController == null)
                progressionController = GetComponent<WorldPenProgressionController>() ?? FindAnyObjectByType<WorldPenProgressionController>();
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
