using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Displays the current in-game time and day number at the top-center
    /// of the screen using OnGUI. Reads from FarmDayClockDriver.Instance.
    /// </summary>
    public sealed class DayNightHUD : MonoBehaviour
    {
        private const float BOX_WIDTH = 200f;
        private const float BOX_HEIGHT = 50f;
        private const float BOX_Y = 10f;
        private const int TIME_FONT_SIZE = 18;
        private const int DAY_FONT_SIZE = 13;
        private const float HOURS_IN_DAY = 24f;
        private const float MIDNIGHT_HOUR = 0f;

        private GUIStyle _timeStyle;
        private GUIStyle _dayStyle;
        private GUIStyle _boxStyle;
        private bool _stylesInitialised;

        private void InitStyles()
        {
            if (_stylesInitialised) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.45f)) }
            };

            _timeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = TIME_FONT_SIZE,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _dayStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = DAY_FONT_SIZE,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            _stylesInitialised = true;
        }

        private void OnGUI()
        {
            if (FarmDayClockDriver.Instance == null) return;

            InitStyles();

            var clock = FarmDayClockDriver.Instance.Clock;
            float normTime = clock.NormalisedTime;

            // Convert normalised time to 24h clock (0.0 = midnight = 00:00)
            float totalHours = normTime * HOURS_IN_DAY;
            int hours = Mathf.FloorToInt(totalHours) % 24;
            int minutes = Mathf.FloorToInt((totalHours - Mathf.Floor(totalHours)) * 60f);

            string timeStr = $"{hours:D2}:{minutes:D2}";
            string periodLabel = GetPeriodLabel(clock.Phase);
            string dayStr = $"Day {clock.DayCount + 1} - {periodLabel}";

            float boxX = (Screen.width - BOX_WIDTH) * 0.5f;
            Rect boxRect = new Rect(boxX, BOX_Y, BOX_WIDTH, BOX_HEIGHT);

            GUI.Box(boxRect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(boxX, BOX_Y + 2f, BOX_WIDTH, 26f), timeStr, _timeStyle);
            GUI.Label(new Rect(boxX, BOX_Y + 24f, BOX_WIDTH, 22f), dayStr, _dayStyle);
        }

        /// <summary>Returns a human-readable label for the current phase.</summary>
        private static string GetPeriodLabel(DayPhase phase)
        {
            return phase switch
            {
                DayPhase.Dawn      => "Dawn",
                DayPhase.Morning   => "Morning",
                DayPhase.Noon      => "Noon",
                DayPhase.Afternoon => "Afternoon",
                DayPhase.Dusk      => "Dusk",
                DayPhase.Night     => "Night",
                _                  => "",
            };
        }

        /// <summary>Creates a solid-colour texture for GUI backgrounds.</summary>
        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = col;

            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
