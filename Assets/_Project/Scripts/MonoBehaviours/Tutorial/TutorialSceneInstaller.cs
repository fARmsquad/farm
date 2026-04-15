using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public static class TutorialSceneInstaller
    {
        private const float QuickCutsceneDelay = TutorialDevTuning.PlaceholderCutsceneAutoAdvanceDelay;
        private static readonly string[] AuthoredPostChickenObjectNames =
        {
            "SlideshowPanel",
            "SlideshowDirector",
            "TextOverlayCanvas",
        };

        public static void InstallForScene(string sceneName, TutorialFlowController controller)
        {
            if (controller == null)
                return;

            sceneName = TutorialSceneCatalog.NormalizeRuntimeSceneName(sceneName);

            switch (sceneName)
            {
                case TutorialSceneCatalog.PostChickenCutsceneSceneName:
                    EnsureCutscene(
                        sceneName,
                        "Post Chicken Bridge",
                        "The chicken chase is over. Follow the story into the next task.",
                        QuickCutsceneDelay);
                    DisableAuthoredPostChickenCutscene();
                    break;
                case TutorialSceneCatalog.CoreSceneSceneName:
                    break;
                case TutorialSceneCatalog.PreFarmCutsceneSceneName:
                    EnsureCutscene(
                        sceneName,
                        "Ready To Farm",
                        "The tools are back in your hands. Head to the plots, plant a seed, water it, and bring in your first harvest.",
                        QuickCutsceneDelay);
                    break;
                case TutorialSceneCatalog.ChickenGameSceneName:
                    EnsureComponent<TutorialChickenSceneController>("TutorialChickenSceneController");
                    break;
                case TutorialSceneCatalog.FindToolsSceneName:
                    EnsureComponent<TutorialFindToolsSceneController>("TutorialFindToolsSceneController");
                    break;
                case TutorialSceneCatalog.FarmTutorialSceneName:
                    EnsureComponent<TutorialFarmSceneController>("TutorialFarmSceneController");
                    break;
                case SceneWorkCatalog.HorseTrainingSceneName:
                    EnsureComponent<TutorialHorseTrainingSceneController>("TutorialHorseTrainingSceneController");
                    break;
            }
        }

        private static void EnsureCutscene(string objectName, string title, string body, float autoAdvanceDelay)
        {
            if (StoryPackageRuntimeCatalog.TryGetStoryboard(objectName, out var storyboardTitle, out var storyboard))
            {
                if (!string.IsNullOrWhiteSpace(storyboardTitle))
                    title = storyboardTitle;

                var storyboardController = EnsureComponent<TutorialCutsceneSceneController>(objectName);
                storyboardController.ConfigureStoryboard(title, storyboard.Shots, autoAdvanceDelay);
                return;
            }

            if (StoryPackageRuntimeCatalog.TryGetCutsceneDisplayText(objectName, out var packageTitle, out var packageBody))
            {
                if (!string.IsNullOrWhiteSpace(packageTitle))
                    title = packageTitle;
                if (!string.IsNullOrWhiteSpace(packageBody))
                    body = packageBody;
            }

            var controller = EnsureComponent<TutorialCutsceneSceneController>(objectName);
            controller.Configure(title, body, autoAdvanceDelay);
        }

        private static void DisableAuthoredPostChickenCutscene()
        {
            for (int i = 0; i < AuthoredPostChickenObjectNames.Length; i++)
                DisableSceneObject(AuthoredPostChickenObjectNames[i]);
        }

        private static T EnsureComponent<T>(string objectName) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>();
            if (existing != null)
                return existing;

            var gameObject = new GameObject(objectName);
            return gameObject.AddComponent<T>();
        }

        private static void DisableSceneObject(string objectName)
        {
            var sceneObject = GameObject.Find(objectName);
            if (sceneObject == null)
                return;

            sceneObject.SetActive(false);
            GeneratedStorySliceDiagnostics.Log(
                nameof(TutorialSceneInstaller),
                $"Disabled authored PostChicken object '{objectName}' in favor of runtime cutscene playback.");
        }
    }
}
