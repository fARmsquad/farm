using System.Collections.Generic;
using UnityEngine;
using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Inventory;
using FarmSimVR.MonoBehaviours.UI;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed partial class FarmSimDriver : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private bool showLegacyDebugPanel = false;

        [Header("Player rig")]
        [Tooltip("When true, FarmPlotInteractionController spawns a third-person orbit camera instead of first-person.")]
        [SerializeField] private bool useThirdPersonRig;

        public bool UseThirdPersonRig => useThirdPersonRig;

        [Header("Soil")]
        [SerializeField] private SoilType defaultSoilType = SoilType.Loam;

        [Header("Inventory")]
        [SerializeField] private int starterSeeds = 5;
        [SerializeField] private ItemIconDatabase iconDatabase;

        [Header("Simulation")]
        [SerializeField] private float fastForwardSeconds = 30f;

        private SoilManager _soil;
        private InventorySystem _inv;
        private IItemDatabase _db;
        private FarmSimulation _sim;
        private WorldFarmProgressionController _progression;
        private readonly List<GameObject> _plots = new();

        private ToolEquipState _toolEquip;
        private WateringCanState _waterCan;

        /// <summary>Current tool equip state for UI and enforcement.</summary>
        public ToolEquipState ToolEquip => _toolEquip;

        /// <summary>Watering can water level state for UI and enforcement.</summary>
        public WateringCanState WaterCan => _waterCan;

        /// <summary>Inventory accessor for UI controllers.</summary>
        public IInventorySystem Inventory => _inv;

        private static readonly string[] SeedIds = { "seed_tomato", "seed_carrot", "seed_lettuce" };
        private static readonly string[] CropIds = { "crop_tomato", "crop_carrot", "crop_lettuce" };
        private static readonly string[] SeedLabels = { "Tomato", "Carrot", "Lettuce" };
        private static readonly CropData[] Crops =
        {
            // baseGrowthRate: 0.022/s → full growth in ~45 seconds at standard conditions
            new CropData(baseGrowthRate: 0.022f, maxGrowth: 1f),
            new CropData(baseGrowthRate: 0.022f, maxGrowth: 1f),
            new CropData(baseGrowthRate: 0.022f, maxGrowth: 1f),
        };

        private int _plot;
        private int _seed;
        private int _selectedSeedIndex;
        private string _log = "Walk to a plot and look at it to farm.";

        private void Start()
        {
            _db = ItemDatabase.CreateStarterDatabase();
            _inv = new InventorySystem(_db, 24);
            _progression = GetComponent<WorldFarmProgressionController>();
            foreach (var id in SeedIds)
                _inv.AddItem(id, starterSeeds);

            // Add starter tools to inventory
            _inv.AddItem("tool_watering_can", 1);
            _inv.AddItem("tool_basket", 1);
            _inv.AddItem("tool_hoe", 1);
            _inv.AddItem("tool_seed_pouch", 1);

            // Initialize tool equip state
            _toolEquip = new ToolEquipState();

            // Initialize watering can state (starts empty — player must visit well)
            _waterCan = new WateringCanState();

            var wellController = FindAnyObjectByType<WellInteractionController>();
            if (wellController != null)
                wellController.Initialize(_waterCan, _toolEquip);

            var feedbackController = FindAnyObjectByType<WateringCanFeedbackController>();
            if (feedbackController != null)
                feedbackController.Initialize(_waterCan, _toolEquip);

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

            // Initialize UI controllers
            var inventoryUI = FindAnyObjectByType<InventoryUIController>();
            if (inventoryUI != null)
                inventoryUI.Initialize(_inv, _db, _toolEquip, iconDatabase);

            var hotbarUI = FindAnyObjectByType<HotbarUIController>();
            if (hotbarUI != null)
                hotbarUI.Initialize(_inv, _db, _toolEquip, iconDatabase, _waterCan);

            // Initialize tool visual controller
            var toolVisual = FindAnyObjectByType<ToolVisualController>();
            if (toolVisual != null)
                toolVisual.Initialize(_toolEquip);

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

        /// <summary>
        /// Sets the currently selected seed index for context-sensitive planting.
        /// </summary>
        public void SetSelectedSeed(int index)
        {
            if (index >= 0 && index < SeedIds.Length)
                _selectedSeedIndex = index;
        }

        /// <summary>Current selected seed index (0 = Tomato, 1 = Carrot, 2 = Lettuce).</summary>
        public int SelectedSeedIndex => _selectedSeedIndex;

        public FarmPlotActionPrompt BuildPrompt(string plotName)
        {
            if (!TryGetPlotIndex(plotName, out var index))
                return null;

            return FarmPlotActionResolver.Build(
                _soil.AllPlots[index],
                _sim.Plots[index],
                _inv.GetCount(SeedIds[0]),
                _inv.GetCount(SeedIds[1]),
                _inv.GetCount(SeedIds[2]),
                _selectedSeedIndex);
        }

        public bool TryExecuteAction(string plotName, FarmPlotAction action, out string message)
        {
            if (!TryGetPlotIndex(plotName, out var index))
            {
                message = $"Unknown plot '{plotName}'.";
                return false;
            }

            // Tool enforcement: check that the correct tool is equipped before executing
            var requiredTool = FarmToolMap.RequiredToolFor(action);
            if (requiredTool != FarmToolId.None && _toolEquip != null && _toolEquip.EquippedTool != requiredTool)
                return Fail($"Equip {requiredTool} first.", out message);

            bool success = action switch
            {
                FarmPlotAction.PrimaryInteract => TryPrimaryInteract(index, out message),
                FarmPlotAction.Till => TryTill(index, out message),
                FarmPlotAction.PlantTomato => TryPlant(index, 0, out message),
                FarmPlotAction.PlantCarrot => TryPlant(index, 1, out message),
                FarmPlotAction.PlantLettuce => TryPlant(index, 2, out message),
                FarmPlotAction.PlantSelected => TryPlant(index, _selectedSeedIndex, out message),
                FarmPlotAction.Water => TryWater(index, out message),
                FarmPlotAction.Harvest => TryHarvest(index, out message),
                FarmPlotAction.Compost => TryCompost(index, out message),
                FarmPlotAction.ClearDead => TryClearDead(index, out message),
                _ => Fail("Unsupported action.", out message)
            };

            _log = message;
            return success;
        }

        public bool TryCompleteCurrentCropTask(string plotName, out string message)
        {
            if (!TryGetPlotIndex(plotName, out var index))
                return Fail($"Unknown plot '{plotName}'.", out message);

            var soil = _soil.AllPlots[index];
            var cropPlot = _sim.Plots[index];
            if (!cropPlot.IsTutorialTaskMode)
                return Fail("No stage task is active on this plot.", out message);

            var taskId = cropPlot.CurrentTaskId;
            if (taskId == CropTaskId.None)
                return Fail("No stage task is active on this plot.", out message);

            var finishedActionLabel = cropPlot.CurrentActionLabel;
            var completesHarvest = cropPlot.CurrentTaskCompletesHarvest;
            if (!cropPlot.TryCompleteTask(taskId))
                return Fail("That task is not ready yet.", out message);

            if (cropPlot.Phase == PlotPhase.Ready)
            {
                if (soil.Status == PlotStatus.Planted)
                    soil.SetStatus(PlotStatus.Growing);
                if (soil.Status == PlotStatus.Growing)
                    _soil.MarkHarvestable(soil.PlotId);
            }

            if (!completesHarvest)
            {
                message = string.IsNullOrEmpty(cropPlot.CurrentActionLabel)
                    ? $"{finishedActionLabel} complete."
                    : $"{finishedActionLabel} complete. Next: {cropPlot.CurrentActionLabel}.";
                return true;
            }

            return TryHarvest(index, out message);
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
                var cropPlot = _sim.Plots[i];

                if (soil.Moisture > cropPlot.Moisture + 0.0001f)
                    cropPlot.NotifyWatered();

                // Sync live moisture into the crop state so it can gate growth and trigger wilt
                cropPlot.SetMoisture(soil.Moisture);

                var conditions = FarmGrowthConditionResolver.Build(
                    weather,
                    dayPhase,
                    soilQuality,
                    soil.CurrentCropId,
                    season,
                    22f);

                cropPlot.Tick(conditions, deltaTime * growthMultiplier);

                if (cropPlot.Phase == PlotPhase.Ready && soil.Status == PlotStatus.Growing)
                    _soil.MarkHarvestable(soil.PlotId);

                // Dead plant — reset soil so the plot can be replanted after clearing
                if (cropPlot.Phase == PlotPhase.Dead && soil.Status == PlotStatus.Growing)
                    _soil.MarkDead(soil.PlotId);
            }
        }

        private bool TryTill(int plotIndex, out string message)
        {
            var soil = _soil.AllPlots[plotIndex];
            if (soil.Status != PlotStatus.Untilled)
                return Fail($"Can't till — {soil.Status}", out message);

            if (!_soil.Till(soil.PlotId))
                return Fail("Tilling failed.", out message);

            message = $"Tilled {soil.PlotId} — ready to plant.";
            return true;
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

        private bool TryPrimaryInteract(int plotIndex, out string message)
        {
            var soil = _soil.AllPlots[plotIndex];
            var cropPlot = _sim.Plots[plotIndex];

            if (soil.Status == PlotStatus.Untilled)
                return TryTill(plotIndex, out message);

            if (soil.Status == PlotStatus.Empty && cropPlot.Phase == PlotPhase.Empty)
                return TryPlant(plotIndex, _selectedSeedIndex, out message);

            if (cropPlot.IsTutorialTaskMode)
                return Fail("Open the stage minigame to complete that task.", out message);

            return Fail("Primary interaction is not available here.", out message);
        }

        private bool TryWater(int plotIndex, out string message)
        {
            if (_waterCan == null || _waterCan.IsEmpty)
                return Fail("Watering can is empty \u2014 refill at the well.", out message);

            if (!_waterCan.TryDrain(WateringCanState.DrainPerUse, out _))
                return Fail("Watering can is empty \u2014 refill at the well.", out message);

            var soil = _soil.AllPlots[plotIndex];
            var waterAmount = 0.4f * (_progression != null ? _progression.WateringMultiplier : 1f);
            _soil.Water(soil.PlotId, waterAmount);
            _sim.Plots[plotIndex].NotifyWatered();
            int waterPercent = Mathf.RoundToInt(_waterCan.WaterLevel * 100f);
            message = $"Watered {soil.PlotId} \u2014 moisture now {soil.Moisture:F2}, can {waterPercent}%";
            return true;
        }

        private bool TryHarvest(int plotIndex, out string message)
        {
            var soil = _soil.AllPlots[plotIndex];
            var cropPlot = _sim.Plots[plotIndex];
            if (cropPlot.Phase != PlotPhase.Ready)
                return Fail($"Not ready to harvest — plant is {cropPlot.Phase}", out message);

            string cropId = (soil.CurrentCropId ?? SeedIds[0]).Replace("seed_", "crop_");
            _soil.Harvest(soil.PlotId);
            cropPlot.Harvest();
            _inv.AddItem(cropId, 1);
            message = $"Harvested {cropId.Replace("crop_", string.Empty)}! bag: {_inv.GetCount(cropId)}";
            return true;
        }

        private bool TryClearDead(int plotIndex, out string message)
        {
            var soil = _soil.AllPlots[plotIndex];
            var cropPlot = _sim.Plots[plotIndex];
            if (cropPlot.Phase != PlotPhase.Dead)
                return Fail($"Plot is not dead — currently {cropPlot.Phase}", out message);

            cropPlot.ClearDead();
            _soil.ClearDead(soil.PlotId);
            message = $"Cleared dead plant from {soil.PlotId}";
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

        /// <summary>
        /// Advances the simulation by <paramref name="seconds"/> from an external caller
        /// (e.g. <see cref="FarmSimVR.MonoBehaviours.Tutorial.TutorialFarmSceneController"/>).
        /// </summary>
        public void TryFastForward(float seconds)
        {
            if (seconds > 0f)
                TickSimulation(seconds);
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
