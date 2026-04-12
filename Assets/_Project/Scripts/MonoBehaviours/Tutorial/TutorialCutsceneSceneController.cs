using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialCutsceneSceneController : MonoBehaviour
    {
        private string _title = "Cutscene";
        private string _body = "Placeholder";
        private float _autoAdvanceDelay = 5f;
        private float _sceneStartTime;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _hintStyle;
        private bool _stylesReady;
        private bool _advanceQueued;

        public void Configure(string title, string body, float autoAdvanceDelay)
        {
            _title = title;
            _body = body;
            _autoAdvanceDelay = autoAdvanceDelay;
        }

        private void Start()
        {
            _sceneStartTime = Time.time;
            var camera = Camera.main;
            if (camera != null)
                camera.backgroundColor = new Color(0.16f, 0.22f, 0.28f, 1f);
        }

        private void Update()
        {
            if (_advanceQueued)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                QueueAdvance();
                return;
            }

            if (Time.time - _sceneStartTime >= _autoAdvanceDelay)
                QueueAdvance();
        }

        private void OnGUI()
        {
            BuildStyles();

            var width = Mathf.Min(720f, Screen.width - 80f);
            var height = 250f;
            var x = (Screen.width - width) * 0.5f;
            var y = (Screen.height - height) * 0.5f;

            GUI.color = new Color(0.03f, 0.04f, 0.05f, 0.86f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 28f, y + 26f, width - 56f, 44f), _title, _titleStyle);
            GUI.Label(new Rect(x + 28f, y + 88f, width - 56f, 96f), _body, _bodyStyle);

            var countdown = Mathf.Max(0f, _autoAdvanceDelay - (Time.time - _sceneStartTime));
            var hint = _advanceQueued
                ? "Continuing..."
                : $"Press Space or Enter to continue. Auto-advance in {countdown:0.0}s";
            GUI.Label(new Rect(x + 28f, y + height - 46f, width - 56f, 24f), hint, _hintStyle);
        }

        private void QueueAdvance()
        {
            if (_advanceQueued)
                return;

            _advanceQueued = true;
            TutorialFlowController.Instance?.CompleteCurrentSceneAndLoadNext();
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            _titleStyle.normal.textColor = Color.white;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = new Color(0.9f, 0.94f, 0.9f);

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            _hintStyle.normal.textColor = new Color(1f, 0.92f, 0.65f);
            _stylesReady = true;
        }
    }
}
