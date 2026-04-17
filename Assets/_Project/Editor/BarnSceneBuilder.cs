using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Hunting;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds the Barn interior scene and wires the barn door portal in FarmMain.
    /// Run: fARm/Setup/Build Barn Interior  (run while Barn.unity is open)
    ///      fARm/Setup/Wire Barn Door in FarmMain  (run while FarmMain.unity is open)
    /// </summary>
    public static class BarnSceneBuilder
    {
        private const string BarnScenePath = "Assets/_Project/Scenes/Barn.unity";
        private const string FarmMainScenePath = "Assets/_Project/Scenes/FarmMain.unity";
        private const string BarnEntrySpawnName = "BarnEntrySpawn";
        private const string BarnExitSpawnName = "BarnExitSpawn";

        // Interior dimensions
        private const float HalfWidth = 6f;    // 12 m wide
        private const float HalfDepth = 8f;    // 16 m deep
        private const float Height = 5f;
        private const float DoorHalfWidth = 1.5f; // 3 m door
        private const float DoorHeight = 3f;
        private const float WallThickness = 0.2f;

        // Barn building position in FarmMain (door faces -Z / south)
        // Placed past the animal pen (pen center is 22,0,17) to the east side.
        private static readonly Vector3 BarnPosition = new Vector3(32f, 0f, 17f);
        private const string BarnPrefabPath = "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Barn_01.prefab";

        // ---------- Barn interior ----------

        [MenuItem("fARm/Setup/Build Barn Interior")]
        public static void BuildBarnInterior()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != BarnScenePath)
            {
                EditorUtility.DisplayDialog("Wrong scene",
                    "Open Barn.unity first, then run this menu item.", "OK");
                return;
            }

            ClearExistingSetup();

            var root = new GameObject("BarnInterior");
            BuildFloor(root);
            BuildWalls(root);
            BuildCeiling(root);
            BuildEntrySpawn();
            BuildExitTrigger();
            BuildAnimalPen();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BarnSceneBuilder] Barn interior built and saved.");
        }

        private static void ClearExistingSetup()
        {
            foreach (var name in new[] { "BarnInterior", BarnEntrySpawnName, "BarnExitDoor", "BarnAnimalPen" })
            {
                var existing = GameObject.Find(name);
                if (existing != null)
                    Object.DestroyImmediate(existing);
            }
        }

        private static void BuildFloor(GameObject parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(parent.transform);
            // Plane is 10x10 at scale 1; scale to 12x16
            floor.transform.localScale = new Vector3(HalfWidth * 2f / 10f, 1f, HalfDepth * 2f / 10f);
            floor.transform.localPosition = Vector3.zero;
            ApplyWoodColor(floor, new Color(0.45f, 0.30f, 0.15f));
        }

        private static void BuildWalls(GameObject parent)
        {
            var wallColor = new Color(0.52f, 0.36f, 0.18f);

            // Left wall
            MakeWall(parent, "Wall_Left",
                new Vector3(-HalfWidth - WallThickness * 0.5f, Height * 0.5f, 0f),
                new Vector3(WallThickness, Height, HalfDepth * 2f + WallThickness * 2f),
                wallColor);

            // Right wall
            MakeWall(parent, "Wall_Right",
                new Vector3(HalfWidth + WallThickness * 0.5f, Height * 0.5f, 0f),
                new Vector3(WallThickness, Height, HalfDepth * 2f + WallThickness * 2f),
                wallColor);

            // Back wall
            MakeWall(parent, "Wall_Back",
                new Vector3(0f, Height * 0.5f, HalfDepth + WallThickness * 0.5f),
                new Vector3(HalfWidth * 2f + WallThickness * 2f, Height, WallThickness),
                wallColor);

            // Front wall — left of door
            float sideWidth = HalfWidth - DoorHalfWidth;
            MakeWall(parent, "Wall_Front_L",
                new Vector3(-(DoorHalfWidth + sideWidth * 0.5f), Height * 0.5f, -HalfDepth - WallThickness * 0.5f),
                new Vector3(sideWidth, Height, WallThickness),
                wallColor);

            // Front wall — right of door
            MakeWall(parent, "Wall_Front_R",
                new Vector3(DoorHalfWidth + sideWidth * 0.5f, Height * 0.5f, -HalfDepth - WallThickness * 0.5f),
                new Vector3(sideWidth, Height, WallThickness),
                wallColor);

            // Front wall — above door lintel
            MakeWall(parent, "Wall_Front_Top",
                new Vector3(0f, DoorHeight + (Height - DoorHeight) * 0.5f, -HalfDepth - WallThickness * 0.5f),
                new Vector3(DoorHalfWidth * 2f, Height - DoorHeight, WallThickness),
                wallColor);
        }

        private static void BuildCeiling(GameObject parent)
        {
            MakeWall(parent, "Ceiling",
                new Vector3(0f, Height + WallThickness * 0.5f, 0f),
                new Vector3(HalfWidth * 2f + WallThickness * 2f, WallThickness, HalfDepth * 2f + WallThickness * 2f),
                new Color(0.35f, 0.22f, 0.10f));
        }

        private static GameObject MakeWall(GameObject parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent.transform);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            ApplyWoodColor(wall, color);
            return wall;
        }

        private static void BuildEntrySpawn()
        {
            var spawn = new GameObject(BarnEntrySpawnName);
            // Just inside the door, facing into the barn (+Z)
            spawn.transform.position = new Vector3(0f, 0.15f, -HalfDepth + 1.5f);
            spawn.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        private static void BuildExitTrigger()
        {
            var go = new GameObject("BarnExitDoor");
            // At the door opening, facing -Z (toward exit)
            go.transform.position = new Vector3(0f, DoorHeight * 0.5f, -HalfDepth + 0.2f);

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(DoorHalfWidth * 2f, DoorHeight, 1.2f);

            var trigger = go.AddComponent<BarnDoorTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("barnScenePath").stringValue = BarnScenePath;
            so.FindProperty("spawnPointName").stringValue = BarnExitSpawnName;
            so.FindProperty("isEntrance").boolValue = false;
            so.ApplyModifiedProperties();
        }

        private static void BuildAnimalPen()
        {
            var host = new GameObject("BarnAnimalPen");
            // Place pen in the back half of the barn
            var penCenter = new Vector3(0f, 0f, HalfDepth * 0.4f);
            host.transform.position = Vector3.zero;

            var pen = host.AddComponent<AnimalPen>();
            pen.ConfigureRuntime(BuildPenEntries(), penCenter, 3.5f, true);
            host.AddComponent<FarmPenSpawner>();
        }

        private static PenAnimalEntry[] BuildPenEntries()
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
                Debug.LogWarning($"[BarnSceneBuilder] Prefab not found: {path} — assign manually in Inspector.");
            return new PenAnimalEntry { type = type, prefab = prefab };
        }

        private static void ApplyWoodColor(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;
            rend.sharedMaterial = new Material(shader) { color = color };
        }

        // ---------- FarmMain wiring ----------

        [MenuItem("fARm/Setup/Wire Barn Door in FarmMain")]
        public static void WireBarnDoorInFarmMain()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != FarmMainScenePath)
            {
                EditorUtility.DisplayDialog("Wrong scene",
                    "Open FarmMain.unity first, then run this menu item.", "OK");
                return;
            }

            // Remove stale versions
            foreach (var name in new[] { "BarnBuilding", BarnExitSpawnName, "BarnEntranceDoor" })
            {
                var existing = GameObject.Find(name);
                if (existing != null)
                    Object.DestroyImmediate(existing);
            }

            // NOTE: Does not add a barn building — place SM_Bld_Barn_02 (or variant)
            // manually. After placing, move BarnExitSpawn and BarnEntranceDoor to align
            // with the actual door. The barn faces +Z by default; door is ~8 m in front.

            // Spawn point outside the door — barn maxZ ≈ 1.17, so place a few metres out
            var exitSpawn = new GameObject(BarnExitSpawnName);
            exitSpawn.transform.position = new Vector3(-16f, 0.15f, 3.5f);
            exitSpawn.transform.rotation = Quaternion.identity;

            // Entrance trigger at the door opening (barn door is at world Z ≈ 1.17)
            var triggerGO = new GameObject("BarnEntranceDoor");
            triggerGO.transform.position = new Vector3(-16f, DoorHeight * 0.5f, 1.5f);

            var col = triggerGO.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(DoorHalfWidth * 2f, DoorHeight, 1.2f);

            var trigger = triggerGO.AddComponent<BarnDoorTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("barnScenePath").stringValue = BarnScenePath;
            so.FindProperty("spawnPointName").stringValue = BarnEntrySpawnName;
            so.FindProperty("isEntrance").boolValue = true;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BarnSceneBuilder] FarmMain barn door wired. Adjust BarnBuilding position as needed.");
        }
    }
}
