using FarmSimVR.Core.Farming;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmProgressionServiceTests
    {
        [Test]
        public void ApplySale_WhenExperienceCrossesThreshold_LevelsUpAndAwardsSkillPoint()
        {
            var service = new FarmProgressionService();

            var reward = service.ApplySale(15, 10);

            Assert.That(reward.CoinsEarned, Is.EqualTo(150));
            Assert.That(service.State.Coins, Is.EqualTo(150));
            Assert.That(service.State.Level, Is.EqualTo(2));
            Assert.That(service.State.SkillPoints, Is.EqualTo(1));
            Assert.That(service.State.Experience, Is.EqualTo(20));
        }

        [Test]
        public void TryBuyWateringUpgrade_WithEnoughCoins_IncreasesTierAndConsumesCoins()
        {
            var service = new FarmProgressionService();
            service.GrantDebugCoins(200);

            var purchased = service.TryBuyWateringUpgrade();

            Assert.That(purchased, Is.True);
            Assert.That(service.State.WateringCanTier, Is.EqualTo(2));
            Assert.That(service.State.Coins, Is.EqualTo(125));
            Assert.That(service.GetWateringMultiplier(), Is.EqualTo(1.2f).Within(0.001f));
        }

        [Test]
        public void TrySpendSkillPoint_WhenPointAvailable_IncreasesSelectedSkillRank()
        {
            var service = new FarmProgressionService();
            service.GrantDebugExperience(100);

            var spent = service.TrySpendSkillPoint(FarmSkillType.GreenThumb);

            Assert.That(spent, Is.True);
            Assert.That(service.State.SkillPoints, Is.EqualTo(0));
            Assert.That(service.State.GreenThumbRank, Is.EqualTo(1));
            Assert.That(service.GetGrowthMultiplier(), Is.EqualTo(1.05f).Within(0.001f));
        }

        [Test]
        public void CreateSnapshot_AndRestore_RoundTripsProgressionState()
        {
            var source = new FarmProgressionService();
            source.GrantDebugCoins(350);
            source.GrantDebugExperience(220);
            source.TryBuyWateringUpgrade();
            source.TryUnlockNextExpansion();
            source.TrySpendSkillPoint(FarmSkillType.GreenThumb);
            source.TrySpendSkillPoint(FarmSkillType.Merchant);

            var snapshot = source.CreateSnapshot();
            var restored = new FarmProgressionService();
            restored.Restore(snapshot);

            Assert.That(restored.State.Coins, Is.EqualTo(source.State.Coins));
            Assert.That(restored.State.Level, Is.EqualTo(source.State.Level));
            Assert.That(restored.State.Experience, Is.EqualTo(source.State.Experience));
            Assert.That(restored.State.SkillPoints, Is.EqualTo(source.State.SkillPoints));
            Assert.That(restored.State.WateringCanTier, Is.EqualTo(source.State.WateringCanTier));
            Assert.That(restored.State.ExpansionLevel, Is.EqualTo(source.State.ExpansionLevel));
            Assert.That(restored.State.GreenThumbRank, Is.EqualTo(source.State.GreenThumbRank));
            Assert.That(restored.State.MerchantRank, Is.EqualTo(source.State.MerchantRank));
        }
    }
}
