using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Tutorial
{
    /// <summary>
    /// Decides whether the Town interaction scripted demo should be skipped based on
    /// which Unity scenes are currently loaded (e.g. additive hub flow).
    /// </summary>
    public static class TownAutoplayPolicy
    {
        /// <summary>
        /// Returns true when the persistent core hub scene is loaded alongside Town,
        /// so the player should get immediate control instead of the autoplay walkthrough.
        /// </summary>
        public static bool ShouldSkipTownInteractionDemo(IReadOnlyList<string> loadedSceneNames)
        {
            if (loadedSceneNames == null || loadedSceneNames.Count == 0)
                return false;

            for (int i = 0; i < loadedSceneNames.Count; i++)
            {
                if (string.Equals(
                        loadedSceneNames[i],
                        TutorialSceneCatalog.CoreSceneSceneName,
                        StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
