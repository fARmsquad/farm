using FarmSimVR.Core.Tutorial;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public static class TutorialSceneInstaller
    {
        private const float QuickCutsceneDelay = TutorialDevTuning.PlaceholderCutsceneAutoAdvanceDelay;

        public static void InstallForScene(string sceneName, TutorialFlowController controller)
        {
            if (controller == null)
                return;

            switch (sceneName)
            {
                case TutorialSceneCatalog.PostChickenCutsceneSceneName:
                    EnsureCutscene(
                        sceneName,
                        "Chicken Hunt Complete",
                        "You proved you can handle the first job. Now gather what you need to start farming.",
                        QuickCutsceneDelay);
                    break;
                case TutorialSceneCatalog.MidpointPlaceholderSceneName:
                    EnsureCutscene(
                        sceneName,
                        "Story Beat Placeholder",
                        "This bridge scene is intentionally lightweight for now. It preserves pacing until the real story content lands.",
                        QuickCutsceneDelay);
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
            var controller = EnsureComponent<TutorialCutsceneSceneController>(objectName);
            controller.Configure(title, body, autoAdvanceDelay);
        }

        private static T EnsureComponent<T>(string objectName) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>();
            if (existing != null)
                return existing;

            var gameObject = new GameObject(objectName);
            return gameObject.AddComponent<T>();
        }
    }
}
