using FarmSimVR.Core.Farming;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed class FarmWeatherDebugShortcuts : MonoBehaviour
    {
        public const string SunnyShortcutLabel = "Shift+Y";
        public const string CloudyShortcutLabel = "Shift+U";
        public const string RainShortcutLabel = "Shift+I";
        public const string AutoShortcutLabel = "Shift+O";
        public const string ShortcutSummary =
            SunnyShortcutLabel + " Sun  " +
            CloudyShortcutLabel + " Cloud  " +
            RainShortcutLabel + " Rain  " +
            AutoShortcutLabel + " Auto";

        [SerializeField] private bool showOverlay = true;

        private FarmWeatherDebugController _controller;
        private string _message;
        private float _messageUntil;
        private GUIStyle _overlayStyle;
        private GUIStyle _messageStyle;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !TryResolveController())
                return;

            if (IsShiftPressed(keyboard) && keyboard.yKey.wasPressedThisFrame)
                SetMessage(_controller.Apply(FarmWeatherDebugCommand.ForceSunny));
            else if (IsShiftPressed(keyboard) && keyboard.uKey.wasPressedThisFrame)
                SetMessage(_controller.Apply(FarmWeatherDebugCommand.ForceCloudy));
            else if (IsShiftPressed(keyboard) && keyboard.iKey.wasPressedThisFrame)
                SetMessage(_controller.Apply(FarmWeatherDebugCommand.ForceRain));
            else if (IsShiftPressed(keyboard) && keyboard.oKey.wasPressedThisFrame)
                SetMessage(_controller.Apply(FarmWeatherDebugCommand.AutoWeather));
        }

        private bool TryResolveController()
        {
            if (FarmWeatherDriver.Instance?.Provider == null)
                return false;

            if (_controller == null)
                _controller = new FarmWeatherDebugController(FarmWeatherDriver.Instance.Provider);

            return true;
        }

        private void SetMessage(string message)
        {
            _message = message;
            _messageUntil = Time.time + 2.5f;
        }

        private void OnGUI()
        {
            if (!showOverlay && (string.IsNullOrEmpty(_message) || Time.time > _messageUntil))
                return;

            BuildStyles();
            DrawOverlay();
            DrawMessage();
        }

        private void BuildStyles()
        {
            if (_overlayStyle != null)
                return;

            _overlayStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperRight
            };
            _overlayStyle.normal.textColor = new Color(0.92f, 0.95f, 1f);

            _messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            _messageStyle.normal.textColor = Color.white;
        }

        private void DrawOverlay()
        {
            if (!showOverlay || FarmWeatherDriver.Instance?.Provider == null)
                return;

            var provider = FarmWeatherDriver.Instance.Provider;
            var mode = provider.IsForced ? "[forced]" : "[auto]";
            var label = $"Weather {provider.Current} {mode}\n{ShortcutSummary}";

            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(Screen.width - 380f, 18f, 360f, 48f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width - 370f, 24f, 340f, 40f), label, _overlayStyle);
        }

        private void DrawMessage()
        {
            if (string.IsNullOrEmpty(_message) || Time.time > _messageUntil)
                return;

            var width = 360f;
            var x = (Screen.width - width) * 0.5f;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(x, 68f, width, 34f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 12f, 74f, width - 24f, 22f), _message, _messageStyle);
        }

        private static bool IsShiftPressed(Keyboard keyboard)
        {
            return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }
    }
}
