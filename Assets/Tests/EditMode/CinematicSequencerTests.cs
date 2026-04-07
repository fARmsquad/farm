using System;
using NUnit.Framework;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class CinematicStepTypeTests
    {
        [Test]
        public void StepType_Has17Values()
        {
            var values = Enum.GetValues(typeof(CinematicStepType));
            Assert.AreEqual(17, values.Length);
        }

        [Test]
        [TestCase(CinematicStepType.CameraMove)]
        [TestCase(CinematicStepType.Dialogue)]
        [TestCase(CinematicStepType.Wait)]
        [TestCase(CinematicStepType.PlaySFX)]
        [TestCase(CinematicStepType.PlayMusic)]
        [TestCase(CinematicStepType.StopMusic)]
        [TestCase(CinematicStepType.Fade)]
        [TestCase(CinematicStepType.Shake)]
        [TestCase(CinematicStepType.Letterbox)]
        [TestCase(CinematicStepType.ObjectivePopup)]
        [TestCase(CinematicStepType.MissionStart)]
        [TestCase(CinematicStepType.MissionComplete)]
        [TestCase(CinematicStepType.EnablePlayerControl)]
        [TestCase(CinematicStepType.DisablePlayerControl)]
        [TestCase(CinematicStepType.ActivateNPC)]
        [TestCase(CinematicStepType.DeactivateNPC)]
        [TestCase(CinematicStepType.SetLighting)]
        public void StepType_ContainsExpectedValue(CinematicStepType type)
        {
            Assert.IsTrue(Enum.IsDefined(typeof(CinematicStepType), type));
        }
    }
}
