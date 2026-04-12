using FarmSimVR.MonoBehaviours.Farming;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmPlotFocusSelectorTests
    {
        [Test]
        public void ChooseBest_PrefersPromptBearingCandidateOverCloserSilentCandidate()
        {
            var choice = FarmPlotFocusSelector.ChooseBest(
                new[]
                {
                    new FarmPlotFocusCandidate<string>("silent", 0.5f, hasVisiblePrompt: false),
                    new FarmPlotFocusCandidate<string>("hero", 1.1f, hasVisiblePrompt: true),
                });

            Assert.AreEqual("hero", choice);
        }

        [Test]
        public void ChooseBest_WhenNoPromptExists_FallsBackToNearestCandidate()
        {
            var choice = FarmPlotFocusSelector.ChooseBest(
                new[]
                {
                    new FarmPlotFocusCandidate<string>("far", 3.5f, hasVisiblePrompt: false),
                    new FarmPlotFocusCandidate<string>("near", 1.2f, hasVisiblePrompt: false),
                });

            Assert.AreEqual("near", choice);
        }

        [Test]
        public void ChooseBest_WhenMultiplePromptCandidatesExist_UsesNearestPromptCandidate()
        {
            var choice = FarmPlotFocusSelector.ChooseBest(
                new[]
                {
                    new FarmPlotFocusCandidate<string>("raycast", -1f, hasVisiblePrompt: true),
                    new FarmPlotFocusCandidate<string>("nearby", 0.8f, hasVisiblePrompt: true),
                    new FarmPlotFocusCandidate<string>("silent", 0.2f, hasVisiblePrompt: false),
                });

            Assert.AreEqual("raycast", choice);
        }
    }
}
