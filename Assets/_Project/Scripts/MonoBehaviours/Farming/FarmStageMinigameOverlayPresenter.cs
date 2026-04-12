using FarmSimVR.Core.Farming;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    internal sealed class FarmStageMinigameOverlayPresenter
    {
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _sequenceStyle;
        private bool _stylesReady;

        public void Draw(FarmStageMinigameSession session)
        {
            if (session == null)
                return;

            BuildStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            const float width = 620f;
            const float height = 320f;
            var x = (Screen.width - width) * 0.5f;
            var y = (Screen.height - height) * 0.5f;

            GUI.color = new Color(0.06f, 0.07f, 0.05f, 0.97f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = new Color(0.38f, 0.72f, 0.28f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, 4f, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 24f, y + 22f, width - 48f, 28f), session.Definition.Title, _titleStyle);
            GUI.Label(new Rect(x + 24f, y + 56f, width - 48f, 44f), session.Definition.ThemeText, _bodyStyle);

            DrawMinigameBody(new Rect(x + 24f, y + 112f, width - 48f, 128f), session);

            GUI.Label(new Rect(x + 24f, y + 250f, width - 48f, 22f), session.StatusText, _statusStyle);
            GUI.Label(new Rect(x + 24f, y + 280f, width - 48f, 20f), BuildHint(session.Definition.Type), _hintStyle);
        }

        private void DrawMinigameBody(Rect rect, FarmStageMinigameSession session)
        {
            switch (session.Definition.Type)
            {
                case FarmStageMinigameType.StopZone:
                    DrawStopZone(rect, session);
                    break;
                case FarmStageMinigameType.Sequence:
                    DrawSequence(rect, session);
                    break;
                case FarmStageMinigameType.RapidTap:
                case FarmStageMinigameType.Alternate:
                    DrawProgress(rect, session);
                    break;
            }
        }

        private static void DrawStopZone(Rect rect, FarmStageMinigameSession session)
        {
            var track = new Rect(rect.x, rect.y + 42f, rect.width, 18f);
            GUI.color = new Color(0.2f, 0.22f, 0.18f, 1f);
            GUI.DrawTexture(track, Texture2D.whiteTexture);

            var successX = track.x + track.width * session.Definition.SuccessMin;
            var successWidth = track.width * (session.Definition.SuccessMax - session.Definition.SuccessMin);
            GUI.color = new Color(0.32f, 0.7f, 0.28f, 1f);
            GUI.DrawTexture(new Rect(successX, track.y, successWidth, track.height), Texture2D.whiteTexture);

            var markerX = track.x + track.width * session.MarkerPosition - 4f;
            GUI.color = new Color(1f, 0.9f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(markerX, track.y - 8f, 8f, track.height + 16f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x, rect.y + 78f, rect.width, 22f), "Press Space or Enter in the green band.", GUI.skin.label);
        }

        private void DrawProgress(Rect rect, FarmStageMinigameSession session)
        {
            var fill = new Rect(rect.x, rect.y + 30f, rect.width * Mathf.Clamp01(session.Progress), 26f);
            GUI.color = new Color(0.2f, 0.22f, 0.18f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y + 30f, rect.width, 26f), Texture2D.whiteTexture);
            GUI.color = session.Definition.Type == FarmStageMinigameType.Alternate
                ? new Color(0.95f, 0.72f, 0.18f, 1f)
                : new Color(0.38f, 0.72f, 0.28f, 1f);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var hint = session.Definition.Type == FarmStageMinigameType.Alternate
                ? $"Alternate {FormatInput(session.NextAlternateInput)} and {FormatInput(Toggle(session.NextAlternateInput))}."
                : "Tap Space or Enter repeatedly before the bar drains.";
            GUI.Label(new Rect(rect.x, rect.y + 72f, rect.width, 22f), hint, _bodyStyle);
        }

        private void DrawSequence(Rect rect, FarmStageMinigameSession session)
        {
            const float boxWidth = 74f;
            const float boxHeight = 52f;
            const float gap = 12f;
            var totalWidth = session.Definition.InputSequence.Count * boxWidth + (session.Definition.InputSequence.Count - 1) * gap;
            var startX = rect.x + (rect.width - totalWidth) * 0.5f;

            for (var i = 0; i < session.Definition.InputSequence.Count; i++)
            {
                var box = new Rect(startX + i * (boxWidth + gap), rect.y + 12f, boxWidth, boxHeight);
                var completed = i < session.SequenceIndex;
                GUI.color = completed
                    ? new Color(0.38f, 0.72f, 0.28f, 1f)
                    : new Color(0.2f, 0.22f, 0.18f, 1f);
                GUI.DrawTexture(box, Texture2D.whiteTexture);
                GUI.color = completed ? new Color(0.08f, 0.1f, 0.05f, 1f) : Color.white;
                GUI.Label(box, FormatInput(session.Definition.InputSequence[i]), _sequenceStyle);
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x, rect.y + 82f, rect.width, 22f), "Hit the arrow pattern in order.", _bodyStyle);
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _titleStyle.normal.textColor = new Color(0.96f, 0.97f, 0.93f);

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = new Color(0.9f, 0.93f, 0.87f);

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            _statusStyle.normal.textColor = new Color(0.97f, 0.88f, 0.44f);

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            _hintStyle.normal.textColor = new Color(0.8f, 0.85f, 0.78f);

            _sequenceStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesReady = true;
        }

        private static string BuildHint(FarmStageMinigameType type)
        {
            return type switch
            {
                FarmStageMinigameType.Sequence => "Arrow keys to perform the pattern. Esc cancels.",
                FarmStageMinigameType.Alternate => "Left and Right alternate the motion. Esc cancels.",
                _ => "Space or Enter performs the task. Esc cancels.",
            };
        }

        private static FarmStageMinigameInput Toggle(FarmStageMinigameInput input)
        {
            return input == FarmStageMinigameInput.Left ? FarmStageMinigameInput.Right : FarmStageMinigameInput.Left;
        }

        private static string FormatInput(FarmStageMinigameInput input)
        {
            return input switch
            {
                FarmStageMinigameInput.Left => "LEFT",
                FarmStageMinigameInput.Right => "RIGHT",
                FarmStageMinigameInput.Up => "UP",
                FarmStageMinigameInput.Down => "DOWN",
                _ => "SPACE",
            };
        }
    }
}
