using System.Linq;
using FarmSimVR.Core.Farming;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmPlotActionResolverTests
    {
        private CropPlotState _crop;
        private SoilState _soil;

        [SetUp]
        public void SetUp()
        {
            _crop = new CropPlotState(new CropGrowthCalculator());
            _soil = new SoilState("CropPlot_0", SoilType.Loam);
        }

        [Test]
        public void Build_EmptyPlot_ShowsAvailablePlantActionsAndCompost()
        {
            var prompt = FarmPlotActionResolver.Build(_soil, _crop, tomatoSeeds: 3, carrotSeeds: 1, lettuceSeeds: 0);

            var actions = prompt.Actions.Select(x => x.Action).ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    FarmPlotAction.PlantTomato,
                    FarmPlotAction.PlantCarrot,
                    FarmPlotAction.Compost
                },
                actions);
        }

        [Test]
        public void Build_GrowingPlot_ShowsWaterAndCompostOnly()
        {
            _soil.SetStatus(PlotStatus.Growing);
            _soil.SetCropId("seed_tomato");
            _crop.Plant(new CropData(10f, 100f));
            _crop.NotifyWatered();
            _crop.Tick(new GrowthConditions(WeatherType.Sunny, 25f, SoilQuality.Normal), 1f);

            var prompt = FarmPlotActionResolver.Build(_soil, _crop, tomatoSeeds: 3, carrotSeeds: 1, lettuceSeeds: 2);

            var actions = prompt.Actions.Select(x => x.Action).ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    FarmPlotAction.Water,
                    FarmPlotAction.Compost
                },
                actions);
        }

        [Test]
        public void Build_HarvestablePlot_ShowsHarvestWaterAndCompost()
        {
            _soil.SetStatus(PlotStatus.Harvestable);
            _soil.SetCropId("seed_tomato");
            _crop.Plant(new CropData(10f, 100f));
            _crop.NotifyWatered();
            _crop.Tick(new GrowthConditions(WeatherType.Rain, 25f, SoilQuality.Rich), 10f);

            var prompt = FarmPlotActionResolver.Build(_soil, _crop, tomatoSeeds: 0, carrotSeeds: 0, lettuceSeeds: 0);

            var actions = prompt.Actions.Select(x => x.Action).ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    FarmPlotAction.Harvest,
                    FarmPlotAction.Water,
                    FarmPlotAction.Compost
                },
                actions);
        }

        [Test]
        public void Build_DepletedPlot_ShowsOnlyCompost()
        {
            _soil.SetStatus(PlotStatus.Depleted);

            var prompt = FarmPlotActionResolver.Build(_soil, _crop, tomatoSeeds: 4, carrotSeeds: 4, lettuceSeeds: 4);

            var actions = prompt.Actions.Select(x => x.Action).ToArray();

            CollectionAssert.AreEqual(new[] { FarmPlotAction.Compost }, actions);
        }

        [Test]
        public void Build_TutorialEmptyPlot_ShowsPrimaryInteractPlantOnly()
        {
            _crop.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);

            var prompt = FarmPlotActionResolver.Build(_soil, _crop, tomatoSeeds: 3, carrotSeeds: 9, lettuceSeeds: 9);

            Assert.AreEqual("Plot 0 [Tomato Task]", prompt.Title);
            Assert.AreEqual(1, prompt.Actions.Count);
            Assert.AreEqual(FarmPlotAction.PrimaryInteract, prompt.Actions[0].Action);
            Assert.AreEqual("E", prompt.Actions[0].KeyLabel);
            Assert.AreEqual("Plant Tomato Seed (3)", prompt.Actions[0].Label);
            StringAssert.Contains("Plant the tomato seed", prompt.Detail);
        }

        [Test]
        public void Build_TutorialPlantedPlot_ShowsOnlyCurrentTaskWithoutWater()
        {
            _crop.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);
            _crop.Plant(new CropData(0.04f, 1f));
            _soil.SetStatus(PlotStatus.Planted);
            _soil.SetCropId("seed_tomato");

            var prompt = FarmPlotActionResolver.Build(_soil, _crop, tomatoSeeds: 3, carrotSeeds: 1, lettuceSeeds: 2);

            Assert.AreEqual(1, prompt.Actions.Count);
            Assert.AreEqual(FarmPlotAction.PrimaryInteract, prompt.Actions[0].Action);
            Assert.AreEqual("Pat Soil", prompt.Actions[0].Label);
            StringAssert.Contains("Pat the soil closed", prompt.Detail);
            StringAssert.DoesNotContain("Water", prompt.Detail);
        }

        [Test]
        public void Build_TutorialReadyState_ShowsTwistHarvestAsPrimaryInteract()
        {
            _crop.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);
            _crop.Plant(new CropData(0.04f, 1f));
            AdvanceTomatoToHarvestTask(_crop);
            _soil.SetStatus(PlotStatus.Harvestable);
            _soil.SetCropId("seed_tomato");

            var prompt = FarmPlotActionResolver.Build(_soil, _crop, tomatoSeeds: 0, carrotSeeds: 0, lettuceSeeds: 0);

            Assert.AreEqual(1, prompt.Actions.Count);
            Assert.AreEqual(FarmPlotAction.PrimaryInteract, prompt.Actions[0].Action);
            Assert.AreEqual("Twist Harvest", prompt.Actions[0].Label);
            StringAssert.Contains("Tomato_07", prompt.Detail);
            StringAssert.DoesNotContain("Water", prompt.Detail);
        }

        private static void AdvanceTomatoToHarvestTask(CropPlotState crop)
        {
            Assert.IsTrue(crop.TryCompleteTask(CropTaskId.PatSoil));
            Assert.IsTrue(crop.TryCompleteTask(CropTaskId.ClearWeeds));
            Assert.IsTrue(crop.TryCompleteTask(CropTaskId.TieVine));
            Assert.IsTrue(crop.TryCompleteTask(CropTaskId.PinchSuckers));
            Assert.IsTrue(crop.TryCompleteTask(CropTaskId.BrushBlossoms));
            Assert.IsTrue(crop.TryCompleteTask(CropTaskId.StripLowerLeaves));
            Assert.IsTrue(crop.TryCompleteTask(CropTaskId.CheckRipeness));
        }
    }
}
