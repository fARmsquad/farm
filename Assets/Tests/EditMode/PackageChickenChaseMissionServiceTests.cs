using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class PackageChickenChaseMissionServiceTests
    {
        [Test]
        public void RegisterSuccessfulCapture_BeforeRequiredTarget_StaysInProgress()
        {
            var service = new PackageChickenChaseMissionService();
            service.Configure(
                objectiveText: "Catch 2 of 3 chickens before sunset.",
                requiredCaptureCount: 2,
                chickenCount: 3,
                arenaPresetId: "tutorial_pen_medium",
                guidanceLevel: "medium");

            var completed = service.RegisterSuccessfulCapture();

            Assert.That(completed, Is.False);
            Assert.That(service.IsComplete, Is.False);
            Assert.That(service.CapturedCount, Is.EqualTo(1));
            Assert.That(service.RequiredCaptureCount, Is.EqualTo(2));
            Assert.That(service.ConfiguredChickenCount, Is.EqualTo(3));
            Assert.That(service.CurrentObjective, Does.Contain("1/2"));
        }

        [Test]
        public void RegisterSuccessfulCapture_ReachingRequiredTarget_CompletesMission()
        {
            var service = new PackageChickenChaseMissionService();
            service.Configure(
                objectiveText: "Catch 2 of 3 chickens before sunset.",
                requiredCaptureCount: 2,
                chickenCount: 3,
                arenaPresetId: "tutorial_pen_medium",
                guidanceLevel: "medium");

            service.RegisterSuccessfulCapture();
            var completed = service.RegisterSuccessfulCapture();

            Assert.That(completed, Is.True);
            Assert.That(service.IsComplete, Is.True);
            Assert.That(service.CapturedCount, Is.EqualTo(2));
            Assert.That(service.CurrentObjective, Is.EqualTo("Chicken secured."));
        }
    }
}
