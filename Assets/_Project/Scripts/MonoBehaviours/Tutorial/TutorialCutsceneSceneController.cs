using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    /// <summary>
    /// Displays a placeholder cutscene with a title and body text,
    /// then auto-advances to the next tutorial scene after a delay.
    /// </summary>
    public sealed class TutorialCutsceneSceneController : MonoBehaviour
    {
        private string _title;
        private string _body;
        private float _autoAdvanceDelay;
        private float _advanceAt = -1f;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private bool _stylesReady;

        public void Configure(string title, string body, float autoAdvanceDelay)
        {
            _title = title;
            _body = body;
            _autoAdvanceDelay = autoAdvanceDelay;
        }

        private void Start()
        {
            if (_autoAdvanceDelay > 0f)
                _advanceAt = Time.time + _autoAdvanceDelay;
        }

        private void Update()
        {
            if (_advanceAt < 0f || Time.time < _advanceAt)
                return;

            _advanceAt = -1f;
            TutorialFlowController.Instance?.CompleteCurrentSceneAndLoadNext();
        }

        private void OnGUI()
        {
            BuildStyles();

            GUI.color = new Color(0.04f, 0.05f, 0.04f, 0.85f);
            GUI.DrawTexture(new Rect(18f, 120f, 400f, 100f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(_title))
                GUI.Label(new Rect(32f, 128f, 372f, 28f), _title, _titleStyle);

            if (!string.IsNullOrEmpty(_body))
                GUI.Label(new Rect(32f, 158f, 372f, 52f), _body, _bodyStyle);
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _titleStyle.normal.textColor = Color.white;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _stylesReady = true;
        }
    }
}
