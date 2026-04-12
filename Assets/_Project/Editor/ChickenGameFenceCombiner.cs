using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Merges many fence prefab instances into one <see cref="Mesh"/> / renderer to cut draw calls.
    /// Used by <see cref="ChickenGameSceneBuilder"/> and the one-shot menu for existing scenes.
    /// </summary>
    public static class ChickenGameFenceCombiner
    {
        const string ChickenGameScenePath = "Assets/_Project/Scenes/ChickenGame.unity";

        [MenuItem("FarmSim/Optimize/Combine ChickenGame Fence Meshes")]
        public static void CombineFencesMenu()
        {
            if (!TryCombineFencesInOpenScene(out var message))
            {
                Debug.LogWarning("[ChickenGameFenceCombiner] " + message);
                return;
            }

            Debug.Log("[ChickenGameFenceCombiner] " + message);
        }

        /// <summary>Batch-mode entry: open ChickenGame, combine fence meshes, save, exit 0.</summary>
        public static void BatchCombineChickenGameFences()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ChickenGameScenePath);
            if (!TryCombineFencesInOpenScene(out var message))
            {
                Debug.LogError("[ChickenGameFenceCombiner] " + message);
                EditorApplication.Exit(1);
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ChickenGameFenceCombiner] " + message);
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Finds <c>Fences</c> (under <c>Environment</c> or root), merges all child mesh renderers into one mesh.
        /// </summary>
        public static bool TryCombineFencesInOpenScene(out string message)
        {
            Transform fences = FindFencesRoot();
            if (fences == null)
            {
                message = "Could not find a GameObject named \"Fences\" in the open scene.";
                return false;
            }

            if (!TryCombineUnder(fences, "Fence_Combined", out message))
                return false;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fences.gameObject.scene);
            return true;
        }

        static Transform FindFencesRoot()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name == "Fences")
                    return root.transform;
                var env = root.transform.Find("Environment");
                if (env == null) continue;
                var f = env.Find("Fences");
                if (f != null) return f;
            }

            return GameObject.Find("Fences")?.transform;
        }

        /// <summary>Used when rebuilding ChickenGame from <see cref="ChickenGameSceneBuilder"/>.</summary>
        public static void CreateCombinedFencePanels(
            Transform parent,
            List<CombineInstance> combines,
            GameObject fencePrefab,
            string objectName)
        {
            if (combines == null || combines.Count == 0 || fencePrefab == null || parent == null)
                return;

            var mat = fencePrefab.GetComponentInChildren<MeshRenderer>()?.sharedMaterial;
            if (mat == null)
                return;

            int verts = CountVertices(combines);
            var go = BuildCombinedObject(parent, combines, mat, objectName, verts);
            ApplyVisualStaticAndShadows(go);
        }

        /// <summary>Appends mesh instances from one placed prefab into <paramref name="combines"/>.</summary>
        public static void AppendPrefabAtWorldPose(
            GameObject prefab,
            Vector3 worldPosition,
            Quaternion worldRotation,
            List<CombineInstance> combines)
        {
            if (prefab == null || combines == null)
                return;

            var temp = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (temp == null)
                return;

            temp.hideFlags = HideFlags.HideAndDontSave;
            temp.transform.SetPositionAndRotation(worldPosition, worldRotation);

            foreach (var mf in temp.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                combines.Add(new CombineInstance
                {
                    mesh       = mf.sharedMesh,
                    transform  = mf.transform.localToWorldMatrix,
                    subMeshIndex = 0
                });
            }

            Object.DestroyImmediate(temp);
        }

        internal static bool TryCombineUnder(Transform fencesRoot, string combinedObjectName, out string message)
        {
            if (fencesRoot == null)
            {
                message = "Fences root is null.";
                return false;
            }

            int childCount = fencesRoot.childCount;
            if (childCount == 0)
            {
                message = "Fences has no children — nothing to combine.";
                return false;
            }

            var children = new List<GameObject>(childCount);
            for (int i = 0; i < childCount; i++)
                children.Add(fencesRoot.GetChild(i).gameObject);

            var byMaterial = new Dictionary<Material, List<CombineInstance>>();

            foreach (var child in children)
            {
                if (child.name.StartsWith(combinedObjectName))
                    continue;

                foreach (var mf in child.GetComponentsInChildren<MeshFilter>())
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr == null || mf.sharedMesh == null) continue;

                    Material mat = mr.sharedMaterial;
                    if (mat == null) continue;

                    if (!byMaterial.TryGetValue(mat, out var list))
                    {
                        list = new List<CombineInstance>();
                        byMaterial[mat] = list;
                    }

                    list.Add(new CombineInstance
                    {
                        mesh         = mf.sharedMesh,
                        transform    = mf.transform.localToWorldMatrix,
                        subMeshIndex = 0
                    });
                }
            }

            int totalInstances = 0;
            foreach (var kv in byMaterial)
                totalInstances += kv.Value.Count;

            if (totalInstances == 0)
            {
                message = childCount == 0
                    ? "Fences has no children — nothing to combine."
                    : "No fence instances to combine (already merged, or only combined mesh present).";
                return false;
            }

            int idx = 0;
            foreach (var kv in byMaterial)
            {
                var list  = kv.Value;
                int verts = CountVertices(list);
                string nm = byMaterial.Count == 1
                    ? combinedObjectName
                    : $"{combinedObjectName}_{idx}";
                var combinedGo = BuildCombinedObject(fencesRoot, list, kv.Key, nm, verts);
                ApplyVisualStaticAndShadows(combinedGo);
                idx++;
            }

            foreach (var child in children)
            {
                if (child == null || child.name.StartsWith(combinedObjectName)) continue;
                Undo.DestroyObjectImmediate(child);
            }

            message = $"Combined {totalInstances} mesh instance(s) into {byMaterial.Count} mesh(es) under \"{combinedObjectName}\".";
            return true;
        }

        static GameObject BuildCombinedObject(
            Transform parent,
            List<CombineInstance> combines,
            Material sharedMaterial,
            string objectName,
            int vertexCount)
        {
            var mesh = new Mesh { name = objectName + "_Mesh" };
            if (vertexCount > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.CombineMeshes(combines.ToArray(), true, true);

            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = sharedMaterial;

            return go;
        }

        static void ApplyVisualStaticAndShadows(GameObject go)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode   = ShadowCastingMode.Off;
                mr.receiveShadows      = false;
                mr.lightProbeUsage     = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            GameObjectUtility.SetStaticEditorFlags(
                go,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        static int CountVertices(List<CombineInstance> combines)
        {
            int n = 0;
            foreach (var c in combines)
            {
                if (c.mesh != null)
                    n += c.mesh.vertexCount;
            }

            return n;
        }
    }
}
