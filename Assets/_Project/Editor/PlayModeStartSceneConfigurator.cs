using UnityEditor;
using UnityEditor.SceneManagement;
using FarmSimVR.Core.Tutorial;

namespace FarmSimVR.Editor
{
    [InitializeOnLoad]
    public static class PlayModeStartSceneConfigurator
    {
        public const string TitleScreenScenePath = SceneWorkCatalog.TitleScreenScenePath;

        static PlayModeStartSceneConfigurator()
        {
            Apply();
        }

        public static SceneAsset Apply()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScreenScenePath);
            if (sceneAsset != null)
                EditorSceneManager.playModeStartScene = sceneAsset;

            return sceneAsset;
        }
    }
}
