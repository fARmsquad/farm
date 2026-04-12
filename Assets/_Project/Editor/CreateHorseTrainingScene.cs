using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmSimVR.Editor
{
    public static class CreateHorseTrainingScene
    {
        [MenuItem("FarmSimVR/Create Horse Training Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var controllerRoot = new GameObject("HorseTrainingSceneController");
            controllerRoot.AddComponent<TutorialHorseTrainingSceneController>();

            EditorSceneManager.SaveScene(scene, SceneWorkCatalog.HorseTrainingScenePath);
            AssetDatabase.Refresh();
            CreateTitleScene.SyncBuildSettings();
            Debug.Log("[HorseTrainingScene] HorseTrainingGame.unity created and build settings synced.");
        }
    }
}
