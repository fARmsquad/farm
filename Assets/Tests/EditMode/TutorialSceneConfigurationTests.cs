using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using FarmSimVR.Core;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.Editor;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Autoplay;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Farming;
using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;
using TMPro;
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
        public void PlayModeStartScene_DoesNotForceFixedScene()
        {
            Assert.That(
                EditorSceneManager.playModeStartScene,
                Is.Null,
                "Play Mode should use the scene open in the Editor, not a forced asset.");
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
                    "Assets/_Project/Scenes/Town.unity",
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

        [TestCase("Tutorial_PostChickenCutscene", TutorialStep.PostChickenCutscene)]
        [TestCase("Tutorial_MidpointPlaceholder", TutorialStep.MidpointPlaceholder)]
        [TestCase("FindToolsGame", TutorialStep.FindTools)]
        [TestCase("Tutorial_PreFarmCutscene", TutorialStep.PreFarmCutscene)]
        public void TutorialSceneCatalog_NormalizesRuntimeSceneAliases(string runtimeSceneName, TutorialStep expectedStep)
        {
            Assert.That(TutorialSceneCatalog.GetStepForScene(runtimeSceneName), Is.EqualTo(expectedStep));
        }

        [Test]
        public void TitleScreenManager_StartBuildsTutorialSliceLauncherFromSharedSceneCatalogAndStoryPackageSampleEntry()
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
            Assert.That(buttons.Length, Is.EqualTo(SceneWorkCatalog.TitleScreenLaunchableScenes.Count + 1));

            var labels = buttons
                .Select(button => button.GetComponentInChildren<Text>())
                .Select(text => text != null ? text.text : string.Empty)
                .ToArray();

            Assert.That(
                labels,
                Is.EqualTo((new[] { TitleScreenManager.StoryPackageSampleLabel }).Concat(
                    SceneWorkCatalog.TitleScreenLaunchableScenes
                        .Select(scene => $"{scene.NumberLabel} {scene.DisplayName}"))
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

        [Test]
        public void TownScene_ContainsPlayableConversationSlice()
        {
            var scene = EditorSceneManager.OpenScene(SceneWorkCatalog.TownScenePath, OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(Object.FindFirstObjectByType<LLMConversationController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<DialogueChoiceUI>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<TownPlayerController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<TownInteractionAutoplay>(), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                SceneWorkCatalog.TitleScreenLaunchableScenes.Any(sceneDefinition => sceneDefinition.SceneName == SceneWorkCatalog.TownSceneName),
                Is.True);
        }

        [Test]
        public void NPCPersonaCatalog_PromptsDirectNpcSpeechInsteadOfJson()
        {
            string prompt = FarmSimVR.Core.NPCPersonaCatalog.GetSystemPrompt("Old Garrett");

            Assert.That(prompt, Does.Contain("spoken reply"));
            Assert.That(prompt, Does.Not.Contain("valid JSON"));
            Assert.That(prompt, Does.Not.Contain("\"response\""));
        }

        [Test]
        public void OpenAIClient_BuildRequestJson_UsesResponsesTextStreamingShape()
        {
            var gameObject = new GameObject("OpenAIClient");
            var client = gameObject.AddComponent<OpenAIClient>();

            SetPrivateField(client, "model", "gpt-4o-mini");

            string json = InvokePrivateInstance<string>(
                client,
                "BuildRequestJson",
                new List<FarmSimVR.Core.ChatMessage>
                {
                    new("system", "You are a helper."),
                    new("user", "Hello there."),
                    new("assistant", "Howdy, traveler.")
                },
                true);

            Assert.That(json, Does.Contain("\"stream\":true"));
            Assert.That(json, Does.Contain("\"input\":"));
            Assert.That(json, Does.Contain("\"text\":{\"format\":{\"type\":\"text\"}}"));
            Assert.That(json, Does.Not.Contain("\"messages\":"));
            Assert.That(json, Does.Contain("\"role\":\"assistant\",\"content\":\"Howdy, traveler.\""));
            Assert.That(json, Does.Not.Contain("\"type\":\"input_text\""));
        }

        [Test]
        public void LLMConversationController_TryParseResponse_ExtractsLegacyJsonPayload()
        {
            const string payload =
                "{\"response\":\"Oh, bless my soul! I do love makin' tomato pie.\"," +
                "\"options\":[\"Can you share the recipe?\",\"What other dishes do you recommend?\",\"Goodbye.\"]}";

            object parsed = InvokePrivateStatic<object>(
                typeof(LLMConversationController),
                "TryParseResponse",
                payload);

            Assert.That(parsed, Is.Not.Null);
            Assert.That(
                ReadField<string>(parsed, "response"),
                Is.EqualTo("Oh, bless my soul! I do love makin' tomato pie."));
            Assert.That(
                ReadField<string[]>(parsed, "options"),
                Is.EqualTo(new[]
                {
                    "Can you share the recipe?",
                    "What other dishes do you recommend?",
                    "Goodbye."
                }));
        }

        [Test]
        public void FarmMainScene_FarmControllerHasNoMissingScripts()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/FarmMain.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            var farmController = GameObject.Find("FarmController");
            Assert.That(farmController, Is.Not.Null);
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(farmController), Is.EqualTo(0));
            Assert.That(farmController.GetComponents<TutorialFarmSceneController>().Length, Is.EqualTo(1));
            Assert.That(farmController.GetComponent<FarmSimDriver>(), Is.Not.Null);
        }

        [Test]
        public void FarmMainToolWrapperPrefabs_AreStandaloneAssets()
        {
            var wrappers = new[]
            {
                ("Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Bucket.prefab", "6b58c11b87f85e144aeaa25c638a21f3"),
                ("Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Hoe.prefab", "b78750eed6179e54f85d89fd770e6cc9"),
                ("Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Sickle.prefab", "b83de2dc15ae7724d813d14ee94d8522"),
                ("Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/Spade.prefab", "8e0274d0422891640b7ee8811ae16a5f"),
                ("Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/SpadingFork.prefab", "70dc12851c11b444aa5573d6ea1e3292"),
            };

            foreach (var (path, missingNestedSourceGuid) in wrappers)
            {
                var text = File.ReadAllText(path);

                Assert.That(text.Contains("PrefabInstance:"), Is.False, $"{path} should be a standalone prefab asset.");
                Assert.That(text.Contains("m_SourcePrefab:"), Is.False, $"{path} should not chain through a nested source prefab.");
                Assert.That(text.Contains(missingNestedSourceGuid), Is.False, $"{path} still references missing nested source {missingNestedSourceGuid}.");
            }
        }

        [Test]
        public void FarmMainScene_ContainsVisibleTutorialTomatoPatch()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/FarmMain.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            var heroPatch = GameObject.Find("CropPlot_TutorialTomato");
            Assert.That(heroPatch, Is.Not.Null);
            Assert.That(heroPatch.CompareTag("CropPlot"), Is.True);
            Assert.That(heroPatch.GetComponent<CropPlotController>(), Is.Not.Null);
            Assert.That(heroPatch.GetComponent<CropVisualUpdater>(), Is.Not.Null);

            var playerSpawn = GameObject.Find("PlayerSpawn");
            Assert.That(playerSpawn, Is.Not.Null);
            Assert.That(
                Vector3.Distance(playerSpawn.transform.position, heroPatch.transform.position),
                Is.LessThanOrEqualTo(2.5f),
                "The tutorial tomato plot should be immediately reachable from the spawn area.");

            var tutorialController = Object.FindFirstObjectByType<TutorialFarmSceneController>();
            Assert.That(tutorialController, Is.Not.Null);

            var serializedController = new SerializedObject(tutorialController);
            var heroCropPlot = serializedController.FindProperty("_heroCropPlot");

            Assert.That(heroCropPlot, Is.Not.Null);
            Assert.That(heroCropPlot.objectReferenceValue, Is.EqualTo(heroPatch));
        }

        private static void InvokePrivateInstance(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(target, args);
        }

        private static T InvokePrivateInstance<T>(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            return (T)method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static T InvokePrivateStatic<T>(System.Type targetType, string methodName, params object[] args)
        {
            var method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing private static method '{methodName}'.");
            return (T)method.Invoke(null, args);
        }

        private static T ReadField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
            return (T)field.GetValue(target);
        }

        private static bool ReadPrivateBool(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return (bool)field.GetValue(target);
        }
    }
}
