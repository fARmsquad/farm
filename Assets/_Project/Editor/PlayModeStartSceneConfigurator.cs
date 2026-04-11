using UnityEditor;
using UnityEditor.SceneManagement;

namespace FarmSimVR.Editor
{
    [InitializeOnLoad]
    public static class PlayModeStartSceneConfigurator
    {
        public const string IntroScenePath = "Assets/_Project/Scenes/Intro.unity";

        static PlayModeStartSceneConfigurator()
        {
            Apply();
        }

        public static SceneAsset Apply()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroScenePath);
            if (sceneAsset != null)
                EditorSceneManager.playModeStartScene = sceneAsset;

            return sceneAsset;
        }
    }
}
