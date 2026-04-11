using System.Reflection;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.Editor;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class TutorialSceneConfigurationTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void TitleScreenScene_StartGameTargetsIntro()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/TitleScreen.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            var manager = Object.FindFirstObjectByType<TitleScreenManager>();
            Assert.That(manager, Is.Not.Null);

            var serializedObject = new SerializedObject(manager);
            var targetSceneName = serializedObject.FindProperty("targetSceneName");

            Assert.That(targetSceneName, Is.Not.Null);
            Assert.That(targetSceneName.stringValue, Is.EqualTo(TutorialSceneCatalog.IntroSceneName));
        }

        [Test]
        public void FindToolsController_StartBuildsWalkToSquarePlaceholder()
        {
            var gameObject = new GameObject("TutorialFindToolsSceneController");
            var controller = gameObject.AddComponent<TutorialFindToolsSceneController>();

            InvokePrivateInstance(controller, "Start");

            Assert.That(GameObject.Find("GoalSquare"), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<TutorialToolPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindFirstObjectByType<FirstPersonExplorer>(), Is.Not.Null);
        }

        [Test]
        public void PlayModeStartScene_IsConfiguredToIntro()
        {
            var sceneAsset = PlayModeStartSceneConfigurator.Apply();

            Assert.That(sceneAsset, Is.Not.Null);
            Assert.That(EditorSceneManager.playModeStartScene, Is.SameAs(sceneAsset));
            Assert.That(AssetDatabase.GetAssetPath(sceneAsset), Is.EqualTo(PlayModeStartSceneConfigurator.IntroScenePath));
        }

        [Test]
        public void IntroScene_AutoplayUsesDevPlaybackSpeedAndFarmFallback()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Intro.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            var autoPlay = Object.FindFirstObjectByType<IntroCinematicAutoPlay>();
            Assert.That(autoPlay, Is.Not.Null);

            var serializedObject = new SerializedObject(autoPlay);
            var completionSceneName = serializedObject.FindProperty("completionSceneName");
            var playbackSpeed = serializedObject.FindProperty("playbackSpeed");

            Assert.That(completionSceneName, Is.Not.Null);
            Assert.That(completionSceneName.stringValue, Is.EqualTo(TutorialSceneCatalog.FarmTutorialSceneName));

            Assert.That(playbackSpeed, Is.Not.Null);
            Assert.That(playbackSpeed.floatValue, Is.GreaterThan(1f));
        }

        [TestCase(TutorialSceneCatalog.PostChickenCutsceneSceneName)]
        [TestCase(TutorialSceneCatalog.MidpointPlaceholderSceneName)]
        [TestCase(TutorialSceneCatalog.PreFarmCutsceneSceneName)]
        public void Installer_ConfiguresPlaceholderCutscenesForQuickAutoAdvance(string sceneName)
        {
            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();

            TutorialSceneInstaller.InstallForScene(sceneName, controller);

            var cutsceneController = Object.FindFirstObjectByType<TutorialCutsceneSceneController>();
            Assert.That(cutsceneController, Is.Not.Null);

            var delayField = typeof(TutorialCutsceneSceneController).GetField("_autoAdvanceDelay", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(delayField, Is.Not.Null);

            var delay = (float)delayField.GetValue(cutsceneController);
            Assert.That(delay, Is.LessThanOrEqualTo(1.5f));
        }

        private static void InvokePrivateInstance(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(target, null);
        }
    }
}
