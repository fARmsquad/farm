using FarmSimVR.Core.Cinematics;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class IntroBlackoutFadeMathTests
    {
        [Test]
        public void ComputeAlpha_BeforeFadeStart_ReturnsZero()
        {
            Assert.AreEqual(0f, IntroBlackoutFadeMath.ComputeAlpha(10d, 51.5d, 4d), 1e-6f);
        }

        [Test]
        public void ComputeAlpha_AtFadeEnd_ReturnsOne()
        {
            Assert.AreEqual(1f, IntroBlackoutFadeMath.ComputeAlpha(56d, 51.5d, 4d), 1e-6f);
        }

        [Test]
        public void ComputeAlpha_HalfwayThroughFade_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, IntroBlackoutFadeMath.ComputeAlpha(53.5d, 51.5d, 4d), 1e-6f);
        }

        [Test]
        public void ComputeAlpha_ZeroDuration_JumpsToOneAtOrAfterStart()
        {
            Assert.AreEqual(0f, IntroBlackoutFadeMath.ComputeAlpha(51d, 51.5d, 0d), 1e-6f);
            Assert.AreEqual(1f, IntroBlackoutFadeMath.ComputeAlpha(51.5d, 51.5d, 0d), 1e-6f);
        }
    }
}
