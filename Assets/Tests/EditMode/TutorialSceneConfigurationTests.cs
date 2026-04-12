using System.Reflection;
using System.Linq;
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
using UnityEngine.UI;
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
        public void TitleScreenScene_StartGameTargetsFirstTutorialSlice()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/TitleScreen.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            var manager = Object.FindFirstObjectByType<TitleScreenManager>();
            Assert.That(manager, Is.Not.Null);

            var serializedObject = new SerializedObject(manager);
            var targetSceneName = serializedObject.FindProperty("targetSceneName");

            Assert.That(targetSceneName, Is.Not.Null);
            Assert.That(targetSceneName.stringValue, Is.EqualTo(SceneWorkCatalog.FirstTutorialSceneName));
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
        public void PlayModeStartScene_IsConfiguredToTitleScreen()
        {
            var sceneAsset = PlayModeStartSceneConfigurator.Apply();

            Assert.That(sceneAsset, Is.Not.Null);
            Assert.That(EditorSceneManager.playModeStartScene, Is.SameAs(sceneAsset));
            Assert.That(AssetDatabase.GetAssetPath(sceneAsset), Is.EqualTo(PlayModeStartSceneConfigurator.TitleScreenScenePath));
        }

        [Test]
        public void CreateTitleScene_UsesSharedTutorialBuildPathOrder()
        {
            Assert.That(
                CreateTitleScene.GetOrderedBuildScenePaths(),
                Is.EqualTo(new[]
                {
                    "Assets/_Project/Scenes/TitleScreen.unity",
                    "Assets/_Project/Scenes/Intro.unity",
                    "Assets/_Project/Scenes/ChickenGame.unity",
                    "Assets/_Project/Scenes/Tutorial_PostChickenCutscene.unity",
                    "Assets/_Project/Scenes/Tutorial_MidpointPlaceholder.unity",
                    "Assets/_Project/Scenes/FindToolsGame.unity",
                    "Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity",
                    "Assets/_Project/Scenes/FarmMain.unity",
                    "Assets/_Project/Scenes/HorseTrainingGame.unity",
                    "Assets/_Project/Scenes/FarmVegetableStates.unity",
                    "Assets/_Project/Scenes/WorldMain.unity",
                }));
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

        [Test]
        public void SceneWorkLabelOverlay_ResolvesIntroSceneAssignment()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Intro.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            var overlayObject = new GameObject("SceneWorkLabelOverlay");
            var overlay = overlayObject.AddComponent<SceneWorkLabelOverlay>();

            Assert.That(overlay.TryGetCurrentScene(out var definition), Is.True);
            Assert.That(definition.Number, Is.EqualTo(1));
            Assert.That(definition.DisplayName, Is.EqualTo("Intro Cutscene"));
        }

        [Test]
        public void RuntimeTutorialOverlays_DefaultToHidden()
        {
            var shortcuts = new GameObject("TutorialDevShortcuts").AddComponent<TutorialDevShortcuts>();
            var labels = new GameObject("SceneWorkLabelOverlay").AddComponent<SceneWorkLabelOverlay>();

            Assert.That(ReadPrivateBool(shortcuts, "showOverlay"), Is.False);
            Assert.That(ReadPrivateBool(labels, "showOverlay"), Is.False);
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

        [Test]
        public void TitleScreenManager_StartBuildsTutorialSliceLauncherFromSharedSceneCatalog()
        {
            var canvasObject = new GameObject("Canvas");
            canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var managerObject = new GameObject("TitleScreenManager");
            managerObject.AddComponent<AudioSource>();
            var manager = managerObject.AddComponent<TitleScreenManager>();

            InvokePrivateInstance(manager, "Start");

            var launcherRoot = GameObject.Find(TitleScreenManager.TutorialSliceLauncherRootName);
            Assert.That(launcherRoot, Is.Not.Null);

            var buttons = launcherRoot.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.EqualTo(SceneWorkCatalog.TitleScreenLaunchableScenes.Count));

            var labels = buttons
                .Select(button => button.GetComponentInChildren<Text>())
                .Select(text => text != null ? text.text : string.Empty)
                .ToArray();

            Assert.That(
                labels,
                Is.EqualTo(SceneWorkCatalog.TitleScreenLaunchableScenes
                    .Select(scene => $"{scene.NumberLabel} {scene.DisplayName}")
                    .ToArray()));
        }

        [Test]
        public void HorseTrainingController_StartBuildsTrainingGroundSlice()
        {
            var gameObject = new GameObject("TutorialHorseTrainingSceneController");
            var controller = gameObject.AddComponent<TutorialHorseTrainingSceneController>();

            InvokePrivateInstance(controller, "Start");

            Assert.That(GameObject.Find("HorseTrainingGrounds_Root"), Is.Not.Null);
            Assert.That(GameObject.Find("HorseProxy"), Is.Not.Null);
            Assert.That(GameObject.Find("HorseTrainingCourse"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FirstPersonExplorer>(), Is.Not.Null);
        }

        [Test]
        public void FarmVegetableStatesScene_ContainsReviewControllerAndShowcaseRoot()
        {
            var scene = EditorSceneManager.OpenScene(SceneWorkCatalog.FarmVegetableStatesScenePath, OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(Object.FindFirstObjectByType<FarmVegetableStatesSceneController>(), Is.Not.Null);
            Assert.That(GameObject.Find("VegetableStates_Showcase"), Is.Not.Null);
            Assert.That(GameObject.Find("TomatoState_Tomato_02a"), Is.Not.Null);
            Assert.That(GameObject.Find("CornState_Corn_06"), Is.Not.Null);
            Assert.That(GameObject.Find("WheatState_Wheat_04a"), Is.Not.Null);
        }

        [Test]
        public void FarmVegetableStatesController_StartBuildsReviewEnvironmentAndRig()
        {
            var gameObject = new GameObject("FarmVegetableStatesSceneController");
            var controller = gameObject.AddComponent<FarmVegetableStatesSceneController>();

            InvokePrivateInstance(controller, "Start");

            Assert.That(GameObject.Find("FarmVegetableStates_Root"), Is.Not.Null);
            Assert.That(GameObject.Find("SpawnPoint"), Is.Not.Null);
            Assert.That(GameObject.Find("Ground"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FirstPersonExplorer>(), Is.Not.Null);
        }

        private static void InvokePrivateInstance(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(target, null);
        }

        private static bool ReadPrivateBool(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return (bool)field.GetValue(target);
        }
    }
}
