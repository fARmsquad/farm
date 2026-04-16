using System;
using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    public sealed class TownAutoplayPolicyTests
    {
        [Test]
        public void ShouldSkipTownInteractionDemo_WithCoreSceneAndTown_ReturnsTrue()
        {
            var names = new[] { TutorialSceneCatalog.CoreSceneSceneName, SceneWorkCatalog.TownSceneName };

            Assert.That(TownAutoplayPolicy.ShouldSkipTownInteractionDemo(names), Is.True);
        }

        [Test]
        public void ShouldSkipTownInteractionDemo_WithTownOnly_ReturnsFalse()
        {
            var names = new[] { SceneWorkCatalog.TownSceneName };

            Assert.That(TownAutoplayPolicy.ShouldSkipTownInteractionDemo(names), Is.False);
        }

        [Test]
        public void ShouldSkipTownInteractionDemo_WithEmptyList_ReturnsFalse()
        {
            Assert.That(TownAutoplayPolicy.ShouldSkipTownInteractionDemo(Array.Empty<string>()), Is.False);
        }

        [Test]
        public void ShouldSkipTownInteractionDemo_WithNull_ReturnsFalse()
        {
            Assert.That(TownAutoplayPolicy.ShouldSkipTownInteractionDemo(null), Is.False);
        }
    }
}
