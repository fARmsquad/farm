using FarmSimVR.Core.Farming;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed class WorldFarmReferenceOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;

        private ZoneTracker _zoneTracker;
        private WorldFarmProgressionController _progression;
        private WorldFarmDevShortcuts _shortcuts;
        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _accent;
        private GUIStyle _message;

        private void Update()
        {
            if (_zoneTracker == null)
                _zoneTracker = FindAnyObjectByType<ZoneTracker>();

            if (_progression == null)
                _progression = GetComponent<WorldFarmProgressionController>() ?? FindAnyObjectByType<WorldFarmProgressionController>();

            if (_shortcuts == null)
                _shortcuts = GetComponent<WorldFarmDevShortcuts>() ?? FindAnyObjectByType<WorldFarmDevShortcuts>();
        }

        private void OnGUI()
        {
            if (!showOverlay)
                return;

            BuildStyles();
            DrawReferencePanel();
            DrawStatusMessage();
        }

        private void DrawReferencePanel()
        {
            var rect = new Rect(20f, 20f, 420f, 320f);
            GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.88f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, rect.height - 24f));
            GUILayout.Label("World Farming Slice", _title);
            GUILayout.Label(BuildStateLine(), _body);
            GUILayout.Label(BuildProgressionLine(), _body);
            GUILayout.Label(BuildModifierLine(), _body);
            GUILayout.Space(6f);
            GUILayout.Label("Context", _accent);
            GUILayout.Label(BuildZoneGuidance(), _body);
            GUILayout.Space(6f);
            GUILayout.Label("Shortcuts", _accent);
            GUILayout.Label($"{FarmWeatherDebugShortcuts.ShortcutSummary}  |  {WorldFarmDevShortcuts.SaveShortcutLabel} save  |  {WorldFarmDevShortcuts.LoadShortcutLabel} load", _body);
            GUILayout.Label("K sell  |  U upgrade can  |  O expansion  |  [ +100 coins  |  ] +100 XP", _body);
            GUILayout.Label("1 Green Thumb  |  2 Merchant  |  3 Rain Tender", _body);
            GUILayout.EndArea();
        }

        private void DrawStatusMessage()
        {
            var status = !string.IsNullOrEmpty(_shortcuts?.StatusMessage)
                ? _shortcuts.StatusMessage
                : _progression?.StatusMessage;

            if (string.IsNullOrEmpty(status))
                return;

            var width = 440f;
            var x = (Screen.width - width) * 0.5f;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(x, 20f, width, 32f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 12f, 24f, width - 24f, 24f), status, _message);
        }

        private string BuildStateLine()
        {
            var zone = string.IsNullOrEmpty(_zoneTracker?.CurrentZone) ? "Approach a zone" : _zoneTracker.CurrentZone;
            var weather = FarmWeatherDriver.Instance?.Provider.Current.ToString() ?? "Unknown";
            var phase = FarmDayClockDriver.Instance?.Clock.Phase.ToString() ?? "Unknown";
            var season = FarmSeasonDriver.Instance?.Provider.Current.ToString() ?? "Pending";
            return $"Zone: {zone}  |  Weather: {weather}  |  Day: {phase}  |  Season: {season}";
        }

        private string BuildProgressionLine()
        {
            if (_progression == null)
                return "Progression loading...";

            var state = _progression.Service.State;
            return $"Coins: {state.Coins}  |  Level: {state.Level}  |  XP: {state.Experience}  |  Skill Points: {state.SkillPoints}";
        }

        private string BuildModifierLine()
        {
            if (_progression == null)
                return "Modifiers unavailable.";

            var state = _progression.Service.State;
            return $"Watering Tier: {state.WateringCanTier}  |  Growth x{_progression.GrowthMultiplier:F2}  |  Rain x{_progression.RainMultiplier:F2}  |  Sell x{_progression.Service.GetSaleMultiplier():F2}  |  Expansion: {_progression.CurrentExpansionLabel}";
        }

        private string BuildZoneGuidance()
        {
            var zone = _zoneTracker?.CurrentZone ?? string.Empty;
            if (zone == "Farm Plots")
                return "Look at a plot to work it directly. T tomato, C carrot, L lettuce, P water, H harvest, M compost. Rain waters outdoor plots automatically and your Rain Tender skill boosts it.";

            if (zone == "Farm House")
                return "The house is the planning hub. Sell crops here with K, upgrade your watering can with U, unlock the next expansion hook with O, then spend points on 1/2/3.";

            if (zone == "Chicken Coop")
                return "The coop zone now doubles as the animal pen game. Walk in, press G to start, catch with E, and use the right-side pen overlay plus Shift+J and Shift+H to validate animal-handling progression.";

            return "Move between the plots, house, and coop to see how one compact world slice carries farming, progression, weather, and atmosphere.";
        }

        private void BuildStyles()
        {
            if (_title != null)
                return;

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            _title.normal.textColor = Color.white;

            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true
            };
            _body.normal.textColor = new Color(0.86f, 0.93f, 0.86f);

            _accent = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            _accent.normal.textColor = new Color(0.98f, 0.88f, 0.62f);

            _message = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            _message.normal.textColor = Color.white;
        }
    }
}
