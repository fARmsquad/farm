using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds the greybox farm layout per L1-001 spec.
    /// Menu: FarmSim > Build Farm Layout (Greybox)
    /// </summary>
    public static class FarmSceneBuilder
    {
        private const float FarmSize = 20f;
        private const float FenceHeight = 1.5f;
        private const float FenceThickness = 0.15f;
        private const float FencePostSpacing = 2f;
        private const float PlotSize = 1f;
        private const float PathWidth = 1.2f;
        private const float PathY = 0.01f;

        // Colors
        private static readonly Color GrassColor = new(0.35f, 0.55f, 0.25f);
        private static readonly Color DirtColor = new(0.55f, 0.4f, 0.25f);
        private static readonly Color FenceColor = new(0.45f, 0.35f, 0.25f);
        private static readonly Color PlotColor = new(0.4f, 0.3f, 0.15f);
        private static readonly Color ExpansionFogColor = new(0.6f, 0.6f, 0.65f, 0.5f);

        [MenuItem("FarmSim/Create Farm Scene (New)")]
        public static void CreateFarmScene()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            BuildFarmLayout();
            EditorSceneManager.SaveScene(scene,
                "Assets/_Project/Scenes/FarmMain.unity");
            Debug.Log("[FarmSceneBuilder] FarmMain.unity created and saved.");
        }

        [MenuItem("FarmSim/Build Farm Layout (Greybox)")]
        public static void BuildFarmLayout()
        {
            if (!EditorUtility.DisplayDialog(
                "Build Farm Layout",
                "This will create the greybox farm layout in the current scene. Continue?",
                "Build", "Cancel"))
                return;

            // Root hierarchy containers
            var farm = CreateEmpty("Farm", Vector3.zero);
            var ground = CreateEmpty("Ground", Vector3.zero, farm);
            var paths = CreateEmpty("Paths", Vector3.zero, farm);
            var structures = CreateEmpty("Structures", Vector3.zero, farm);
            var plots = CreateEmpty("Plots", Vector3.zero, farm);
            var markers = CreateEmpty("Markers", Vector3.zero, farm);
            var fence = CreateEmpty("Fence", Vector3.zero, farm);
            var expansionZones = CreateEmpty("ExpansionZones", Vector3.zero, farm);
            var lighting = CreateEmpty("Lighting", Vector3.zero, farm);

            // Task 1: Ground Plane (20m x 20m)
            BuildGroundPlane(ground);

            // Task 2: Dirt Paths
            BuildPaths(paths);

            // Task 3: Perimeter Fence
            BuildPerimeterFence(fence);

            // Task 4: Structure Markers
            BuildStructureMarkers(markers, structures);

            // Task 5: Plot Grid (2x3)
            BuildPlotGrid(plots);

            // Task 6: Spawn Point
            BuildSpawnPoint(markers);

            // Task 7: Expansion Zones
            BuildExpansionZones(expansionZones);

            // Task 8: Lighting
            BuildLighting(lighting);

            // Task 9: Hierarchy is already organized via parenting above.

            // Mark scene dirty so user can save
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("[FarmSceneBuilder] L1-001 Greybox farm layout built successfully.");
        }

        [MenuItem("FarmSim/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            // Legacy demo — calls full layout now
            BuildFarmLayout();
        }

        // ── Task 1: Ground Plane ──────────────────────────────────

        private static void BuildGroundPlane(Transform parent)
        {
            // Unity Plane primitive is 10x10 units at scale 1
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "GroundPlane";
            plane.transform.SetParent(parent);
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(
                FarmSize / 10f, 1f, FarmSize / 10f);

            SetColor(plane, GrassColor);
        }

        // ── Task 2: Paths ─────────────────────────────────────────

        private static void BuildPaths(Transform parent)
        {
            // Main north-south path: entrance (0,0,-8) to barn (0,0,8)
            CreatePathSegment(parent, "Path_NS_Main",
                new Vector3(0f, PathY, 0f),
                new Vector3(PathWidth, 0.02f, 16f));

            // East-west path: well (-6,0,0) to rack (6,0,0)
            CreatePathSegment(parent, "Path_EW_Main",
                new Vector3(0f, PathY, 0f),
                new Vector3(12f, 0.02f, PathWidth));

            // Well spur: from main cross to well
            CreatePathSegment(parent, "Path_Well",
                new Vector3(-3.5f, PathY, 0f),
                new Vector3(5f, 0.02f, PathWidth));

            // Rack spur: from main cross to rack
            CreatePathSegment(parent, "Path_Rack",
                new Vector3(3.5f, PathY, 0f),
                new Vector3(5f, 0.02f, PathWidth));
        }

        private static void CreatePathSegment(
            Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = name;
            path.transform.SetParent(parent);
            path.transform.position = position;
            path.transform.localScale = scale;
            SetColor(path, DirtColor);
        }

        // ── Task 3: Perimeter Fence ───────────────────────────────

        private static void BuildPerimeterFence(Transform parent)
        {
            float half = FarmSize / 2f;
            float y = FenceHeight / 2f;

            // South fence — leave a 2m gap for entrance
            BuildFenceRun(parent, "Fence_S_Left",
                new Vector3(-half + (half - 1f) / 2f, y, -half),
                half - 1f, true);
            BuildFenceRun(parent, "Fence_S_Right",
                new Vector3(half - (half - 1f) / 2f, y, -half),
                half - 1f, true);

            // North fence
            BuildFenceRun(parent, "Fence_N",
                new Vector3(0f, y, half), FarmSize, true);

            // East fence
            BuildFenceRun(parent, "Fence_E",
                new Vector3(half, y, 0f), FarmSize, false);

            // West fence
            BuildFenceRun(parent, "Fence_W",
                new Vector3(-half, y, 0f), FarmSize, false);
        }

        private static void BuildFenceRun(
            Transform parent, string name, Vector3 center,
            float length, bool alongX)
        {
            var fenceObj = CreateEmpty(name, center, parent);
            int postCount = Mathf.CeilToInt(length / FencePostSpacing) + 1;
            float startOffset = -length / 2f;

            for (int i = 0; i < postCount; i++)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Post_{i}";
                post.transform.SetParent(fenceObj.transform);

                float offset = startOffset + i * FencePostSpacing;
                Vector3 localPos = alongX
                    ? new Vector3(offset, 0f, 0f)
                    : new Vector3(0f, 0f, offset);

                post.transform.localPosition = localPos;
                post.transform.localScale = new Vector3(
                    FenceThickness, FenceHeight, FenceThickness);
                SetColor(post, FenceColor);
            }

            // Horizontal rail
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Rail";
            rail.transform.SetParent(fenceObj.transform);
            rail.transform.localPosition = new Vector3(0f, FenceHeight * 0.3f, 0f);

            if (alongX)
                rail.transform.localScale = new Vector3(
                    length, FenceThickness, FenceThickness);
            else
                rail.transform.localScale = new Vector3(
                    FenceThickness, FenceThickness, length);

            SetColor(rail, FenceColor);
        }

        // ── Task 4: Structure Markers ─────────────────────────────

        private static void BuildStructureMarkers(
            Transform markers, Transform structures)
        {
            // Barn marker + placeholder cube (north)
            var barnMarker = CreateEmpty("BarnPosition", new Vector3(0f, 0f, 8f), markers);
            var barnPlaceholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barnPlaceholder.name = "Barn_Placeholder";
            barnPlaceholder.transform.SetParent(structures);
            barnPlaceholder.transform.position = new Vector3(0f, 1.5f, 8f);
            barnPlaceholder.transform.localScale = new Vector3(4f, 3f, 3f);
            SetColor(barnPlaceholder, new Color(0.6f, 0.45f, 0.3f));

            // Well marker + placeholder cylinder (west)
            var wellMarker = CreateEmpty("WellPosition", new Vector3(-6f, 0f, 0f), markers);
            var wellPlaceholder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wellPlaceholder.name = "Well_Placeholder";
            wellPlaceholder.transform.SetParent(structures);
            wellPlaceholder.transform.position = new Vector3(-6f, 0.5f, 0f);
            wellPlaceholder.transform.localScale = new Vector3(1.2f, 0.5f, 1.2f);
            SetColor(wellPlaceholder, new Color(0.5f, 0.5f, 0.55f));

            // Tool rack marker + placeholder (east)
            var rackMarker = CreateEmpty("ToolRackPosition", new Vector3(6f, 0f, 0f), markers);
            var rackPlaceholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rackPlaceholder.name = "ToolRack_Placeholder";
            rackPlaceholder.transform.SetParent(structures);
            rackPlaceholder.transform.position = new Vector3(6f, 0.75f, 0f);
            rackPlaceholder.transform.localScale = new Vector3(0.3f, 1.5f, 1.5f);
            SetColor(rackPlaceholder, new Color(0.5f, 0.4f, 0.3f));
        }

        // ── Task 5: Plot Grid (2 cols x 3 rows) ──────────────────

        private static void BuildPlotGrid(Transform parent)
        {
            // Ensure CropPlot tag exists
            EnsureTag("CropPlot");

            float spacing = 1.5f; // gap between plot centers
            int cols = 2, rows = 3;
            float xStart = -(cols - 1) * spacing / 2f;
            float zStart = -(rows - 1) * spacing / 2f;
            int plotIndex = 0;

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plot.name = $"CropPlot_{plotIndex}";
                    plot.transform.SetParent(parent);

                    float x = xStart + col * spacing;
                    float z = zStart + row * spacing;
                    plot.transform.position = new Vector3(x, 0.05f, z);
                    plot.transform.localScale = new Vector3(
                        PlotSize, 0.1f, PlotSize);

                    plot.tag = "CropPlot";
                    SetColor(plot, PlotColor);

                    plotIndex++;
                }
            }
        }

        // ── Task 6: Spawn Point ───────────────────────────────────

        private static void BuildSpawnPoint(Transform parent)
        {
            EnsureTag("SpawnPoint");
            var spawn = CreateEmpty("SpawnPoint", new Vector3(0f, 0f, -8f), parent);
            spawn.tag = "SpawnPoint";

            // Visual gizmo helper: small sphere so it's visible in Scene view
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "SpawnIndicator";
            indicator.transform.SetParent(spawn.transform);
            indicator.transform.localPosition = Vector3.up * 0.3f;
            indicator.transform.localScale = Vector3.one * 0.4f;
            SetColor(indicator, Color.yellow);
            // Remove collider so it doesn't interfere
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
        }

        // ── Task 7: Expansion Zones ───────────────────────────────

        private static void BuildExpansionZones(Transform parent)
        {
            float half = FarmSize / 2f;
            float zoneDepth = 6f;

            // East expansion zone
            BuildExpansionZone(parent, "ExpansionZone_East",
                new Vector3(half + zoneDepth / 2f, 0.02f, 0f),
                new Vector3(zoneDepth, 0.04f, FarmSize));

            // West expansion zone
            BuildExpansionZone(parent, "ExpansionZone_West",
                new Vector3(-half - zoneDepth / 2f, 0.02f, 0f),
                new Vector3(zoneDepth, 0.04f, FarmSize));

            // North expansion zone
            BuildExpansionZone(parent, "ExpansionZone_North",
                new Vector3(0f, 0.02f, half + zoneDepth / 2f),
                new Vector3(FarmSize + zoneDepth * 2f, 0.04f, zoneDepth));
        }

        private static void BuildExpansionZone(
            Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = name;
            zone.transform.SetParent(parent);
            zone.transform.position = position;
            zone.transform.localScale = scale;

            var renderer = zone.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetFloat("_Surface", 1f); // transparent
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.color = ExpansionFogColor;
            renderer.material = mat;

            // Boundary marker
            var marker = CreateEmpty($"{name}_Boundary", position, parent);
        }

        // ── Task 8: Lighting ──────────────────────────────────────

        private static void BuildLighting(Transform parent)
        {
            // Remove existing directional lights
            foreach (var existingLight in Object.FindObjectsByType<Light>(
                FindObjectsSortMode.None))
            {
                if (existingLight.type == LightType.Directional)
                    Object.DestroyImmediate(existingLight.gameObject);
            }

            var sunObj = new GameObject("Sun_Directional");
            sunObj.transform.SetParent(parent);
            var light = sunObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f); // warm white
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

            // Position camera for a good default view
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 12f, -14f);
                cam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            }
        }

        // ── Helpers ───────────────────────────────────────────────

        private static Transform CreateEmpty(
            string name, Vector3 position, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            if (parent != null)
                go.transform.SetParent(parent);
            return go.transform;
        }

        private static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var mat = new Material(
                Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            renderer.material = mat;
        }

        private static void EnsureTag(string tag)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(
                "ProjectSettings/TagManager.asset");
            if (asset == null) return;

            var so = new SerializedObject(asset);
            var tags = so.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    return;
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }
    }
}
