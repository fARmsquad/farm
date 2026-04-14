using FarmSimVR.Core.Story;
using FarmSimVR.MonoBehaviours.Cinematics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class StoryPackageContractTests
    {
        [Test]
        public void Validate_DuplicateBeatIds_ReturnsReadableError()
        {
            var package = new StoryPackageSnapshot
            {
                PackageId = "storypkg_duplicate",
                SchemaVersion = 1,
                PackageVersion = 1,
                Beats = new[]
                {
                    new StoryBeatSnapshot
                    {
                        BeatId = "beat_same",
                        Kind = "Cutscene",
                        SceneName = "Intro",
                        SequenceSteps = new[]
                        {
                            new StorySequenceStepSnapshot { StepType = "Wait", Duration = 1f }
                        }
                    },
                    new StoryBeatSnapshot
                    {
                        BeatId = "beat_same",
                        Kind = "Minigame",
                        SceneName = "ChickenGame",
                        Minigame = new StoryMinigameConfigSnapshot { AdapterId = "tutorial.chicken_chase" }
                    }
                }
            };

            var result = StoryPackageContract.Validate(package);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Does.Contain("Beat 1 has duplicate BeatId 'beat_same'."));
        }

        [Test]
        public void Validate_MinigameWithoutAdapterId_ReturnsReadableError()
        {
            var package = new StoryPackageSnapshot
            {
                PackageId = "storypkg_missing_adapter",
                SchemaVersion = 1,
                PackageVersion = 1,
                Beats = new[]
                {
                    new StoryBeatSnapshot
                    {
                        BeatId = "chicken_chase",
                        Kind = "Minigame",
                        SceneName = "ChickenGame",
                        Minigame = new StoryMinigameConfigSnapshot()
                    }
                }
            };

            var result = StoryPackageContract.Validate(package);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Does.Contain("Beat 0 minigame requires AdapterId."));
        }

        [Test]
        public void Validate_StoryboardCutsceneWithoutAudioPath_ReturnsReadableError()
        {
            var package = new StoryPackageSnapshot
            {
                PackageId = "storypkg_storyboard_missing_audio",
                SchemaVersion = 1,
                PackageVersion = 1,
                Beats = new[]
                {
                    new StoryBeatSnapshot
                    {
                        BeatId = "post_chicken_bridge",
                        Kind = "Cutscene",
                        SceneName = "PostChickenCutscene",
                        Storyboard = new StoryStoryboardSnapshot
                        {
                            StylePresetId = "farm_storybook_v1",
                            Shots = new[]
                            {
                                new StoryStoryboardShotSnapshot
                                {
                                    ShotId = "shot_01",
                                    SubtitleText = "Old Garrett points toward the carrot beds.",
                                    ImageResourcePath = "GeneratedStoryboards/storypkg_intro_chicken_sample/post_chicken_bridge/shot_01",
                                    DurationSeconds = 3f
                                }
                            }
                        }
                    }
                }
            };

            var result = StoryPackageContract.Validate(package);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Does.Contain("Beat 0 storyboard shot 0 requires AudioResourcePath."));
        }
    }

    [TestFixture]
    public class StoryPackageImportTests
    {
        private const string SamplePackagePath = "Assets/_Project/Data/StoryPackage_IntroChickenSample.json";

        [TearDown]
        public void TearDown()
        {
            foreach (var binding in Object.FindObjectsByType<StoryPackageSceneBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(binding.gameObject);

            foreach (var sequencer in Object.FindObjectsByType<CinematicSequencer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(sequencer.gameObject);
        }

        [Test]
        public void Importer_LoadsAndValidatesSamplePackage()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(SamplePackagePath);

            Assert.That(asset, Is.Not.Null);
            Assert.That(StoryPackageImporter.TryImport(asset, out var package, out var error), Is.True, error);
            Assert.That(package.PackageId, Is.EqualTo("storypkg_intro_chicken_sample"));
            Assert.That(package.DisplayName, Is.EqualTo("Generative Story Slice"));
            Assert.That(package.Beats, Has.Length.EqualTo(7));

            Assert.That(StoryPackageNavigator.TryGetBeatBySceneName(package, "FarmMain", out var farmBeat), Is.True);
            Assert.That(farmBeat, Is.Not.Null);
            Assert.That(farmBeat.BeatId, Is.EqualTo("plant_rows_intro"));
            Assert.That(farmBeat.Minigame, Is.Not.Null);
            Assert.That(farmBeat.Minigame.AdapterId, Is.EqualTo("tutorial.plant_rows"));
            Assert.That(farmBeat.Minigame.RequiredCount, Is.EqualTo(3));
            Assert.That(farmBeat.Minigame.TimeLimitSeconds, Is.EqualTo(300f));
            Assert.That(farmBeat.NextSceneName, Is.EqualTo("CoreScene"));
            Assert.That(farmBeat.Minigame.ResolvedParameterEntries, Is.Not.Null);
            Assert.That(farmBeat.Minigame.ResolvedParameterEntries, Has.Length.GreaterThanOrEqualTo(3));
            Assert.That(farmBeat.Minigame.ResolvedParameterEntries[0].Name, Is.Not.Empty);

            Assert.That(StoryPackageNavigator.TryGetBeatBySceneName(package, "PlayerCollectTools", out var findToolsBeat), Is.True);
            Assert.That(findToolsBeat, Is.Not.Null);
            Assert.That(findToolsBeat.BeatId, Is.EqualTo("find_tools_intro"));
            Assert.That(findToolsBeat.Minigame, Is.Not.Null);
            Assert.That(findToolsBeat.Minigame.AdapterId, Is.EqualTo("tutorial.find_tools"));
            Assert.That(findToolsBeat.Minigame.RequiredCount, Is.EqualTo(3));
            Assert.That(findToolsBeat.Minigame.TimeLimitSeconds, Is.EqualTo(0f));
            Assert.That(findToolsBeat.Minigame.ResolvedParameterEntries, Is.Not.Null);
            Assert.That(findToolsBeat.Minigame.ResolvedParameterEntries, Has.Length.GreaterThanOrEqualTo(4));

            Assert.That(StoryPackageNavigator.TryGetBeatBySceneName(package, "Tutorial_PreFarmCutscene", out var preFarmBeat), Is.True);
            Assert.That(preFarmBeat, Is.Not.Null);
            Assert.That(preFarmBeat.BeatId, Is.EqualTo("pre_farm_bridge"));
            Assert.That(preFarmBeat.Kind, Is.EqualTo("Cutscene"));
            Assert.That(preFarmBeat.NextSceneName, Is.EqualTo("FarmMain"));
            Assert.That(preFarmBeat.Storyboard, Is.Not.Null);
            Assert.That(preFarmBeat.Storyboard.Shots, Has.Length.EqualTo(3));
        }

        [Test]
        public void Navigator_ResolvesChickenGameBeat_BySceneName()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(SamplePackagePath);
            Assert.That(StoryPackageImporter.TryImport(asset, out var package, out var error), Is.True, error);

            var found = StoryPackageNavigator.TryGetBeatBySceneName(package, "ChickenGame", out var beat);

            Assert.That(found, Is.True);
            Assert.That(beat, Is.Not.Null);
            Assert.That(beat.BeatId, Is.EqualTo("chicken_chase"));
            Assert.That(beat.Minigame.AdapterId, Is.EqualTo("tutorial.chicken_chase"));
        }

        [Test]
        public void SceneBinding_LoadsIntroBeat_AndBuildsValidSequence()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(SamplePackagePath);
            var bindingObject = new GameObject("StoryPackageBinding");
            var binding = bindingObject.AddComponent<StoryPackageSceneBinding>();
            binding.PackageAsset = asset;
            binding.SceneNameOverride = "Intro";

            Assert.That(binding.LoadNow(), Is.True, binding.LastError);
            Assert.That(binding.CurrentBeat, Is.Not.Null);
            Assert.That(binding.CurrentBeat.BeatId, Is.EqualTo("intro_opening"));
            Assert.That(binding.NextSceneName, Is.EqualTo("ChickenGame"));

            Assert.That(binding.TryBuildCurrentSequence(out var sequence, out var error), Is.True, error);
            Assert.That(sequence, Is.Not.Null);
            Assert.That(sequence.steps, Has.Length.EqualTo(4));

            var sequencerObject = new GameObject("Sequencer");
            var sequencer = sequencerObject.AddComponent<CinematicSequencer>();
            Assert.That(sequencer.Validate(sequence), Is.True);

            Object.DestroyImmediate(sequence);
        }
    }
}
