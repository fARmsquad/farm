namespace FarmSimVR.Core.Tutorial
{
    /// <summary>
    /// Linear tutorial scene names and lookups (title / standalone scenes are named but not tutorial steps).
    /// </summary>
    public static class TutorialSceneCatalog
    {
        public const string IntroSceneName = "Intro";
        public const string ChickenGameSceneName = "ChickenGame";
        public const string PostChickenCutsceneSceneName = "PostChickenCutscene";
        public const string FindToolsSceneName = "PlayerGettingSeeds";
        public const string MidpointPlaceholderSceneName = "MidpointPlaceholder";
        public const string PreFarmCutsceneSceneName = "PreFarmCutscene";
        public const string FarmTutorialSceneName = "FarmMain";

        /// <summary>Main menu; not a <see cref="TutorialStep"/>.</summary>
        public const string TitleScreenSceneName = "TitleScreen";

        /// <summary>Top-down horse taming minigame (standalone; not in tutorial flow unless wired).</summary>
        public const string HorseTamingSceneName = "HorseTaming";

        public static readonly string[] SceneOrder =
        {
            IntroSceneName,
            ChickenGameSceneName,
            PostChickenCutsceneSceneName,
            MidpointPlaceholderSceneName,
            FindToolsSceneName,
            PreFarmCutsceneSceneName,
            FarmTutorialSceneName,
        };

        public static string NormalizeRuntimeSceneName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return sceneName;

            switch (sceneName)
            {
                case "Tutorial_PostChickenCutscene":
                    return PostChickenCutsceneSceneName;
                case "Tutorial_MidpointPlaceholder":
                    return MidpointPlaceholderSceneName;
                case "FindToolsGame":
                    return FindToolsSceneName;
                case "Tutorial_PreFarmCutscene":
                    return PreFarmCutsceneSceneName;
                default:
                    return sceneName;
            }
        }

        public static string GetSceneName(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Intro:
                    return IntroSceneName;
                case TutorialStep.ChickenHunt:
                    return ChickenGameSceneName;
                case TutorialStep.PostChickenCutscene:
                    return PostChickenCutsceneSceneName;
                case TutorialStep.MidpointPlaceholder:
                    return MidpointPlaceholderSceneName;
                case TutorialStep.FindTools:
                    return FindToolsSceneName;
                case TutorialStep.PreFarmCutscene:
                    return PreFarmCutsceneSceneName;
                case TutorialStep.FarmTutorial:
                    return FarmTutorialSceneName;
                default:
                    return null;
            }
        }

        public static TutorialStep GetStepForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return TutorialStep.None;

            sceneName = NormalizeRuntimeSceneName(sceneName);

            if (sceneName == IntroSceneName) return TutorialStep.Intro;
            if (sceneName == ChickenGameSceneName) return TutorialStep.ChickenHunt;
            if (sceneName == PostChickenCutsceneSceneName) return TutorialStep.PostChickenCutscene;
            if (sceneName == MidpointPlaceholderSceneName) return TutorialStep.MidpointPlaceholder;
            if (sceneName == FindToolsSceneName) return TutorialStep.FindTools;
            if (sceneName == PreFarmCutsceneSceneName) return TutorialStep.PreFarmCutscene;
            if (sceneName == FarmTutorialSceneName) return TutorialStep.FarmTutorial;

            return TutorialStep.None;
        }

        public static TutorialStep GetNextStep(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Intro:
                    return TutorialStep.ChickenHunt;
                case TutorialStep.ChickenHunt:
                    return TutorialStep.PostChickenCutscene;
                case TutorialStep.PostChickenCutscene:
                    return TutorialStep.MidpointPlaceholder;
                case TutorialStep.MidpointPlaceholder:
                    return TutorialStep.FindTools;
                case TutorialStep.FindTools:
                    return TutorialStep.PreFarmCutscene;
                case TutorialStep.PreFarmCutscene:
                    return TutorialStep.FarmTutorial;
                case TutorialStep.FarmTutorial:
                    return TutorialStep.None;
                default:
                    return TutorialStep.None;
            }
        }

        public static TutorialStep GetPreviousStep(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.ChickenHunt:
                    return TutorialStep.Intro;
                case TutorialStep.PostChickenCutscene:
                    return TutorialStep.ChickenHunt;
                case TutorialStep.MidpointPlaceholder:
                    return TutorialStep.PostChickenCutscene;
                case TutorialStep.FindTools:
                    return TutorialStep.MidpointPlaceholder;
                case TutorialStep.PreFarmCutscene:
                    return TutorialStep.FindTools;
                case TutorialStep.FarmTutorial:
                    return TutorialStep.PreFarmCutscene;
                default:
                    return TutorialStep.None;
            }
        }
    }
}
