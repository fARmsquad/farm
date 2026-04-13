namespace FarmSimVR.Core.Story
{
    public static class StoryPackageNavigator
    {
        public static bool TryGetBeatBySceneName(
            StoryPackageSnapshot package,
            string sceneName,
            out StoryBeatSnapshot beat)
        {
            beat = null;
            if (package == null || package.Beats == null || string.IsNullOrWhiteSpace(sceneName))
                return false;

            for (int i = 0; i < package.Beats.Length; i++)
            {
                var candidate = package.Beats[i];
                if (candidate == null)
                    continue;

                if (SceneNamesMatch(candidate.SceneName, sceneName))
                {
                    beat = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool SceneNamesMatch(string left, string right)
        {
            return string.Equals(NormalizeSceneName(left), NormalizeSceneName(right), System.StringComparison.Ordinal);
        }

        private static string NormalizeSceneName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return string.Empty;

            var trimmed = sceneName.Trim();
            const string tutorialPrefix = "Tutorial_";
            return trimmed.StartsWith(tutorialPrefix, System.StringComparison.Ordinal)
                ? trimmed.Substring(tutorialPrefix.Length)
                : trimmed;
        }
    }
}
