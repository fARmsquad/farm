#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace FarmSimVR.Editor.Fonts
{
    /// <summary>
    /// Assigns <see cref="LoraTmpFontGenerator"/> output to all TextMeshPro UGUI in Intro.unity.
    /// </summary>
    public static class IntroSceneLoraFontApplier
    {
        private const string IntroScenePath = "Assets/_Project/Scenes/Intro.unity";

        [MenuItem("FarmSim/Fonts/Apply Lora To Intro Scene")]
        public static void Apply()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LoraTmpFontPaths.OutputPath);
            if (font == null)
            {
                EditorUtility.DisplayDialog(
                    "Lora SDF",
                    "Lora SDF asset not found. Use FarmSim → Fonts → Generate Lora SDF first, or wait for auto-generation after import.",
                    "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(IntroScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var count = 0;
            foreach (var root in roots)
            {
                var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    tmp.font = font;
                    count++;
                    EditorUtility.SetDirty(tmp);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[IntroSceneLoraFontApplier] Set Lora on {count} TextMeshProUGUI in Intro.unity.");
        }
    }
}
#endif
