using FarmSimVR.Core.Town;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    public sealed class TownLocomotionAnimPolicyTests
    {
        [Test]
        public void GetWalkSpeedParameter_WithForwardPositive_ReturnsOne()
        {
            Assert.That(TownLocomotionAnimPolicy.GetWalkSpeedParameter(1f), Is.EqualTo(1f));
        }

        [Test]
        public void GetWalkSpeedParameter_WithForwardNegative_ReturnsOne()
        {
            Assert.That(TownLocomotionAnimPolicy.GetWalkSpeedParameter(-1f), Is.EqualTo(1f));
        }

        [Test]
        public void GetWalkSpeedParameter_WithForwardZero_ReturnsZero()
        {
            Assert.That(TownLocomotionAnimPolicy.GetWalkSpeedParameter(0f), Is.EqualTo(0f));
        }

        [Test]
        public void GetWalkSpeedParameter_BelowDeadZone_ReturnsZero()
        {
            Assert.That(
                TownLocomotionAnimPolicy.GetWalkSpeedParameter(0.005f, deadZone: 0.01f),
                Is.EqualTo(0f));
        }

        [Test]
        public void GetWalkSpeedParameter_AboveDeadZone_ReturnsOne()
        {
            Assert.That(
                TownLocomotionAnimPolicy.GetWalkSpeedParameter(0.02f, deadZone: 0.01f),
                Is.EqualTo(1f));
        }
    }
}
