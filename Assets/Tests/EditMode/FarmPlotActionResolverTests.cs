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
    }
}
