using UnityEngine;
using UnityEditor;
using System.IO;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Generates wrapper prefabs for every GLB in the project's model folders.
    /// Structure:  Root (empty, identity) → Model (GLB child, +90° X to stand upright)
    /// Scripts, colliders, and tags go on Root — never on the raw GLB.
    /// </summary>
    public static class GLBWrapperPrefabBuilder
    {
        private static readonly string[] SourceFolders =
        {
            "Assets/_Project/Art/Models/Source",
            "Assets/_Project/Art/Models/Imported",
        };

        private const string OutputFolder = "Assets/_Project/Prefabs/Models";

        // Rotation applied to the GLB child to correct Blender Z-up → Unity Y-up
        private static readonly Quaternion ModelCorrection = Quaternion.Euler(90f, 0f, 0f);

        [MenuItem("FarmSim/Build GLB Wrapper Prefabs")]
        public static void BuildAll()
        {
            EnsureFolder(OutputFolder);

            int created = 0, skipped = 0;

            foreach (var folder in SourceFolders)
            {
                var guids = AssetDatabase.FindAssets("t:GameObject", new[] { folder });
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPath.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    var modelName  = Path.GetFileNameWithoutExtension(assetPath);
                    var prefabPath = $"{OutputFolder}/{modelName}.prefab";

                    // Skip if prefab already exists (don't overwrite manual edits)
                    if (File.Exists(prefabPath))
                    {
                        skipped++;
                        continue;
                    }

                    if (CreateWrapperPrefab(assetPath, modelName, prefabPath))
                        created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GLBWrapperPrefabBuilder] Done — {created} created, {skipped} already existed.");
        }

        [MenuItem("FarmSim/Rebuild ALL GLB Wrapper Prefabs (overwrite)")]
        public static void RebuildAll()
        {
            EnsureFolder(OutputFolder);
            int count = 0;

            foreach (var folder in SourceFolders)
            {
                var guids = AssetDatabase.FindAssets("t:GameObject", new[] { folder });
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPath.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    var modelName  = Path.GetFileNameWithoutExtension(assetPath);
                    var prefabPath = $"{OutputFolder}/{modelName}.prefab";

                    if (CreateWrapperPrefab(assetPath, modelName, prefabPath))
                        count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GLBWrapperPrefabBuilder] Rebuilt {count} wrapper prefabs.");
        }

        static bool CreateWrapperPrefab(string assetPath, string modelName, string prefabPath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"[GLBWrapperPrefabBuilder] Could not load model at {assetPath}");
                return false;
            }

            // Build hierarchy in memory
            var root  = new GameObject(modelName);
            var child = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            child.transform.SetLocalPositionAndRotation(Vector3.zero, ModelCorrection);

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            return true;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts  = path.Split('/');
            var parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(parent, parts[i]);
                parent = next;
            }
        }
    }
}
