using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed partial class FarmSimDriver
    {
        private GUIStyle _h1;
        private GUIStyle _body;
        private GUIStyle _dim;
        private GUIStyle _btn;
        private GUIStyle _btnOn;
        private GUIStyle _logStyle;
        private bool _stylesBuilt;

        private void OnGUI()
        {
            if (!showLegacyDebugPanel || _soil == null)
                return;

            BuildStyles();

            const float panelWidth = 290f;
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.94f);
            GUI.DrawTexture(new Rect(0, 0, panelWidth, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(10, 8, panelWidth - 16, Screen.height - 16));
            GUILayout.Label("FARM TESTBED", _h1);

            DrawTimeSummary();
            DrawWeatherControls();
            DrawSeasonControls();
            DrawPlotSelection();
            DrawSelectedPlotPanel();
            DrawInventory();

            HR();
            GUILayout.Label(_log, _logStyle);
            GUILayout.EndArea();
        }

        private void DrawTimeSummary()
        {
            if (FarmDayClockDriver.Instance == null)
                return;

            var clock = FarmDayClockDriver.Instance.Clock;
            GUILayout.Label(
                $"Day {clock.DayCount + 1}  {clock.Phase}  {clock.NormalisedTime * 24f:F1}h",
                _dim);
        }

        private void DrawWeatherControls()
        {
            if (FarmWeatherDriver.Instance == null)
                return;

            var provider = FarmWeatherDriver.Instance.Provider;
            var timer = provider.IsForced ? " [forced]" : $"  {provider.SecondsRemaining:F0}s left";
            GUILayout.Label($"Weather  {provider.Current}{timer}", _dim);
            GUILayout.BeginHorizontal();
            foreach (WeatherType weather in System.Enum.GetValues(typeof(WeatherType)))
            {
                var active = provider.IsForced && provider.Current == weather;
                if (!GUILayout.Button(weather.ToString(), active ? _btnOn : _btn))
                    continue;

                provider.Force(weather);
                FindAnyObjectByType<FarmLightingController>()?.ApplyWeather(weather);
                _log = $"Weather forced: {weather}";
            }

            if (GUILayout.Button("Auto", !provider.IsForced ? _btnOn : _btn))
            {
                provider.ReleaseForce();
                _log = "Weather set to auto";
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSeasonControls()
        {
            if (FarmSeasonDriver.Instance == null)
                return;

            var provider = FarmSeasonDriver.Instance.Provider;
            GUILayout.Label(
                $"Season  {provider.Current}  (Day {provider.DayOfSeason + 1}/{provider.DaysPerSeason})",
                _dim);
            GUILayout.BeginHorizontal();
            foreach (FarmSeason season in System.Enum.GetValues(typeof(FarmSeason)))
            {
                var active = provider.Current == season;
                if (!GUILayout.Button(season.ToString(), active ? _btnOn : _btn))
                    continue;

                provider.Force(season);
                _log = $"Season forced: {season}";
            }
            GUILayout.EndHorizontal();
        }

        private void DrawPlotSelection()
        {
            HR();
            GUILayout.Label("SELECT PLOT", _dim);
            for (var i = 0; i < _soil.AllPlots.Count; i++)
            {
                var soil = _soil.AllPlots[i];
                var label = $"{soil.PlotId.Replace("CropPlot_", "Plot ")}  [{soil.Status}]";
                if (GUILayout.Button(label, i == _plot ? _btnOn : _btn))
                    _plot = i;
            }
        }

        private void DrawSelectedPlotPanel()
        {
            HR();
            if (_plot >= _soil.AllPlots.Count)
                return;

            var soil = _soil.AllPlots[_plot];
            var crop = _sim.Plots[_plot];

            GUILayout.Label(soil.Status == PlotStatus.Depleted ? "DEPLETED ⚠" : soil.Status.ToString(), _h1);
            GUILayout.Label($"Growth    {Bar(crop.GrowthPercent)}  {crop.GrowthPercent:P0}", _body);
            GUILayout.Label($"Moisture  {Bar(soil.Moisture)}  {soil.Moisture:F2}", _body);
            GUILayout.Label($"Nutrients {Bar(soil.Nutrients)}  {soil.Nutrients:F2}", _body);
            GUILayout.Label(BuildPlotSummary(soil), _dim);

            DrawSeedSelection();
            DrawActionButtons(soil, crop);
        }

        private string BuildPlotSummary(SoilState soil)
        {
            return soil.CurrentCropId != null
                ? $"{soil.CurrentCropId.Replace("seed_", string.Empty)}  ×{soil.GrowthMultiplier:F1} growth"
                : $"{soil.Type} soil  ×{soil.GrowthMultiplier:F1} growth";
        }

        private void DrawSeedSelection()
        {
            HR();
            GUILayout.Label("SEED", _dim);
            GUILayout.BeginHorizontal();
            for (var i = 0; i < SeedIds.Length; i++)
            {
                var suitability = FarmSeasonDriver.Instance != null
                    ? CropSeasonSuitability.Label(SeedIds[i], FarmSeasonDriver.Instance.Provider.Current)
                    : string.Empty;
                if (GUILayout.Button($"{SeedLabels[i]}\nx{_inv.GetCount(SeedIds[i])}  {suitability}", i == _seed ? _btnOn : _btn))
                    _seed = i;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        private void DrawActionButtons(SoilState soil, CropPlotState crop)
        {
            GUILayout.Label("ACTIONS", _dim);
            var canPlant = soil.Status == PlotStatus.Empty &&
                           _inv.HasItem(SeedIds[_seed]) &&
                           CanPlantCurrentSeed();
            GUI.enabled = canPlant;
            if (GUILayout.Button($"Plant {SeedLabels[_seed]}", _btn))
                TryPlant(_plot, _seed, out _log);
            GUI.enabled = true;

            if (GUILayout.Button("Water", _btn))
                TryWater(_plot, out _log);

            GUI.enabled = soil.Status == PlotStatus.Harvestable && crop.Phase == PlotPhase.Ready;
            if (GUILayout.Button("Harvest", _btn))
                TryHarvest(_plot, out _log);
            GUI.enabled = true;

            if (GUILayout.Button("Compost (+nutrients)", _btn))
                TryCompost(_plot, out _log);

            GUILayout.Space(4);
            if (GUILayout.Button($"Fast-forward +{fastForwardSeconds:F0}s", _btn))
                DoFastForward();

            HR();
        }

        private bool CanPlantCurrentSeed()
        {
            return FarmSeasonDriver.Instance == null ||
                   CropSeasonSuitability.CanPlant(SeedIds[_seed], FarmSeasonDriver.Instance.Provider.Current);
        }

        private void DrawInventory()
        {
            GUILayout.Label("INVENTORY", _dim);
            for (var i = 0; i < SeedIds.Length; i++)
            {
                GUILayout.Label(
                    $"  {SeedLabels[i],-7}  ×{_inv.GetCount(SeedIds[i])} seeds   ×{_inv.GetCount(CropIds[i])} crops",
                    _body);
            }
        }

        private static void HR()
        {
            GUILayout.Space(4);
            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUI.color = previous;
            GUILayout.Space(4);
        }

        private static string Bar(float value, int width = 9)
        {
            var fill = Mathf.RoundToInt(Mathf.Clamp01(value) * width);
            return "[" + new string('█', fill) + new string('░', width - fill) + "]";
        }

        private void BuildStyles()
        {
            if (_stylesBuilt)
                return;

            _h1 = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };
            _h1.normal.textColor = Color.white;

            _body = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            _body.normal.textColor = new Color(0.85f, 0.95f, 0.85f);

            _dim = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            _dim.normal.textColor = new Color(0.55f, 0.65f, 0.55f);

            _btn = new GUIStyle(GUI.skin.button) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
            _btnOn = new GUIStyle(_btn);
            _btnOn.normal.textColor = new Color(0.2f, 1f, 0.4f);
            _btnOn.fontStyle = FontStyle.Bold;

            _logStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            _logStyle.normal.textColor = new Color(1f, 0.9f, 0.5f);

            _stylesBuilt = true;
        }
    }
}
