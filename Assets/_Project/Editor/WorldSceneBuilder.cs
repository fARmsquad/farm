using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

        // ── Scene Config Colors ──────────────────────────────────
        public static readonly Color FogColor = new(1f, 0.83f, 0.62f);
        public static readonly Color AmbientSky = new(0.31f, 0.28f, 0.38f);
        public static readonly Color AmbientEquator = new(0.55f, 0.50f, 0.42f);
        public static readonly Color AmbientGround = new(0.23f, 0.20f, 0.16f);
        public static readonly Color SunColor = new(1f, 0.96f, 0.88f);

        // ── Terrain Texture Paths ────────────────────────────────
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
            null,
            "Assets/PolygonNature/Textures/Ground_Textures/Pebbles_normals.png",
            "Assets/PolygonNature/Textures/Ground_Textures/Flowers_normals.png",
            null,
        };

        public const int LAYER_GRASS = 0;
        public const int LAYER_GRASS2 = 1;
        public const int LAYER_MUD = 2;
        public const int LAYER_SAND = 3;
        public const int LAYER_PEBBLES = 4;
        public const int LAYER_FLOWERS = 5;
        public const int LAYER_SAND_DARK = 6;

        // ── Water Prefab Paths ───────────────────────────────────
        public static readonly string[] WaterPrefabPaths = {
            "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
            "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab",
            "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_Corner_01.prefab",
            "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Pond_01.prefab",
            "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Reeds_01.prefab",
            "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Reeds_02.prefab",
            "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Lillypads_01.prefab",
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

        private static void BuildSceneConfig()
        {
            var configRoot = CreateEmpty("_SceneConfig", Vector3.zero);

            // ── Main Camera ──
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 50f, -100f);
            camGo.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 500f;
            camGo.AddComponent<UniversalAdditionalCameraData>();

            // ── Directional Light ──
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.7f;
            lightGo.AddComponent<UniversalAdditionalLightData>();
            lightGo.transform.SetParent(configRoot.transform);

            // ── RenderSettings: Fog ──
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 350f;

            // ── RenderSettings: Ambient ──
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;

            // ── Skybox ──
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/SkyboxProcedural.mat");
            if (skyMat == null)
                skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/SkyboxProcedural.mat");
            if (skyMat != null)
                RenderSettings.skybox = skyMat;

            // ── Reflection Probe ──
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.position = new Vector3(0f, 5f, 0f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.size = new Vector3(400f, 20f, 400f);
            probe.resolution = 256;
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probeGo.transform.SetParent(configRoot.transform);

            Debug.Log("[WorldSceneBuilder] Scene config built (camera, light, fog, ambient, probe).");
        }
        private static void BuildTerrain()
        {
            var terrainRoot = CreateEmpty("Terrain", Vector3.zero);

            // ── TerrainData ──
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = HeightmapRes;
            terrainData.alphamapResolution = AlphamapRes;
            terrainData.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);

            // ── Terrain Layers ──
            var layers = new TerrainLayer[TerrainTexturePaths.Length];
            for (int i = 0; i < TerrainTexturePaths.Length; i++)
            {
                layers[i] = new TerrainLayer();
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTexturePaths[i]);
                if (tex != null) layers[i].diffuseTexture = tex;
                else Debug.LogWarning($"[WorldSceneBuilder] Missing terrain texture: {TerrainTexturePaths[i]}");
                layers[i].tileSize = new Vector2(10f, 10f);

                if (TerrainNormalPaths[i] != null)
                {
                    var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainNormalPaths[i]);
                    if (nrm != null) layers[i].normalMapTexture = nrm;
                }
            }
            terrainData.terrainLayers = layers;

            // ── Paint Textures & Heights ──
            PaintTerrainTextures(terrainData);
            PaintTerrainHeights(terrainData);

            // ── Create Terrain GO ──
            var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            terrainGo.name = "Willowbrook_Terrain";
            terrainGo.transform.position = new Vector3(-TerrainSize / 2f, 0f, -TerrainSize / 2f);

            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 8;
            terrain.basemapDistance = 150f;

            terrainGo.transform.SetParent(terrainRoot.transform);

            // ── Save TerrainData asset ──
            AssetDatabase.CreateAsset(terrainData, "Assets/_Project/Scenes/WorldMain_TerrainData.asset");
            AssetDatabase.SaveAssets();

            Debug.Log("[WorldSceneBuilder] Terrain built with 7 layers, splatmaps, and elevation.");
        }

        private static void PaintTerrainTextures(TerrainData terrainData)
        {
            int res = terrainData.alphamapResolution;
            int layerCount = terrainData.terrainLayers.Length;
            float[,,] alphas = new float[res, res, layerCount];

            // Default: all grass
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                    alphas[y, x, LAYER_GRASS] = 1f;

            // Sandy Shores: (40,120) -> (200,200)
            PaintZoneTexture(alphas, res, 40f, 120f, 200f, 200f, LAYER_SAND, LAYER_GRASS);

            // Farm plots: (60,25) -> (110,65)
            PaintZoneTexture(alphas, res, 60f, 25f, 110f, 65f, LAYER_MUD, LAYER_GRASS);

            // River channel: (-60,-160) -> (120,-80)
            PaintZoneTexture(alphas, res, -60f, -160f, 120f, -80f, LAYER_MUD, LAYER_GRASS);

            // Meadow: (-200,-160) -> (-60,-80) as GRASS2
            PaintZoneTexture(alphas, res, -200f, -160f, -60f, -80f, LAYER_GRASS2, LAYER_GRASS);
            // Inner meadow flowers: (-180,-150) -> (-80,-90)
            PaintZoneTexture(alphas, res, -180f, -150f, -80f, -90f, LAYER_FLOWERS, LAYER_GRASS2);

            // North Field: (-180,110) -> (-60,190)
            PaintZoneTexture(alphas, res, -180f, 110f, -60f, 190f, LAYER_FLOWERS, LAYER_GRASS);

            // Town Main St: (-190,25) -> (-50,35)
            PaintZoneTexture(alphas, res, -190f, 25f, -50f, 35f, LAYER_PEBBLES, LAYER_GRASS);

            // Trail: (-15,0) -> (15,95)
            PaintZoneTexture(alphas, res, -15f, 0f, 15f, 95f, LAYER_PEBBLES, LAYER_GRASS);

            // Wildflower Hills: (-200,-200) -> (200,-160)
            PaintZoneTexture(alphas, res, -200f, -200f, 200f, -160f, LAYER_GRASS2, LAYER_GRASS);

            terrainData.SetAlphamaps(0, 0, alphas);
        }

        private static void PaintZoneTexture(float[,,] alphas, int res,
            float worldMinX, float worldMinZ, float worldMaxX, float worldMaxZ,
            int newLayer, int replaceLayer)
        {
            // Convert world coords to alphamap coords
            // Terrain origin is at (-TerrainSize/2, -TerrainSize/2)
            float halfSize = TerrainSize / 2f;
            int xMin = Mathf.Clamp(Mathf.FloorToInt((worldMinX + halfSize) / TerrainSize * res), 0, res - 1);
            int xMax = Mathf.Clamp(Mathf.CeilToInt((worldMaxX + halfSize) / TerrainSize * res), 0, res - 1);
            int zMin = Mathf.Clamp(Mathf.FloorToInt((worldMinZ + halfSize) / TerrainSize * res), 0, res - 1);
            int zMax = Mathf.Clamp(Mathf.CeilToInt((worldMaxZ + halfSize) / TerrainSize * res), 0, res - 1);

            for (int z = zMin; z <= zMax; z++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    if (alphas[z, x, replaceLayer] > 0f)
                    {
                        alphas[z, x, replaceLayer] = 0f;
                        alphas[z, x, newLayer] = 1f;
                    }
                }
            }
        }

        private static void PaintTerrainHeights(TerrainData terrainData)
        {
            int res = terrainData.heightmapResolution;
            float[,] heights = new float[res, res];

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    // Convert heightmap coords to world coords
                    float worldX = ((float)x / (res - 1)) * TerrainSize - TerrainSize / 2f;
                    float worldZ = ((float)z / (res - 1)) * TerrainSize - TerrainSize / 2f;

                    float h = 0f;

                    // Farm: flat elevated area
                    if (worldX > 40f && worldX < 200f && worldZ > -20f && worldZ < 100f)
                    {
                        h = 0.5f;
                    }
                    // Trail: lerp from west to east
                    else if (worldX > -40f && worldX < 40f && worldZ > 0f && worldZ < 100f)
                    {
                        h = Mathf.Lerp(0f, 0.5f, (worldX + 40f) / 80f);
                    }
                    // Meadow: gentle rolling
                    else if (worldX < -60f && worldZ > -160f && worldZ < -80f)
                    {
                        h = Mathf.Sin(worldX * 0.05f) * Mathf.Sin(worldZ * 0.07f) * 2f;
                    }
                    // River: carved channel
                    else if (worldX > -60f && worldX < 120f && worldZ > -155f && worldZ < -85f)
                    {
                        float riverCenter = -120f;
                        float distFromCenter = Mathf.Abs(worldZ - riverCenter);
                        float blendDist = 15f;
                        float halfWidth = 35f;

                        if (distFromCenter < halfWidth - blendDist)
                        {
                            h = -1f;
                        }
                        else if (distFromCenter < halfWidth)
                        {
                            float t = (distFromCenter - (halfWidth - blendDist)) / blendDist;
                            h = Mathf.Lerp(-1f, 0f, t);
                        }
                    }
                    // Hills: southern hills
                    else if (worldZ < -160f)
                    {
                        h = (Mathf.Sin(worldX * 0.03f + 1.5f) + 1f) * 2f
                            * (Mathf.Cos(worldZ * 0.05f) * 0.5f + 0.5f);
                    }

                    heights[z, x] = h / TerrainHeight;
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }
        private static void BuildZoneHierarchy()
        {
            for (int i = 0; i < ZoneNames.Length; i++)
            {
                var bounds = ZoneBounds[i];
                var zone = CreateEmpty(ZoneNames[i], new Vector3(bounds.x, 0f, bounds.y));

                var trigger = zone.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(bounds.z, 10f, bounds.w);
                trigger.center = Vector3.up * 5f;

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
        private static void BuildWater()
        {
            var waterRoot = CreateEmpty("Water", Vector3.zero);

            // ── River ──
            var riverParent = CreateEmpty("River", Vector3.zero, waterRoot.transform);
            Vector3[] riverPoints = {
                new(-40f, -0.8f, -120f),
                new(-10f, -0.8f, -115f),
                new(20f, -0.8f, -125f),
                new(50f, -0.8f, -118f),
                new(80f, -0.8f, -122f),
                new(110f, -0.8f, -115f),
            };
            for (int i = 0; i < riverPoints.Length; i++)
            {
                float yRot = (i % 2 == 0) ? 0f : 15f;
                var seg = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                    riverPoints[i], Quaternion.Euler(0f, yRot, 0f), riverParent.transform);
                seg.name = $"RiverSegment_{i}";
            }

            // ── River Banks ──
            for (int i = 0; i < riverPoints.Length; i++)
            {
                // North bank (+12 z)
                var northPos = riverPoints[i] + new Vector3(0f, 0f, 12f);
                var northBank = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab",
                    northPos, Quaternion.identity, riverParent.transform);
                northBank.name = $"RiverBank_North_{i}";

                // South bank (-12 z), rotated 180
                var southPos = riverPoints[i] + new Vector3(0f, 0f, -12f);
                var southBank = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab",
                    southPos, Quaternion.Euler(0f, 180f, 0f), riverParent.transform);
                southBank.name = $"RiverBank_South_{i}";
            }

            // ── Reeds along river banks ──
            for (int i = 0; i < 10; i++)
            {
                int ptIdx = i % riverPoints.Length;
                float zOffset = (i % 2 == 0) ? 14f : -14f;
                var reedPos = riverPoints[ptIdx] + new Vector3(i * 3f - 15f, 0.2f, zOffset);
                var reed = InstantiatePrefab(
                    "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Reeds_01.prefab",
                    reedPos, Quaternion.identity, riverParent.transform);
                reed.name = $"Reed_{i}";
            }

            // ── Creek ──
            var creekParent = CreateEmpty("Creek", Vector3.zero, waterRoot.transform);
            var creek0 = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                new Vector3(-10f, -0.3f, 55f), Quaternion.Euler(0f, 30f, 0f), creekParent.transform);
            creek0.transform.localScale = Vector3.one * 0.4f;
            creek0.name = "CreekSegment_0";

            var creek1 = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                new Vector3(10f, -0.3f, 50f), Quaternion.Euler(0f, 30f, 0f), creekParent.transform);
            creek1.transform.localScale = Vector3.one * 0.4f;
            creek1.name = "CreekSegment_1";

            // ── Bridge over creek ──
            var bridge = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Props/SM_Prop_Bridge_Curved_01.prefab",
                new Vector3(0f, 0f, 52f), Quaternion.Euler(0f, 90f, 0f), creekParent.transform);
            bridge.name = "CreekBridge";

            // ── Farm Pond ──
            var pond = InstantiatePrefab(
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Pond_01.prefab",
                new Vector3(165f, 0f, 20f), Quaternion.identity, waterRoot.transform);
            pond.name = "FarmPond";

            // ── Lily pads ──
            var lilypads = InstantiatePrefab(
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Lillypads_01.prefab",
                new Vector3(165f, 0.05f, 20f), Quaternion.identity, waterRoot.transform);
            lilypads.name = "FarmPondLilypads";

            Debug.Log("[WorldSceneBuilder] Water system built (river, creek, bridge, pond).");
        }

        private static void BuildPaths()
        {
            var pathsRoot = CreateEmpty("Paths", Vector3.zero);

            // ── MainStreet ──
            var mainStreet = CreateEmpty("MainStreet", Vector3.zero, pathsRoot.transform);
            const string gravelStraight = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Gravel_Straight_01.prefab";
            const string gravelEnd = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Gravel_End_01.prefab";

            // Straight segments along z=30 from x=-185 to x=-55, step 10
            for (float x = -185f; x <= -55f; x += 10f)
            {
                var seg = InstantiatePrefab(gravelStraight,
                    new Vector3(x, 0.02f, 30f), Quaternion.Euler(0f, 90f, 0f), mainStreet.transform);
                seg.name = $"MainStreet_Straight_{(int)x}";
            }

            // End caps
            var endCapW = InstantiatePrefab(gravelEnd,
                new Vector3(-190f, 0.02f, 30f), Quaternion.Euler(0f, -90f, 0f), mainStreet.transform);
            endCapW.name = "MainStreet_EndCap_West";

            var endCapE = InstantiatePrefab(gravelEnd,
                new Vector3(-50f, 0.02f, 30f), Quaternion.Euler(0f, 90f, 0f), mainStreet.transform);
            endCapE.name = "MainStreet_EndCap_East";

            // ── TrailToFarm ──
            var trailToFarm = CreateEmpty("TrailToFarm", Vector3.zero, pathsRoot.transform);
            const string dirtStraight = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Dirt_Straight_01.prefab";
            const string dirtSwerve = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Dirt_Swerve_01.prefab";

            Vector3[] trailPoints = {
                new(-35f, 0.02f, 30f),
                new(-25f, 0.02f, 35f),
                new(-15f, 0.02f, 42f),
                new(-5f, 0.02f, 48f),
                new(5f, 0.02f, 50f),
                new(15f, 0.02f, 50f),
                new(25f, 0.02f, 48f),
                new(35f, 0.02f, 45f),
            };
            int[] swerveIndices = { 2, 5 };

            for (int i = 0; i < trailPoints.Length; i++)
            {
                bool isSwerve = System.Array.IndexOf(swerveIndices, i) >= 0;
                string prefab = isSwerve ? dirtSwerve : dirtStraight;

                // Calculate y-rotation from direction to next point
                float yRot = 0f;
                if (i < trailPoints.Length - 1)
                {
                    Vector3 dir = trailPoints[i + 1] - trailPoints[i];
                    yRot = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                }
                else
                {
                    // Last point: use same rotation as previous segment
                    Vector3 dir = trailPoints[i] - trailPoints[i - 1];
                    yRot = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                }

                var seg = InstantiatePrefab(prefab,
                    trailPoints[i], Quaternion.Euler(0f, yRot, 0f), trailToFarm.transform);
                seg.name = $"Trail_{(isSwerve ? "Swerve" : "Straight")}_{i}";
            }

            // ── FarmPaths ──
            var farmPaths = CreateEmpty("FarmPaths", Vector3.zero, pathsRoot.transform);

            // N-S: x=100, z from 45 to 75 step 10, y=0.52
            for (float z = 45f; z <= 75f; z += 10f)
            {
                var seg = InstantiatePrefab(dirtStraight,
                    new Vector3(100f, 0.52f, z), Quaternion.identity, farmPaths.transform);
                seg.name = $"FarmPath_NS_{(int)z}";
            }

            // E-W: z=55, x from 70 to 100 step 10, y=0.52, rotated 90 y
            for (float x = 70f; x <= 100f; x += 10f)
            {
                var seg = InstantiatePrefab(dirtStraight,
                    new Vector3(x, 0.52f, 55f), Quaternion.Euler(0f, 90f, 0f), farmPaths.transform);
                seg.name = $"FarmPath_EW_{(int)x}";
            }

            Debug.Log("[WorldSceneBuilder] Path network built (MainStreet, TrailToFarm, FarmPaths).");
        }
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
