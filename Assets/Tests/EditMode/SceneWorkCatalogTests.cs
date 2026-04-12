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
        public void TutorialOrderedScenes_MirrorRuntimeTutorialSequenceAndPaths()
        {
            var tutorial = SceneWorkCatalog.TutorialOrderedScenes.ToArray();

            Assert.That(tutorial.Select(scene => scene.SceneName), Is.EqualTo(TutorialSceneCatalog.SceneOrder));
            Assert.That(tutorial.Select(scene => scene.ScenePath), Is.EqualTo(new[]
            {
                "Assets/_Project/Scenes/Intro.unity",
                "Assets/_Project/Scenes/ChickenGame.unity",
                "Assets/_Project/Scenes/Tutorial_PostChickenCutscene.unity",
                "Assets/_Project/Scenes/Tutorial_MidpointPlaceholder.unity",
                "Assets/_Project/Scenes/FindToolsGame.unity",
                "Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity",
                "Assets/_Project/Scenes/FarmMain.unity",
            }));
        }

        [Test]
        public void TitleScreenLaunchableScenes_AddStandaloneReviewSlicesWithoutChangingLinearTutorialOrder()
        {
            var launchable = SceneWorkCatalog.TitleScreenLaunchableScenes.ToArray();

            Assert.That(launchable.Select(scene => scene.SceneName), Is.EqualTo(new[]
            {
                TutorialSceneCatalog.IntroSceneName,
                TutorialSceneCatalog.ChickenGameSceneName,
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                TutorialSceneCatalog.MidpointPlaceholderSceneName,
                TutorialSceneCatalog.FindToolsSceneName,
                TutorialSceneCatalog.PreFarmCutsceneSceneName,
                TutorialSceneCatalog.FarmTutorialSceneName,
                SceneWorkCatalog.HorseTrainingSceneName,
                SceneWorkCatalog.FarmVegetableStatesSceneName,
            }));

            Assert.That(TutorialSceneCatalog.SceneOrder, Does.Not.Contain(SceneWorkCatalog.HorseTrainingSceneName));
            Assert.That(TutorialSceneCatalog.SceneOrder, Does.Not.Contain(SceneWorkCatalog.FarmVegetableStatesSceneName));
        }

        [Test]
        public void TryGetBySceneName_ResolvesTutorialAndWorldAssignments()
        {
            Assert.That(SceneWorkCatalog.TryGetBySceneName(TutorialSceneCatalog.IntroSceneName, out var intro), Is.True);
            Assert.That(intro.Number, Is.EqualTo(1));
            Assert.That(intro.DisplayName, Is.EqualTo("Intro Cutscene"));
            Assert.That(intro.NextSceneName, Is.EqualTo(TutorialSceneCatalog.ChickenGameSceneName));
            Assert.That(intro.ScenePath, Is.EqualTo("Assets/_Project/Scenes/Intro.unity"));

            Assert.That(SceneWorkCatalog.TryGetBySceneName(SceneWorkCatalog.WorldSandboxSceneName, out var world), Is.True);
            Assert.That(world.Number, Is.EqualTo(8));
            Assert.That(world.Kind, Is.EqualTo(SceneWorkKind.Sandbox));
            Assert.That(world.ScenePath, Is.EqualTo("Assets/_Project/Scenes/WorldMain.unity"));

            Assert.That(SceneWorkCatalog.TryGetBySceneName(SceneWorkCatalog.HorseTrainingSceneName, out var horse), Is.True);
            Assert.That(horse.Number, Is.EqualTo(9));
            Assert.That(horse.DisplayName, Is.EqualTo("Horse Training Grounds"));
            Assert.That(horse.ScenePath, Is.EqualTo(SceneWorkCatalog.HorseTrainingScenePath));

            Assert.That(SceneWorkCatalog.TryGetBySceneName(SceneWorkCatalog.FarmVegetableStatesSceneName, out var vegetables), Is.True);
            Assert.That(vegetables.Number, Is.EqualTo(10));
            Assert.That(vegetables.DisplayName, Is.EqualTo("Farm Vegetable States"));
            Assert.That(vegetables.Kind, Is.EqualTo(SceneWorkKind.Sandbox));
            Assert.That(vegetables.ScenePath, Is.EqualTo(SceneWorkCatalog.FarmVegetableStatesScenePath));

            Assert.That(SceneWorkCatalog.TryGetBySceneName("UnknownScene", out _), Is.False);
        }
    }
}
