using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class PackagePlantRowsMissionServiceTests
    {
        [Test]
        public void Observe_ReachingRequiredCount_CompletesMission()
        {
            var service = new PackagePlantRowsMissionService();
            service.Configure(
                objectiveText: "Plant 3 carrots in 5 minutes.",
                cropType: "carrot",
                requiredCount: 3,
                rowCount: 2,
                timeLimitSeconds: 300f);

            service.Observe(plantedCount: 1, deltaTime: 10f);
            Assert.That(service.IsComplete, Is.False);
            Assert.That(service.CurrentObjective, Does.Contain("1/3"));

            service.Observe(plantedCount: 3, deltaTime: 10f);
            Assert.That(service.IsComplete, Is.True);
            Assert.That(service.CurrentObjective, Is.EqualTo("Planting complete."));
            Assert.That(service.TargetSeedId, Is.EqualTo("seed_carrot"));
        }

        [Test]
        public void IsActionAllowed_BeforeCompletion_OnlyAllowsTillAndPlantSelected()
        {
            var service = new PackagePlantRowsMissionService();
            service.Configure(
                objectiveText: "Plant 2 carrots in 5 minutes.",
                cropType: "carrot",
                requiredCount: 2,
                rowCount: 1,
                timeLimitSeconds: 300f);

            Assert.That(service.IsActionAllowed(FarmPlotAction.Till, PlotStatus.Untilled, null), Is.True);
            Assert.That(service.IsActionAllowed(FarmPlotAction.PlantSelected, PlotStatus.Empty, null), Is.True);
            Assert.That(service.IsActionAllowed(FarmPlotAction.Water, PlotStatus.Planted, "seed_carrot"), Is.False);
            Assert.That(service.GetPrimaryAction(PlotStatus.Untilled, null), Is.EqualTo(FarmPlotAction.Till));
            Assert.That(service.GetPrimaryAction(PlotStatus.Empty, null), Is.EqualTo(FarmPlotAction.PlantSelected));
        }

        [Test]
        public void Configure_RowCountGreaterThanOne_RoundsDesiredPlotCountUpToFullRows()
        {
            var service = new PackagePlantRowsMissionService();
            service.Configure(
                objectiveText: "Plant 5 carrots in 5 minutes.",
                cropType: "carrot",
                requiredCount: 5,
                rowCount: 2,
                timeLimitSeconds: 300f);

            Assert.That(service.DesiredPlotCount, Is.EqualTo(6));
        }
    }
}
