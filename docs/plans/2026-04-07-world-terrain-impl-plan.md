# Willowbrook World Terrain — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the full 400×400m Willowbrook world with terrain, water, paths, and populated Farm + Town zones using Synty/PolygonNature prefabs.

**Architecture:** An Editor script (`WorldSceneBuilder.cs`) in `FarmSimVR.Editor` namespace creates the scene deterministically via `[MenuItem]`, matching the existing `FarmSceneBuilder.cs` pattern. Unity Terrain for ground textures + elevation. Mesh prefabs for water, roads, buildings, vegetation. Hybrid hierarchy: zone groups → type sub-groups.

**Tech Stack:** Unity 6 LTS, URP, PolygonFarm + PolygonNature + PolygonGeneric assets, Unity Terrain API

**Design Doc:** `docs/plans/2026-04-07-world-terrain-design.md`

---

## Task 1: Create WorldSceneBuilder skeleton + test

**Files:**
- Create: `Assets/_Project/Editor/WorldSceneBuilder.cs`
- Test: `Assets/Tests/EditMode/WorldSceneBuilderTests.cs`

**Step 1: Write the failing test**

```csharp
// Assets/Tests/EditMode/WorldSceneBuilderTests.cs
using NUnit.Framework;
using FarmSimVR.Editor;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class WorldSceneBuilderTests
    {
        [Test]
        public void WorldConstants_TerrainSize_Is400()
        {
            Assert.AreEqual(400f, WorldSceneBuilder.TerrainSize);
        }

        [Test]
        public void WorldConstants_TerrainHeight_Is20()
        {
            Assert.AreEqual(20f, WorldSceneBuilder.TerrainHeight);
        }

        [Test]
        public void WorldConstants_ZoneCount_Is9()
        {
            Assert.AreEqual(9, WorldSceneBuilder.ZoneNames.Length);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `./run-tests.sh editmode`
Expected: FAIL — `WorldSceneBuilder` not found

**Step 3: Write minimal implementation**

```csharp
// Assets/_Project/Editor/WorldSceneBuilder.cs
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds the Willowbrook world terrain per world-terrain-design doc.
    /// Menu: FarmSim > Build World Scene
    /// </summary>
    public static class WorldSceneBuilder
    {
        // ── World Constants ──────────────────────────────────────
        public const float TerrainSize = 400f;
        public const float TerrainHeight = 20f;
        public const int HeightmapRes = 513;
        public const int AlphamapRes = 1024;

        public static readonly string[] ZoneNames = {
            "Farm", "Town", "NorthField", "SandyShores",
            "Meadow", "River", "CountyFair", "WildflowerHills", "Trail"
        };

        // ── Zone Bounds (centerX, centerZ, sizeX, sizeZ) ────────
        public static readonly Vector4[] ZoneBounds = {
            new(120f, 40f, 160f, 120f),    // Farm
            new(-120f, 10f, 160f, 180f),   // Town
            new(-120f, 150f, 160f, 100f),  // NorthField
            new(120f, 160f, 160f, 80f),    // SandyShores
            new(-130f, -120f, 140f, 80f),  // Meadow
            new(30f, -120f, 180f, 80f),    // River
            new(160f, -120f, 80f, 80f),    // CountyFair
            new(0f, -180f, 400f, 40f),     // WildflowerHills
            new(0f, 50f, 80f, 100f),       // Trail
        };

        [MenuItem("FarmSim/Build World Scene (New)")]
        public static void CreateWorldScene()
        {
            if (!EditorUtility.DisplayDialog(
                "Build World Scene",
                "This creates a new 400x400m world scene. The existing FarmMain scene is NOT modified. Continue?",
                "Build", "Cancel"))
                return;

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

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
            Debug.Log("[WorldSceneBuilder] WorldMain.unity created.");
        }

        // ── Stub methods (implemented in subsequent tasks) ──────

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

        // ── Helpers (reused from FarmSceneBuilder pattern) ──────

        public static GameObject CreateEmpty(string name, Vector3 pos, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent);
            return go;
        }

        public static GameObject InstantiatePrefab(string path, Vector3 pos, Quaternion rot, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[WorldSceneBuilder] Prefab not found: {path}");
                // Fallback: create a placeholder cube
                var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placeholder.name = $"MISSING_{System.IO.Path.GetFileNameWithoutExtension(path)}";
                placeholder.transform.position = pos;
                placeholder.transform.rotation = rot;
                if (parent != null) placeholder.transform.SetParent(parent);
                placeholder.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = Color.magenta
                };
                return placeholder;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            if (parent != null) instance.transform.SetParent(parent);
            return instance;
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `./run-tests.sh editmode`
Expected: PASS (3 tests)

**Step 5: Commit**

```bash
git add Assets/_Project/Editor/WorldSceneBuilder.cs Assets/Tests/EditMode/WorldSceneBuilderTests.cs
git commit -m "feat(world): add WorldSceneBuilder skeleton with zone constants"
```

---

## Task 2: BuildSceneConfig — Lighting, fog, camera, reflection probe

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Write the failing test**

```csharp
// Add to WorldSceneBuilderTests.cs
[Test]
public void FogColor_IsWarmGolden()
{
    // Synty-exemplar warm fog: ~(1.0, 0.83, 0.62)
    var fog = WorldSceneBuilder.FogColor;
    Assert.That(fog.r, Is.GreaterThan(0.9f));
    Assert.That(fog.g, Is.InRange(0.75f, 0.9f));
    Assert.That(fog.b, Is.InRange(0.55f, 0.7f));
}
```

**Step 2: Run test — FAIL**

**Step 3: Implement BuildSceneConfig**

```csharp
// Add to WorldSceneBuilder.cs

// ── Atmosphere Constants ─────────────────────────────────
public static readonly Color FogColor = new(1f, 0.83f, 0.62f);
public static readonly Color AmbientSky = new(0.31f, 0.28f, 0.38f);
public static readonly Color AmbientEquator = new(0.55f, 0.50f, 0.42f);
public static readonly Color AmbientGround = new(0.23f, 0.20f, 0.16f);
public static readonly Color SunColor = new(1f, 0.96f, 0.88f); // #FFF4E0

private static void BuildSceneConfig()
{
    // -- Camera --
    var camGO = new GameObject("Main Camera");
    camGO.tag = "MainCamera";
    var cam = camGO.AddComponent<Camera>();
    cam.clearFlags = CameraClearFlags.Skybox;
    cam.fieldOfView = 60f;
    cam.farClipPlane = 500f;
    camGO.transform.position = new Vector3(0f, 50f, -100f);
    camGO.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
    camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

    // -- Directional Light (Sun) --
    var lightGO = new GameObject("Directional Light");
    var light = lightGO.AddComponent<Light>();
    light.type = LightType.Directional;
    light.color = SunColor;
    light.intensity = 1.2f;
    light.shadows = LightShadows.Soft;
    light.shadowStrength = 0.7f;
    lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    lightGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();

    // -- Fog --
    RenderSettings.fog = true;
    RenderSettings.fogMode = FogMode.Linear;
    RenderSettings.fogColor = FogColor;
    RenderSettings.fogStartDistance = 80f;
    RenderSettings.fogEndDistance = 350f;

    // -- Ambient --
    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    RenderSettings.ambientSkyColor = AmbientSky;
    RenderSettings.ambientEquatorColor = AmbientEquator;
    RenderSettings.ambientGroundColor = AmbientGround;

    // -- Skybox (use existing procedural mat if available) --
    var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/SkyboxProcedural.mat");
    if (skyMat == null)
        skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/SkyboxProcedural.mat");
    if (skyMat != null) RenderSettings.skybox = skyMat;

    // -- Reflection Probe --
    var probeGO = new GameObject("ReflectionProbe");
    var probe = probeGO.AddComponent<ReflectionProbe>();
    probeGO.transform.position = new Vector3(0f, 5f, 0f);
    probe.size = new Vector3(400f, 20f, 400f);
    probe.resolution = 256;
    probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;

    // -- Parent under _SceneConfig --
    var config = CreateEmpty("_SceneConfig", Vector3.zero);
    lightGO.transform.SetParent(config.transform);
    probeGO.transform.SetParent(config.transform);

    Debug.Log("[WorldSceneBuilder] Scene config (lighting, fog, camera) built.");
}
```

**Step 4: Run test — PASS**

**Step 5: Commit**

```bash
git add -A Assets/_Project/Editor/WorldSceneBuilder.cs Assets/Tests/EditMode/WorldSceneBuilderTests.cs
git commit -m "feat(world): implement BuildSceneConfig with Synty-style fog and lighting"
```

---

## Task 3: BuildTerrain — Unity Terrain with 7 texture layers + elevation

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void TerrainLayerPaths_AllExist()
{
    foreach (var path in WorldSceneBuilder.TerrainTexturePaths)
    {
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Assert.IsNotNull(tex, $"Missing terrain texture: {path}");
    }
}
```

**Step 2: Run test — FAIL**

**Step 3: Implement BuildTerrain**

```csharp
// Add to WorldSceneBuilder.cs

public static readonly string[] TerrainTexturePaths = {
    "Assets/PolygonNature/Textures/Ground_Textures/Grass.png",
    "Assets/PolygonNature/Textures/Ground_Textures/Grass_02.png",
    "Assets/PolygonNature/Textures/Ground_Textures/Mud.png",
    "Assets/PolygonNature/Textures/Ground_Textures/Sand.png",
    "Assets/PolygonNature/Textures/Ground_Textures/Pebbles.png",
    "Assets/PolygonNature/Textures/Ground_Textures/Flowers.png",
    "Assets/PolygonNature/Textures/Ground_Textures/Sand_Darker.png",
};

public static readonly string[] TerrainNormalPaths = {
    "Assets/PolygonNature/Textures/Ground_Textures/BaseGrass_normals.png",
    "Assets/PolygonNature/Textures/Ground_Textures/BaseGrass_normals.png",
    "Assets/PolygonNature/Textures/Ground_Textures/Mud_Normal.png",
    null, // Sand — no normal
    "Assets/PolygonNature/Textures/Ground_Textures/Pebbles_normals.png",
    "Assets/PolygonNature/Textures/Ground_Textures/Flowers_normals.png",
    null, // Sand_Darker — no normal
};

// Layer indices for painting convenience
public const int LAYER_GRASS = 0;
public const int LAYER_GRASS2 = 1;
public const int LAYER_MUD = 2;
public const int LAYER_SAND = 3;
public const int LAYER_PEBBLES = 4;
public const int LAYER_FLOWERS = 5;
public const int LAYER_SAND_DARK = 6;

private static void BuildTerrain()
{
    var terrainData = new TerrainData();
    terrainData.heightmapResolution = HeightmapRes;
    terrainData.alphamapResolution = AlphamapRes;
    terrainData.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);

    // ── Terrain Layers ──
    var layers = new TerrainLayer[TerrainTexturePaths.Length];
    for (int i = 0; i < layers.Length; i++)
    {
        layers[i] = new TerrainLayer();
        layers[i].diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTexturePaths[i]);
        layers[i].tileSize = new Vector2(10f, 10f);
        if (TerrainNormalPaths[i] != null)
            layers[i].normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainNormalPaths[i]);
    }
    terrainData.terrainLayers = layers;

    // ── Paint Splatmaps (zone-based texture assignment) ──
    PaintTerrainTextures(terrainData);

    // ── Elevation (heightmap) ──
    PaintTerrainHeights(terrainData);

    // ── Instantiate Terrain GameObject ──
    // Position so origin (0,0,0) is at terrain center
    var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
    terrainGO.name = "Willowbrook_Terrain";
    terrainGO.transform.position = new Vector3(-TerrainSize / 2f, 0f, -TerrainSize / 2f);

    var terrain = terrainGO.GetComponent<Terrain>();
    terrain.drawInstanced = true;
    terrain.heightmapPixelError = 8;
    terrain.basemapDistance = 150f;

    // Parent
    var terrainRoot = CreateEmpty("Terrain", Vector3.zero);
    terrainGO.transform.SetParent(terrainRoot.transform);

    // Save terrain data asset
    AssetDatabase.CreateAsset(terrainData, "Assets/_Project/Scenes/WorldMain_TerrainData.asset");

    Debug.Log("[WorldSceneBuilder] Terrain built with 7 texture layers.");
}

private static void PaintTerrainTextures(TerrainData td)
{
    int res = td.alphamapResolution;
    float[,,] alphas = new float[res, res, td.terrainLayers.Length];

    // Default: everything is grass (layer 0)
    for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
            alphas[y, x, LAYER_GRASS] = 1f;

    // Paint zones by converting world coords to alphamap coords
    // Alphamap (0,0) = terrain position corner (-200, -200)
    // Alphamap (res,res) = terrain far corner (+200, +200)

    // Sandy Shores: sand texture
    PaintZoneTexture(alphas, res, 40, 120, 200, 200, LAYER_SAND, LAYER_GRASS);

    // Farm plots area: mud
    PaintZoneTexture(alphas, res, 60, 25, 110, 65, LAYER_MUD, LAYER_GRASS);

    // River channel: mud banks
    PaintZoneTexture(alphas, res, -60, -160, 120, -80, LAYER_MUD, LAYER_GRASS);

    // Meadow: grass2 + flowers
    PaintZoneTexture(alphas, res, -200, -160, -60, -80, LAYER_GRASS2, LAYER_GRASS);
    PaintZoneTexture(alphas, res, -180, -150, -80, -90, LAYER_FLOWERS, LAYER_GRASS2);

    // North Field: flowers accent
    PaintZoneTexture(alphas, res, -180, 110, -60, 190, LAYER_FLOWERS, LAYER_GRASS);

    // Town Main Street: pebbles
    PaintZoneTexture(alphas, res, -190, 25, -50, 35, LAYER_PEBBLES, LAYER_GRASS);

    // Trail: pebbles path
    PaintZoneTexture(alphas, res, -15, 0, 15, 95, LAYER_PEBBLES, LAYER_GRASS);

    // Wildflower Hills: grass2
    PaintZoneTexture(alphas, res, -200, -200, 200, -160, LAYER_GRASS2, LAYER_GRASS);

    td.SetAlphamaps(0, 0, alphas);
}

private static void PaintZoneTexture(float[,,] alphas, int res,
    float worldMinX, float worldMinZ, float worldMaxX, float worldMaxZ,
    int newLayer, int replaceLayer)
{
    float halfSize = TerrainSize / 2f;
    // World coords to normalized 0-1
    int xMin = Mathf.Clamp(Mathf.RoundToInt((worldMinX + halfSize) / TerrainSize * res), 0, res - 1);
    int xMax = Mathf.Clamp(Mathf.RoundToInt((worldMaxX + halfSize) / TerrainSize * res), 0, res - 1);
    int zMin = Mathf.Clamp(Mathf.RoundToInt((worldMinZ + halfSize) / TerrainSize * res), 0, res - 1);
    int zMax = Mathf.Clamp(Mathf.RoundToInt((worldMaxZ + halfSize) / TerrainSize * res), 0, res - 1);

    for (int z = zMin; z <= zMax; z++)
    {
        for (int x = xMin; x <= xMax; x++)
        {
            alphas[z, x, replaceLayer] = 0f;
            alphas[z, x, newLayer] = 1f;
        }
    }
}

private static void PaintTerrainHeights(TerrainData td)
{
    int res = td.heightmapResolution;
    float[,] heights = new float[res, res];
    // Heights are normalized 0-1 (multiplied by TerrainHeight=20m)
    // Default y=0 → height = 0. We offset terrain GO so y=0 world = some baseline.
    // Terrain GO at y=0, so height 0 = y=0 world.

    float halfSize = TerrainSize / 2f;

    for (int z = 0; z < res; z++)
    {
        for (int x = 0; x < res; x++)
        {
            float wx = (float)x / res * TerrainSize - halfSize;
            float wz = (float)z / res * TerrainSize - halfSize;

            float h = 0f;

            // Farm plateau: gentle rise to y=0.5
            if (wx > 40 && wx < 200 && wz > -20 && wz < 100)
                h = 0.5f;

            // Trail slope: 0 at town edge, 0.5 at farm edge
            if (wx > -40 && wx < 40 && wz > 0 && wz < 100)
                h = Mathf.Lerp(0f, 0.5f, (wx + 40f) / 80f);

            // Meadow rolling hills
            if (wx < -60 && wz > -160 && wz < -80)
                h = Mathf.Sin(wx * 0.05f) * Mathf.Sin(wz * 0.07f) * 2f;

            // River channel carved to -1
            if (wx > -60 && wx < 120 && wz > -155 && wz < -85)
            {
                float riverCenter = -120f;
                float distFromCenter = Mathf.Abs(wz - riverCenter);
                if (distFromCenter < 15f)
                    h = Mathf.Lerp(-1f, 0f, distFromCenter / 15f);
            }

            // Wildflower Hills: rolling 0-4m
            if (wz < -160)
            {
                h = (Mathf.Sin(wx * 0.03f + 1.5f) + 1f) * 2f
                  * (Mathf.Cos(wz * 0.05f) * 0.5f + 0.5f);
            }

            heights[z, x] = h / TerrainHeight;
        }
    }

    td.SetHeights(0, 0, heights);
}
```

**Step 4: Run test — PASS**

**Step 5: Commit**

```bash
git add -A Assets/_Project/Editor/WorldSceneBuilder.cs Assets/Tests/EditMode/WorldSceneBuilderTests.cs
git commit -m "feat(world): implement terrain with 7 texture layers and elevation"
```

---

## Task 4: BuildZoneHierarchy — Empty zone parents with BoxCollider triggers

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Implement BuildZoneHierarchy**

```csharp
private static void BuildZoneHierarchy()
{
    for (int i = 0; i < ZoneNames.Length; i++)
    {
        var bounds = ZoneBounds[i];
        var zone = CreateEmpty(ZoneNames[i], new Vector3(bounds.x, 0f, bounds.y));

        // Zone trigger collider
        var trigger = zone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(bounds.z, 10f, bounds.w);
        trigger.center = Vector3.up * 5f;

        // Sub-groups per zone type
        CreateEmpty("Markers", Vector3.zero, zone.transform);

        if (ZoneNames[i] == "Farm" || ZoneNames[i] == "Town")
        {
            CreateEmpty("Buildings", Vector3.zero, zone.transform);
            CreateEmpty("Props", Vector3.zero, zone.transform);
            CreateEmpty("Trees", Vector3.zero, zone.transform);
        }
        if (ZoneNames[i] == "Farm")
        {
            CreateEmpty("Plots", Vector3.zero, zone.transform);
            CreateEmpty("Pen", Vector3.zero, zone.transform);
            CreateEmpty("Pasture", Vector3.zero, zone.transform);
            CreateEmpty("FX", Vector3.zero, zone.transform);
        }
        if (ZoneNames[i] == "Town")
        {
            CreateEmpty("MainStreet", Vector3.zero, zone.transform);
        }
    }
    Debug.Log("[WorldSceneBuilder] Zone hierarchy created for 9 zones.");
}
```

**Step 2: No new test needed (structural, verified visually)**

**Step 3: Commit**

```bash
git add Assets/_Project/Editor/WorldSceneBuilder.cs
git commit -m "feat(world): add zone hierarchy with trigger colliders"
```

---

## Task 5: BuildWater — River, creek, farm pond

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Write the failing test**

```csharp
[Test]
public void WaterPrefabPaths_AllExist()
{
    foreach (var path in WorldSceneBuilder.WaterPrefabPaths)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.IsNotNull(prefab, $"Missing water prefab: {path}");
    }
}
```

**Step 2: Run — FAIL**

**Step 3: Implement**

```csharp
public static readonly string[] WaterPrefabPaths = {
    "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
    "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab",
    "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_Corner_01.prefab",
    "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Pond_01.prefab",
    "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Reeds_01.prefab",
    "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Reeds_02.prefab",
    "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Lillypads_01.prefab",
};

private static void BuildWater()
{
    var waterRoot = CreateEmpty("Water", Vector3.zero);
    var river = CreateEmpty("River", Vector3.zero, waterRoot.transform);
    var creek = CreateEmpty("Creek", Vector3.zero, waterRoot.transform);

    // River: 6 segments winding through river zone
    // River runs roughly from (-40, -120) to (100, -120) with slight curve
    Vector3[] riverPoints = {
        new(-40f, -0.8f, -120f),
        new(-10f, -0.8f, -115f),
        new(20f,  -0.8f, -125f),
        new(50f,  -0.8f, -118f),
        new(80f,  -0.8f, -122f),
        new(110f, -0.8f, -115f),
    };

    string riverPrefab = "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab";
    for (int i = 0; i < riverPoints.Length; i++)
    {
        float yRot = (i % 2 == 0) ? 0f : 15f; // slight alternating angle
        InstantiatePrefab(riverPrefab, riverPoints[i],
            Quaternion.Euler(0f, yRot, 0f), river.transform);
    }

    // River bank edges
    string bankPrefab = "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab";
    for (int i = 0; i < riverPoints.Length; i++)
    {
        var northBank = riverPoints[i] + new Vector3(0f, 0f, 12f);
        var southBank = riverPoints[i] + new Vector3(0f, 0f, -12f);
        InstantiatePrefab(bankPrefab, northBank, Quaternion.identity, river.transform);
        InstantiatePrefab(bankPrefab, southBank, Quaternion.Euler(0f, 180f, 0f), river.transform);
    }

    // Reeds along river
    string reedsPath = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Reeds_01.prefab";
    for (int i = 0; i < 10; i++)
    {
        float rx = Mathf.Lerp(-40f, 110f, i / 9f) + Random.Range(-5f, 5f);
        float rz = -120f + (i % 2 == 0 ? 14f : -14f);
        InstantiatePrefab(reedsPath, new Vector3(rx, -0.3f, rz),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), river.transform);
    }

    // Creek: 2 smaller segments in trail zone
    Vector3[] creekPoints = {
        new(-10f, -0.3f, 55f),
        new(10f,  -0.3f, 50f),
    };
    for (int i = 0; i < creekPoints.Length; i++)
    {
        var go = InstantiatePrefab(riverPrefab, creekPoints[i],
            Quaternion.Euler(0f, 30f, 0f), creek.transform);
        go.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
    }

    // Bridge over creek
    string bridgePath = "Assets/PolygonNature/Prefabs/Props/SM_Prop_Bridge_Curved_01.prefab";
    InstantiatePrefab(bridgePath, new Vector3(0f, 0f, 52f),
        Quaternion.Euler(0f, 90f, 0f), creek.transform);

    // Farm pond
    string pondPath = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Pond_01.prefab";
    var pond = InstantiatePrefab(pondPath, new Vector3(165f, 0f, 20f),
        Quaternion.identity, waterRoot.transform);
    pond.name = "FarmPond";

    // Lily pads on pond
    string lilyPath = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Lillypads_01.prefab";
    InstantiatePrefab(lilyPath, new Vector3(165f, 0.05f, 20f), Quaternion.identity, waterRoot.transform);

    Debug.Log("[WorldSceneBuilder] Water system built (river, creek, pond).");
}
```

**Step 4: Run test — PASS**

**Step 5: Commit**

```bash
git add -A Assets/_Project/Editor/WorldSceneBuilder.cs Assets/Tests/EditMode/WorldSceneBuilderTests.cs
git commit -m "feat(world): add river, creek, farm pond with bank edges and reeds"
```

---

## Task 6: BuildPaths — Dirt/gravel road prefab chains

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Implement BuildPaths**

```csharp
private static void BuildPaths()
{
    var pathRoot = CreateEmpty("Paths", Vector3.zero);

    // ── Main Street (Town) — gravel, ~120m E-W ──
    var mainSt = CreateEmpty("MainStreet", Vector3.zero, pathRoot.transform);
    string gravelStraight = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Gravel_Straight_01.prefab";
    string gravelEnd = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Gravel_End_01.prefab";

    // Lay straight segments along z=30 (Main St) from x=-190 to x=-50
    for (float x = -185f; x <= -55f; x += 10f)
    {
        InstantiatePrefab(gravelStraight, new Vector3(x, 0.02f, 30f),
            Quaternion.Euler(0f, 90f, 0f), mainSt.transform);
    }
    // End caps
    InstantiatePrefab(gravelEnd, new Vector3(-190f, 0.02f, 30f),
        Quaternion.Euler(0f, -90f, 0f), mainSt.transform);
    InstantiatePrefab(gravelEnd, new Vector3(-50f, 0.02f, 30f),
        Quaternion.Euler(0f, 90f, 0f), mainSt.transform);

    // ── Trail to Farm — dirt, winding NE ──
    var trail = CreateEmpty("TrailToFarm", Vector3.zero, pathRoot.transform);
    string dirtStraight = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Dirt_Straight_01.prefab";
    string dirtSwerve = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Dirt_Swerve_01.prefab";

    // Trail from town edge (-40, 30) winding to farm entrance (45, 50)
    Vector3[] trailPoints = {
        new(-35f, 0.02f, 30f),
        new(-25f, 0.02f, 35f),
        new(-15f, 0.02f, 42f),
        new(-5f,  0.02f, 48f),
        new(5f,   0.02f, 50f),
        new(15f,  0.02f, 50f),
        new(25f,  0.02f, 48f),
        new(35f,  0.02f, 45f),
    };

    for (int i = 0; i < trailPoints.Length; i++)
    {
        string prefab = (i == 2 || i == 5) ? dirtSwerve : dirtStraight;
        float yRot = 0f;
        if (i < trailPoints.Length - 1)
        {
            var dir = trailPoints[i + 1] - trailPoints[i];
            yRot = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        }
        InstantiatePrefab(prefab, trailPoints[i],
            Quaternion.Euler(0f, yRot, 0f), trail.transform);
    }

    // ── Farm Internal Paths ──
    var farmPaths = CreateEmpty("FarmPaths", Vector3.zero, pathRoot.transform);
    // Main farm road from entrance to barn
    for (float z = 45f; z <= 75f; z += 10f)
    {
        InstantiatePrefab(dirtStraight, new Vector3(100f, 0.52f, z),
            Quaternion.identity, farmPaths.transform);
    }
    // Cross path to plots
    for (float x = 70f; x <= 100f; x += 10f)
    {
        InstantiatePrefab(dirtStraight, new Vector3(x, 0.52f, 55f),
            Quaternion.Euler(0f, 90f, 0f), farmPaths.transform);
    }

    Debug.Log("[WorldSceneBuilder] Path network built (Main St, trail, farm paths).");
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Editor/WorldSceneBuilder.cs
git commit -m "feat(world): add road/path network with gravel and dirt prefabs"
```

---

## Task 7: BuildFarmZone — Buildings, pen, plots, pasture, props

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

This is the largest task. Implement in one method with clear sections.

**Step 1: Write the failing test**

```csharp
[Test]
public void FarmBuildingPrefabPaths_AllExist()
{
    string[] paths = {
        "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Farmhouse_01.prefab",
        "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Barn_01.prefab",
        "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Silo_01.prefab",
        "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Silo_Small_01.prefab",
        "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Greenhouse_01.prefab",
    };
    foreach (var p in paths)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
        Assert.IsNotNull(prefab, $"Missing farm building: {p}");
    }
}
```

**Step 2: Run — FAIL**

**Step 3: Implement BuildFarmZone**

```csharp
private static void BuildFarmZone()
{
    // Find the Farm zone parent created by BuildZoneHierarchy
    var farm = GameObject.Find("Farm");
    if (farm == null) { farm = CreateEmpty("Farm", new Vector3(120f, 0f, 40f)); }

    var buildings = farm.transform.Find("Buildings") ?? CreateEmpty("Buildings", Vector3.zero, farm.transform).transform;
    var props = farm.transform.Find("Props") ?? CreateEmpty("Props", Vector3.zero, farm.transform).transform;
    var plots = farm.transform.Find("Plots") ?? CreateEmpty("Plots", Vector3.zero, farm.transform).transform;
    var pen = farm.transform.Find("Pen") ?? CreateEmpty("Pen", Vector3.zero, farm.transform).transform;
    var pasture = farm.transform.Find("Pasture") ?? CreateEmpty("Pasture", Vector3.zero, farm.transform).transform;
    var trees = farm.transform.Find("Trees") ?? CreateEmpty("Trees", Vector3.zero, farm.transform).transform;
    var fx = farm.transform.Find("FX") ?? CreateEmpty("FX", Vector3.zero, farm.transform).transform;

    string P(string name) => $"Assets/Synty/PolygonFarm/Prefabs/{name}";

    // ── Buildings ──
    InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_01.prefab"),
        new Vector3(100f, 0.5f, 60f), Quaternion.Euler(0f, 180f, 0f), buildings);
    InstantiatePrefab(P("Buildings/SM_Bld_Barn_01.prefab"),
        new Vector3(140f, 0.5f, 70f), Quaternion.Euler(0f, 90f, 0f), buildings);
    InstantiatePrefab(P("Buildings/SM_Bld_Silo_01.prefab"),
        new Vector3(155f, 0.5f, 75f), Quaternion.identity, buildings);
    InstantiatePrefab(P("Buildings/SM_Bld_Silo_Small_01.prefab"),
        new Vector3(150f, 0.5f, 65f), Quaternion.identity, buildings);
    InstantiatePrefab(P("Buildings/SM_Bld_Greenhouse_01.prefab"),
        new Vector3(60f, 0.5f, 30f), Quaternion.identity, buildings);

    // ── Circular Pen (El Pollo Loco) ──
    string fenceRound = P("Props/SM_Prop_Fence_Wood_Round_01.prefab");
    string fenceGate = P("Props/SM_Prop_Fence_Wood_Gate_01.prefab");
    Vector3 penCenter = new(120f, 0.5f, 40f);
    float penRadius = 8f;
    int segments = 12;
    for (int i = 0; i < segments; i++)
    {
        float angle = i * (360f / segments);
        float rad = angle * Mathf.Deg2Rad;
        var pos = penCenter + new Vector3(Mathf.Sin(rad) * penRadius, 0f, Mathf.Cos(rad) * penRadius);
        var rot = Quaternion.Euler(0f, angle, 0f);
        string prefab = (i == 0) ? fenceGate : fenceRound; // gate at position 0
        InstantiatePrefab(prefab, pos, rot, pen);
    }

    // ── Crop Plots (4 clusters) ──
    string dirtRows = P("Environments/SM_Env_Dirt_Rows_Center_01.prefab");
    string dirtSkirt = P("Environments/SM_Env_Dirt_Skirt_01.prefab");
    string vegeRows1 = P("Environments/SM_Env_Vege_Rows_01.prefab");
    string vegeRows2 = P("Environments/SM_Env_Vege_Rows_02.prefab");

    Vector3[] plotCenters = {
        new(70f, 0.5f, 50f), new(85f, 0.5f, 50f),
        new(70f, 0.5f, 35f), new(85f, 0.5f, 35f),
    };

    foreach (var center in plotCenters)
    {
        var plotGroup = CreateEmpty($"Plot_{center.x}_{center.z}", center, plots);
        // 6 row segments per plot
        for (int r = 0; r < 6; r++)
        {
            InstantiatePrefab(dirtRows,
                center + new Vector3(0f, 0f, r * 1.5f - 3.75f),
                Quaternion.identity, plotGroup.transform);
        }
        // Border skirt
        InstantiatePrefab(dirtSkirt, center + new Vector3(0f, 0f, -5f),
            Quaternion.identity, plotGroup.transform);
        InstantiatePrefab(dirtSkirt, center + new Vector3(0f, 0f, 5f),
            Quaternion.Euler(0f, 180f, 0f), plotGroup.transform);
        // Vege overlay (alternating types)
        string vege = (center.x < 80f) ? vegeRows1 : vegeRows2;
        InstantiatePrefab(vege, center, Quaternion.identity, plotGroup.transform);
    }

    // ── Pasture (Wire Fence Enclosure) ──
    string wireF = P("Props/SM_Prop_Fence_Wire_01.prefab");
    string wireP = P("Props/SM_Prop_Fence_Wire_Pole_01.prefab");
    string wireG = P("Props/SM_Prop_Fence_Wire_Gate_01.prefab");

    Vector3 pastureMin = new(155f, 0.5f, 15f);
    Vector3 pastureMax = new(185f, 0.5f, 45f);
    // North/south runs
    for (float x = pastureMin.x; x < pastureMax.x; x += 5f)
    {
        InstantiatePrefab(wireF, new Vector3(x, 0.5f, pastureMin.z), Quaternion.Euler(0f, 90f, 0f), pasture);
        InstantiatePrefab(wireF, new Vector3(x, 0.5f, pastureMax.z), Quaternion.Euler(0f, 90f, 0f), pasture);
    }
    // East/west runs
    for (float z = pastureMin.z; z < pastureMax.z; z += 5f)
    {
        InstantiatePrefab(wireF, new Vector3(pastureMin.x, 0.5f, z), Quaternion.identity, pasture);
        InstantiatePrefab(wireF, new Vector3(pastureMax.x, 0.5f, z), Quaternion.identity, pasture);
    }
    // Poles at corners
    InstantiatePrefab(wireP, new Vector3(pastureMin.x, 0.5f, pastureMin.z), Quaternion.identity, pasture);
    InstantiatePrefab(wireP, new Vector3(pastureMax.x, 0.5f, pastureMin.z), Quaternion.identity, pasture);
    InstantiatePrefab(wireP, new Vector3(pastureMin.x, 0.5f, pastureMax.z), Quaternion.identity, pasture);
    InstantiatePrefab(wireP, new Vector3(pastureMax.x, 0.5f, pastureMax.z), Quaternion.identity, pasture);
    // Gate on south side
    InstantiatePrefab(wireG, new Vector3(170f, 0.5f, pastureMin.z), Quaternion.Euler(0f, 90f, 0f), pasture);

    // ── Props ──
    InstantiatePrefab(P("Props/SM_Prop_Well_01.prefab"), new Vector3(95f, 0.5f, 55f), Quaternion.identity, props);
    InstantiatePrefab(P("Characters/SM_Chr_Scarecrow_01.prefab"), new Vector3(78f, 0.5f, 45f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_HayBale_02.prefab"), new Vector3(145f, 0.5f, 62f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_HayBale_02.prefab"), new Vector3(148f, 0.5f, 60f), Quaternion.Euler(0f, 30f, 0f), props);
    InstantiatePrefab(P("Props/SM_Prop_HayBale_01.prefab"), new Vector3(143f, 0.5f, 65f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_Crate_01.prefab"), new Vector3(137f, 0.5f, 68f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_Crate_02.prefab"), new Vector3(139f, 0.5f, 68f), Quaternion.Euler(0f, 45f, 0f), props);
    InstantiatePrefab(P("Props/SM_Prop_Wheelbarrow_01.prefab"), new Vector3(75f, 0.5f, 40f), Quaternion.Euler(0f, -20f, 0f), props);
    InstantiatePrefab(P("Props/SM_Prop_WateringCan_01.prefab"), new Vector3(93f, 0.5f, 56f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_Barrel_01.prefab"), new Vector3(105f, 0.5f, 58f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_Barrel_02.prefab"), new Vector3(107f, 0.5f, 59f), Quaternion.Euler(0f, 90f, 0f), props);
    InstantiatePrefab(P("Props/SM_Prop_Trough_01.prefab"), new Vector3(170f, 0.5f, 30f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_Letterbox_01.prefab"), new Vector3(50f, 0.5f, 45f), Quaternion.Euler(0f, -90f, 0f), props);

    // ── Farm Trees ──
    InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(90f, 0.5f, 70f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(85f, 0.5f, 75f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(95f, 0.5f, 76f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Cherry_Grown_01.prefab"), new Vector3(55f, 0.5f, 48f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Cherry_Grown_01.prefab"), new Vector3(50f, 0.5f, 52f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"), new Vector3(110f, 0.5f, 80f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"), new Vector3(160f, 0.5f, 85f), Quaternion.identity, trees);

    // ── FX ──
    InstantiatePrefab(P("FX/FX_Pollen_Wind_01.prefab"), new Vector3(80f, 2f, 45f), Quaternion.identity, fx);
    InstantiatePrefab(P("FX/FX_Sprinkler_01.prefab"), new Vector3(75f, 0.5f, 50f), Quaternion.identity, fx);
    InstantiatePrefab(P("FX/FX_Sprinkler_01.prefab"), new Vector3(85f, 0.5f, 35f), Quaternion.identity, fx);

    Debug.Log("[WorldSceneBuilder] Farm zone fully populated.");
}
```

**Step 4: Run test — PASS**

**Step 5: Commit**

```bash
git add -A Assets/_Project/Editor/WorldSceneBuilder.cs Assets/Tests/EditMode/WorldSceneBuilderTests.cs
git commit -m "feat(world): populate farm zone with buildings, pen, plots, pasture, props"
```

---

## Task 8: BuildTownZone — Buildings, Main Street furniture, props

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Implement BuildTownZone**

```csharp
private static void BuildTownZone()
{
    var town = GameObject.Find("Town");
    if (town == null) { town = CreateEmpty("Town", new Vector3(-120f, 0f, 10f)); }

    var buildings = town.transform.Find("Buildings") ?? CreateEmpty("Buildings", Vector3.zero, town.transform).transform;
    var mainSt = town.transform.Find("MainStreet") ?? CreateEmpty("MainStreet", Vector3.zero, town.transform).transform;
    var props = town.transform.Find("Props") ?? CreateEmpty("Props", Vector3.zero, town.transform).transform;
    var trees = town.transform.Find("Trees") ?? CreateEmpty("Trees", Vector3.zero, town.transform).transform;

    string P(string name) => $"Assets/Synty/PolygonFarm/Prefabs/{name}";
    string PN(string name) => $"Assets/PolygonNature/Prefabs/{name}";

    // ── Buildings ──
    // Player's house
    InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_02.prefab"),
        new Vector3(-160f, 0f, 20f), Quaternion.Euler(0f, 90f, 0f), buildings);
    // Cluckin' Bell (produce stand)
    InstantiatePrefab(P("Buildings/SM_Bld_ProduceStand_01.prefab"),
        new Vector3(-120f, 0f, 38f), Quaternion.Euler(0f, 180f, 0f), buildings);
    // Community center (garage — largest non-barn)
    InstantiatePrefab(P("Buildings/SM_Bld_Garage_01.prefab"),
        new Vector3(-75f, 0f, 38f), Quaternion.Euler(0f, 180f, 0f), buildings);
    // Cafe (shelter)
    InstantiatePrefab(P("Buildings/SM_Bld_Shelter_01.prefab"),
        new Vector3(-160f, 0f, -10f), Quaternion.identity, buildings);
    // Library (large greenhouse)
    InstantiatePrefab(P("Buildings/SM_Bld_Greenhouse_Large_01.prefab"),
        new Vector3(-120f, 0f, -10f), Quaternion.identity, buildings);
    // Town houses
    InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_01.prefab"),
        new Vector3(-80f, 0f, -20f), Quaternion.Euler(0f, 45f, 0f), buildings);
    InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_01.prefab"),
        new Vector3(-100f, 0f, -40f), Quaternion.Euler(0f, -30f, 0f), buildings);

    // ── Main Street Furniture ──
    // Painted fences along Main Street
    string paintedFence = P("Props/SM_Prop_Fence_Painted_01.prefab");
    for (float x = -185f; x <= -55f; x += 8f)
    {
        InstantiatePrefab(paintedFence, new Vector3(x, 0f, 25f),
            Quaternion.Euler(0f, 90f, 0f), mainSt);
        InstantiatePrefab(paintedFence, new Vector3(x, 0f, 35f),
            Quaternion.Euler(0f, 90f, 0f), mainSt);
    }

    // Lamp posts
    string lamp = P("Props/SM_Prop_Lamp_01.prefab");
    for (float x = -180f; x <= -60f; x += 25f)
    {
        InstantiatePrefab(lamp, new Vector3(x, 0f, 24f), Quaternion.identity, mainSt);
    }

    // Signs
    InstantiatePrefab(P("Props/SM_Prop_Sign_01.prefab"),
        new Vector3(-145f, 0f, 36f), Quaternion.Euler(0f, 180f, 0f), mainSt);
    InstantiatePrefab(P("Props/SM_Prop_Sign_02.prefab"),
        new Vector3(-95f, 0f, 36f), Quaternion.Euler(0f, 180f, 0f), mainSt);

    // Letterboxes at houses
    InstantiatePrefab(P("Props/SM_Prop_Letterbox_01.prefab"),
        new Vector3(-155f, 0f, 18f), Quaternion.identity, mainSt);
    InstantiatePrefab(P("Props/SM_Prop_Letterbox_01.prefab"),
        new Vector3(-75f, 0f, -18f), Quaternion.identity, mainSt);
    InstantiatePrefab(P("Props/SM_Prop_Letterbox_01.prefab"),
        new Vector3(-95f, 0f, -38f), Quaternion.identity, mainSt);

    // ── Town Props ──
    InstantiatePrefab(P("Props/SM_Prop_Barrel_01.prefab"), new Vector3(-118f, 0f, 36f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_Barrel_02.prefab"), new Vector3(-116f, 0f, 37f), Quaternion.Euler(0f, 60f, 0f), props);
    InstantiatePrefab(P("Props/SM_Prop_Crate_01.prefab"), new Vector3(-122f, 0f, 36f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_Crate_02.prefab"), new Vector3(-124f, 0f, 37f), Quaternion.identity, props);
    InstantiatePrefab(P("Props/SM_Prop_Bench_01.prefab"), new Vector3(-73f, 0f, 28f), Quaternion.Euler(0f, 90f, 0f), props);
    InstantiatePrefab(P("Props/SM_Prop_Bench_01.prefab"), new Vector3(-73f, 0f, 22f), Quaternion.Euler(0f, 90f, 0f), props);
    InstantiatePrefab(P("Props/SM_Prop_Beehive_01.prefab"), new Vector3(-85f, 0f, -25f), Quaternion.identity, props);

    // Decorative bushes
    string bush1 = P("Props/SM_Prop_Plant_Bush_01.prefab");
    string bush2 = P("Props/SM_Prop_Plant_Bush_02.prefab");
    float[] bushX = { -165f, -155f, -125f, -115f, -80f, -70f, -105f, -95f };
    for (int i = 0; i < bushX.Length; i++)
    {
        string b = (i % 2 == 0) ? bush1 : bush2;
        InstantiatePrefab(b, new Vector3(bushX[i], 0f, 18f + (i % 3) * 2f),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), props);
    }

    // ── Town Trees ──
    InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"),
        new Vector3(-170f, 0f, 30f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"),
        new Vector3(-140f, 0f, 30f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"),
        new Vector3(-100f, 0f, 30f), Quaternion.identity, trees);
    InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"),
        new Vector3(-60f, 0f, 30f), Quaternion.identity, trees);

    // Tree cluster at town edges
    string treePatch = P("Generic/SM_Generic_Tree_Patch_01.prefab");
    InstantiatePrefab(treePatch, new Vector3(-190f, 0f, -50f), Quaternion.identity, trees);
    InstantiatePrefab(treePatch, new Vector3(-50f, 0f, -60f), Quaternion.Euler(0f, 45f, 0f), trees);

    Debug.Log("[WorldSceneBuilder] Town zone fully populated.");
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Editor/WorldSceneBuilder.cs
git commit -m "feat(world): populate town zone with buildings, Main Street, and props"
```

---

## Task 9: BuildUnpopulatedZones — Markers + minimal vegetation

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Implement**

```csharp
private static void BuildUnpopulatedZones()
{
    string P(string name) => $"Assets/Synty/PolygonFarm/Prefabs/{name}";
    string PN(string name) => $"Assets/PolygonNature/Prefabs/{name}";

    // ── North Field ──
    var nf = GameObject.Find("NorthField");
    if (nf != null)
    {
        var m = nf.transform.Find("Markers");
        var marker = CreateEmpty("FestivalCenter", new Vector3(-120f, 0f, 150f), m);
        marker.AddComponent<BoxCollider>().isTrigger = true;
    }

    // ── Sandy Shores ──
    var ss = GameObject.Find("SandyShores");
    if (ss != null)
    {
        var m = ss.transform.Find("Markers");
        var trailerSpot = CreateEmpty("TrevorTrailerPosition", new Vector3(120f, 0f, 160f), m);
        // Pebbles at boundary
        for (int i = 0; i < 5; i++)
            InstantiatePrefab(P("Environments/SM_Env_Pebbles_01.prefab"),
                new Vector3(50f + i * 30f, 0f, 118f), Quaternion.identity, ss.transform);
    }

    // ── Meadow ──
    var meadow = GameObject.Find("Meadow");
    if (meadow != null)
    {
        var m = meadow.transform.Find("Markers");
        CreateEmpty("TruffleSpot_01", new Vector3(-150f, 0f, -110f), m);
        CreateEmpty("TruffleSpot_02", new Vector3(-100f, 0f, -130f), m);
        CreateEmpty("TruffleSpot_03", new Vector3(-170f, 0f, -140f), m);

        // Wildflower species markers (5 for Michael's quest)
        for (int i = 0; i < 5; i++)
        {
            var spot = CreateEmpty($"WildflowerSpecies_{i + 1}",
                new Vector3(-180f + i * 25f, 0f, -100f - i * 10f), m);
            spot.AddComponent<BoxCollider>().isTrigger = true;
        }
    }

    // ── County Fair ──
    var fair = GameObject.Find("CountyFair");
    if (fair != null)
    {
        var m = fair.transform.Find("Markers");
        CreateEmpty("TrainLoopCenter", new Vector3(160f, 0f, -120f), m);
        CreateEmpty("PettingZooArea", new Vector3(150f, 0f, -100f), m);

        // Fancy fence perimeter hint
        string fancyFence = P("Props/SM_Prop_Fence_Fancy_01.prefab");
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            var pos = new Vector3(160f + Mathf.Sin(angle) * 35f, 0f,
                                  -120f + Mathf.Cos(angle) * 35f);
            InstantiatePrefab(fancyFence, pos,
                Quaternion.Euler(0f, i * 45f, 0f), fair.transform);
        }
    }

    // ── River Zone ──
    var river = GameObject.Find("River");
    if (river != null)
    {
        var m = river.transform.Find("Markers");
        CreateEmpty("TheTruthVanPosition", new Vector3(10f, 0f, -140f), m);
    }

    // ── Wildflower Hills ──
    var hills = GameObject.Find("WildflowerHills");
    if (hills != null)
    {
        var m = hills.transform.Find("Markers");
        CreateEmpty("MichaelEaselPosition", new Vector3(0f, 2f, -175f), m);
        for (int i = 0; i < 5; i++)
            CreateEmpty($"FlowerSpawnPoint_{i + 1}",
                new Vector3(-150f + i * 75f, 0f, -180f), m);
    }

    // ── Trail ──
    var trail = GameObject.Find("Trail");
    if (trail != null)
    {
        var m = trail.transform.Find("Markers");
        CreateEmpty("TenpennyLampPost", new Vector3(0f, 0f, 60f), m);
    }

    Debug.Log("[WorldSceneBuilder] Unpopulated zones markers placed.");
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Editor/WorldSceneBuilder.cs
git commit -m "feat(world): add markers and boundaries for unpopulated zones"
```

---

## Task 10: BuildVegetation — World-scattered trees, grass, rocks

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Implement**

```csharp
private static void BuildVegetation()
{
    var vegRoot = CreateEmpty("Vegetation", Vector3.zero);
    var treesGrp = CreateEmpty("Trees", Vector3.zero, vegRoot.transform);
    var groundGrp = CreateEmpty("GroundCover", Vector3.zero, vegRoot.transform);
    var flowersGrp = CreateEmpty("Flowers", Vector3.zero, vegRoot.transform);
    var bushesGrp = CreateEmpty("Bushes", Vector3.zero, vegRoot.transform);
    var rocksGrp = CreateEmpty("Rocks", Vector3.zero, vegRoot.transform);

    string PN(string name) => $"Assets/PolygonNature/Prefabs/{name}";
    string PF(string name) => $"Assets/Synty/PolygonFarm/Prefabs/{name}";

    // ── Scattered Trees (world-wide, outside Farm/Town) ──
    string[] treePrefabs = {
        PN("Trees/SM_Tree_Birch_01.prefab"),
        PN("Trees/SM_Tree_Birch_02.prefab"),
        PN("Trees/SM_Tree_Birch_03.prefab"),
        PN("Trees/SM_Tree_Pine_01.prefab"),
        PN("Trees/SM_Tree_Pine_02.prefab"),
        PN("Trees/SM_Tree_Pine_03.prefab"),
        PN("Trees/SM_Tree_Willow_Medium_01.prefab"),
        PN("Trees/SM_Tree_Willow_Large_01.prefab"),
    };

    // Scatter points for world trees (avoid Farm and Town zones)
    Vector3[] worldTreePositions = {
        // North Field
        new(-180f, 0f, 180f), new(-100f, 0f, 160f), new(-60f, 0f, 130f),
        // Meadow edges
        new(-190f, 0f, -90f), new(-70f, 0f, -100f), new(-140f, 0f, -150f),
        // River banks (willows)
        new(-50f, 0f, -100f), new(0f, 0f, -105f), new(50f, 0f, -95f),
        new(90f, 0f, -100f),
        // Trail sides
        new(-30f, 0f, 70f), new(30f, 0f, 65f), new(-25f, 0f, 40f),
        // Sandy Shores edges
        new(50f, 0f, 140f), new(190f, 0f, 130f),
        // Hills
        new(-100f, 2f, -175f), new(50f, 2f, -185f), new(150f, 1f, -170f),
        // County Fair edges
        new(125f, 0f, -85f), new(195f, 0f, -90f),
    };

    for (int i = 0; i < worldTreePositions.Length; i++)
    {
        string prefab = treePrefabs[i % treePrefabs.Length];
        InstantiatePrefab(prefab, worldTreePositions[i],
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), treesGrp.transform);
    }

    // Dead trees along river
    string deadTree = PN("Trees/SM_Tree_Dead_01.prefab");
    InstantiatePrefab(deadTree, new Vector3(-30f, 0f, -130f), Quaternion.identity, treesGrp.transform);
    InstantiatePrefab(deadTree, new Vector3(70f, 0f, -135f), Quaternion.Euler(0f, 120f, 0f), treesGrp.transform);

    // Stumps
    string stump = PN("Trees/SM_Tree_Stump_01.prefab");
    InstantiatePrefab(stump, new Vector3(-150f, 0f, 110f), Quaternion.identity, treesGrp.transform);
    InstantiatePrefab(stump, new Vector3(30f, 0f, -170f), Quaternion.identity, treesGrp.transform);

    // ── Grass Patches (~150 scattered) ──
    string[] grassPrefabs = {
        PF("Generic/SM_Generic_Grass_Patch_01.prefab"),
        PF("Generic/SM_Generic_Grass_Patch_02.prefab"),
        PF("Generic/SM_Generic_Grass_Patch_03.prefab"),
    };

    // Semi-random scatter across world
    var rng = new System.Random(42); // Deterministic seed
    for (int i = 0; i < 150; i++)
    {
        float x = (float)(rng.NextDouble() * 380 - 190);
        float z = (float)(rng.NextDouble() * 380 - 190);
        // Skip inside Farm and Town populated areas (they have their own)
        if (x > 45 && x < 195 && z > -15 && z < 95) continue; // Farm
        if (x < -45 && x > -195 && z > -75 && z < 95) continue; // Town
        string gp = grassPrefabs[i % 3];
        InstantiatePrefab(gp, new Vector3(x, 0f, z),
            Quaternion.Euler(0f, (float)(rng.NextDouble() * 360), 0f), groundGrp.transform);
    }

    // ── Flowers (~40 in Meadow, Hills, North Field) ──
    string[] flowerPrefabs = {
        PF("Environments/SM_Env_Flowers_01.prefab"),
        PF("Environments/SM_Env_Flowers_02.prefab"),
        PF("Environments/SM_Env_Flowers_03.prefab"),
    };

    for (int i = 0; i < 40; i++)
    {
        float x, z;
        int zone = i % 3;
        if (zone == 0) { // Meadow
            x = -180f + (float)(rng.NextDouble() * 120);
            z = -155f + (float)(rng.NextDouble() * 70);
        } else if (zone == 1) { // North Field
            x = -190f + (float)(rng.NextDouble() * 140);
            z = 105f + (float)(rng.NextDouble() * 90);
        } else { // Hills
            x = -190f + (float)(rng.NextDouble() * 380);
            z = -195f + (float)(rng.NextDouble() * 35);
        }
        InstantiatePrefab(flowerPrefabs[i % 3], new Vector3(x, 0f, z),
            Quaternion.Euler(0f, (float)(rng.NextDouble() * 360), 0f), flowersGrp.transform);
    }

    // ── Bushes (~20 at zone boundaries) ──
    string[] bushPrefabs = {
        PN("Plants/SM_Bush_01.prefab"),
        PN("Plants/SM_Bush_02.prefab"),
        PN("Plants/SM_Bush_03.prefab"),
    };
    for (int i = 0; i < 20; i++)
    {
        float x = -190f + (float)(rng.NextDouble() * 380);
        float z = -190f + (float)(rng.NextDouble() * 380);
        if (x > 45 && x < 195 && z > -15 && z < 95) continue;
        if (x < -45 && x > -195 && z > -75 && z < 95) continue;
        InstantiatePrefab(bushPrefabs[i % 3], new Vector3(x, 0f, z),
            Quaternion.Euler(0f, (float)(rng.NextDouble() * 360), 0f), bushesGrp.transform);
    }

    // ── Rocks (~30 along river, hills, boundaries) ──
    string[] rockPrefabs = {
        PN("Rocks/SM_Rock_Small_01.prefab"),
        PN("Rocks/SM_Rock_Small_02.prefab"),
        PN("Rocks/SM_Rock_Large_01.prefab"),
        PN("Rocks/SM_Rock_Boulder_01.prefab"),
    };
    // Along river
    for (int i = 0; i < 15; i++)
    {
        float x = -50f + (float)(rng.NextDouble() * 160);
        float z = -140f + (float)(rng.NextDouble() * 40);
        InstantiatePrefab(rockPrefabs[i % rockPrefabs.Length], new Vector3(x, 0f, z),
            Quaternion.Euler(0f, (float)(rng.NextDouble() * 360), 0f), rocksGrp.transform);
    }
    // Along hills
    for (int i = 0; i < 10; i++)
    {
        float x = -180f + (float)(rng.NextDouble() * 360);
        float z = -195f + (float)(rng.NextDouble() * 30);
        InstantiatePrefab(rockPrefabs[i % rockPrefabs.Length], new Vector3(x, 0f, z),
            Quaternion.Euler(0f, (float)(rng.NextDouble() * 360), 0f), rocksGrp.transform);
    }

    // Ferns along meadow/river
    string[] fernPrefabs = {
        PN("Plants/SM_Fern_01.prefab"),
        PN("Plants/SM_Fern_02.prefab"),
        PN("Plants/SM_Fern_03.prefab"),
    };
    for (int i = 0; i < 15; i++)
    {
        float x = -170f + (float)(rng.NextDouble() * 200);
        float z = -155f + (float)(rng.NextDouble() * 80);
        InstantiatePrefab(fernPrefabs[i % 3], new Vector3(x, 0f, z),
            Quaternion.Euler(0f, (float)(rng.NextDouble() * 360), 0f), groundGrp.transform);
    }

    Debug.Log("[WorldSceneBuilder] World vegetation scattered (trees, grass, flowers, rocks).");
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Editor/WorldSceneBuilder.cs
git commit -m "feat(world): scatter world vegetation with deterministic seeding"
```

---

## Task 11: BuildFX + BuildMarkers — Atmosphere and spawn points

**Files:**
- Modify: `Assets/_Project/Editor/WorldSceneBuilder.cs`

**Step 1: Implement**

```csharp
private static void BuildFX()
{
    var fxRoot = CreateEmpty("FX", Vector3.zero);
    string PF(string name) => $"Assets/Synty/PolygonFarm/Prefabs/{name}";

    // Pollen in meadow
    InstantiatePrefab(PF("FX/FX_Pollen_Wind_01.prefab"),
        new Vector3(-130f, 2f, -120f), Quaternion.identity, fxRoot.transform);

    // Dust wind in sandy shores
    InstantiatePrefab(PF("FX/FX_Dust_Wind_01.prefab"),
        new Vector3(120f, 1f, 160f), Quaternion.identity, fxRoot.transform);

    Debug.Log("[WorldSceneBuilder] FX placed.");
}

private static void BuildMarkers()
{
    var markersRoot = CreateEmpty("Markers", Vector3.zero);

    // Spawn point (farm entrance)
    var spawn = CreateEmpty("SpawnPoint", new Vector3(50f, 0.5f, 45f), markersRoot.transform);
    spawn.tag = "SpawnPoint";

    Debug.Log("[WorldSceneBuilder] Markers placed.");
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Editor/WorldSceneBuilder.cs
git commit -m "feat(world): add FX atmosphere and spawn point marker"
```

---

## Task 12: Final validation + run tests + preflight

**Step 1: Run all tests**

```bash
./run-tests.sh editmode
```

Expected: ALL PASS

**Step 2: Open Unity Editor, run FarmSim > Build World Scene (New)**

Visual verification checklist:
- [ ] Terrain renders with correct textures per zone
- [ ] Fog fades world edges to golden haze
- [ ] River water planes visible in carved channel
- [ ] Creek crossing with bridge in trail zone
- [ ] Farm buildings present and correctly positioned
- [ ] Circular pen visible
- [ ] Crop plots with dirt rows visible
- [ ] Town buildings along Main Street
- [ ] Gravel road visible through town
- [ ] Dirt trail connecting town to farm
- [ ] Zone markers present in hierarchy
- [ ] No magenta (missing prefab) cubes

**Step 3: Commit the generated scene**

```bash
git add Assets/_Project/Scenes/WorldMain.unity Assets/_Project/Scenes/WorldMain_TerrainData.asset
git commit -m "feat(world): add generated WorldMain.unity scene"
```

**Step 4: Run preflight**

```bash
./preflight.sh
```

Expected: All gates pass (or warnings only for PlayMode which is empty)

---

## Task Summary

| # | Task | Commits | Est. Lines |
|---|------|---------|-----------|
| 1 | WorldSceneBuilder skeleton + test | 1 | ~80 |
| 2 | BuildSceneConfig (lighting, fog) | 1 | ~60 |
| 3 | BuildTerrain (7 layers + elevation) | 1 | ~120 |
| 4 | BuildZoneHierarchy (9 zones) | 1 | ~40 |
| 5 | BuildWater (river, creek, pond) | 1 | ~70 |
| 6 | BuildPaths (roads) | 1 | ~60 |
| 7 | BuildFarmZone (buildings, pen, plots) | 1 | ~150 |
| 8 | BuildTownZone (buildings, street) | 1 | ~120 |
| 9 | BuildUnpopulatedZones (markers) | 1 | ~80 |
| 10 | BuildVegetation (trees, grass, rocks) | 1 | ~140 |
| 11 | BuildFX + BuildMarkers | 1 | ~30 |
| 12 | Validation + scene generation | 1 | ~0 (testing) |
| **Total** | | **12 commits** | **~950 lines** |
