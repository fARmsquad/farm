using System.Collections.Generic;
using NUnit.Framework;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.Tests.EditMode
{
    // ─────────────────────────────────────────────────────────────────────────
    // CropPlotStateTests — unit contracts on CropPlotState state machine
    // ─────────────────────────────────────────────────────────────────────────
    [TestFixture]
    public class CropPlotStateTests
    {
        private CropPlotState _plot;
        // Flat conditions: Sunny, 22°C, Normal soil, no season modifier → rate = 0.04/s exactly
        private static readonly GrowthConditions FlatConditions =
            new GrowthConditions(WeatherType.Sunny, 22f, SoilQuality.Normal, 1f);
        // Tomato data matching FarmSimDriver: rate=0.04, max=1.0
        private static readonly CropData TomatoData = new CropData(0.04f, 1.0f);

        [SetUp]
        public void SetUp()
        {
            _plot = new CropPlotState(new CropGrowthCalculator());
            _plot.SetMoisture(1f);
        }

        // ── Initial state ──────────────────────────────────────────────────
        [Test] public void NewPlot_IsEmpty()
        {
            Assert.AreEqual(PlotPhase.Empty, _plot.Phase);
            Assert.AreEqual(0f, _plot.CurrentGrowth);
        }

        // ── Plant ──────────────────────────────────────────────────────────
        [Test] public void Plant_TransitionsToPlanted()
        {
            _plot.Plant(TomatoData);
            Assert.AreEqual(PlotPhase.Planted, _plot.Phase);
        }

        [Test] public void Plant_WhenNotEmpty_Throws()
        {
            _plot.Plant(TomatoData);
            Assert.Throws<System.InvalidOperationException>(() => _plot.Plant(TomatoData));
        }

        // ── Germination gate ───────────────────────────────────────────────
        [Test] public void Planted_WithoutMoisture_BlocksGrowth()
        {
            _plot.Plant(TomatoData);
            _plot.SetMoisture(0f);
            _plot.Tick(FlatConditions, 10f);
            Assert.AreEqual(PlotPhase.Planted, _plot.Phase);
            Assert.AreEqual(0f, _plot.CurrentGrowth);
        }

        [Test] public void Planted_WithMoistureButWithoutWatering_StaysPlanted()
        {
            _plot.Plant(TomatoData);
            _plot.SetMoisture(1f);
            _plot.Tick(FlatConditions, 0.1f);  // tiny tick — any positive growth moves past Planted
            Assert.AreEqual(PlotPhase.Planted, _plot.Phase);
        }

        [Test] public void Planted_AfterWatering_AdvancesToSprout()
        {
            _plot.Plant(TomatoData);
            _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 0.1f);  // tiny tick — any positive growth moves past Planted
            Assert.AreEqual(PlotPhase.Sprout, _plot.Phase);
        }

        [Test] public void TutorialWateringMode_SproutDoesNotAutoAdvanceWithoutNewWater()
        {
            _plot.RequireWateringPerPhase = true;
            _plot.Plant(TomatoData);
            _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 0.1f);

            Assert.AreEqual(PlotPhase.Sprout, _plot.Phase);
            Assert.IsTrue(_plot.NeedsWaterToAdvance);

            _plot.Tick(FlatConditions, 10f);

            Assert.AreEqual(PlotPhase.Sprout, _plot.Phase);
            Assert.Less(_plot.CurrentGrowth, 0.2f);
        }

        [Test] public void TutorialWateringMode_NewWaterAllowsNextPhaseAdvance()
        {
            _plot.RequireWateringPerPhase = true;
            _plot.Plant(TomatoData);
            _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 0.1f);

            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 10f);

            Assert.AreEqual(PlotPhase.YoungPlant, _plot.Phase);
            Assert.IsTrue(_plot.NeedsWaterToAdvance);
        }

        [Test]
        public void TutorialLifecycle_TickAlone_DoesNotAdvanceOrWilt()
        {
            _plot.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);
            _plot.Plant(TomatoData);
            _plot.SetMoisture(0f);

            _plot.Tick(FlatConditions, 999f);
            _plot.Tick(FlatConditions, 999f);

            Assert.AreEqual(0, _plot.CurrentStageIndex);
            Assert.AreEqual(PlotPhase.Planted, _plot.Phase);
            Assert.AreEqual(CropTaskId.PatSoil, _plot.CurrentTaskId);
            Assert.AreEqual("TomatoSeed_01", _plot.CurrentVisualAssetId);
            Assert.IsFalse(_plot.NeedsWaterToAdvance);
        }

        [Test]
        public void TutorialLifecycle_WrongTask_DoesNotAdvance()
        {
            _plot.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);
            _plot.Plant(TomatoData);

            var advanced = _plot.TryCompleteTask(CropTaskId.ClearWeeds);

            Assert.IsFalse(advanced);
            Assert.AreEqual(0, _plot.CurrentStageIndex);
            Assert.AreEqual(CropTaskId.PatSoil, _plot.CurrentTaskId);
            Assert.AreEqual("TomatoSeed_01", _plot.CurrentVisualAssetId);
        }

        [Test]
        public void TutorialLifecycle_MatchingTask_AdvancesToNextStage()
        {
            _plot.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);
            _plot.Plant(TomatoData);

            var advanced = _plot.TryCompleteTask(CropTaskId.PatSoil);

            Assert.IsTrue(advanced);
            Assert.AreEqual(1, _plot.CurrentStageIndex);
            Assert.AreEqual(PlotPhase.Sprout, _plot.Phase);
            Assert.AreEqual(CropTaskId.ClearWeeds, _plot.CurrentTaskId);
            Assert.AreEqual("Tomato_01a", _plot.CurrentVisualAssetId);
        }

        [Test]
        public void TutorialLifecycle_ReachesReadyHarvestTask_ThenWaitsForHarvest()
        {
            _plot.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);
            _plot.Plant(TomatoData);

            CompleteTomatoTask(_plot, CropTaskId.PatSoil);
            CompleteTomatoTask(_plot, CropTaskId.ClearWeeds);
            CompleteTomatoTask(_plot, CropTaskId.TieVine);
            CompleteTomatoTask(_plot, CropTaskId.PinchSuckers);
            CompleteTomatoTask(_plot, CropTaskId.BrushBlossoms);
            CompleteTomatoTask(_plot, CropTaskId.StripLowerLeaves);
            CompleteTomatoTask(_plot, CropTaskId.CheckRipeness);

            Assert.AreEqual(7, _plot.CurrentStageIndex);
            Assert.AreEqual(PlotPhase.Ready, _plot.Phase);
            Assert.AreEqual("Tomato_07", _plot.CurrentVisualAssetId);
            Assert.AreEqual(CropTaskId.TwistHarvest, _plot.CurrentTaskId);
            Assert.IsTrue(_plot.CurrentTaskCompletesHarvest);

            var completedHarvestTask = _plot.TryCompleteTask(CropTaskId.TwistHarvest);

            Assert.IsTrue(completedHarvestTask);
            Assert.AreEqual(7, _plot.CurrentStageIndex);
            Assert.AreEqual(PlotPhase.Ready, _plot.Phase);

            _plot.Harvest();

            Assert.AreEqual(-1, _plot.CurrentStageIndex);
            Assert.AreEqual(PlotPhase.Empty, _plot.Phase);
            Assert.AreEqual(CropTaskId.None, _plot.CurrentTaskId);
        }

        // ── Full sequence ──────────────────────────────────────────────────
        [Test] public void FullGrowth_WithConstantMoisture_ReachesReady()
        {
            _plot.Plant(TomatoData);
            _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 100f);
            Assert.AreEqual(PlotPhase.Ready, _plot.Phase);
            Assert.AreEqual(1f, _plot.CurrentGrowth, 0.001f);
        }

        // ── Idle states ────────────────────────────────────────────────────
        [Test] public void Tick_OnEmptyPlot_DoesNothing()
        {
            _plot.Tick(FlatConditions, 1f);
            Assert.AreEqual(PlotPhase.Empty, _plot.Phase);
            Assert.AreEqual(0f, _plot.CurrentGrowth);
        }

        [Test] public void Tick_OnReadyPlot_DoesNothing()
        {
            _plot.Plant(TomatoData); _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 100f);
            float g = _plot.CurrentGrowth;
            _plot.Tick(FlatConditions, 10f);
            Assert.AreEqual(g, _plot.CurrentGrowth);
            Assert.AreEqual(PlotPhase.Ready, _plot.Phase);
        }

        // ── Harvest ────────────────────────────────────────────────────────
        [Test] public void Harvest_Ready_ResetsToEmpty()
        {
            _plot.Plant(TomatoData); _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 100f);
            _plot.Harvest();
            Assert.AreEqual(PlotPhase.Empty, _plot.Phase);
            Assert.AreEqual(0f, _plot.CurrentGrowth);
        }

        [Test] public void Harvest_WhenNotReady_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => _plot.Harvest());
        }

        // ── Wilt ───────────────────────────────────────────────────────────
        [Test] public void Sprout_DropsBelow_WiltThreshold_EntersWilting()
        {
            _plot.Plant(TomatoData); _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 0.1f);       // now Sprout
            Assert.AreEqual(PlotPhase.Sprout, _plot.Phase);

            _plot.SetMoisture(0f);
            _plot.Tick(FlatConditions, 0.01f);
            Assert.AreEqual(PlotPhase.Wilting, _plot.Phase);
        }

        [Test] public void Wilting_WhenWatered_Recovers()
        {
            _plot.Plant(TomatoData); _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 0.1f);
            _plot.SetMoisture(0f);
            _plot.Tick(FlatConditions, 0.01f);
            Assert.AreEqual(PlotPhase.Wilting, _plot.Phase);

            _plot.SetMoisture(1f);
            _plot.Tick(FlatConditions, 0.01f);
            Assert.AreNotEqual(PlotPhase.Wilting, _plot.Phase);
            Assert.AreNotEqual(PlotPhase.Dead, _plot.Phase);
        }

        // ── Death ──────────────────────────────────────────────────────────
        [Test] public void Wilting_TooLong_DiesAndEventFires()
        {
            bool died = false;
            _plot.OnDead += () => died = true;

            _plot.Plant(TomatoData); _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 0.1f);
            _plot.SetMoisture(0f);
            _plot.Tick(FlatConditions, CropPlotState.DeathTimeoutSeconds + 1f);

            Assert.AreEqual(PlotPhase.Dead, _plot.Phase);
            Assert.IsTrue(died);
        }

        [Test] public void Dead_ClearDead_ResetsToEmpty()
        {
            _plot.Plant(TomatoData); _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 0.1f);
            _plot.SetMoisture(0f);
            _plot.Tick(FlatConditions, CropPlotState.DeathTimeoutSeconds + 1f);

            _plot.ClearDead();
            Assert.AreEqual(PlotPhase.Empty, _plot.Phase);
        }

        [Test] public void ClearDead_WhenNotDead_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => _plot.ClearDead());
        }

        // ── Milestones ─────────────────────────────────────────────────────
        [Test] public void AllFiveGateMilestones_FiredInOrder_FullRun()
        {
            var gates = new List<int>();
            _plot.OnMilestone += g => gates.Add(g);

            _plot.Plant(TomatoData); _plot.SetMoisture(1f);
            _plot.NotifyWatered();
            _plot.Tick(FlatConditions, 100f);

            Assert.AreEqual(new List<int> { 1, 2, 3, 4, 5 }, gates);
        }

        private static void CompleteTomatoTask(CropPlotState plot, CropTaskId taskId)
        {
            Assert.IsTrue(plot.TryCompleteTask(taskId), $"Expected {taskId} to advance the tutorial stage.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FarmSimulationTests — basic integration smoke tests
    // ─────────────────────────────────────────────────────────────────────────
    [TestFixture]
    public class FarmSimulationTests
    {
        private static readonly CropData TomatoData = new CropData(0.04f, 1.0f);

        [Test]
        public void PlantAll_PlantsAllEmptyPlots()
        {
            var sim = new FarmSimulation();
            for (int i = 0; i < 9; i++)
                sim.AddPlot(new CropPlotState(new CropGrowthCalculator()));

            sim.PlantAll(TomatoData);
            foreach (var plot in sim.Plots)
                Assert.AreEqual(PlotPhase.Planted, plot.Phase);
        }

        [Test]
        public void Tick_AdvancesAllPlots_WithMoisture()
        {
            var sim = new FarmSimulation();
            sim.Conditions = new GrowthConditions(WeatherType.Sunny, 22f, SoilQuality.Normal, 1f);

            for (int i = 0; i < 3; i++)
            {
                var p = new CropPlotState(new CropGrowthCalculator());
                p.SetMoisture(1f);
                p.NotifyWatered();
                sim.AddPlot(p);
            }

            sim.PlantAll(TomatoData);
            sim.Tick(0.1f);

            foreach (var plot in sim.Plots)
                Assert.Greater(plot.CurrentGrowth, 0f);
        }

    }
}
