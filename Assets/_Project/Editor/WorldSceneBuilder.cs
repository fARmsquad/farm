using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Audio;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds the full world scene per INT-001 spec.
    /// Menu: FarmSim > Build World Scene (New)
    /// </summary>
    public static partial class WorldSceneBuilder
    {
        // ── World Constants ───────────────────────────────────────
        public const float TerrainSize = 250f;
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
            new(72, 24, 96, 72),          // Farm
            new(-72, 6, 96, 108),         // Town
            new(-72, 90, 96, 60),         // NorthField
            new(72, 96, 96, 48),          // SandyShores
            new(-78, -72, 84, 48),        // Meadow
            new(18, -72, 108, 48),        // River
            new(96, -72, 48, 48),         // CountyFair
            new(0, -108, 250, 24),        // WildflowerHills
            new(0, 30, 48, 60)            // Trail
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

            UpgradePolygonNatureMaterials();
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
            BuildZoneSigns();
            BuildExplorationPlayer();
            BuildDebugPanelHost();
            BuildScreenEffectsUI();
            BuildSimpleAudioManager();
            BuildDialogueUI();
            BuildCinematicCamera();
            BuildMissionManager();
            BuildNPCDemos();
            BuildPhase4Demos();
            RegisterScenesInBuildSettings();

            EditorSceneManager.SaveScene(scene,
                "Assets/_Project/Scenes/WorldMain.unity");
            Debug.Log("[WorldSceneBuilder] WorldMain.unity created and saved.");
        }

        // ── Core Build Methods ────────────────────────────────────

        private static void BuildSceneConfig()
        {
            var configRoot = CreateEmpty("_SceneConfig", Vector3.zero);

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
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 220f;

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
            probe.size = new Vector3(250f, 20f, 250f);
            probe.resolution = 256;
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probeGo.transform.SetParent(configRoot.transform);

            Debug.Log("[WorldSceneBuilder] Scene config built (light, fog, ambient, probe).");
        }
        private static void BuildTerrain()
        {
            var terrainRoot = CreateEmpty("Terrain", Vector3.zero);

            // ── TerrainData ──
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = HeightmapRes;
            terrainData.alphamapResolution = AlphamapRes;
            terrainData.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);

            // ── Terrain Layers (saved as assets so textures persist) ──
            const string layerDir = "Assets/_Project/TerrainLayers";
            if (!AssetDatabase.IsValidFolder(layerDir))
                AssetDatabase.CreateFolder("Assets/_Project", "TerrainLayers");

            var layers = new TerrainLayer[TerrainTexturePaths.Length];
            for (int i = 0; i < TerrainTexturePaths.Length; i++)
            {
                layers[i] = new TerrainLayer();
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTexturePaths[i]);
                if (tex != null) layers[i].diffuseTexture = tex;
                else Debug.LogWarning($"[WorldSceneBuilder] Missing terrain texture: {TerrainTexturePaths[i]}");
                layers[i].tileSize = new Vector2(15f, 15f);

                if (TerrainNormalPaths[i] != null)
                {
                    var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainNormalPaths[i]);
                    if (nrm != null) layers[i].normalMapTexture = nrm;
                }

                string layerPath = $"{layerDir}/Layer_{i}_{System.IO.Path.GetFileNameWithoutExtension(TerrainTexturePaths[i])}.terrainlayer";
                AssetDatabase.CreateAsset(layers[i], layerPath);
            }
            AssetDatabase.SaveAssets();
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
            terrain.basemapDistance = 100f;

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

            // Sandy Shores
            PaintZoneTexture(alphas, res, 24f, 72f, 120f, 120f, LAYER_SAND, LAYER_GRASS);

            // Farm plots
            PaintZoneTexture(alphas, res, 36f, 15f, 66f, 39f, LAYER_MUD, LAYER_GRASS);

            // River channel
            PaintZoneTexture(alphas, res, -36f, -96f, 72f, -48f, LAYER_MUD, LAYER_GRASS);

            // Meadow
            PaintZoneTexture(alphas, res, -120f, -96f, -36f, -48f, LAYER_GRASS2, LAYER_GRASS);
            // Inner meadow flowers
            PaintZoneTexture(alphas, res, -108f, -90f, -48f, -54f, LAYER_FLOWERS, LAYER_GRASS2);

            // North Field
            PaintZoneTexture(alphas, res, -108f, 66f, -36f, 114f, LAYER_FLOWERS, LAYER_GRASS);

            // Town Main St
            PaintZoneTexture(alphas, res, -114f, 15f, -30f, 21f, LAYER_PEBBLES, LAYER_GRASS);

            // Trail
            PaintZoneTexture(alphas, res, -9f, 0f, 9f, 57f, LAYER_PEBBLES, LAYER_GRASS);

            // Wildflower Hills
            PaintZoneTexture(alphas, res, -120f, -120f, 120f, -96f, LAYER_GRASS2, LAYER_GRASS);

            terrainData.SetAlphamaps(0, 0, alphas);
        }

        private static void PaintZoneTexture(float[,,] alphas, int res,
            float worldMinX, float worldMinZ, float worldMaxX, float worldMaxZ,
            int newLayer, int replaceLayer)
        {
            float halfSize = TerrainSize / 2f;
            int xMin = Mathf.Clamp(Mathf.FloorToInt((worldMinX + halfSize) / TerrainSize * res), 0, res - 1);
            int xMax = Mathf.Clamp(Mathf.CeilToInt((worldMaxX + halfSize) / TerrainSize * res), 0, res - 1);
            int zMin = Mathf.Clamp(Mathf.FloorToInt((worldMinZ + halfSize) / TerrainSize * res), 0, res - 1);
            int zMax = Mathf.Clamp(Mathf.CeilToInt((worldMaxZ + halfSize) / TerrainSize * res), 0, res - 1);

            for (int z = zMin; z <= zMax; z++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    int feather = 5;
                    float dx = Mathf.Min(x - xMin, xMax - x, feather) / (float)feather;
                    float dz = Mathf.Min(z - zMin, zMax - z, feather) / (float)feather;
                    float blend = Mathf.Clamp01(Mathf.Min(dx, dz));

                    alphas[z, x, replaceLayer] = 1f - blend;
                    alphas[z, x, newLayer] = blend;
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
                    float worldX = ((float)x / (res - 1)) * TerrainSize - TerrainSize / 2f;
                    float worldZ = ((float)z / (res - 1)) * TerrainSize - TerrainSize / 2f;

                    float h = 0f;

                    // Farm: flat elevated area
                    if (worldX > 24f && worldX < 120f && worldZ > -12f && worldZ < 60f)
                    {
                        h = 0.5f;
                    }
                    // Trail: lerp from west to east
                    else if (worldX > -24f && worldX < 24f && worldZ > 0f && worldZ < 60f)
                    {
                        h = Mathf.Lerp(0f, 0.5f, (worldX + 24f) / 48f);
                    }
                    // Meadow: gentle rolling
                    else if (worldX < -36f && worldZ > -96f && worldZ < -48f)
                    {
                        h = Mathf.Sin(worldX * 0.08f) * Mathf.Sin(worldZ * 0.12f) * 2f;
                    }
                    // River: carved channel
                    else if (worldX > -36f && worldX < 72f && worldZ > -93f && worldZ < -51f)
                    {
                        float riverCenter = -72f;
                        float distFromCenter = Mathf.Abs(worldZ - riverCenter);
                        float blendDist = 9f;
                        float halfWidth = 21f;

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
                    else if (worldZ < -96f)
                    {
                        h = (Mathf.Sin(worldX * 0.05f + 1.5f) + 1f) * 2f
                            * (Mathf.Cos(worldZ * 0.08f) * 0.5f + 0.5f);
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
        // ── Screen Effects UI + Bootstrap ────────────────────────

        private static void BuildScreenEffectsUI()
        {
            var root = new GameObject("ScreenEffectsCanvas");

            // Canvas
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            // Fade overlay (full-screen black image + CanvasGroup for alpha)
            var fadeGo = new GameObject("FadeOverlay");
            fadeGo.transform.SetParent(root.transform, false);
            var fadeRect = fadeGo.AddComponent<RectTransform>();
            StretchFull(fadeRect);
            var fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
            var fadeGroup = fadeGo.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1;
            fadeGroup.blocksRaycasts = false;

            // Letterbox bars
            var topBar = CreateLetterboxBar("TopBar", root.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            var bottomBar = CreateLetterboxBar("BottomBar", root.transform,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));

            // Objective popup container
            var objContainer = new GameObject("ObjectiveContainer");
            objContainer.transform.SetParent(root.transform, false);
            var objRect = objContainer.AddComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0, 0.5f);
            objRect.anchorMax = new Vector2(1, 0.5f);
            objRect.sizeDelta = new Vector2(0, 80);
            var objBg = objContainer.AddComponent<Image>();
            objBg.color = new Color(0, 0, 0, 0.6f);
            objContainer.SetActive(false);

            var objTextGo = new GameObject("ObjectiveText");
            objTextGo.transform.SetParent(objContainer.transform, false);
            StretchFull(objTextGo.AddComponent<RectTransform>());
            var objTmp = objTextGo.AddComponent<TextMeshProUGUI>();
            objTmp.fontSize = 32;
            objTmp.color = Color.white;
            objTmp.alignment = TextAlignmentOptions.Center;

            // Mission passed banner
            var missionGo = new GameObject("MissionPassed");
            missionGo.transform.SetParent(root.transform, false);
            var missionRect = missionGo.AddComponent<RectTransform>();
            missionRect.anchorMin = new Vector2(0, 0.4f);
            missionRect.anchorMax = new Vector2(1, 0.6f);
            missionRect.sizeDelta = Vector2.zero;
            var missionBg = missionGo.AddComponent<Image>();
            missionBg.color = new Color(0, 0, 0, 0.7f);
            var missionGroup = missionGo.AddComponent<CanvasGroup>();
            missionGroup.alpha = 0;
            missionGo.SetActive(false);

            var missionTextGo = new GameObject("MissionText");
            missionTextGo.transform.SetParent(missionGo.transform, false);
            StretchFull(missionTextGo.AddComponent<RectTransform>());
            var missionTmp = missionTextGo.AddComponent<TextMeshProUGUI>();
            missionTmp.fontSize = 48;
            missionTmp.color = Color.white;
            missionTmp.alignment = TextAlignmentOptions.Center;

            // Attach ScreenEffects and wire serialized fields
            var fx = root.AddComponent<ScreenEffects>();
            var so = new SerializedObject(fx);
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeImage;
            so.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeGroup;
            so.FindProperty("topBar").objectReferenceValue = topBar;
            so.FindProperty("bottomBar").objectReferenceValue = bottomBar;
            so.FindProperty("objectiveContainer").objectReferenceValue = objRect;
            so.FindProperty("objectiveText").objectReferenceValue = objTmp;
            so.FindProperty("missionPassedGroup").objectReferenceValue = missionGroup;
            so.FindProperty("missionPassedText").objectReferenceValue = missionTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Demo overlay
            var demoGo = new GameObject("ScreenEffectsDemo");
            var demo = demoGo.AddComponent<ScreenEffectsDemo>();
            var demoSo = new SerializedObject(demo);
            demoSo.FindProperty("screenEffects").objectReferenceValue = fx;
            demoSo.ApplyModifiedPropertiesWithoutUndo();

            // Attach bootstrap
            var bootstrapGo = new GameObject("WorldSceneBootstrap");
            bootstrapGo.AddComponent<WorldSceneBootstrap>();

            Debug.Log("[WorldSceneBuilder] ScreenEffects UI canvas, demo, and bootstrap added.");
        }

        // ── Debug Panel Host ─────────────────────────────────────

        private static void BuildDebugPanelHost()
        {
            var go = new GameObject("DebugPanelHost");
            go.AddComponent<DebugPanelHost>();
            Debug.Log("[WorldSceneBuilder] DebugPanelHost added (press Tab to open master menu).");
        }

        // ── INT-002: Simple Audio Manager ──────────────────────

        private static void BuildSimpleAudioManager()
        {
            var go = new GameObject("SimpleAudioManager");
            go.AddComponent<SimpleAudioManager>();
            var demoGo = new GameObject("SimpleAudioManagerDemo");
            demoGo.AddComponent<SimpleAudioManagerDemo>();
            Debug.Log("[WorldSceneBuilder] SimpleAudioManager and demo added.");
        }

        // ── INT-003: Dialogue System ─────────────────────────────

        private static void BuildDialogueUI()
        {
            var root = new GameObject("DialogueCanvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            // Panel — bottom-center, 80% width, 180px tall
            var panelGo = new GameObject("DialoguePanel");
            panelGo.transform.SetParent(root.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0f);
            panelRect.anchorMax = new Vector2(0.9f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(0, 180);
            panelRect.anchoredPosition = new Vector2(0, 30);
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

            // Speaker name
            var speakerGo = new GameObject("SpeakerName");
            speakerGo.transform.SetParent(panelGo.transform, false);
            var speakerRect = speakerGo.AddComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0, 0.78f);
            speakerRect.anchorMax = new Vector2(1, 1);
            speakerRect.sizeDelta = Vector2.zero;
            speakerRect.offsetMin = new Vector2(24, 0);
            speakerRect.offsetMax = new Vector2(-24, -8);
            var speakerTmp = speakerGo.AddComponent<TextMeshProUGUI>();
            speakerTmp.fontSize = 26;
            speakerTmp.fontStyle = FontStyles.Bold;
            speakerTmp.color = Color.yellow;
            speakerTmp.alignment = TextAlignmentOptions.BottomLeft;

            // Dialogue text
            var dialogueGo = new GameObject("DialogueText");
            dialogueGo.transform.SetParent(panelGo.transform, false);
            var dialogueRect = dialogueGo.AddComponent<RectTransform>();
            dialogueRect.anchorMin = Vector2.zero;
            dialogueRect.anchorMax = new Vector2(1, 0.78f);
            dialogueRect.sizeDelta = Vector2.zero;
            dialogueRect.offsetMin = new Vector2(24, 12);
            dialogueRect.offsetMax = new Vector2(-24, -4);
            var dialogueTmp = dialogueGo.AddComponent<TextMeshProUGUI>();
            dialogueTmp.fontSize = 22;
            dialogueTmp.color = Color.white;
            dialogueTmp.alignment = TextAlignmentOptions.TopLeft;
            dialogueTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            var mgr = root.AddComponent<DialogueManager>();
            var so = new SerializedObject(mgr);
            so.FindProperty("dialogueCanvas").objectReferenceValue = canvas;
            so.FindProperty("speakerNameText").objectReferenceValue = speakerTmp;
            so.FindProperty("dialogueText").objectReferenceValue = dialogueTmp;
            so.FindProperty("panelBackground").objectReferenceValue = panelBg;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);

            var demoGo = new GameObject("DialogueDemo");
            demoGo.AddComponent<DialogueDemo>();
            Debug.Log("[WorldSceneBuilder] DialogueManager canvas and demo added.");
        }

        // ── INT-004: Cinematic Camera ────────────────────────────

        private static void BuildCinematicCamera()
        {
            var camGo = new GameObject("CinematicCamera");
            camGo.transform.position = new Vector3(0f, 20f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.enabled = false;
            camGo.AddComponent<UniversalAdditionalCameraData>();

            Camera gameplayCam = null;
            foreach (var c in Object.FindObjectsByType<Camera>())
            {
                if (c.gameObject.CompareTag("MainCamera"))
                { gameplayCam = c; break; }
            }

            var cineCam = camGo.AddComponent<CinematicCamera>();
            var so = new SerializedObject(cineCam);
            so.FindProperty("cinematicCam").objectReferenceValue = cam;
            if (gameplayCam != null)
                so.FindProperty("gameplayCam").objectReferenceValue = gameplayCam;
            so.ApplyModifiedPropertiesWithoutUndo();

            var demoGo = new GameObject("CinematicCameraDemo");
            demoGo.AddComponent<CinematicCameraDemo>();
            Debug.Log("[WorldSceneBuilder] CinematicCamera and demo added.");
        }

        // ── INT-007: Mission Manager ─────────────────────────────

        private static void BuildMissionManager()
        {
            var go = new GameObject("MissionManager");
            go.AddComponent<MissionManager>();
            var demoGo = new GameObject("MissionManagerDemo");
            demoGo.AddComponent<MissionManagerDemo>();
            Debug.Log("[WorldSceneBuilder] MissionManager and demo added.");
        }

        // ── INT-006: NPC Demo ────────────────────────────────────

        private static void BuildNPCDemos()
        {
            var demoGo = new GameObject("NPCControllerDemo");
            demoGo.AddComponent<NPCControllerDemo>();
            Debug.Log("[WorldSceneBuilder] NPCController demo added.");
        }

        // ── Phase 4: Lighting, Particles, Comic Text, Skip, Props ──

        private static void BuildPhase4Demos()
        {
            // INT-009: Lighting Transition + Demo
            var lightingGo = new GameObject("LightingTransition");
            lightingGo.AddComponent<LightingTransition>();
            var cascadeGo = new GameObject("WindowCascade");
            cascadeGo.AddComponent<WindowCascade>();
            new GameObject("LightingDemo").AddComponent<LightingDemo>();

            // INT-010: Particle Effects Demo
            new GameObject("ParticleEffectsDemo").AddComponent<ParticleEffectsDemo>();

            // INT-011: Comic Text Manager + Demo
            new GameObject("ComicTextManager").AddComponent<ComicTextManager>();
            new GameObject("ComicTextDemo").AddComponent<ComicTextDemo>();

            // INT-012: Skip Prompt + Demo
            new GameObject("SkipPrompt").AddComponent<SkipPrompt>();
            new GameObject("SkipAndSaveDemo").AddComponent<SkipAndSaveDemo>();

            // INT-013: Intro Props Demo (props spawned at runtime)
            new GameObject("IntroPropsDemo").AddComponent<IntroPropsDemo>();

            Debug.Log("[WorldSceneBuilder] Phase 4 demos added (Lighting, Particles, ComicText, Skip, IntroProps).");
        }

        private static RectTransform CreateLetterboxBar(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(0, 0);
            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return rect;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void RegisterScenesInBuildSettings()
        {
            string[] requiredScenes = {
                "Assets/_Project/Scenes/TitleScreen.unity",
                "Assets/_Project/Scenes/WorldMain.unity",
                "Assets/_Project/Scenes/FarmMain.unity",
            };

            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (string path in requiredScenes)
            {
                bool found = false;
                foreach (var s in existing)
                {
                    if (s.path == path) { found = true; break; }
                }
                if (!found)
                    existing.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = existing.ToArray();
            Debug.Log("[WorldSceneBuilder] Build Settings updated with TitleScreen, WorldMain, FarmMain.");
        }

        // ── Material Upgrade ──────────────────────────────────────
        /// <summary>
        /// Upgrades PolygonNature materials from built-in/custom shaders to URP Lit.
        /// Without this, trees, bushes, rocks, ferns etc. render invisible in URP.
        /// </summary>
        private static void UpgradePolygonNatureMaterials()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogWarning("[WorldSceneBuilder] URP Lit shader not found, skipping material upgrade.");
                return;
            }

            // Shaders that need upgrading (built-in Standard + all custom Amplify shaders)
            string[] incompatibleShaderNames = {
                "Standard",
                "SyntyStudios/Trees",
                "SyntyStudios/Moss",
                "SyntyStudios/Water",
                "SyntyStudios/LOD",
                "SyntyStudios/Vines",
                "SyntyStudios/SkyGradient",
            };

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/PolygonNature/Materials" });
            int upgraded = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) continue;

                bool needsUpgrade = false;
                foreach (string shaderName in incompatibleShaderNames)
                {
                    if (mat.shader.name == shaderName)
                    { needsUpgrade = true; break; }
                }
                if (!needsUpgrade) continue;

                // Preserve texture and color before switching shader
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                if (mainTex == null && mat.HasProperty("_MainTexture"))
                    mainTex = mat.GetTexture("_MainTexture");
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                if (mat.HasProperty("_ColorTint"))
                {
                    var tint = mat.GetColor("_ColorTint");
                    if (tint != Color.black) color = tint;
                }

                bool wasTransparent = mat.renderQueue > 2500;

                mat.shader = urpLit;

                if (mainTex != null)
                    mat.SetTexture("_BaseMap", mainTex);
                if (color != Color.black)
                    mat.SetColor("_BaseColor", color);

                // Handle transparency for leaves/foliage
                if (wasTransparent || (mainTex != null && mainTex.name.ToLower().Contains("leaf")))
                {
                    mat.SetFloat("_Surface", 0); // Opaque with alpha clip
                    mat.SetFloat("_AlphaClip", 1);
                    mat.SetFloat("_Cutoff", 0.5f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.renderQueue = 2450;
                }

                EditorUtility.SetDirty(mat);
                upgraded++;
            }

            if (upgraded > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[WorldSceneBuilder] Upgraded {upgraded} PolygonNature materials to URP Lit.");
            }
        }

        // ── Helpers ───────────────────────────────────────────────

        /// <summary>
        /// Returns the terrain surface height at the given world XZ position.
        /// </summary>
        public static float SampleTerrainHeight(float worldX, float worldZ)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return 0f;
            return terrain.SampleHeight(new Vector3(worldX, 0f, worldZ));
        }

        /// <summary>
        /// Returns a world position snapped to terrain surface with an optional Y offset.
        /// </summary>
        public static Vector3 GroundPos(float x, float z, float yOffset = 0f)
        {
            return new Vector3(x, SampleTerrainHeight(x, z) + yOffset, z);
        }

        public static GameObject CreateEmpty(string name, Vector3 pos, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            if (parent != null)
                go.transform.SetParent(parent);
            return go;
        }

        /// <summary>
        /// Instantiates a prefab. When snapToTerrain is true (default), pos.y is ignored
        /// and the object is placed on the terrain surface. Pass snapToTerrain=false for
        /// water or objects that need an explicit Y position.
        /// </summary>
        public static GameObject InstantiatePrefab(string path, Vector3 pos, Quaternion rot,
            Transform parent, bool snapToTerrain = true)
        {
            Vector3 finalPos = snapToTerrain
                ? new Vector3(pos.x, SampleTerrainHeight(pos.x, pos.z), pos.z)
                : pos;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = finalPos;
                instance.transform.rotation = rot;
                if (parent != null)
                    instance.transform.SetParent(parent);
                return instance;
            }

            Debug.LogWarning($"[WorldSceneBuilder] Prefab not found at '{path}', using placeholder.");
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = $"MISSING_{System.IO.Path.GetFileNameWithoutExtension(path)}";
            placeholder.transform.position = finalPos;
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
