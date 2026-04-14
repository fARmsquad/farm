using System;
using System.Collections.Generic;
using System.IO;

namespace FarmSimVR.Core.Tutorial
{
    public static class SceneWorkCatalog
    {
        public const string TitleScreenScenePath = "Assets/_Project/Scenes/TitleScreen.unity";
        public const string WorldSandboxSceneName = "WorldMain";
        public const string WorldSandboxScenePath = "Assets/_Project/Scenes/WorldMain.unity";
        public const string HorseTrainingSceneName = "HorseTrainingGame";
        public const string HorseTrainingScenePath = "Assets/_Project/Scenes/HorseTrainingGame.unity";
        public const string TownSceneName = "Town";
        public const string TownScenePath = "Assets/_Project/Scenes/Town.unity";
        public const string FarmVegetableStatesSceneName = "FarmVegetableStates";
        public const string FarmVegetableStatesScenePath = "Assets/_Project/Scenes/FarmVegetableStates.unity";

        private static readonly Dictionary<string, SceneWorkDefinition> BySceneName = new(StringComparer.Ordinal);

        static SceneWorkCatalog()
        {
            OrderedScenes = BuildOrderedScenes();
            TutorialOrderedScenes = BuildTutorialOrderedScenes(OrderedScenes);
            TitleScreenLaunchableScenes = BuildTitleScreenLaunchableScenes(TutorialOrderedScenes);
            TitleScreenBuildScenePaths = BuildTitleScreenBuildScenePaths(TitleScreenLaunchableScenes);

            RegisterScenes(OrderedScenes);
            RegisterScenes(TitleScreenLaunchableScenes);
        }

        public static IReadOnlyList<SceneWorkDefinition> OrderedScenes { get; }
        public static IReadOnlyList<SceneWorkDefinition> TutorialOrderedScenes { get; }
        public static IReadOnlyList<SceneWorkDefinition> TitleScreenLaunchableScenes { get; }
        public static IReadOnlyList<string> TitleScreenBuildScenePaths { get; }
        public static string FirstTutorialSceneName => TutorialOrderedScenes.Count > 0
            ? TutorialOrderedScenes[0].SceneName
            : TutorialSceneCatalog.IntroSceneName;

        public static bool TryGetBySceneName(string sceneName, out SceneWorkDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                definition = default;
                return false;
            }

            return BySceneName.TryGetValue(sceneName, out definition);
        }

        public static string GetLoadableSceneName(string sceneName)
        {
            if (!TryGetBySceneName(sceneName, out var definition) || string.IsNullOrWhiteSpace(definition.ScenePath))
                return sceneName;

            return Path.GetFileNameWithoutExtension(definition.ScenePath);
        }

        private static IReadOnlyList<SceneWorkDefinition> BuildOrderedScenes()
        {
            var ordered = new[]
            {
                Define(1, TutorialSceneCatalog.IntroSceneName, "Assets/_Project/Scenes/Intro.unity", "Intro Cutscene", "Own the opening image sequence, timing, and handoff into the chicken chase.", SceneWorkKind.Cutscene, TutorialSceneCatalog.ChickenGameSceneName),
                Define(2, TutorialSceneCatalog.ChickenGameSceneName, "Assets/_Project/Scenes/ChickenGame.unity", "Chicken Chase Game", "Own the first playable beat: catch the chicken and complete into the next bridge scene.", SceneWorkKind.Gameplay, TutorialSceneCatalog.PostChickenCutsceneSceneName),
                Define(3, TutorialSceneCatalog.PostChickenCutsceneSceneName, "Assets/_Project/Scenes/CaughtChickenCutscene.unity", "Caught chicken cutscene", "Own the short bridge after the chase and tee up the tools arc.", SceneWorkKind.Cutscene, TutorialSceneCatalog.CoreSceneSceneName),
                Define(4, TutorialSceneCatalog.CoreSceneSceneName, "Assets/_Project/Scenes/CoreScene.unity", "Core Hub", "Own the persistent hub scene and portal handoff into the find-tools beat.", SceneWorkKind.Gameplay, TutorialSceneCatalog.FindToolsSceneName),
                Define(5, TutorialSceneCatalog.FindToolsSceneName, "Assets/_Project/Scenes/FindToolsGame.unity", "Find Tools Game", "Own the isolated tool-recovery beat and its transition into the final farm setup cutscene.", SceneWorkKind.Gameplay, TutorialSceneCatalog.PreFarmCutsceneSceneName),
                Define(6, TutorialSceneCatalog.PreFarmCutsceneSceneName, "Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity", "Pre-Farm Cutscene", "Own the final short bridge that frames the first farm loop.", SceneWorkKind.Cutscene, TutorialSceneCatalog.FarmTutorialSceneName),
                Define(7, TutorialSceneCatalog.FarmTutorialSceneName, "Assets/_Project/Scenes/FarmMain.unity", "Farm Tutorial Game", "Own the first complete plant-water-harvest loop and any onboarding polish in FarmMain.", SceneWorkKind.Gameplay, null),
                Define(8, WorldSandboxSceneName, WorldSandboxScenePath, "World Sandbox", "Own the open-world integration slice used for farming, pen-game, and sandbox iteration outside the tutorial chain.", SceneWorkKind.Sandbox, null),
            };

            return ordered;
        }

        private static IReadOnlyList<SceneWorkDefinition> BuildTutorialOrderedScenes(IReadOnlyList<SceneWorkDefinition> orderedScenes)
        {
            var tutorialScenes = new List<SceneWorkDefinition>(orderedScenes.Count);
            foreach (var scene in orderedScenes)
            {
                if (scene.Kind == SceneWorkKind.Sandbox)
                    continue;

                tutorialScenes.Add(scene);
            }

            return tutorialScenes;
        }

        private static IReadOnlyList<SceneWorkDefinition> BuildTitleScreenLaunchableScenes(IReadOnlyList<SceneWorkDefinition> tutorialScenes)
        {
            var launchableScenes = new List<SceneWorkDefinition>(tutorialScenes.Count + 3);
            foreach (var scene in tutorialScenes)
                launchableScenes.Add(scene);

            launchableScenes.Add(Define(
                9,
                HorseTrainingSceneName,
                HorseTrainingScenePath,
                "Horse Training Grounds",
                "Own the standalone greybox horse-training slice launched from the title screen.",
                SceneWorkKind.Gameplay,
                null));

            launchableScenes.Add(Define(
                10,
                TownSceneName,
                TownScenePath,
                "Town Conversation",
                "Own the standalone town interaction slice with NPC conversation, autoplay approach, and free-roam follow-up.",
                SceneWorkKind.Gameplay,
                null));

            launchableScenes.Add(Define(
                11,
                FarmVegetableStatesSceneName,
                FarmVegetableStatesScenePath,
                "Farm Vegetable States",
                "Own the dedicated crop-state review slice used to choose Scene 7 vegetable visuals before more lifecycle tuning.",
                SceneWorkKind.Sandbox,
                null));

            return launchableScenes;
        }

        private static IReadOnlyList<string> BuildTitleScreenBuildScenePaths(IReadOnlyList<SceneWorkDefinition> launchableScenes)
        {
            var paths = new List<string>(launchableScenes.Count + 2)
            {
                TitleScreenScenePath
            };

            foreach (var scene in launchableScenes)
                paths.Add(scene.ScenePath);

            paths.Add(WorldSandboxScenePath);
            return paths;
        }

        private static void RegisterScenes(IEnumerable<SceneWorkDefinition> scenes)
        {
            foreach (var scene in scenes)
                BySceneName[scene.SceneName] = scene;
        }

        private static SceneWorkDefinition Define(
            int number,
            string sceneName,
            string scenePath,
            string displayName,
            string focusDescription,
            SceneWorkKind kind,
            string nextSceneName)
        {
            return new SceneWorkDefinition(number, sceneName, scenePath, displayName, focusDescription, kind, nextSceneName);
        }
    }
}
