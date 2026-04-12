using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Tutorial
{
    public static class TutorialSceneCatalog
    {
        public const string TitleScreenSceneName = "TitleScreen";
        public const string IntroSceneName = "Intro";
        public const string ChickenGameSceneName = "ChickenGame";
        public const string PostChickenCutsceneSceneName = "Tutorial_PostChickenCutscene";
        public const string MidpointPlaceholderSceneName = "Tutorial_MidpointPlaceholder";
        public const string FindToolsSceneName = "FindToolsGame";
        public const string PreFarmCutsceneSceneName = "Tutorial_PreFarmCutscene";
        public const string FarmTutorialSceneName = "FarmMain";

        private static readonly Dictionary<TutorialStep, string> StepToScene = new()
        {
            { TutorialStep.Intro, IntroSceneName },
            { TutorialStep.ChickenHunt, ChickenGameSceneName },
            { TutorialStep.PostChickenCutscene, PostChickenCutsceneSceneName },
            { TutorialStep.MidpointPlaceholder, MidpointPlaceholderSceneName },
            { TutorialStep.FindTools, FindToolsSceneName },
            { TutorialStep.PreFarmCutscene, PreFarmCutsceneSceneName },
            { TutorialStep.FarmTutorial, FarmTutorialSceneName },
        };

        private static readonly Dictionary<string, TutorialStep> SceneToStep = new(StringComparer.Ordinal)
        {
            { IntroSceneName, TutorialStep.Intro },
            { ChickenGameSceneName, TutorialStep.ChickenHunt },
            { PostChickenCutsceneSceneName, TutorialStep.PostChickenCutscene },
            { MidpointPlaceholderSceneName, TutorialStep.MidpointPlaceholder },
            { FindToolsSceneName, TutorialStep.FindTools },
            { PreFarmCutsceneSceneName, TutorialStep.PreFarmCutscene },
            { FarmTutorialSceneName, TutorialStep.FarmTutorial },
        };

        public static IReadOnlyList<string> SceneOrder { get; } = new[]
        {
            IntroSceneName,
            ChickenGameSceneName,
            PostChickenCutsceneSceneName,
            MidpointPlaceholderSceneName,
            FindToolsSceneName,
            PreFarmCutsceneSceneName,
            FarmTutorialSceneName,
        };

        public static string GetSceneName(TutorialStep step)
        {
            return StepToScene.TryGetValue(step, out var sceneName) ? sceneName : null;
        }

        public static TutorialStep GetStepForScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return TutorialStep.None;

            return SceneToStep.TryGetValue(sceneName, out var step) ? step : TutorialStep.None;
        }

        public static TutorialStep GetNextStep(TutorialStep step)
        {
            return step switch
            {
                TutorialStep.Intro => TutorialStep.ChickenHunt,
                TutorialStep.ChickenHunt => TutorialStep.PostChickenCutscene,
                TutorialStep.PostChickenCutscene => TutorialStep.MidpointPlaceholder,
                TutorialStep.MidpointPlaceholder => TutorialStep.FindTools,
                TutorialStep.FindTools => TutorialStep.PreFarmCutscene,
                TutorialStep.PreFarmCutscene => TutorialStep.FarmTutorial,
                _ => TutorialStep.None
            };
        }

        public static TutorialStep GetPreviousStep(TutorialStep step)
        {
            return step switch
            {
                TutorialStep.ChickenHunt => TutorialStep.Intro,
                TutorialStep.PostChickenCutscene => TutorialStep.ChickenHunt,
                TutorialStep.MidpointPlaceholder => TutorialStep.PostChickenCutscene,
                TutorialStep.FindTools => TutorialStep.MidpointPlaceholder,
                TutorialStep.PreFarmCutscene => TutorialStep.FindTools,
                TutorialStep.FarmTutorial => TutorialStep.PreFarmCutscene,
                _ => TutorialStep.None
            };
        }
    }
}
