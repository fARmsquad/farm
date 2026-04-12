using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class ToolRecoveryServiceTests
    {
        [Test]
        public void RecoverTool_FirstTime_ReturnsTrueAndReducesRemainingCount()
        {
            var service = new ToolRecoveryService();

            var recovered = service.Recover(TutorialToolId.WateringCan);

            Assert.That(recovered, Is.True);
            Assert.That(service.IsRecovered(TutorialToolId.WateringCan), Is.True);
            Assert.That(service.RemainingCount, Is.EqualTo(2));
        }

        [Test]
        public void RecoverTool_DuplicateRecovery_DoesNotDoubleCountProgress()
        {
            var service = new ToolRecoveryService();
            service.Recover(TutorialToolId.HarvestBasket);

            var recoveredAgain = service.Recover(TutorialToolId.HarvestBasket);

            Assert.That(recoveredAgain, Is.False);
            Assert.That(service.RecoveredCount, Is.EqualTo(1));
            Assert.That(service.RemainingCount, Is.EqualTo(2));
        }

        [Test]
        public void IsComplete_BecomesTrueWhenAllRequiredToolsRecovered()
        {
            var service = new ToolRecoveryService();

            service.Recover(TutorialToolId.WateringCan);
            service.Recover(TutorialToolId.SeedPouch);
            service.Recover(TutorialToolId.HarvestBasket);

            Assert.That(service.IsComplete, Is.True);
        }
    }
}
