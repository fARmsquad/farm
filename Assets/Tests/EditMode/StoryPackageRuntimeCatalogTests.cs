using System.Reflection;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.Core.Story;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class StoryPackageRuntimeCatalogTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryPackageRuntimeCatalog.ResetCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var controller in Object.FindObjectsByType<TutorialCutsceneSceneController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(controller.gameObject);

            foreach (var flow in Object.FindObjectsByType<TutorialFlowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(flow.gameObject);
        }

        [Test]
        public void RuntimeCatalog_ResolvesIntroNextScene_FromResourcePackage()
        {
            var nextScene = StoryPackageRuntimeCatalog.GetNextSceneOrNull(TutorialSceneCatalog.IntroSceneName);

            Assert.That(nextScene, Is.EqualTo("ChickenGame"));
        }

        [Test]
        public void RuntimeCatalog_ProvidesPostChickenDisplayText_FromResourcePackage()
        {
            var found = StoryPackageRuntimeCatalog.TryGetCutsceneDisplayText(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                out var title,
                out var body);

            Assert.That(found, Is.True);
            Assert.That(title, Is.EqualTo("Post Chicken Bridge"));
            Assert.That(body, Is.EqualTo("Nice work. The carrot beds are finally ready for you."));
        }

        [Test]
        public void RuntimeCatalog_ProvidesPostChickenStoryboard_FromResourcePackage()
        {
            var found = StoryPackageRuntimeCatalog.TryGetStoryboard(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                out var title,
                out var storyboard);

            Assert.That(found, Is.True);
            Assert.That(title, Is.EqualTo("Post Chicken Bridge"));
            Assert.That(storyboard, Is.Not.Null);
            Assert.That(storyboard.StylePresetId, Is.EqualTo("farm_storybook_v1"));
            Assert.That(storyboard.Shots, Has.Length.EqualTo(3));
            Assert.That(storyboard.Shots[0].AudioResourcePath, Does.Contain("GeneratedStoryboards/storypkg_intro_chicken_sample/post_chicken_bridge/shot_01"));
        }

        [Test]
        public void RuntimeCatalog_ProvidesPreFarmStoryboard_FromResourcePackage()
        {
            var found = StoryPackageRuntimeCatalog.TryGetStoryboard(
                TutorialSceneCatalog.PreFarmCutsceneSceneName,
                out var title,
                out var storyboard);

            Assert.That(found, Is.True);
            Assert.That(title, Is.EqualTo("Pre-Farm Bridge"));
            Assert.That(storyboard, Is.Not.Null);
            Assert.That(storyboard.StylePresetId, Is.EqualTo("farm_storybook_v1"));
            Assert.That(storyboard.Shots, Has.Length.EqualTo(3));
            Assert.That(storyboard.Shots[0].SubtitleText, Does.Contain("starter tools"));
        }

        [Test]
        public void RuntimeCatalog_TryGetNextScene_FindsTerminalPackageBeat()
        {
            var found = StoryPackageRuntimeCatalog.TryGetNextScene(
                TutorialSceneCatalog.PreFarmCutsceneSceneName,
                out var nextScene);

            Assert.That(found, Is.True);
            Assert.That(nextScene, Is.Empty);
        }

        [Test]
        public void RuntimeCatalog_ProvidesFarmTutorialMinigameConfig_FromResourcePackage()
        {
            var found = StoryPackageRuntimeCatalog.TryGetMinigameConfig(
                TutorialSceneCatalog.FarmTutorialSceneName,
                out var title,
                out var minigame);

            Assert.That(found, Is.True);
            Assert.That(title, Is.EqualTo("Plant Rows Intro"));
            Assert.That(minigame, Is.Not.Null);
            Assert.That(minigame.AdapterId, Is.EqualTo("tutorial.plant_rows"));
            Assert.That(minigame.RequiredCount, Is.EqualTo(3));
            Assert.That(minigame.TimeLimitSeconds, Is.EqualTo(300f));
            Assert.That(minigame.ResolvedParameterEntries, Is.Not.Null);
            Assert.That(minigame.ResolvedParameterEntries, Has.Length.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void RuntimeCatalog_ProvidesFindToolsMinigameConfig_FromResourcePackage()
        {
            var found = StoryPackageRuntimeCatalog.TryGetMinigameConfig(
                "FindToolsGame",
                out var title,
                out var minigame);

            Assert.That(found, Is.True);
            Assert.That(title, Is.EqualTo("Find Tools Intro"));
            Assert.That(minigame, Is.Not.Null);
            Assert.That(minigame.AdapterId, Is.EqualTo("tutorial.find_tools"));
            Assert.That(minigame.RequiredCount, Is.EqualTo(2));
            Assert.That(minigame.TimeLimitSeconds, Is.EqualTo(240f));
            Assert.That(minigame.TryGetStringParameter("targetToolSet", out var targetToolSet), Is.True);
            Assert.That(targetToolSet, Is.EqualTo("starter"));
        }

        [Test]
        public void Installer_DoesNotInjectCutscene_OnCoreScene()
        {
            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();

            TutorialSceneInstaller.InstallForScene(TutorialSceneCatalog.CoreSceneSceneName, controller);

            var cutsceneController = Object.FindFirstObjectByType<TutorialCutsceneSceneController>();
            Assert.That(cutsceneController, Is.Null);
        }

        [Test]
        public void RuntimeCatalog_ResolvesPostChickenNextScene_FromResourcePackage()
        {
            var nextScene = StoryPackageRuntimeCatalog.GetNextSceneOrNull(TutorialSceneCatalog.PostChickenCutsceneSceneName);

            Assert.That(nextScene, Is.EqualTo(TutorialSceneCatalog.CoreSceneSceneName));
        }

        [Test]
        public void Installer_DoesNotInjectStoryboardCutscene_OnPostChickenScene()
        {
            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();

            TutorialSceneInstaller.InstallForScene(TutorialSceneCatalog.PostChickenCutsceneSceneName, controller);

            var cutsceneController = Object.FindFirstObjectByType<TutorialCutsceneSceneController>();
            Assert.That(cutsceneController, Is.Null);
        }

        [Test]
        public void StoryboardController_PlayAudio_CreatesAudioSource_WhenMissing()
        {
            var sceneObject = new GameObject("PostChickenCutscene");
            var controller = sceneObject.AddComponent<TutorialCutsceneSceneController>();

            Assert.That(sceneObject.GetComponent<AudioSource>(), Is.Null);

            Assert.That(
                () => InvokePrivate(controller, "PlayAudio", (object)null),
                Throws.Nothing);

            Assert.That(sceneObject.GetComponent<AudioSource>(), Is.Not.Null);
        }

        private static string ReadPrivateString(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(instance) as string;
        }

        private static T ReadPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(instance);
        }

        private static object InvokePrivate(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(instance, args);
        }
    }
}
