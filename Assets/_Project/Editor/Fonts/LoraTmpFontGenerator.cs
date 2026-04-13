#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace FarmSimVR.Editor.Fonts
{
    /// <summary>Paths for Lora TMP assets (Google Fonts OFL).</summary>
    public static class LoraTmpFontPaths
    {
        public const string FontPath = "Assets/_Project/Art/Fonts/Lora[wght].ttf";
        public const string OutputPath = "Assets/_Project/Art/Fonts/Lora SDF.asset";
    }

    /// <summary>Creates <see cref="LoraTmpFontPaths.OutputPath"/> on first editor load if missing.</summary>
    [InitializeOnLoad]
    internal sealed class LoraTmpFontBootstrap
    {
        static LoraTmpFontBootstrap()
        {
            if (Application.isBatchMode)
                return;

            EditorApplication.delayCall += EnsureSdfExistsAfterLoad;
        }

        private static void EnsureSdfExistsAfterLoad()
        {
            EditorApplication.delayCall -= EnsureSdfExistsAfterLoad;
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LoraTmpFontPaths.OutputPath) != null)
            {
                return;
            }

            LoraTmpFontGenerator.TryGenerate(out _);
        }
    }

    /// <summary>
    /// Generates Lora TMP SDF. Use menu or batchmode; bootstrap also runs once after import.
    /// </summary>
    public static class LoraTmpFontGenerator
    {
        [MenuItem("FarmSim/Fonts/Generate Lora SDF")]
        public static void Generate()
        {
            if (!TryGenerate(out var message))
            {
                EditorUtility.DisplayDialog("Lora SDF", message, "OK");
            }
        }

        /// <summary>
        /// For Unity batchmode: <c>-executeMethod FarmSimVR.Editor.Fonts.LoraTmpFontGenerator.GenerateForBatch</c>
        /// </summary>
        public static void GenerateForBatch()
        {
            if (!TryGenerate(out var message))
            {
                Debug.LogError("[LoraTmpFontGenerator] " + message);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[LoraTmpFontGenerator] Created " + LoraTmpFontPaths.OutputPath);
            EditorApplication.Exit(0);
        }

        internal static bool TryGenerate(out string message)
        {
            message = string.Empty;
            var font = AssetDatabase.LoadAssetAtPath<Font>(LoraTmpFontPaths.FontPath);
            if (font == null)
            {
                message = $"Font not found: {LoraTmpFontPaths.FontPath}";
                return false;
            }

            if (File.Exists(LoraTmpFontPaths.OutputPath))
            {
                AssetDatabase.DeleteAsset(LoraTmpFontPaths.OutputPath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 72,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic);

            fontAsset.name = "Lora SDF";
            AssetDatabase.CreateAsset(fontAsset, LoraTmpFontPaths.OutputPath);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            message = "OK";
            return true;
        }
    }
}
#endif
