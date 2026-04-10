using System.Collections.Generic;
using UnityEngine;
using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed partial class FarmSimDriver : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private bool showLegacyDebugPanel = false;

        [Header("Soil")]
        [SerializeField] private SoilType defaultSoilType = SoilType.Loam;

        [Header("Inventory")]
        [SerializeField] private int starterSeeds = 5;

        [Header("Simulation")]
        [SerializeField] private float fastForwardSeconds = 30f;

        private SoilManager _soil;
        private InventorySystem _inv;
        private IItemDatabase _db;
        private FarmSimulation _sim;
        private WorldFarmProgressionController _progression;
        private readonly List<GameObject> _plots = new();

        private static readonly string[] SeedIds = { "seed_tomato", "seed_carrot", "seed_lettuce" };
        private static readonly string[] CropIds = { "crop_tomato", "crop_carrot", "crop_lettuce" };
        private static readonly string[] SeedLabels = { "Tomato", "Carrot", "Lettuce" };
        private static readonly CropData[] Crops =
        {
            new CropData(baseGrowthRate: 0.05f, maxGrowth: 1f),
            new CropData(baseGrowthRate: 0.08f, maxGrowth: 1f),
            new CropData(baseGrowthRate: 0.10f, maxGrowth: 1f),
        };

        private int _plot;
        private int _seed;
        private string _log = "Walk to a plot and look at it to farm.";

        private void Start()
        {
            _db = ItemDatabase.CreateStarterDatabase();
            _inv = new InventorySystem(_db, 24);
            _progression = GetComponent<WorldFarmProgressionController>();
            foreach (var id in SeedIds)
                _inv.AddItem(id, starterSeeds);

            var found = GameObject.FindGameObjectsWithTag("CropPlot");
            System.Array.Sort(found, (a, b) =>
                string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            _plots.AddRange(found);

            _soil = new SoilManager();
            foreach (var go in _plots)
                _soil.AddPlot(go.name, defaultSoilType);

            _sim = new FarmSimulation();
            var calc = new CropGrowthCalculator();
            foreach (var go in _plots)
            {
                var state = new CropPlotState(calc);
                _sim.AddPlot(state);

                var ctrl = go.GetComponent<CropPlotController>()
                           ?? go.AddComponent<CropPlotController>();
                ctrl.Initialize(state, _soil.GetPlot(go.name));
                EnsureCropVisual(go);
            }

            EnsureInteractionController();

            _log = $"Ready — {_plots.Count} plots found.";
            Debug.Log($"[FarmSimDriver] Ready — {_plots.Count} plots.");

            var weatherDriver = GetComponent<FarmWeatherDriver>();
            var lighting = FindFirstObjectByType<FarmLightingController>();
            weatherDriver?.Initialize(_soil, lighting);
        }

        private void Update()
        {
            if (_soil == null)
                return;

            if (_progression == null)
                _progression = GetComponent<WorldFarmProgressionController>() ?? FindAnyObjectByType<WorldFarmProgressionController>();

            TickSimulation(Time.deltaTime);
        }

        public FarmPlotActionPrompt BuildPrompt(string plotName)
        {
            if (!TryGetPlotIndex(plotName, out var index))
                return null;

            return FarmPlotActionResolver.Build(
                _soil.AllPlots[index],
                _sim.Plots[index],
                _inv.GetCount(SeedIds[0]),
                _inv.GetCount(SeedIds[1]),
                _inv.GetCount(SeedIds[2]));
        }

        public bool TryExecuteAction(string plotName, FarmPlotAction action, out string message)
        {
            if (!TryGetPlotIndex(plotName, out var index))
            {
                message = $"Unknown plot '{plotName}'.";
                return false;
            }

            bool success = action switch
            {
                FarmPlotAction.PlantTomato => TryPlant(index, 0, out message),
                FarmPlotAction.PlantCarrot => TryPlant(index, 1, out message),
                FarmPlotAction.PlantLettuce => TryPlant(index, 2, out message),
                FarmPlotAction.Water => TryWater(index, out message),
                FarmPlotAction.Harvest => TryHarvest(index, out message),
                FarmPlotAction.Compost => TryCompost(index, out message),
                _ => Fail("Unsupported action.", out message)
            };

            _log = message;
            return success;
        }

        public bool TrySellAllHarvested(FarmProgressionService progression, out string message)
        {
            if (progression == null)
            {
                message = "Progression service not ready.";
                return false;
            }

            var soldAnything = false;
            var totalCoins = 0;

            for (var i = 0; i < CropIds.Length; i++)
            {
                var count = _inv.GetCount(CropIds[i]);
                if (count <= 0)
                    continue;

                _inv.RemoveItem(CropIds[i], count);
                var sellValue = _db.GetItem(CropIds[i]).SellValue;
                var reward = progression.ApplySale(sellValue, count);
                totalCoins += reward.CoinsEarned;
                soldAnything = true;
            }

            if (!soldAnything)
            {
                message = "No harvested crops to sell.";
                return false;
            }

            message = $"Sold harvest basket for {totalCoins} coins.";
            return true;
        }

        private SoilQuality ResolveSoilQuality()
        {
            float totalMoisture = 0f;
            foreach (var state in _soil.AllPlots)
                totalMoisture += state.Moisture;

            float avg = _soil.AllPlots.Count > 0 ? totalMoisture / _soil.AllPlots.Count : 0.5f;
            return avg > 0.6f ? SoilQuality.Rich : avg > 0.3f ? SoilQuality.Normal : SoilQuality.Poor;
        }

        private WeatherType ResolveWeather()
        {
            var weather = WeatherType.Sunny;
            if (FarmWeatherDriver.Instance != null)
            {
                weather = FarmWeatherDriver.Instance.Provider.Current;
            }
            else if (FarmDayClockDriver.Instance != null)
            {
                weather = FarmDayClockDriver.Instance.Clock.Phase switch
                {
                    DayPhase.Night => WeatherType.Cloudy,
                    DayPhase.Dawn => WeatherType.Cloudy,
                    DayPhase.Dusk => WeatherType.Cloudy,
                    _ => WeatherType.Sunny,
                };
            }

            return weather;
        }

        private void TickSimulation(float deltaTime)
        {
            var soilQuality = ResolveSoilQuality();
            var weather = ResolveWeather();
            DayPhase? dayPhase = FarmDayClockDriver.Instance?.Clock.Phase;
            FarmSeason? season = FarmSeasonDriver.Instance?.Provider.Current;
            var growthMultiplier = _progression != null ? _progression.GrowthMultiplier : 1f;

            _soil.Tick(deltaTime);

            for (int i = 0; i < _sim.Plots.Count && i < _soil.AllPlots.Count; i++)
            {
                var soil = _soil.AllPlots[i];
                var conditions = FarmGrowthConditionResolver.Build(
                    weather,
                    dayPhase,
                    soilQuality,
                    soil.CurrentCropId,
                    season,
                    22f);

                _sim.Plots[i].Tick(conditions, deltaTime * growthMultiplier);

                if (_sim.Plots[i].Phase == PlotPhase.Ready && soil.Status == PlotStatus.Growing)
                    _soil.MarkHarvestable(soil.PlotId);
            }
        }

        private bool TryPlant(int plotIndex, int seedIndex, out string message)
        {
            var soil = _soil.AllPlots[plotIndex];
            if (soil.Status != PlotStatus.Empty)
                return Fail($"Can't plant — {soil.Status}", out message);
            if (!_inv.HasItem(SeedIds[seedIndex]))
                return Fail($"No {SeedLabels[seedIndex]} seeds left", out message);

            // Block planting of season-hostile crops (multiplier == 0).
            if (FarmSeasonDriver.Instance != null &&
                !CropSeasonSuitability.CanPlant(SeedIds[seedIndex], FarmSeasonDriver.Instance.Provider.Current))
                return Fail($"{SeedLabels[seedIndex]} won't grow in {FarmSeasonDriver.Instance.Provider.Current}", out message);

            _soil.Plant(soil.PlotId, SeedIds[seedIndex]);

            var cropPlot = _sim.Plots[plotIndex];
            if (cropPlot.Phase == PlotPhase.Empty)
                cropPlot.Plant(Crops[seedIndex]);

            _inv.RemoveItem(SeedIds[seedIndex], 1);
            _seed = seedIndex;
            message = $"Planted {SeedLabels[seedIndex]} in {soil.PlotId}";
            return true;
        }

        private bool TryWater(int plotIndex, out string message)
        {
            var soil = _soil.AllPlots[plotIndex];
            var waterAmount = 0.4f * (_progression != null ? _progression.WateringMultiplier : 1f);
            _soil.Water(soil.PlotId, waterAmount);
            message = $"Watered {soil.PlotId} — moisture now {soil.Moisture:F2}";
            return true;
        }

        private bool TryHarvest(int plotIndex, out string message)
        {
            var soil = _soil.AllPlots[plotIndex];
            var cropPlot = _sim.Plots[plotIndex];
            if (soil.Status != PlotStatus.Harvestable || cropPlot.Phase != PlotPhase.Ready)
                return Fail($"Not ready — {soil.Status}", out message);

            string cropId = (soil.CurrentCropId ?? SeedIds[0]).Replace("seed_", "crop_");
            _soil.Harvest(soil.PlotId);
            cropPlot.Harvest();
            _inv.AddItem(cropId, 1);
            message = $"Harvested {cropId.Replace("crop_", string.Empty)}! bag: {_inv.GetCount(cropId)}";
            return true;
        }

        private bool TryCompost(int plotIndex, out string message)
        {
            var soil = _soil.AllPlots[plotIndex];
            _soil.Compost(soil.PlotId, 0.4f);
            message = $"Composted {soil.PlotId} — nutrients now {soil.Nutrients:F2}";
            return true;
        }

        private void DoFastForward()
        {
            TickSimulation(fastForwardSeconds);
            _log = $"+{fastForwardSeconds:F0}s — check status";
        }

        private static void EnsureCropVisual(GameObject plotGo)
        {
            if (plotGo.GetComponentInChildren<PlotVisualUpdater>(true) == null)
            {
                var plotRenderer = plotGo.GetComponent<Renderer>();
                if (plotRenderer != null)
                {
                    plotGo.AddComponent<PlotVisualUpdater>();
                }
                else
                {
                    var surface = plotGo.transform.Find("PlotSurface");
                    if (surface != null && surface.GetComponent<PlotVisualUpdater>() == null)
                        surface.gameObject.AddComponent<PlotVisualUpdater>();
                }
            }

            if (plotGo.GetComponentInChildren<CropVisualUpdater>(true) != null)
                return;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CropVisual";
            cube.transform.SetParent(plotGo.transform, false);
            cube.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            cube.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            cube.GetComponent<Renderer>().enabled = false;

            Object.Destroy(cube.GetComponent<BoxCollider>());
            cube.AddComponent<CropVisualUpdater>();
        }

        private void EnsureInteractionController()
        {
            if (GetComponent<FarmPlotInteractionController>() == null)
                gameObject.AddComponent<FarmPlotInteractionController>();
        }

        private bool TryGetPlotIndex(string plotName, out int index)
        {
            if (_soil == null)
            {
                index = -1;
                return false;
            }

            for (int i = 0; i < _soil.AllPlots.Count; i++)
            {
                if (_soil.AllPlots[i].PlotId == plotName)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private static bool Fail(string text, out string message)
        {
            message = text;
            return false;
        }

    }
}
