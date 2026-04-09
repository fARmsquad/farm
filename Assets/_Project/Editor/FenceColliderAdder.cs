using UnityEngine;
using UnityEditor;

namespace FarmSimVR.Editor
{
    public static class FenceColliderAdder
    {
        [MenuItem("FarmSim/Add Colliders to Placed Fences")]
        public static void AddFenceColliders()
        {
            var allObjects = Object.FindObjectsByType<MeshFilter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int added = 0;
            foreach (var mf in allObjects)
            {
                var go = mf.gameObject;

                // Target manually placed gatedfence pieces only
                if (!go.name.Contains("fence", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (mf.sharedMesh == null) continue;

                // Reuse existing MeshCollider or add a new one
                var mc = go.GetComponent<MeshCollider>() ?? go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex     = false;
                // Disable fast midphase — required for meshes with >2M triangles to avoid missed collisions
                mc.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                                  | MeshColliderCookingOptions.EnableMeshCleaning
                                  | MeshColliderCookingOptions.WeldColocatedVertices;

                EditorUtility.SetDirty(go);
                added++;
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"[FenceColliderAdder] Added MeshColliders to {added} fence object(s).");
        }
    }
}
