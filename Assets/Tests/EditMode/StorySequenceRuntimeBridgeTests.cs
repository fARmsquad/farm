using System;
using System.Collections;
using System.Reflection;
using System.IO;
using FarmSimVR.Core.Story;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class StorySequenceRuntimeBridgeTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryPackageRuntimeCatalog.ResetCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            StoryPackageRuntimeCatalog.ResetCacheForTests();

            foreach (var controller in Object.FindObjectsByType<StorySequenceRuntimeController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(controller.gameObject);
            }

            foreach (var flow in Object.FindObjectsByType<TutorialFlowController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(flow.gameObject);
            }
        }

        [Test]
        public void StoryPackageNavigator_TryGetBeatBySceneName_PrefersExactSceneMatchOverTutorialAliasFallback()
        {
            var package = new StoryPackageSnapshot
            {
                PackageId = "storypkg_test",
                SchemaVersion = 1,
                PackageVersion = 1,
                Beats = new[]
                {
                    BuildCutsceneBeat(
                        "post_chicken_bridge",
                        "Tutorial_PostChickenCutscene",
                        "FarmMain",
                        "Authored Bridge",
                        "authored"),
                    BuildCutsceneBeat(
                        "sequence_turn_000_cutscene",
                        TutorialSceneCatalog.PostChickenCutsceneSceneName,
                        "FarmMain",
                        "Generated Bridge",
                        "generated"),
                },
            };

            var found = StoryPackageNavigator.TryGetBeatBySceneName(
                package,
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                out var beat);

            Assert.That(found, Is.True);
            Assert.That(beat, Is.Not.Null);
            Assert.That(beat.BeatId, Is.EqualTo("sequence_turn_000_cutscene"));
        }

        [Test]
        public void RuntimeCatalog_UsesRuntimeOverridePackage_WhenPresent()
        {
            var json = JsonUtility.ToJson(
                BuildPackage(
                    "Generated Bridge",
                    "sequence_turn_000_cutscene",
                    TutorialSceneCatalog.PostChickenCutsceneSceneName,
                    TutorialSceneCatalog.FarmTutorialSceneName,
                    "generated_01"));

            var imported = StoryPackageRuntimeCatalog.TrySetRuntimeOverrideJson(json, out var error);
            Assert.That(imported, Is.True, error);

            var found = StoryPackageRuntimeCatalog.TryGetStoryboard(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                out var title,
                out var storyboard);

            Assert.That(found, Is.True);
            Assert.That(title, Is.EqualTo("Generated Bridge"));
            Assert.That(storyboard, Is.Not.Null);
            Assert.That(storyboard.Shots, Has.Length.EqualTo(1));
            Assert.That(storyboard.Shots[0].ImageResourcePath, Does.Contain("generated_01"));
        }

        [Test]
        public void StorySequenceRuntimeController_BeginSequenceAndLoadRoutine_AppliesGeneratedPayloadAndLoadsEntryScene()
        {
            var runtimeObject = new GameObject("StorySequenceRuntime");
            var controller = runtimeObject.AddComponent<StorySequenceRuntimeController>();
            string loadedSceneName = null;

            SetEnsureReadySuccess(controller);

            SetPrivateField(
                controller,
                "_beginSequenceRequestOverride",
                (Func<Action<StorySequenceAdvancePayload>, IEnumerator>)(callback =>
                    CompleteImmediately(
                        callback,
                        new StorySequenceAdvancePayload(
                            "http://127.0.0.1:8012",
                            "session-001",
                            TutorialSceneCatalog.PostChickenCutsceneSceneName,
                            BuildPackage(
                                "Generated Bridge One",
                                "sequence_turn_000_cutscene",
                                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                                TutorialSceneCatalog.FarmTutorialSceneName,
                                "generated_begin")))));

            SetPrivateField(
                controller,
                "_sceneLoadOverride",
                (Action<string>)(sceneName => loadedSceneName = sceneName));

            var routine = InvokePrivate<IEnumerator>(
                controller,
                "BeginSequenceAndLoadRoutine",
                TutorialSceneCatalog.PostChickenCutsceneSceneName);

            Drain(routine);

            Assert.That(controller.ActiveSessionId, Is.EqualTo("session-001"));
            Assert.That(
                loadedSceneName,
                Is.EqualTo(SceneWorkCatalog.GetLoadableSceneName(TutorialSceneCatalog.PostChickenCutsceneSceneName)));
            Assert.That(
                StoryPackageRuntimeCatalog.TryGetStoryboard(
                    TutorialSceneCatalog.PostChickenCutsceneSceneName,
                    out var title,
                    out _),
                Is.True);
            Assert.That(title, Is.EqualTo("Generated Bridge One"));
        }

        [Test]
        public void StorySequenceRuntimeController_BeginSequencePreparationRoutine_AppliesGeneratedPayloadWithoutLoadingScene()
        {
            var runtimeObject = new GameObject("StorySequenceRuntime");
            var controller = runtimeObject.AddComponent<StorySequenceRuntimeController>();
            string loadedSceneName = null;
            string preparedSceneName = null;

            SetEnsureReadySuccess(controller);

            SetPrivateField(
                controller,
                "_beginSequenceRequestOverride",
                (Func<Action<StorySequenceAdvancePayload>, IEnumerator>)(callback =>
                    CompleteImmediately(
                        callback,
                        new StorySequenceAdvancePayload(
                            "http://127.0.0.1:8012",
                            "session-001",
                            TutorialSceneCatalog.PostChickenCutsceneSceneName,
                            BuildPackage(
                                "Generated Bridge Prepared",
                                "sequence_turn_000_cutscene",
                                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                                TutorialSceneCatalog.FarmTutorialSceneName,
                                "generated_prepared")))));

            SetPrivateField(
                controller,
                "_sceneLoadOverride",
                (Action<string>)(sceneName => loadedSceneName = sceneName));

            var routine = InvokePrivate<IEnumerator>(
                controller,
                "BeginSequencePreparationRoutine",
                (Action<string>)(sceneName => preparedSceneName = sceneName));

            Drain(routine);

            Assert.That(loadedSceneName, Is.Null);
            Assert.That(preparedSceneName, Is.EqualTo(TutorialSceneCatalog.PostChickenCutsceneSceneName));
            Assert.That(controller.ActiveSessionId, Is.EqualTo("session-001"));
            Assert.That(ReadPrivateField<string>(controller, "_preparedEntrySceneName"), Is.EqualTo(TutorialSceneCatalog.PostChickenCutsceneSceneName));
            Assert.That(
                StoryPackageRuntimeCatalog.TryGetStoryboard(
                    TutorialSceneCatalog.PostChickenCutsceneSceneName,
                    out var title,
                    out _),
                Is.True);
            Assert.That(title, Is.EqualTo("Generated Bridge Prepared"));
        }

        [Test]
        public void StorySequenceRuntimeController_BeginSequencePreparationRoutine_WaitsForLocalOrchestratorReadinessBeforeRequest()
        {
            var runtimeObject = new GameObject("StorySequenceRuntime");
            var controller = runtimeObject.AddComponent<StorySequenceRuntimeController>();
            bool orchestratorEnsured = false;
            bool requestStartedAfterEnsure = false;

            SetPrivateField(
                controller,
                "_ensureOrchestratorReadyOverride",
                (Func<Action<LocalStoryOrchestratorReadyResult>, IEnumerator>)(callback =>
                    CompleteImmediately(
                        callback,
                        MarkEnsured(ref orchestratorEnsured))));

            SetPrivateField(
                controller,
                "_beginSequenceRequestOverride",
                (Func<Action<StorySequenceAdvancePayload>, IEnumerator>)(callback =>
                    CompleteImmediately(
                        callback,
                        BuildPayloadAfterEnsure(orchestratorEnsured, value => requestStartedAfterEnsure = value))));

            var routine = InvokePrivate<IEnumerator>(
                controller,
                "BeginSequencePreparationRoutine",
                (Action<string>)(_ => { }));

            Drain(routine);

            Assert.That(orchestratorEnsured, Is.True);
            Assert.That(requestStartedAfterEnsure, Is.True);
        }

        [Test]
        public void StorySequenceRuntimeController_BeginSequenceAndLoadRoutine_DoesNotLoadFallbackWhenGeneratedPayloadUnavailable()
        {
            var runtimeObject = new GameObject("StorySequenceRuntime");
            var controller = runtimeObject.AddComponent<StorySequenceRuntimeController>();
            string loadedSceneName = null;

            SetEnsureReadySuccess(controller);

            SetPrivateField(
                controller,
                "_beginSequenceRequestOverride",
                (Func<Action<StorySequenceAdvancePayload>, IEnumerator>)(callback =>
                    CompleteImmediately(
                        callback,
                        new StorySequenceAdvancePayload(
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            null,
                            "Generated story sequence is unavailable right now."))));

            SetPrivateField(
                controller,
                "_sceneLoadOverride",
                (Action<string>)(sceneName => loadedSceneName = sceneName));

            var routine = InvokePrivate<IEnumerator>(
                controller,
                "BeginSequenceAndLoadRoutine",
                TutorialSceneCatalog.PostChickenCutsceneSceneName);

            Drain(routine);

            Assert.That(loadedSceneName, Is.Null);
            Assert.That(controller.HasActiveSession, Is.False);
            Assert.That(controller.LastError, Is.EqualTo("Generated story sequence is unavailable right now."));
        }

        [Test]
        public void StorySequenceRuntimeController_AdvanceSequenceAndLoadRoutine_AppliesLaterGeneratedTurn()
        {
            var runtimeObject = new GameObject("StorySequenceRuntime");
            var controller = runtimeObject.AddComponent<StorySequenceRuntimeController>();
            var flowObject = new GameObject("TutorialFlow");
            var flowController = flowObject.AddComponent<TutorialFlowController>();
            string loadedSceneName = null;

            SetEnsureReadySuccess(controller);

            var seeded = StoryPackageRuntimeCatalog.TrySetRuntimeOverride(
                BuildPackage(
                    "Generated Bridge One",
                    "sequence_turn_000_cutscene",
                    TutorialSceneCatalog.PostChickenCutsceneSceneName,
                    TutorialSceneCatalog.FarmTutorialSceneName,
                    "generated_seed"),
                out var seedError);

            Assert.That(seeded, Is.True, seedError);

            SetPrivateField(controller, "_activeSessionId", "session-001");
            SetPrivateField(
                controller,
                "_advanceSequenceRequestOverride",
                (Func<string, Action<StorySequenceAdvancePayload>, IEnumerator>)((sessionId, callback) =>
                    CompleteImmediately(
                        callback,
                        new StorySequenceAdvancePayload(
                            "http://127.0.0.1:8012",
                            sessionId,
                            TutorialSceneCatalog.PostChickenCutsceneSceneName,
                            BuildPackage(
                                "Generated Bridge Two",
                                "sequence_turn_001_cutscene",
                                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                                TutorialSceneCatalog.FarmTutorialSceneName,
                                "generated_advance")))));

            SetPrivateField(
                controller,
                "_sceneLoadOverride",
                (Action<string>)(sceneName => loadedSceneName = sceneName));

            var routine = InvokePrivate<IEnumerator>(
                controller,
                "AdvanceSequenceAndLoadRoutine",
                flowController,
                TutorialSceneCatalog.FarmTutorialSceneName);

            Drain(routine);

            Assert.That(controller.ActiveSessionId, Is.EqualTo("session-001"));
            Assert.That(
                loadedSceneName,
                Is.EqualTo(SceneWorkCatalog.GetLoadableSceneName(TutorialSceneCatalog.PostChickenCutsceneSceneName)));
            Assert.That(
                StoryPackageRuntimeCatalog.TryGetStoryboard(
                    TutorialSceneCatalog.PostChickenCutsceneSceneName,
                    out var title,
                    out _),
                Is.True);
            Assert.That(title, Is.EqualTo("Generated Bridge Two"));
        }

        [Test]
        public void StorySequenceRuntimeController_ExecuteBeginSequenceRequest_EnsuresLocalOrchestratorReady_FromSource()
        {
            var source = File.ReadAllText("Assets/_Project/Scripts/MonoBehaviours/Cinematics/StorySequenceRuntimeController.cs");

            Assert.That(source, Does.Contain("EnsureLocalOrchestratorReady("));
            Assert.That(source, Does.Contain("yield return ensureRoutine;"));
        }

        [Test]
        public void StorySequenceServiceClient_UsesResolvedBackendUrlWithoutRebuildingFallbackCandidatePorts_FromSource()
        {
            var source = File.ReadAllText("Assets/_Project/Scripts/MonoBehaviours/Cinematics/StorySequenceServiceClient.cs");

            Assert.That(source, Does.Contain("ResolveRequestBaseUrl("));
            Assert.That(source, Does.Not.Contain("BuildCandidateBaseUrls("));
        }

        [Test]
        public void StorySequenceServiceClient_TimeoutBudget_AllowsRailwayTurnCompletion()
        {
            Type clientType = typeof(StorySequenceRuntimeController).Assembly
                .GetType("FarmSimVR.MonoBehaviours.Cinematics.StorySequenceServiceClient");

            Assert.That(clientType, Is.Not.Null);

            FieldInfo timeoutField = clientType.GetField(
                "RequestTimeoutSeconds",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(timeoutField, Is.Not.Null);
            Assert.That((int)timeoutField.GetRawConstantValue(), Is.GreaterThan(240));
        }

        [Test]
        public void StorySequenceRuntimeController_EnsureLocalOrchestratorReady_ClearsStaleHealthyBaseUrlWhenCurrentCheckFails()
        {
            var runtimeObject = new GameObject("StorySequenceRuntime");
            var controller = runtimeObject.AddComponent<StorySequenceRuntimeController>();
            LocalStoryOrchestratorReadyResult readyResult = null;

            SetPrivateField(controller, "_lastOrchestratorReadyBaseUrl", "http://127.0.0.1:8000");
            SetPrivateField(
                controller,
                "_ensureOrchestratorReadyOverride",
                (Func<Action<LocalStoryOrchestratorReadyResult>, IEnumerator>)(callback =>
                    CompleteImmediately(
                        callback,
                        new LocalStoryOrchestratorReadyResult(
                            "http://127.0.0.1:8012",
                            false,
                            false,
                            "Cannot connect to destination host"))));

            var routine = InvokePrivate<IEnumerator>(
                controller,
                "EnsureLocalOrchestratorReady",
                (Action<LocalStoryOrchestratorReadyResult>)(result => readyResult = result));

            Drain(routine);

            Assert.That(readyResult, Is.Not.Null);
            Assert.That(readyResult.Success, Is.False);
            Assert.That(readyResult.ErrorMessage, Is.EqualTo("Cannot connect to destination host"));
            Assert.That(
                ReadPrivateField<string>(controller, "_lastOrchestratorReadyBaseUrl"),
                Is.EqualTo(string.Empty));
        }

        private static StoryPackageSnapshot BuildPackage(
            string title,
            string beatId,
            string cutsceneSceneName,
            string nextSceneName,
            string resourceSuffix)
        {
            return new StoryPackageSnapshot
            {
                PackageId = "storypkg_runtime_bridge_test",
                SchemaVersion = 1,
                PackageVersion = 1,
                DisplayName = "Runtime Bridge Test",
                Beats = new[]
                {
                    BuildCutsceneBeat(beatId, cutsceneSceneName, nextSceneName, title, resourceSuffix),
                    new StoryBeatSnapshot
                    {
                        BeatId = beatId.Replace("_cutscene", "_minigame"),
                        DisplayName = "Generated Farm Loop",
                        Kind = "Minigame",
                        SceneName = TutorialSceneCatalog.FarmTutorialSceneName,
                        NextSceneName = string.Empty,
                        Minigame = new StoryMinigameConfigSnapshot
                        {
                            AdapterId = "tutorial.plant_rows",
                            ObjectiveText = "Plant 6 carrots in 5 minutes.",
                            RequiredCount = 6,
                            TimeLimitSeconds = 300f,
                            GeneratorId = "plant_rows_v1",
                            MinigameId = "generated_plant_rows",
                            FallbackGeneratorIds = Array.Empty<string>(),
                            ResolvedParameterEntries = new[]
                            {
                                new StoryMinigameParameterSnapshot
                                {
                                    Name = "cropType",
                                    ValueType = "String",
                                    StringValue = "carrot",
                                },
                            },
                        },
                    },
                },
            };
        }

        private static StoryBeatSnapshot BuildCutsceneBeat(
            string beatId,
            string sceneName,
            string nextSceneName,
            string title,
            string resourceSuffix)
        {
            return new StoryBeatSnapshot
            {
                BeatId = beatId,
                DisplayName = title,
                Kind = "Cutscene",
                SceneName = sceneName,
                NextSceneName = nextSceneName,
                Storyboard = new StoryStoryboardSnapshot
                {
                    StylePresetId = "farm_storybook_v1",
                    Shots = new[]
                    {
                        new StoryStoryboardShotSnapshot
                        {
                            ShotId = "shot_01",
                            SubtitleText = title,
                            NarrationText = title,
                            DurationSeconds = 3f,
                            ImageResourcePath = $"GeneratedStoryboards/storypkg_runtime_bridge_test/{resourceSuffix}/shot_01",
                            AudioResourcePath = $"GeneratedStoryboards/storypkg_runtime_bridge_test/{resourceSuffix}/shot_01",
                        },
                    },
                },
            };
        }

        private static IEnumerator CompleteImmediately(
            Action<StorySequenceAdvancePayload> callback,
            StorySequenceAdvancePayload payload)
        {
            callback(payload);
            yield break;
        }

        private static void SetEnsureReadySuccess(StorySequenceRuntimeController controller)
        {
            SetPrivateField(
                controller,
                "_ensureOrchestratorReadyOverride",
                (Func<Action<LocalStoryOrchestratorReadyResult>, IEnumerator>)(callback =>
                    CompleteImmediately(
                        callback,
                        new LocalStoryOrchestratorReadyResult(
                            "http://127.0.0.1:8012",
                            true,
                            false,
                            string.Empty))));
        }

        private static LocalStoryOrchestratorReadyResult MarkEnsured(ref bool orchestratorEnsured)
        {
            orchestratorEnsured = true;
            return new LocalStoryOrchestratorReadyResult(
                "http://127.0.0.1:8012",
                true,
                false,
                string.Empty);
        }

        private static StorySequenceAdvancePayload BuildPayloadAfterEnsure(
            bool orchestratorEnsured,
            Action<bool> recordRequestOrdering)
        {
            recordRequestOrdering(orchestratorEnsured);
            return new StorySequenceAdvancePayload(
                "http://127.0.0.1:8012",
                "session-ensure-order",
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                BuildPackage(
                    "Generated Bridge Ordered",
                    "sequence_turn_000_cutscene",
                    TutorialSceneCatalog.PostChickenCutsceneSceneName,
                    TutorialSceneCatalog.FarmTutorialSceneName,
                    "generated_ordered"));
        }

        private static IEnumerator CompleteImmediately(
            Action<LocalStoryOrchestratorReadyResult> callback,
            LocalStoryOrchestratorReadyResult payload)
        {
            callback(payload);
            yield break;
        }

        private static void Drain(IEnumerator routine)
        {
            while (routine != null && routine.MoveNext())
            {
                if (routine.Current is IEnumerator nested)
                    Drain(nested);
            }
        }

        private static T InvokePrivate<T>(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (T)method.Invoke(instance, args);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(instance, value);
        }

        private static T ReadPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(instance);
        }
    }
}
