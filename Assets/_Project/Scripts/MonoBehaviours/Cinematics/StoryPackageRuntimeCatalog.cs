using FarmSimVR.Core.Story;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public static class StoryPackageRuntimeCatalog
    {
        public const string IntroChickenSampleResourcePath = "StoryPackages/StoryPackage_IntroChickenSample";

        private static StoryPackageSnapshot _cachedPackage;
        private static string _cachedError;
        private static bool _loadAttempted;

        public static bool TryGetPackage(out StoryPackageSnapshot package, out string error)
        {
            if (!_loadAttempted)
                LoadPackage();

            package = _cachedPackage;
            error = _cachedError ?? string.Empty;
            return package != null;
        }

        public static bool TryGetBeat(string sceneName, out StoryBeatSnapshot beat)
        {
            beat = null;
            return TryGetPackage(out var package, out _)
                && StoryPackageNavigator.TryGetBeatBySceneName(package, sceneName, out beat);
        }

        public static string GetNextSceneOrNull(string sceneName)
        {
            return TryGetBeat(sceneName, out var beat) ? beat.NextSceneName : null;
        }

        public static bool TryGetCutsceneDisplayText(string sceneName, out string title, out string body)
        {
            title = string.Empty;
            body = string.Empty;

            if (!TryGetBeat(sceneName, out var beat))
                return false;

            if (!StoryBeatKindParser.TryParse(beat.Kind, out var kind) || kind != StoryBeatKind.Cutscene)
                return false;

            title = beat.DisplayName ?? string.Empty;
            body = ExtractBodyText(beat);
            if (string.IsNullOrWhiteSpace(body) &&
                beat.Storyboard != null &&
                beat.Storyboard.Shots != null &&
                beat.Storyboard.Shots.Length > 0)
            {
                body = beat.Storyboard.Shots[0]?.SubtitleText ?? string.Empty;
            }

            return !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body);
        }

        public static bool TryGetStoryboard(string sceneName, out string title, out StoryStoryboardSnapshot storyboard)
        {
            title = string.Empty;
            storyboard = null;

            if (!TryGetBeat(sceneName, out var beat))
                return false;

            if (!StoryBeatKindParser.TryParse(beat.Kind, out var kind) || kind != StoryBeatKind.Cutscene)
                return false;

            if (beat.Storyboard == null || beat.Storyboard.Shots == null || beat.Storyboard.Shots.Length == 0)
                return false;

            title = beat.DisplayName ?? string.Empty;
            storyboard = beat.Storyboard;
            return true;
        }

        public static void ResetCacheForTests()
        {
            _cachedPackage = null;
            _cachedError = string.Empty;
            _loadAttempted = false;
        }

        private static void LoadPackage()
        {
            _loadAttempted = true;
            _cachedPackage = null;
            _cachedError = string.Empty;

            var asset = Resources.Load<TextAsset>(IntroChickenSampleResourcePath);
            if (asset == null)
            {
                _cachedError = $"Story package resource '{IntroChickenSampleResourcePath}' was not found.";
                return;
            }

            if (!StoryPackageImporter.TryImport(asset, out _cachedPackage, out _cachedError))
                _cachedPackage = null;
        }

        private static string ExtractBodyText(StoryBeatSnapshot beat)
        {
            if (beat?.SequenceSteps == null)
                return string.Empty;

            for (int i = 0; i < beat.SequenceSteps.Length; i++)
            {
                var step = beat.SequenceSteps[i];
                if (step == null || string.IsNullOrWhiteSpace(step.StringParam))
                    continue;

                if (string.Equals(step.StepType, "ObjectivePopup", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(step.StepType, "Dialogue", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(step.StepType, "MissionStart", System.StringComparison.OrdinalIgnoreCase))
                {
                    return step.StringParam;
                }
            }

            return string.Empty;
        }
    }
}
