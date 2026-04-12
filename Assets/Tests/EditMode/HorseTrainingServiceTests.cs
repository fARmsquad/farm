using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class HorseTrainingServiceTests
    {
        [Test]
        public void BeginAndCompleteFullRun_ReachesSuccessThroughStoryboardBeats()
        {
            var service = new HorseTrainingService();

            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.Setup));

            service.Begin();
            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.GuidedWalk));

            service.RecordTreatMarkerReached();
            service.RecordTreatMarkerReached();
            service.RecordTreatMarkerReached();

            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.Jumping));
            Assert.That(service.Snapshot.TreatMarkersCleared, Is.EqualTo(3));

            service.RecordJumpRailCleared();
            service.RecordJumpRailCleared();

            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.Slalom));
            Assert.That(service.Snapshot.JumpRailsCleared, Is.EqualTo(2));

            service.RecordSlalomGateCleared();
            service.RecordSlalomGateCleared();
            service.RecordSlalomGateCleared();
            service.RecordSlalomGateCleared();

            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.Success));
            Assert.That(service.Snapshot.FailureReason, Is.EqualTo(HorseTrainingFailureReason.None));
            Assert.That(service.Snapshot.IsComplete, Is.True);
        }

        [Test]
        public void RecordJumpMiss_FailsSliceWithJumpReason()
        {
            var service = new HorseTrainingService();

            service.Begin();
            service.RecordTreatMarkerReached();
            service.RecordTreatMarkerReached();
            service.RecordTreatMarkerReached();

            service.RecordJumpMissed();

            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.Failure));
            Assert.That(service.Snapshot.FailureReason, Is.EqualTo(HorseTrainingFailureReason.FailedJump));
            Assert.That(service.Snapshot.IsComplete, Is.True);
        }

        [Test]
        public void SlalomMissesDrainBalanceUntilFailure()
        {
            var service = new HorseTrainingService();

            service.Begin();
            service.RecordTreatMarkerReached();
            service.RecordTreatMarkerReached();
            service.RecordTreatMarkerReached();
            service.RecordJumpRailCleared();
            service.RecordJumpRailCleared();

            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.Slalom));

            service.RecordSlalomMiss();
            Assert.That(service.Snapshot.Balance, Is.LessThan(1f));
            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.Slalom));

            service.RecordSlalomMiss();
            service.RecordSlalomMiss();

            Assert.That(service.Snapshot.Step, Is.EqualTo(HorseTrainingStep.Failure));
            Assert.That(service.Snapshot.FailureReason, Is.EqualTo(HorseTrainingFailureReason.FailedSlalom));
        }
    }
}
