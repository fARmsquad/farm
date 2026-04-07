using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Diagnostics;
using FarmSimVR.MonoBehaviours.Audio;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds the Feature Showcase scene — a guided step-by-step demo of every
    /// system built so far. Menu: FarmSim > Build Feature Showcase
    /// </summary>
    public static class FeatureShowcaseBuilder
    {
        [MenuItem("FarmSim/Build Feature Showcase")]
        public static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BuildSceneConfig();
            BuildGround();
            BuildPlayer();
            BuildScreenEffectsUI();
            BuildDialogueUI();
            BuildCinematicCamera();
            BuildAudioManager();
            BuildMissionManager();
            BuildGameStateLogger();
            BuildShowcaseManager();
            RegisterScene();

            EditorSceneManager.SaveScene(scene,
                "Assets/_Project/Scenes/FeatureShowcase.unity");
            Debug.Log("[FeatureShowcaseBuilder] FeatureShowcase.unity created and saved.");
        }

        // ── Scene Config ─────────────────────────────────────────

        private static void BuildSceneConfig()
        {
            // Remove default camera (we'll make our own on the player)
            var defaultCam = Object.FindAnyObjectByType<Camera>();
            if (defaultCam != null)
                Object.DestroyImmediate(defaultCam.gameObject);

            // Light
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightGo.AddComponent<UniversalAdditionalLightData>();

            // Fog + ambient
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.85f, 0.9f, 0.95f);
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 150f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.4f, 0.45f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.5f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.18f);

            // Skybox
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/SkyboxProcedural.mat");
            if (skyMat == null)
                skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/SkyboxProcedural.mat");
            if (skyMat != null)
                RenderSettings.skybox = skyMat;

            Debug.Log("[FeatureShowcaseBuilder] Scene config built.");
        }

        // ── Ground plane ──────────────────────────────────────────

        private static void BuildGround()
        {
            // Simple flat ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            ground.isStatic = true;

            var renderer = ground.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.35f, 0.55f, 0.25f));
            renderer.material = mat;

            // NPC spawn marker
            var npcSpawn = new GameObject("NPCSpawnPoint");
            npcSpawn.transform.position = new Vector3(5f, 0f, 5f);

            // Some simple props to make it feel like a space
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Farmhouse_01.prefab",
                new Vector3(15f, 0f, 10f), Quaternion.Euler(0f, 180f, 0f));
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Barn_01.prefab",
                new Vector3(-15f, 0f, 12f), Quaternion.Euler(0f, 90f, 0f));
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Well_01.prefab",
                new Vector3(8f, 0f, -5f), Quaternion.identity);
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Scarecrow_01.prefab",
                new Vector3(-5f, 0f, -8f), Quaternion.identity);

            // Zone triggers for zone detection demo
            CreateZone("Farm", new Vector3(10f, 0f, 10f), new Vector3(30f, 10f, 30f));
            CreateZone("Town", new Vector3(-10f, 0f, 10f), new Vector3(30f, 10f, 30f));

            Debug.Log("[FeatureShowcaseBuilder] Ground and props built.");
        }

        private static void PlaceProp(string prefabPath, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = pos;
                instance.transform.rotation = rot;
            }
            else
            {
                var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placeholder.name = $"MISSING_{System.IO.Path.GetFileNameWithoutExtension(prefabPath)}";
                placeholder.transform.position = pos;
                placeholder.transform.rotation = rot;
                var r = placeholder.GetComponent<Renderer>();
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", Color.magenta);
                r.material = m;
            }
        }

        private static void CreateZone(string zoneName, Vector3 center, Vector3 size)
        {
            var zone = new GameObject(zoneName);
            zone.transform.position = center;
            var trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;
            trigger.center = Vector3.up * (size.y / 2f);

            var marker = zone.AddComponent<ZoneMarker>();
            var so = new SerializedObject(marker);
            so.FindProperty("zoneName").stringValue = zoneName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Player ────────────────────────────────────────────────

        private static void BuildPlayer()
        {
            var player = new GameObject("ExplorationPlayer");
            player.transform.position = new Vector3(0f, 1.5f, 0f);
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Camera
            var camGo = new GameObject("FirstPersonCamera");
            camGo.transform.SetParent(player.transform);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<UniversalAdditionalCameraData>();

            // Remove duplicate AudioListeners
            foreach (var al in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                Object.DestroyImmediate(al);
            camGo.AddComponent<AudioListener>();

            player.AddComponent<FirstPersonExplorer>();
            player.AddComponent<ZoneTracker>();

            Debug.Log("[FeatureShowcaseBuilder] Player built.");
        }

        // ── Screen Effects (same pattern as WorldSceneBuilder) ───

        private static void BuildScreenEffectsUI()
        {
            var root = new GameObject("ScreenEffectsCanvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            // Fade overlay
            var fadeGo = new GameObject("FadeOverlay");
            fadeGo.transform.SetParent(root.transform, false);
            var fadeRect = fadeGo.AddComponent<RectTransform>();
            StretchFull(fadeRect);
            var fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
            var fadeGroup = fadeGo.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0; // start visible (not faded)
            fadeGroup.blocksRaycasts = false;

            // Letterbox bars
            var topBar = CreateLetterboxBar("TopBar", root.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            var bottomBar = CreateLetterboxBar("BottomBar", root.transform,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));

            // Objective popup
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

            Debug.Log("[FeatureShowcaseBuilder] ScreenEffects UI built.");
        }

        // ── Dialogue UI ───────────────────────────────────────────

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
            dialogueTmp.enableWordWrapping = true;

            var mgr = root.AddComponent<DialogueManager>();
            var so = new SerializedObject(mgr);
            so.FindProperty("dialogueCanvas").objectReferenceValue = canvas;
            so.FindProperty("speakerNameText").objectReferenceValue = speakerTmp;
            so.FindProperty("dialogueText").objectReferenceValue = dialogueTmp;
            so.FindProperty("panelBackground").objectReferenceValue = panelBg;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);

            Debug.Log("[FeatureShowcaseBuilder] Dialogue UI built.");
        }

        // ── Cinematic Camera ──────────────────────────────────────

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
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
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

            Debug.Log("[FeatureShowcaseBuilder] Cinematic camera built.");
        }

        // ── Audio Manager ─────────────────────────────────────────

        private static void BuildAudioManager()
        {
            var go = new GameObject("SimpleAudioManager");
            go.AddComponent<SimpleAudioManager>();
            Debug.Log("[FeatureShowcaseBuilder] Audio manager built.");
        }

        // ── Mission Manager ───────────────────────────────────────

        private static void BuildMissionManager()
        {
            var go = new GameObject("MissionManager");
            go.AddComponent<MissionManager>();
            Debug.Log("[FeatureShowcaseBuilder] Mission manager built.");
        }

        // ── Game State Logger ─────────────────────────────────────

        private static void BuildGameStateLogger()
        {
            var go = new GameObject("GameStateLogger");
            go.AddComponent<GameStateLogger>();
            Debug.Log("[FeatureShowcaseBuilder] Game state logger built.");
        }

        // ── Showcase Manager ──────────────────────────────────────

        private static void BuildShowcaseManager()
        {
            var go = new GameObject("FeatureShowcaseManager");
            var mgr = go.AddComponent<FeatureShowcaseManager>();

            var so = new SerializedObject(mgr);

            var player = GameObject.Find("ExplorationPlayer");
            if (player != null)
            {
                var explorer = player.GetComponent<FirstPersonExplorer>();
                so.FindProperty("explorer").objectReferenceValue = explorer;
            }

            var npcSpawn = GameObject.Find("NPCSpawnPoint");
            if (npcSpawn != null)
                so.FindProperty("npcSpawnPoint").objectReferenceValue = npcSpawn.transform;

            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[FeatureShowcaseBuilder] Showcase manager built and wired.");
        }

        // ── Register Scene ────────────────────────────────────────

        private static void RegisterScene()
        {
            string scenePath = "Assets/_Project/Scenes/FeatureShowcase.unity";
            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            bool found = false;
            foreach (var s in existing)
            {
                if (s.path == scenePath) { found = true; break; }
            }
            if (!found)
                existing.Add(new EditorBuildSettingsScene(scenePath, true));

            EditorBuildSettings.scenes = existing.ToArray();
            Debug.Log("[FeatureShowcaseBuilder] Scene registered in build settings.");
        }

        // ── Helpers ───────────────────────────────────────────────

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
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
    }
}
