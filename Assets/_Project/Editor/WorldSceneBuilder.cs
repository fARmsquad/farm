using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds the full world scene per INT-001 spec.
    /// Menu: FarmSim > Build World Scene (New)
    /// </summary>
    public static class WorldSceneBuilder
    {
        // ── World Constants ───────────────────────────────────────
        public const float TerrainSize = 400f;
        public const float TerrainHeight = 20f;
        public const int HeightmapRes = 513;
        public const int AlphamapRes = 1024;

        // ── Zone Definitions ──────────────────────────────────────
        public static readonly string[] ZoneNames =
        {
            "Farm",
            "Town",
            "NorthField",
            "SandyShores",
            "Meadow",
            "River",
            "CountyFair",
            "WildflowerHills",
            "Trail"
        };

        /// <summary>
        /// One Vector4 per zone: (centerX, centerZ, sizeX, sizeZ).
        /// </summary>
        public static readonly Vector4[] ZoneBounds =
        {
            new(120, 40, 160, 120),        // Farm
            new(-120, 10, 160, 180),       // Town
            new(-120, 150, 160, 100),      // NorthField
            new(120, 160, 160, 80),        // SandyShores
            new(-130, -120, 140, 80),      // Meadow
            new(30, -120, 180, 80),        // River
            new(160, -120, 80, 80),        // CountyFair
            new(0, -180, 400, 40),         // WildflowerHills
            new(0, 50, 80, 100)            // Trail
        };

        // ── Menu Entry ────────────────────────────────────────────

        [MenuItem("FarmSim/Build World Scene (New)")]
        public static void CreateWorldScene()
        {
            if (!EditorUtility.DisplayDialog(
                "Build World Scene",
                "This will create a new world scene with all zones. Continue?",
                "Build", "Cancel"))
                return;

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BuildSceneConfig();
            BuildTerrain();
            BuildZoneHierarchy();
            BuildWater();
            BuildPaths();
            BuildFarmZone();
            BuildTownZone();
            BuildUnpopulatedZones();
            BuildVegetation();
            BuildFX();
            BuildMarkers();

            EditorSceneManager.SaveScene(scene,
                "Assets/_Project/Scenes/WorldMain.unity");
            Debug.Log("[WorldSceneBuilder] WorldMain.unity created and saved.");
        }

        // ── Stub Methods (filled in by subsequent tasks) ──────────

        private static void BuildSceneConfig() { }
        private static void BuildTerrain() { }
        private static void BuildZoneHierarchy() { }
        private static void BuildWater() { }
        private static void BuildPaths() { }
        private static void BuildFarmZone() { }
        private static void BuildTownZone() { }
        private static void BuildUnpopulatedZones() { }
        private static void BuildVegetation() { }
        private static void BuildFX() { }
        private static void BuildMarkers() { }

        // ── Helpers ───────────────────────────────────────────────

        /// <summary>
        /// Creates a new empty GameObject, sets its position, and optionally parents it.
        /// </summary>
        public static GameObject CreateEmpty(string name, Vector3 pos, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            if (parent != null)
                go.transform.SetParent(parent);
            return go;
        }

        /// <summary>
        /// Loads a prefab from AssetDatabase and instantiates it via PrefabUtility.
        /// Falls back to a magenta placeholder cube if the prefab is not found.
        /// </summary>
        public static GameObject InstantiatePrefab(string path, Vector3 pos, Quaternion rot, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = pos;
                instance.transform.rotation = rot;
                if (parent != null)
                    instance.transform.SetParent(parent);
                return instance;
            }

            // Fallback: magenta placeholder cube
            Debug.LogWarning($"[WorldSceneBuilder] Prefab not found at '{path}', using placeholder.");
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = $"MISSING_{System.IO.Path.GetFileNameWithoutExtension(path)}";
            placeholder.transform.position = pos;
            placeholder.transform.rotation = rot;
            if (parent != null)
                placeholder.transform.SetParent(parent);

            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.magenta;
                renderer.material = mat;
            }

            return placeholder;
        }
    }
}
