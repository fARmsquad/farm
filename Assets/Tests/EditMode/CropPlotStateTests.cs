using NUnit.Framework;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class CropPlotStateTests
    {
        private CropGrowthCalculator _calculator;
        private CropPlotState _plot;
        private CropData _tomatoData;
        private GrowthConditions _idealConditions;

        [SetUp]
        public void SetUp()
        {
            _calculator = new CropGrowthCalculator();
            _plot = new CropPlotState(_calculator);
            // baseGrowthRate=10, maxGrowth=100 => at 3x multiplier (rain*rich) = 30/s => ~3.3s to mature
            _tomatoData = new CropData(10f, 100f);
            _idealConditions = new GrowthConditions(WeatherType.Rain, 25f, SoilQuality.Rich);
        }

        [Test]
        public void NewPlot_IsEmpty()
        {
            Assert.AreEqual(PlotPhase.Empty, _plot.Phase);
            Assert.AreEqual(0f, _plot.CurrentGrowth);
        }

        [Test]
        public void Plant_TransitionsToPlanted()
        {
            _plot.Plant(_tomatoData);
            Assert.AreEqual(PlotPhase.Planted, _plot.Phase);
        }

        [Test]
        public void FirstTick_TransitionsToGrowing()
        {
            _plot.Plant(_tomatoData);
            _plot.Tick(_idealConditions, 0.1f);
            Assert.AreEqual(PlotPhase.Growing, _plot.Phase);
        }

        [Test]
        public void Tick_AccumulatesGrowth()
        {
            _plot.Plant(_tomatoData);
            _plot.Tick(_idealConditions, 1f);
            // Rain(2x) * Temp(1x) * Rich(1.5x) * base(10) * dt(1) = 30
            Assert.AreEqual(30f, _plot.CurrentGrowth, 0.01f);
        }

        [Test]
        public void FullGrowth_TransitionsToReady()
        {
            _plot.Plant(_tomatoData);
            _plot.Tick(_idealConditions, 10f); // 30*10 = 300, capped at 100
            Assert.AreEqual(PlotPhase.Ready, _plot.Phase);
            Assert.AreEqual(100f, _plot.CurrentGrowth, 0.01f);
        }

        [Test]
        public void Tick_OnEmptyPlot_DoesNothing()
        {
            _plot.Tick(_idealConditions, 1f);
            Assert.AreEqual(PlotPhase.Empty, _plot.Phase);
            Assert.AreEqual(0f, _plot.CurrentGrowth);
        }

        [Test]
        public void Tick_OnReadyPlot_DoesNothing()
        {
            _plot.Plant(_tomatoData);
            _plot.Tick(_idealConditions, 10f);
            Assert.AreEqual(PlotPhase.Ready, _plot.Phase);
            float growthBefore = _plot.CurrentGrowth;
            _plot.Tick(_idealConditions, 1f);
            Assert.AreEqual(growthBefore, _plot.CurrentGrowth);
        }

        [Test]
        public void Harvest_ResetsToEmpty()
        {
            _plot.Plant(_tomatoData);
            _plot.Tick(_idealConditions, 10f);
            _plot.Harvest();
            Assert.AreEqual(PlotPhase.Empty, _plot.Phase);
            Assert.AreEqual(0f, _plot.CurrentGrowth);
        }

        [Test]
        public void Plant_WhenNotEmpty_Throws()
        {
            _plot.Plant(_tomatoData);
            Assert.Throws<System.InvalidOperationException>(() => _plot.Plant(_tomatoData));
        }

        [Test]
        public void Harvest_WhenNotReady_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => _plot.Harvest());
        }

        [Test]
        public void GrowthPercent_ReflectsProgress()
        {
            _plot.Plant(_tomatoData);
            _plot.Tick(_idealConditions, 1f);
            Assert.AreEqual(0.3f, _plot.GrowthPercent, 0.01f);
        }

        [Test]
        public void Milestones_FireAtCorrectThresholds()
        {
            var milestones = new System.Collections.Generic.List<int>();
            _plot.OnMilestone += m => milestones.Add(m);

            _plot.Plant(_tomatoData);
            // Tick slowly: 30 growth per second
            _plot.Tick(_idealConditions, 0.9f);  // 27 => 25% milestone
            Assert.Contains(25, milestones);

            _plot.Tick(_idealConditions, 0.8f);  // 27+24=51 => 50% milestone
            Assert.Contains(50, milestones);

            _plot.Tick(_idealConditions, 0.9f);  // 51+27=78 => 75% milestone
            Assert.Contains(75, milestones);

            _plot.Tick(_idealConditions, 1f);  // 78+22(capped)=100 => 100% milestone
            Assert.Contains(100, milestones);

            Assert.AreEqual(4, milestones.Count);
        }
    }

    [TestFixture]
    public class FarmSimulationTests
    {
        [Test]
        public void PlantAll_PlantsAllEmptyPlots()
        {
            var calc = new CropGrowthCalculator();
            var sim = new FarmSimulation();
            for (int i = 0; i < 9; i++)
                sim.AddPlot(new CropPlotState(calc));

            var cropData = new CropData(10f, 100f);
            sim.PlantAll(cropData);

            foreach (var plot in sim.Plots)
                Assert.AreEqual(PlotPhase.Planted, plot.Phase);
        }

        [Test]
        public void Tick_AdvancesAllPlots()
        {
            var calc = new CropGrowthCalculator();
            var sim = new FarmSimulation();
            sim.Conditions = new GrowthConditions(WeatherType.Rain, 25f, SoilQuality.Rich);

            for (int i = 0; i < 3; i++)
                sim.AddPlot(new CropPlotState(calc));

            sim.PlantAll(new CropData(10f, 100f));
            sim.Tick(1f);

            foreach (var plot in sim.Plots)
                Assert.AreEqual(30f, plot.CurrentGrowth, 0.01f);
        }
    }
}
