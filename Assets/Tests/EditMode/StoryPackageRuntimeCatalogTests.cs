using System.Reflection;
using FarmSimVR.Core.Tutorial;
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
            Assert.That(body, Is.EqualTo("Nice work. The farm still needs your hands."));
        }

        [Test]
        public void Installer_UsesStoryPackageText_WhenAvailable()
        {
            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();

            TutorialSceneInstaller.InstallForScene(TutorialSceneCatalog.PostChickenCutsceneSceneName, controller);

            var cutsceneController = Object.FindFirstObjectByType<TutorialCutsceneSceneController>();
            Assert.That(cutsceneController, Is.Not.Null);

            Assert.That(ReadPrivateString(cutsceneController, "_title"), Is.EqualTo("Post Chicken Bridge"));
            Assert.That(ReadPrivateString(cutsceneController, "_body"), Is.EqualTo("Nice work. The farm still needs your hands."));
        }

        private static string ReadPrivateString(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(instance) as string;
        }
    }
}
