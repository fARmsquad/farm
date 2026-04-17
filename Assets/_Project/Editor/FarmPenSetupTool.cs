using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FarmSimVR.MonoBehaviours.Hunting;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.Editor
{
    public static class FarmPenSetupTool
    {
        [MenuItem("fARm/Setup/Install FarmMain Animal Pen")]
        public static void InstallFarmMainPen()
        {
            // Pen center placed beside the chicken coop (which is at 16,0,17)
            var penCenter = new Vector3(22f, 0f, 17f);
            const float penRadius = 5f;

            // --- AnimalPenHost ---
            var existing = GameObject.Find("AnimalPenHost");
            if (existing != null)
            {
                Debug.Log("[FarmPenSetupTool] AnimalPenHost already exists — removing and recreating.");
                Object.DestroyImmediate(existing);
            }

            var host = new GameObject("AnimalPenHost");
            host.transform.position = Vector3.zero;

            // AnimalPen
            var pen = host.AddComponent<AnimalPen>();
            pen.ConfigureRuntime(BuildPrefabEntries(), penCenter, penRadius, true);

            // FarmPenSpawner
            host.AddComponent<FarmPenSpawner>();

            EditorSceneManager.MarkSceneDirty(host.scene);
            Debug.Log($"[FarmPenSetupTool] AnimalPenHost created at world origin. Pen center={penCenter}, radius={penRadius}. Save the scene to persist.");
        }

        private static PenAnimalEntry[] BuildPrefabEntries()
        {
            return new[]
            {
                LoadEntry(AnimalType.Chicken, "Assets/_Project/Prefabs/Animals/Chicken.prefab"),
                LoadEntry(AnimalType.Pig,     "Assets/_Project/Prefabs/Animals/Pig.prefab"),
                LoadEntry(AnimalType.Horse,   "Assets/_Project/Prefabs/Animals/Horse.prefab"),
            };
        }

        private static PenAnimalEntry LoadEntry(AnimalType type, string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                Debug.LogWarning($"[FarmPenSetupTool] Prefab not found at: {path}");

            return new PenAnimalEntry { type = type, prefab = prefab };
        }
    }
}
