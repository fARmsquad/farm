using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using FarmSimVR.Core;
using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Story;
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
            DestroyPersistentRuntimeControllers();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            DestroyPersistentRuntimeControllers();
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
        public void FindToolsGameScene_StartUsesPackageDrivenToolRecoveryBeat_WhenStoryBeatExists()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/PlayerCollectTools.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            StoryPackageRuntimeCatalog.ResetCacheForTests();

            var gameObject = new GameObject("TutorialFindToolsSceneController");
            var controller = gameObject.AddComponent<TutorialFindToolsSceneController>();

            InvokePrivateInstance(controller, "Start");

            Assert.That(controller.CurrentObjectiveText, Does.Contain("Find the 3 hidden tools."));

            var pickups = Object.FindObjectsByType<TutorialToolPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(pickups.Length, Is.EqualTo(3));
            Assert.That(pickups.All(pickup => !string.IsNullOrWhiteSpace(pickup.ToolName)), Is.True);
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
                    "Assets/_Project/Scenes/CaughtChickenCutscene.unity",
                    "Assets/_Project/Scenes/PlayerCollectTools.unity",
                    "Assets/_Project/Scenes/PlayerGettingSeeds.unity",
                    "Assets/_Project/Scenes/CoreScene.unity",
                    "Assets/_Project/Scenes/HorseTrainingGame.unity",
                    "Assets/_Project/Scenes/Town.unity",
                    "Assets/_Project/Scenes/FarmVegetableStates.unity",
                    "Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity",
                    "Assets/_Project/Scenes/FarmMain.unity",
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
            Assert.That(completionSceneName.stringValue, Is.EqualTo(TutorialSceneCatalog.ChickenGameSceneName));

            Assert.That(playbackSpeed, Is.Not.Null);
            Assert.That(playbackSpeed.floatValue, Is.EqualTo(TutorialDevTuning.IntroCutscenePlaybackSpeed));
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
        [TestCase("CaughtChickenCutscene")]
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
        public void Installer_PostChickenCutscene_UsesRuntimeStoryboardAndDisablesAuthoredSlideshow_WhenGeneratedBeatExists()
        {
            StoryPackageRuntimeCatalog.ResetCacheForTests();

            var imported = StoryPackageRuntimeCatalog.TrySetRuntimeOverride(
                BuildGeneratedTitleDiagnosticsPackage(),
                out var importError);
            Assert.That(imported, Is.True, importError);

            var slideshowPanel = new GameObject("SlideshowPanel");
            var slideshowDirector = new GameObject("SlideshowDirector");

            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();

            TutorialSceneInstaller.InstallForScene(TutorialSceneCatalog.PostChickenCutsceneSceneName, controller);

            var cutsceneController = Object.FindFirstObjectByType<TutorialCutsceneSceneController>();
            Assert.That(cutsceneController, Is.Not.Null);
            Assert.That(cutsceneController.gameObject.name, Is.EqualTo(TutorialSceneCatalog.PostChickenCutsceneSceneName));

            var shots = ReadField<StoryStoryboardShotSnapshot[]>(cutsceneController, "_storyboardShots");
            Assert.That(shots, Is.Not.Null);
            Assert.That(shots.Length, Is.EqualTo(1));
            Assert.That(shots[0].SubtitleText, Is.EqualTo("Garrett points toward the next task."));

            Assert.That(slideshowPanel.activeSelf, Is.False);
            Assert.That(slideshowDirector.activeSelf, Is.False);
        }

        [TestCase("Tutorial_PostChickenCutscene", TutorialStep.PostChickenCutscene)]
        [TestCase("CaughtChickenCutscene", TutorialStep.PostChickenCutscene)]
        [TestCase("Tutorial_MidpointPlaceholder", TutorialStep.MidpointPlaceholder)]
        [TestCase(TutorialSceneCatalog.CoreSceneSceneName, TutorialStep.MidpointPlaceholder)]
        [TestCase("FindToolsGame", TutorialStep.FindTools)]
        [TestCase("Tutorial_PreFarmCutscene", TutorialStep.PreFarmCutscene)]
        public void TutorialSceneCatalog_NormalizesRuntimeSceneAliases(string runtimeSceneName, TutorialStep expectedStep)
        {
            Assert.That(TutorialSceneCatalog.GetStepForScene(runtimeSceneName), Is.EqualTo(expectedStep));
        }

        [Test]
        public void TitleScreenManager_StartBuildsGenerateAndPlayUniquePlaythroughButtons()
        {
            Assert.That(
                typeof(TitleScreenManager)
                    .GetField("StoryPackageSampleSceneName", BindingFlags.NonPublic | BindingFlags.Static)?
                    .GetRawConstantValue(),
                Is.EqualTo(TutorialSceneCatalog.PostChickenCutsceneSceneName));

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
            Assert.That(buttons.Length, Is.EqualTo(SceneWorkCatalog.TitleScreenLaunchableScenes.Count + 2));

            var labels = buttons
                .Select(button => button.GetComponentInChildren<Text>())
                .Select(text => text != null ? text.text : string.Empty)
                .ToArray();

            Assert.That(
                labels,
                Is.EqualTo((new[] { "Generate Unique Playthrough", "Play Unique Playthrough" }).Concat(
                    SceneWorkCatalog.TitleScreenLaunchableScenes
                        .Select(scene => $"{scene.NumberLabel} {scene.DisplayName}"))
                    .ToArray()));

            var playButton = GameObject.Find("TutorialSlice_PlayUniquePlaythrough")?.GetComponent<Button>();
            Assert.That(playButton, Is.Not.Null);
            Assert.That(playButton.interactable, Is.False);

            var statusLabel = GameObject.Find(TitleScreenManager.GeneratedStorySliceStatusName)?.GetComponent<Text>();
            Assert.That(statusLabel, Is.Not.Null);
            Assert.That(statusLabel.text, Does.Contain("State: Idle"));
            Assert.That(statusLabel.text, Does.Contain("Session: none"));
        }

        [Test]
        public void TitleScreenManager_CreateTutorialSliceLauncher_IsNotEditorOnly()
        {
            var source = File.ReadAllText("Assets/_Project/Scripts/MonoBehaviours/TitleScreenManager.cs");
            var methodIndex = source.IndexOf("private void CreateTutorialSliceLauncher()", StringComparison.Ordinal);

            Assert.That(methodIndex, Is.GreaterThanOrEqualTo(0));

            var guardIndex = source.IndexOf("#if UNITY_EDITOR || DEVELOPMENT_BUILD", StringComparison.Ordinal);
            var endIfIndex = source.IndexOf("#endif", StringComparison.Ordinal);

            Assert.That(
                guardIndex < 0 || guardIndex > methodIndex || endIfIndex < methodIndex,
                Is.True,
                "CreateTutorialSliceLauncher should ship in non-development builds.");
        }

        [Test]
        public void TitleScreenManager_Start_WarmsLocalStoryOrchestratorInBackground_FromSource()
        {
            var source = File.ReadAllText("Assets/_Project/Scripts/MonoBehaviours/TitleScreenManager.cs");

            Assert.That(
                source,
                Does.Contain("StorySequenceRuntimeController.GetOrCreate().EnsureLocalOrchestratorRunningInBackground();"));
        }

        [Test]
        public void TitleScreenManager_GenerateUniquePlaythrough_ShowsVisibleLoadingStateImmediately()
        {
            var canvasObject = new GameObject("Canvas");
            canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var managerObject = new GameObject("TitleScreenManager");
            managerObject.AddComponent<AudioSource>();
            var manager = managerObject.AddComponent<TitleScreenManager>();

            InvokePrivateInstance(manager, "Start");

            var runtimeController = StorySequenceRuntimeController.GetOrCreate();
            SetPrivateField(
                runtimeController,
                "_beginSequenceRequestOverride",
                (Func<System.Action<StorySequenceAdvancePayload>, System.Collections.IEnumerator>)(_ => HoldForever()));

            var generateButton = GameObject.Find("TutorialSlice_GenerateUniquePlaythrough")?.GetComponent<Button>();
            var playButton = GameObject.Find("TutorialSlice_PlayUniquePlaythrough")?.GetComponent<Button>();
            Assert.That(generateButton, Is.Not.Null);
            Assert.That(playButton, Is.Not.Null);

            generateButton.onClick.Invoke();

            var loadingOverlay = ReadField<GameObject>(manager, "generatedStoryLoadingOverlay");
            var statusLabel = GameObject.Find(TitleScreenManager.GeneratedStorySliceStatusName)?.GetComponent<Text>();

            Assert.That(ReadPrivateBool(manager, "isTransitioning"), Is.True);
            Assert.That(loadingOverlay, Is.Not.Null);
            Assert.That(loadingOverlay.name, Is.EqualTo(TitleScreenManager.GeneratedStorySliceLoadingOverlayName));
            Assert.That(loadingOverlay.activeSelf, Is.True);
            Assert.That(generateButton.interactable, Is.False);
            Assert.That(playButton.interactable, Is.False);
            Assert.That(statusLabel, Is.Not.Null);
            Assert.That(statusLabel.text, Does.Contain("Generating unique playthrough"));
            Assert.That(statusLabel.text, Does.Contain("State: Generating"));
            Assert.That(statusLabel.text, Does.Contain("Session: pending"));
        }

        [Test]
        public void TitleScreenManager_HandleGeneratedPlaythroughPrepared_EnablesPlayButtonAndShowsReadyState()
        {
            var canvasObject = new GameObject("Canvas");
            canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var managerObject = new GameObject("TitleScreenManager");
            managerObject.AddComponent<AudioSource>();
            var manager = managerObject.AddComponent<TitleScreenManager>();

            InvokePrivateInstance(manager, "Start");

            var runtimeController = StorySequenceRuntimeController.GetOrCreate();
            SetPrivateField(runtimeController, "_activeSessionId", "session-title-diagnostics");
            SetPrivateField(runtimeController, "_preparedEntrySceneName", TutorialSceneCatalog.PostChickenCutsceneSceneName);

            var imported = StoryPackageRuntimeCatalog.TrySetRuntimeOverride(
                BuildGeneratedTitleDiagnosticsPackage(),
                out var importError);
            Assert.That(imported, Is.True, importError);

            InvokePrivateInstance(manager, "HandleGeneratedStorySlicePrepared", TutorialSceneCatalog.PostChickenCutsceneSceneName);

            var generateButton = GameObject.Find("TutorialSlice_GenerateUniquePlaythrough")?.GetComponent<Button>();
            var playButton = GameObject.Find("TutorialSlice_PlayUniquePlaythrough")?.GetComponent<Button>();
            var statusLabel = GameObject.Find(TitleScreenManager.GeneratedStorySliceStatusName)?.GetComponent<Text>();

            Assert.That(ReadPrivateBool(manager, "isTransitioning"), Is.False);
            Assert.That(generateButton, Is.Not.Null);
            Assert.That(generateButton.interactable, Is.True);
            Assert.That(playButton, Is.Not.Null);
            Assert.That(playButton.interactable, Is.True);
            Assert.That(statusLabel, Is.Not.Null);
            Assert.That(statusLabel.text, Does.Contain("ready").IgnoreCase);
            Assert.That(statusLabel.text, Does.Contain("State: Ready"));
            Assert.That(statusLabel.text, Does.Contain("Session: session-title-diagnostics"));
            Assert.That(statusLabel.text, Does.Contain("Package: Generated Diagnostics Package"));
            Assert.That(statusLabel.text, Does.Contain("Beat: Generated Diagnostics Bridge"));
            Assert.That(statusLabel.text, Does.Contain("Shots: 1"));
        }

        [Test]
        public void TitleScreenManager_Update_RestoresPreparedGeneratedPlaythroughStateFromRuntimeController()
        {
            var canvasObject = new GameObject("Canvas");
            canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var managerObject = new GameObject("TitleScreenManager");
            managerObject.AddComponent<AudioSource>();
            var manager = managerObject.AddComponent<TitleScreenManager>();

            InvokePrivateInstance(manager, "Start");

            var runtimeController = StorySequenceRuntimeController.GetOrCreate();
            SetPrivateField(runtimeController, "_activeSessionId", "session-runtime-recovery");
            SetPrivateField(runtimeController, "_preparedEntrySceneName", TutorialSceneCatalog.PostChickenCutsceneSceneName);

            var imported = StoryPackageRuntimeCatalog.TrySetRuntimeOverride(
                BuildGeneratedTitleDiagnosticsPackage(),
                out var importError);
            Assert.That(imported, Is.True, importError);

            InvokePrivateInstance(manager, "Update");

            var generateButton = GameObject.Find("TutorialSlice_GenerateUniquePlaythrough")?.GetComponent<Button>();
            var playButton = GameObject.Find("TutorialSlice_PlayUniquePlaythrough")?.GetComponent<Button>();
            var statusLabel = GameObject.Find(TitleScreenManager.GeneratedStorySliceStatusName)?.GetComponent<Text>();

            Assert.That(generateButton, Is.Not.Null);
            Assert.That(generateButton.interactable, Is.True);
            Assert.That(playButton, Is.Not.Null);
            Assert.That(playButton.interactable, Is.True);
            Assert.That(statusLabel, Is.Not.Null);
            Assert.That(statusLabel.text, Does.Contain("State: Ready"));
            Assert.That(statusLabel.text, Does.Contain("Session: session-runtime-recovery"));
            Assert.That(statusLabel.text, Does.Contain("Beat: Generated Diagnostics Bridge"));
        }

        [Test]
        public void TitleScreenManager_TransitionToGame_UnlocksTitleWhenGeneratedSliceRequestDoesNotStart()
        {
            var canvasObject = new GameObject("Canvas");
            canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var managerObject = new GameObject("TitleScreenManager");
            managerObject.AddComponent<AudioSource>();
            var manager = managerObject.AddComponent<TitleScreenManager>();

            InvokePrivateInstance(manager, "Start");

            var runtimeController = StorySequenceRuntimeController.GetOrCreate();
            SetPrivateField(runtimeController, "_requestInFlight", true);
            SetPrivateField(manager, "targetSceneName", TutorialSceneCatalog.PostChickenCutsceneSceneName);
            SetPrivateField(manager, "launchGeneratedStorySlice", true);
            SetPrivateField(manager, "isTransitioning", true);

            var routine = InvokePrivateInstance<System.Collections.IEnumerator>(manager, "TransitionToGame");
            while (routine.MoveNext())
            {
            }

            var generateButton = GameObject.Find("TutorialSlice_GenerateUniquePlaythrough")?.GetComponent<Button>();
            var playButton = GameObject.Find("TutorialSlice_PlayUniquePlaythrough")?.GetComponent<Button>();
            var statusLabel = GameObject.Find(TitleScreenManager.GeneratedStorySliceStatusName)?.GetComponent<Text>();

            Assert.That(ReadPrivateBool(manager, "isTransitioning"), Is.False);
            Assert.That(generateButton, Is.Not.Null);
            Assert.That(generateButton.interactable, Is.True);
            Assert.That(playButton, Is.Not.Null);
            Assert.That(playButton.interactable, Is.False);
            Assert.That(statusLabel, Is.Not.Null);
            Assert.That(statusLabel.text, Does.Contain("already starting"));
        }

        [TestCase(TutorialSceneCatalog.IntroSceneName, "Intro")]
        [TestCase(TutorialSceneCatalog.PostChickenCutsceneSceneName, "CaughtChickenCutscene")]
        [TestCase(TutorialSceneCatalog.CoreSceneSceneName, "CoreScene")]
        [TestCase(TutorialSceneCatalog.PreFarmCutsceneSceneName, "Tutorial_PreFarmCutscene")]
        public void SceneWorkCatalog_GetLoadableSceneName_ResolvesBuildProfileSceneNames(
            string sceneName,
            string expectedLoadableSceneName)
        {
            Assert.That(
                SceneWorkCatalog.GetLoadableSceneName(sceneName),
                Is.EqualTo(expectedLoadableSceneName));
        }

        [Test]
        public void TutorialFlowController_ResolveLoadableSceneRequest_UsesBuildProfileNameForStoryPackageNextScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/CaughtChickenCutscene.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            StoryPackageRuntimeCatalog.ResetCacheForTests();

            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();

            Assert.That(
                controller.ResolveLoadableSceneRequest(TutorialSceneCatalog.FindToolsSceneName),
                Is.EqualTo("PlayerCollectTools"));
        }

        [Test]
        public void TutorialFlowController_CompleteCurrentSceneAndLoadNext_ShowsCompletionBanner_ForTerminalStoryPackageBeat()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/CoreScene.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            StoryPackageRuntimeCatalog.ResetCacheForTests();

            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();
            var flow = new TutorialFlowService();
            flow.EnterScene(TutorialSceneCatalog.CoreSceneSceneName);
            SetPrivateField(controller, "<Flow>k__BackingField", flow);
            SetPrivateField(controller, "<ToolRecovery>k__BackingField", new ToolRecoveryService());

            controller.CompleteCurrentSceneAndLoadNext();

            Assert.That(controller.ShowCompletionBanner, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("CoreScene"));
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

        [Test]
        public void FarmMainScene_StartUsesPackageDrivenPlantRowsObjective_WhenStoryBeatExists()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/FarmMain.unity", OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            StoryPackageRuntimeCatalog.ResetCacheForTests();

            var tutorialController = Object.FindFirstObjectByType<TutorialFarmSceneController>();
            Assert.That(tutorialController, Is.Not.Null);

            InvokePrivateInstance(tutorialController, "Start");

            Assert.That(tutorialController.CurrentObjectiveText, Does.Contain("Plant 3 carrots in 5 minutes."));

            var plots = Object.FindObjectsByType<CropPlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(plots.Length, Is.EqualTo(6));
            Assert.That(
                plots.Select(plot => plot.gameObject.name).Distinct().Count(),
                Is.EqualTo(6));
            Assert.That(
                plots.Count(plot => tutorialController.IsActionAllowed(FarmPlotAction.Till, plot)),
                Is.EqualTo(6));
        }

        private static void DestroyPersistentRuntimeControllers()
        {
            foreach (var controller in Object.FindObjectsByType<StorySequenceRuntimeController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(controller.gameObject);
            }
        }

        private static System.Collections.IEnumerator HoldForever()
        {
            while (true)
                yield return null;
        }

        private static StoryPackageSnapshot BuildGeneratedTitleDiagnosticsPackage()
        {
            return new StoryPackageSnapshot
            {
                PackageId = "storypkg_title_diagnostics",
                SchemaVersion = 1,
                PackageVersion = 1,
                DisplayName = "Generated Diagnostics Package",
                Beats = new[]
                {
                    new StoryBeatSnapshot
                    {
                        BeatId = "sequence_turn_000_cutscene",
                        DisplayName = "Generated Diagnostics Bridge",
                        Kind = "Cutscene",
                        SceneName = TutorialSceneCatalog.PostChickenCutsceneSceneName,
                        NextSceneName = TutorialSceneCatalog.FarmTutorialSceneName,
                        Storyboard = new StoryStoryboardSnapshot
                        {
                            StylePresetId = "farm_storybook_v1",
                            Shots = new[]
                            {
                                new StoryStoryboardShotSnapshot
                                {
                                    ShotId = "shot_01",
                                    SubtitleText = "Garrett points toward the next task.",
                                    NarrationText = "Garrett points toward the next task.",
                                    DurationSeconds = 3f,
                                    ImageResourcePath = "GeneratedStoryboards/storypkg_title_diagnostics/sequence_turn_000_cutscene/shot_01",
                                    AudioResourcePath = "GeneratedStoryboards/storypkg_title_diagnostics/sequence_turn_000_cutscene/shot_01",
                                },
                            },
                        },
                    },
                },
            };
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
