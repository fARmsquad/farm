using FarmSimVR.MonoBehaviours.HorseTaming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmSimVR.Editor
{
    public static class CreateHorseTamingScene
    {
        private const string ScenePath = "Assets/_Project/Scenes/HorseTaming.unity";

        [MenuItem("FarmSimVR/Create HorseTaming Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            HorseTamingWorldBuilder.BuildIfNeeded();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            AddSceneToBuild(ScenePath);
            Debug.Log("[HorseTaming] Scene saved to " + ScenePath + " and added to Build Settings.");
        }

        /// <summary>For batchmode: -executeMethod FarmSimVR.Editor.CreateHorseTamingScene.GenerateForBatch</summary>
        public static void GenerateForBatch()
        {
            Create();
            EditorApplication.Exit(0);
        }

        private static void AddSceneToBuild(string path)
        {
            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < existing.Count; i++)
            {
                if (existing[i].path == path)
                    return;
            }

            existing.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = existing.ToArray();
        }
    }
}
