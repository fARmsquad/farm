using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmTutorialMissionServiceTests
    {
        private FarmTutorialMissionService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new FarmTutorialMissionService();
        }

        [Test]
        public void NewService_StartsAtAwaitPlant_WithTomatoObjective()
        {
            Assert.AreEqual(FarmTutorialMissionStep.AwaitPlant, _service.CurrentStep);
            Assert.AreEqual("Plant the tomato seed.", _service.CurrentObjective);
            Assert.IsFalse(_service.IsComplete);
        }

        [Test]
        public void Observe_CarrotPlant_DoesNotAdvanceFromAwaitPlant()
        {
            _service.Observe("seed_carrot", PlotStatus.Planted, CropTaskId.None);

            Assert.AreEqual(FarmTutorialMissionStep.AwaitPlant, _service.CurrentStep);
            Assert.AreEqual("Plant the tomato seed.", _service.CurrentObjective);
        }

        [Test]
        public void Observe_TomatoPlant_AdvancesToPatSoil()
        {
            _service.Observe("seed_tomato", PlotStatus.Planted, CropTaskId.PatSoil);

            Assert.AreEqual(FarmTutorialMissionStep.PatSoil, _service.CurrentStep);
            Assert.AreEqual("Pat the soil closed.", _service.CurrentObjective);
        }

        [Test]
        public void Observe_TaskSequence_TracksExactTomatoOrder()
        {
            _service.Observe("seed_tomato", PlotStatus.Planted, CropTaskId.PatSoil);
            Assert.AreEqual(FarmTutorialMissionStep.PatSoil, _service.CurrentStep);

            _service.Observe("seed_tomato", PlotStatus.Growing, CropTaskId.ClearWeeds);
            Assert.AreEqual(FarmTutorialMissionStep.ClearWeeds, _service.CurrentStep);

            _service.Observe("seed_tomato", PlotStatus.Growing, CropTaskId.TieVine);
            Assert.AreEqual(FarmTutorialMissionStep.TieVine, _service.CurrentStep);

            _service.Observe("seed_tomato", PlotStatus.Growing, CropTaskId.PinchSuckers);
            Assert.AreEqual(FarmTutorialMissionStep.PinchSuckers, _service.CurrentStep);

            _service.Observe("seed_tomato", PlotStatus.Growing, CropTaskId.BrushBlossoms);
            Assert.AreEqual(FarmTutorialMissionStep.BrushBlossoms, _service.CurrentStep);

            _service.Observe("seed_tomato", PlotStatus.Growing, CropTaskId.StripLowerLeaves);
            Assert.AreEqual(FarmTutorialMissionStep.StripLowerLeaves, _service.CurrentStep);

            _service.Observe("seed_tomato", PlotStatus.Growing, CropTaskId.CheckRipeness);
            Assert.AreEqual(FarmTutorialMissionStep.CheckRipeness, _service.CurrentStep);

            _service.Observe("seed_tomato", PlotStatus.Harvestable, CropTaskId.TwistHarvest);
            Assert.AreEqual(FarmTutorialMissionStep.HarvestTomato, _service.CurrentStep);
            Assert.AreEqual("Twist harvest the ripe tomato.", _service.CurrentObjective);
        }

        [Test]
        public void Observe_ReadyThenEmpty_CompletesMission()
        {
            _service.Observe("seed_tomato", PlotStatus.Harvestable, CropTaskId.TwistHarvest);
            _service.Observe(null, PlotStatus.Empty, CropTaskId.None);

            Assert.AreEqual(FarmTutorialMissionStep.Complete, _service.CurrentStep);
            Assert.IsTrue(_service.IsComplete);
            Assert.AreEqual(string.Empty, _service.CurrentObjective);
        }

        [Test]
        public void IsActionAllowed_OnlyAllowsPrimaryInteractUntilComplete()
        {
            _service.Observe("seed_tomato", PlotStatus.Growing, CropTaskId.ClearWeeds);

            Assert.IsTrue(_service.IsActionAllowed(FarmPlotAction.PrimaryInteract, "seed_tomato", PlotStatus.Growing, CropTaskId.ClearWeeds));
            Assert.IsFalse(_service.IsActionAllowed(FarmPlotAction.Water, "seed_tomato", PlotStatus.Growing, CropTaskId.ClearWeeds));
            Assert.IsFalse(_service.IsActionAllowed(FarmPlotAction.Harvest, "seed_tomato", PlotStatus.Growing, CropTaskId.ClearWeeds));
        }

        [Test]
        public void GetPrimaryAction_ReturnsPrimaryInteractDuringTutorial()
        {
            _service.Observe("seed_tomato", PlotStatus.Growing, CropTaskId.CheckRipeness);

            Assert.AreEqual(
                FarmPlotAction.PrimaryInteract,
                _service.GetPrimaryAction("seed_tomato", PlotStatus.Growing, CropTaskId.CheckRipeness));
        }

        [Test]
        public void ConsumeFastForwardRequest_AlwaysReturnsFalse()
        {
            Assert.IsFalse(_service.ConsumeFastForwardRequest());
        }
    }
}
