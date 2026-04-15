using System.IO;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmSimVR.Editor
{
    public static class CreateGenerativePlaythroughMenuScene
    {
        [MenuItem("FarmSimVR/Create Generative Playthrough Menu Scene")]
        public static void Create()
        {
            CreateInternal();
        }

        public static void CreateIfMissing()
        {
            if (File.Exists(SceneWorkCatalog.GenerativePlaythroughMenuScenePath))
                return;

            CreateInternal();
        }

        private static void CreateInternal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.06f, 0.12f);
            cameraObject.AddComponent<AudioListener>();

            var controllerObject = new GameObject("GenerativePlaythroughMenuController");
            controllerObject.AddComponent<GenerativePlaythroughMenuController>();

            Directory.CreateDirectory(Path.GetDirectoryName(SceneWorkCatalog.GenerativePlaythroughMenuScenePath) ?? "Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, SceneWorkCatalog.GenerativePlaythroughMenuScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[GenerativePlaythroughMenu] Scene created.");
        }
    }
}
