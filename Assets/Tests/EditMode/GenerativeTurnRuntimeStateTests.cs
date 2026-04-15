using System.Reflection;
using FarmSimVR.Core;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public sealed class GenerativeTurnRuntimeStateTests
    {
        [SetUp]
        public void SetUp()
        {
            GenerativeTurnRuntimeState.Clear();
            StoryPackageRuntimeCatalog.ResetCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GenerativeTurnRuntimeState.Clear();
            StoryPackageRuntimeCatalog.ResetCacheForTests();

            foreach (var controller in Object.FindObjectsByType<TutorialCutsceneSceneController>(
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
        public void RuntimeState_TryGetCutscene_ReturnsPreparedRuntimeCutsceneForMatchingScene()
        {
            var envelope = BuildEnvelope(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                TutorialSceneCatalog.FarmTutorialSceneName,
                "runtime");
            var assets = BuildAssets(envelope);

            GenerativeTurnRuntimeState.SetPreparedTurn(envelope, assets);

            var found = GenerativeTurnRuntimeState.TryGetCutscene(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                out var title,
                out var shots);

            Assert.That(found, Is.True);
            Assert.That(title, Is.EqualTo("Runtime Bridge"));
            Assert.That(shots, Has.Length.EqualTo(3));
            Assert.That(shots[0].Image, Is.SameAs(assets.ImagesByAssetId["runtime_image_01"]));
            Assert.That(shots[0].AudioClip, Is.SameAs(assets.AudioByAssetId["runtime_audio_01"]));
        }

        [Test]
        public void RuntimeState_TryGetMinigameContract_ReturnsPreparedMinigameForMatchingScene()
        {
            var envelope = BuildEnvelope(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                TutorialSceneCatalog.FarmTutorialSceneName,
                "runtime");

            GenerativeTurnRuntimeState.SetPreparedTurn(envelope, BuildAssets(envelope));

            var found = GenerativeTurnRuntimeState.TryGetMinigameContract(
                TutorialSceneCatalog.FarmTutorialSceneName,
                out var minigame);

            Assert.That(found, Is.True);
            Assert.That(minigame, Is.Not.Null);
            Assert.That(minigame.adapter_id, Is.EqualTo("tutorial.plant_rows"));
            Assert.That(minigame.required_count, Is.EqualTo(6));
        }

        [Test]
        public void Installer_UsesRuntimeCutsceneBeforeLegacyStoryPackage()
        {
            var envelope = BuildEnvelope(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                TutorialSceneCatalog.FarmTutorialSceneName,
                "runtime");
            GenerativeTurnRuntimeState.SetPreparedTurn(envelope, BuildAssets(envelope));

            var runtime = new GameObject("TutorialRuntime");
            var controller = runtime.AddComponent<TutorialFlowController>();

            TutorialSceneInstaller.InstallForScene(TutorialSceneCatalog.PostChickenCutsceneSceneName, controller);

            var cutsceneController = Object.FindFirstObjectByType<TutorialCutsceneSceneController>();
            Assert.That(cutsceneController, Is.Not.Null);
            Assert.That(ReadPrivateField<string>(cutsceneController, "_title"), Is.EqualTo("Runtime Bridge"));
            Assert.That(ReadPrivateField<RuntimeCutsceneShotHandle[]>(cutsceneController, "_runtimeShots"), Has.Length.EqualTo(3));
        }

        [Test]
        public void RuntimeCutsceneController_AdvancesAcrossAllRuntimeShots_BeforeCompleting()
        {
            var controllerObject = new GameObject("RuntimeCutsceneController");
            var controller = controllerObject.AddComponent<TutorialCutsceneSceneController>();
            var runtimeShots = BuildRuntimeShots();

            controller.ConfigureRuntimeStoryboard("Runtime Bridge", runtimeShots, 0f);
            InvokePrivateMethod(controller, "Start");

            Assert.That(ReadPrivateField<int>(controller, "_currentShotIndex"), Is.EqualTo(0));
            Assert.That(ReadPrivateField<bool>(controller, "_completionHandled"), Is.False);

            SetPrivateField(controller, "_advanceAt", 0f);
            InvokePrivateMethod(controller, "UpdateStoryboardPlayback");
            Assert.That(ReadPrivateField<int>(controller, "_currentShotIndex"), Is.EqualTo(1));
            Assert.That(ReadPrivateField<bool>(controller, "_completionHandled"), Is.False);

            SetPrivateField(controller, "_advanceAt", 0f);
            InvokePrivateMethod(controller, "UpdateStoryboardPlayback");
            Assert.That(ReadPrivateField<int>(controller, "_currentShotIndex"), Is.EqualTo(2));
            Assert.That(ReadPrivateField<bool>(controller, "_completionHandled"), Is.False);

            SetPrivateField(controller, "_advanceAt", 0f);
            InvokePrivateMethod(controller, "UpdateStoryboardPlayback");
            Assert.That(ReadPrivateField<bool>(controller, "_completionHandled"), Is.True);
        }

        [Test]
        public void SetPreparedTurn_RejectsEnvelopeWithTooFewRuntimeShots()
        {
            var envelope = BuildEnvelope(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                TutorialSceneCatalog.FarmTutorialSceneName,
                "runtime");
            envelope.cutscene.shots = new[] { envelope.cutscene.shots[0] };

            GenerativeTurnRuntimeState.SetPreparedTurn(envelope, BuildAssets(envelope));

            var found = GenerativeTurnRuntimeState.TryGetCutscene(
                TutorialSceneCatalog.PostChickenCutsceneSceneName,
                out _,
                out var shots);

            Assert.That(GenerativeTurnRuntimeState.HasPreparedTurn, Is.False);
            Assert.That(found, Is.False);
            Assert.That(shots, Is.Empty);
        }

        private static GenerativePlayableTurnEnvelope BuildEnvelope(
            string cutsceneSceneName,
            string minigameSceneName,
            string assetPrefix)
        {
            return new GenerativePlayableTurnEnvelope
            {
                contract_version = "runtime/v1",
                session_id = "session-001",
                turn_id = "turn-001",
                status = "ready",
                entry_scene_name = cutsceneSceneName,
                cutscene = new GenerativeCutsceneContract
                {
                    beat_id = "beat-cutscene",
                    display_name = "Runtime Bridge",
                    scene_name = cutsceneSceneName,
                    next_scene_name = minigameSceneName,
                    style_preset_id = "farm_storybook_v1",
                    shots = new[]
                    {
                        new GenerativeCutsceneShotContract
                        {
                            shot_id = "shot_01",
                            subtitle_text = "Garrett checks the first row.",
                            narration_text = "Garrett checks the first row.",
                            duration_seconds = 3f,
                            image_asset_id = $"{assetPrefix}_image_01",
                            audio_asset_id = $"{assetPrefix}_audio_01",
                            alignment_asset_id = $"{assetPrefix}_alignment_01"
                        },
                        new GenerativeCutsceneShotContract
                        {
                            shot_id = "shot_02",
                            subtitle_text = "The cart snags on broken wood.",
                            narration_text = "The cart snags on broken wood.",
                            duration_seconds = 3f,
                            image_asset_id = $"{assetPrefix}_image_02",
                            audio_asset_id = $"{assetPrefix}_audio_02",
                            alignment_asset_id = $"{assetPrefix}_alignment_02"
                        },
                        new GenerativeCutsceneShotContract
                        {
                            shot_id = "shot_03",
                            subtitle_text = "Clear the path and plant.",
                            narration_text = "Clear the path and plant.",
                            duration_seconds = 3f,
                            image_asset_id = $"{assetPrefix}_image_03",
                            audio_asset_id = $"{assetPrefix}_audio_03",
                            alignment_asset_id = $"{assetPrefix}_alignment_03"
                        }
                    }
                },
                minigame = new GenerativeMinigameContract
                {
                    beat_id = "beat-minigame",
                    display_name = "Plant Runtime Rows",
                    scene_name = minigameSceneName,
                    adapter_id = "tutorial.plant_rows",
                    objective_text = "Plant 6 carrots in 5 minutes.",
                    required_count = 6,
                    time_limit_seconds = 300f,
                    generator_id = "plant_rows_v1",
                    minigame_id = "planting",
                    resolved_parameter_entries = new[]
                    {
                        new GenerativeMinigameParameterEntry
                        {
                            Name = "cropType",
                            ValueType = "String",
                            StringValue = "carrot"
                        }
                    }
                }
            };
        }

        private static PreloadedGenerativeTurnAssets BuildAssets(GenerativePlayableTurnEnvelope envelope)
        {
            var assets = new PreloadedGenerativeTurnAssets(
                envelope.session_id,
                envelope.turn_id,
                "/tmp/runtime-cache");

            for (var index = 0; index < envelope.cutscene.shots.Length; index++)
            {
                var shot = envelope.cutscene.shots[index];
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, index == 0 ? Color.white : (index == 1 ? Color.yellow : Color.green));
                texture.Apply();
                var audioClip = AudioClip.Create($"runtime-audio-{index}", 8, 1, 8000, false);
                assets.RegisterImage(shot.image_asset_id, texture, $"/tmp/runtime-image-{index}.png");
                assets.RegisterAudio(shot.audio_asset_id, audioClip, $"/tmp/runtime-audio-{index}.mp3");
                assets.RegisterAlignment(shot.alignment_asset_id, "{\"characters\":[\"r\"]}", $"/tmp/runtime-alignment-{index}.json");
            }
            return assets;
        }

        private static RuntimeCutsceneShotHandle[] BuildRuntimeShots()
        {
            var first = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            first.SetPixel(0, 0, Color.white);
            first.Apply();
            var second = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            second.SetPixel(0, 0, Color.yellow);
            second.Apply();
            var third = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            third.SetPixel(0, 0, Color.green);
            third.Apply();

            return new[]
            {
                new RuntimeCutsceneShotHandle(
                    "shot_01",
                    "Garrett checks the first row.",
                    "Garrett checks the first row.",
                    3f,
                    first,
                    AudioClip.Create("runtime-clip-01", 8, 1, 8000, false),
                    "{\"characters\":[\"g\"]}"),
                new RuntimeCutsceneShotHandle(
                    "shot_02",
                    "The cart snags on broken wood.",
                    "The cart snags on broken wood.",
                    3f,
                    second,
                    AudioClip.Create("runtime-clip-02", 8, 1, 8000, false),
                    "{\"characters\":[\"c\"]}"),
                new RuntimeCutsceneShotHandle(
                    "shot_03",
                    "Clear the path and plant.",
                    "Clear the path and plant.",
                    3f,
                    third,
                    AudioClip.Create("runtime-clip-03", 8, 1, 8000, false),
                    "{\"characters\":[\"p\"]}"),
            };
        }

        private static T ReadPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(instance);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(instance, value);
        }

        private static void InvokePrivateMethod(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(instance, args);
        }
    }
}
