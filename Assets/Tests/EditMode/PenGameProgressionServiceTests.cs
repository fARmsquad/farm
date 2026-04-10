using FarmSimVR.Core.Hunting;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class PenGameProgressionServiceTests
    {
        [Test]
        public void ApplyDeposits_WhenExperienceCrossesThreshold_LevelsUpAndAwardsSkillPoint()
        {
            var service = new PenGameProgressionService();

            var reward = service.ApplyDeposits(5);

            Assert.That(reward.ExperienceEarned, Is.EqualTo(100));
            Assert.That(service.State.Level, Is.EqualTo(2));
            Assert.That(service.State.SkillPoints, Is.EqualTo(1));
            Assert.That(service.State.Experience, Is.EqualTo(0));
        }

        [Test]
        public void TrySpendAnimalHandlingPoint_WhenPointAvailable_IncreasesRank()
        {
            var service = new PenGameProgressionService();
            service.GrantDebugExperience(100);

            var spent = service.TrySpendAnimalHandlingPoint();

            Assert.That(spent, Is.True);
            Assert.That(service.State.SkillPoints, Is.EqualTo(0));
            Assert.That(service.State.AnimalHandlingRank, Is.EqualTo(1));
            Assert.That(service.GetCatchRadiusMultiplier(), Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(service.GetFleeSpeedMultiplier(), Is.EqualTo(0.95f).Within(0.001f));
        }

        [Test]
        public void CreateSnapshot_AndRestore_RoundTripsProgressionState()
        {
            var source = new PenGameProgressionService();
            source.GrantDebugExperience(240);
            source.TrySpendAnimalHandlingPoint();

            var snapshot = source.CreateSnapshot();
            var restored = new PenGameProgressionService();
            restored.Restore(snapshot);

            Assert.That(restored.State.Level, Is.EqualTo(source.State.Level));
            Assert.That(restored.State.Experience, Is.EqualTo(source.State.Experience));
            Assert.That(restored.State.SkillPoints, Is.EqualTo(source.State.SkillPoints));
            Assert.That(restored.State.AnimalHandlingRank, Is.EqualTo(source.State.AnimalHandlingRank));
        }
    }
}
