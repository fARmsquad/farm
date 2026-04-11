using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Tutorial
{
    public static class SceneWorkCatalog
    {
        public const string WorldSandboxSceneName = "WorldMain";

        private static readonly Dictionary<string, SceneWorkDefinition> BySceneName = new(StringComparer.Ordinal);

        public static IReadOnlyList<SceneWorkDefinition> OrderedScenes { get; } = BuildOrderedScenes();

        public static bool TryGetBySceneName(string sceneName, out SceneWorkDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                definition = default;
                return false;
            }

            return BySceneName.TryGetValue(sceneName, out definition);
        }

        private static IReadOnlyList<SceneWorkDefinition> BuildOrderedScenes()
        {
            var ordered = new[]
            {
                Define(1, TutorialSceneCatalog.IntroSceneName, "Intro Cutscene", "Own the opening image sequence, timing, and handoff into the chicken chase.", SceneWorkKind.Cutscene, TutorialSceneCatalog.ChickenGameSceneName),
                Define(2, TutorialSceneCatalog.ChickenGameSceneName, "Chicken Chase Game", "Own the first playable beat: catch the chicken and complete into the next bridge scene.", SceneWorkKind.Gameplay, TutorialSceneCatalog.PostChickenCutsceneSceneName),
                Define(3, TutorialSceneCatalog.PostChickenCutsceneSceneName, "Post-Chicken Cutscene", "Own the short bridge after the chase and tee up the tools arc.", SceneWorkKind.Cutscene, TutorialSceneCatalog.MidpointPlaceholderSceneName),
                Define(4, TutorialSceneCatalog.MidpointPlaceholderSceneName, "Tool Bridge Placeholder", "Own the lightweight placeholder story beat that preserves the structure between minigames.", SceneWorkKind.Cutscene, TutorialSceneCatalog.FindToolsSceneName),
                Define(5, TutorialSceneCatalog.FindToolsSceneName, "Find Tools Game", "Own the isolated tool-recovery beat and its transition into the final farm setup cutscene.", SceneWorkKind.Gameplay, TutorialSceneCatalog.PreFarmCutsceneSceneName),
                Define(6, TutorialSceneCatalog.PreFarmCutsceneSceneName, "Pre-Farm Cutscene", "Own the final short bridge that frames the first farm loop.", SceneWorkKind.Cutscene, TutorialSceneCatalog.FarmTutorialSceneName),
                Define(7, TutorialSceneCatalog.FarmTutorialSceneName, "Farm Tutorial Game", "Own the first complete plant-water-harvest loop and any onboarding polish in FarmMain.", SceneWorkKind.Gameplay, null),
                Define(8, WorldSandboxSceneName, "World Sandbox", "Own the open-world integration slice used for farming, pen-game, and sandbox iteration outside the tutorial chain.", SceneWorkKind.Sandbox, null),
            };

            foreach (var scene in ordered)
                BySceneName[scene.SceneName] = scene;

            return ordered;
        }

        private static SceneWorkDefinition Define(
            int number,
            string sceneName,
            string displayName,
            string focusDescription,
            SceneWorkKind kind,
            string nextSceneName)
        {
            return new SceneWorkDefinition(number, sceneName, displayName, focusDescription, kind, nextSceneName);
        }
    }
}
