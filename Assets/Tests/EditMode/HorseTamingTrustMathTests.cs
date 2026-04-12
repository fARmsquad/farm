using FarmSimVR.Core.HorseTaming;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class HorseTamingTrustMathTests
    {
        private const float Walk = 10f;
        private const float Stand = 3f;
        private const float Spook = 18f;
        private const float CrowdingLoss = 8f;

        [Test]
        public void ProcessComfortTrust_OutsideZone_DoesNotChangeTrust()
        {
            var r = HorseTamingTrustMath.ProcessComfortTrust(40f, false, false, true, false, 1f, Walk, Stand, Spook, CrowdingLoss);
            Assert.IsFalse(r.Spooked);
            Assert.AreEqual(40f, r.Trust, 1e-4f);
        }

        [Test]
        public void ProcessComfortTrust_InsideZone_SlowWalk_AddsWalkRate()
        {
            var r = HorseTamingTrustMath.ProcessComfortTrust(0f, true, false, true, false, 0.5f, Walk, Stand, Spook, CrowdingLoss);
            Assert.IsFalse(r.Spooked);
            Assert.AreEqual(5f, r.Trust, 1e-4f);
        }

        [Test]
        public void ProcessComfortTrust_InsideZone_Standing_AddsStandRate()
        {
            var r = HorseTamingTrustMath.ProcessComfortTrust(0f, true, false, false, false, 1f, Walk, Stand, Spook, CrowdingLoss);
            Assert.IsFalse(r.Spooked);
            Assert.AreEqual(3f, r.Trust, 1e-4f);
        }

        [Test]
        public void ProcessComfortTrust_InsideZone_Sprint_SpooksAndSubtractsPenalty()
        {
            var r = HorseTamingTrustMath.ProcessComfortTrust(50f, true, false, false, true, 1f, Walk, Stand, Spook, CrowdingLoss);
            Assert.IsTrue(r.Spooked);
            Assert.AreEqual(32f, r.Trust, 1e-4f);
        }

        [Test]
        public void ProcessComfortTrust_TooClose_LosesCrowdingRate_NoWalkGain()
        {
            var r = HorseTamingTrustMath.ProcessComfortTrust(40f, true, true, true, false, 1f, Walk, Stand, Spook, CrowdingLoss);
            Assert.IsFalse(r.Spooked);
            Assert.AreEqual(32f, r.Trust, 1e-4f);
        }

        [Test]
        public void ProcessComfortTrust_TooClose_Sprint_StillSpooks()
        {
            var r = HorseTamingTrustMath.ProcessComfortTrust(50f, true, true, false, true, 1f, Walk, Stand, Spook, CrowdingLoss);
            Assert.IsTrue(r.Spooked);
            Assert.AreEqual(32f, r.Trust, 1e-4f);
        }

        [Test]
        public void ApplyCarrotBonus_AddsAndClampsTo100()
        {
            Assert.AreEqual(100f, HorseTamingTrustMath.ApplyCarrotBonus(90f, 22f), 1e-4f);
        }

        [Test]
        public void ClampTrust_ClipsRange()
        {
            Assert.AreEqual(0f, HorseTamingTrustMath.ClampTrust(-5f));
            Assert.AreEqual(100f, HorseTamingTrustMath.ClampTrust(150f));
        }
    }
}
