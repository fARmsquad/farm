using System.Linq;
using FarmSimVR.Core.Tutorial;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class SceneWorkCatalogTests
    {
        [Test]
        public void OrderedScenes_ExposeStableSceneNumbersForTeamHandoffs()
        {
            var ordered = SceneWorkCatalog.OrderedScenes.ToArray();

            Assert.That(ordered.Select(scene => scene.Number), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(ordered.Select(scene => scene.SceneName), Is.EqualTo(new[]
            {
                TutorialSceneCatalog.IntroSceneName,
                TutorialSceneCatalog.ChickenGameSceneName,
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                TutorialSceneCatalog.MidpointPlaceholderSceneName,
                TutorialSceneCatalog.FindToolsSceneName,
                TutorialSceneCatalog.PreFarmCutsceneSceneName,
                TutorialSceneCatalog.FarmTutorialSceneName,
                SceneWorkCatalog.WorldSandboxSceneName,
            }));
        }

        [Test]
        public void TryGetBySceneName_ResolvesTutorialAndWorldAssignments()
        {
            Assert.That(SceneWorkCatalog.TryGetBySceneName(TutorialSceneCatalog.IntroSceneName, out var intro), Is.True);
            Assert.That(intro.Number, Is.EqualTo(1));
            Assert.That(intro.DisplayName, Is.EqualTo("Intro Cutscene"));
            Assert.That(intro.NextSceneName, Is.EqualTo(TutorialSceneCatalog.ChickenGameSceneName));

            Assert.That(SceneWorkCatalog.TryGetBySceneName(SceneWorkCatalog.WorldSandboxSceneName, out var world), Is.True);
            Assert.That(world.Number, Is.EqualTo(8));
            Assert.That(world.Kind, Is.EqualTo(SceneWorkKind.Sandbox));

            Assert.That(SceneWorkCatalog.TryGetBySceneName("UnknownScene", out _), Is.False);
        }
    }
}
