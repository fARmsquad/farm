using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Autoplay;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Audio;
using FarmSimVR.MonoBehaviours.Diagnostics;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Builds autoplay demo scenes for each spec. Each scene auto-runs through
    /// that feature's capabilities when you press Play.
    /// Menu: FarmSim > Autoplay > ...
    /// </summary>
    public static class AutoplaySceneBuilder
    {
        private const string SceneDir = "Assets/_Project/Scenes/Autoplay";

        [MenuItem("FarmSim/Autoplay/Build All Autoplay Scenes")]
        public static void BuildAll()
        {
            EnsureDirectory();
            BuildScreenEffectsScene();
            BuildAudioScene();
            BuildDialogueScene();
            BuildCinematicCameraScene();
            BuildNPCScene();
            BuildMissionScene();
            Debug.Log("[AutoplaySceneBuilder] All 6 autoplay scenes built.");
        }

        [MenuItem("FarmSim/Autoplay/INT-001 Screen Effects")]
        public static void BuildScreenEffectsScene()
        {
            EnsureDirectory();
            BeginScene();
            BuildCommonSceneConfig();
            BuildGround();
            BuildPlayer();
            BuildScreenEffectsUI();

            new GameObject("Autoplay").AddComponent<AutoplayScreenEffects>();

            SaveScene("Autoplay_INT001_ScreenEffects");
        }

        [MenuItem("FarmSim/Autoplay/INT-002 Audio Manager")]
        public static void BuildAudioScene()
        {
            EnsureDirectory();
            BeginScene();
            BuildCommonSceneConfig();
            BuildGround();
            BuildPlayer();
            new GameObject("SimpleAudioManager").AddComponent<SimpleAudioManager>();

            new GameObject("Autoplay").AddComponent<AutoplayAudio>();

            SaveScene("Autoplay_INT002_AudioManager");
        }

        [MenuItem("FarmSim/Autoplay/INT-003 Dialogue System")]
        public static void BuildDialogueScene()
        {
            EnsureDirectory();
            BeginScene();
            BuildCommonSceneConfig();
            BuildGround();
            BuildPlayer();
            BuildDialogueUI();

            new GameObject("Autoplay").AddComponent<AutoplayDialogue>();

            SaveScene("Autoplay_INT003_Dialogue");
        }

        [MenuItem("FarmSim/Autoplay/INT-004 Cinematic Camera")]
        public static void BuildCinematicCameraScene()
        {
            EnsureDirectory();
            BeginScene();
            BuildCommonSceneConfig();
            BuildGround();
            var player = BuildPlayer();
            BuildCinematicCamera();

            // Place some props so camera has something to fly around
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Farmhouse_01.prefab",
                new Vector3(15f, 0f, 10f), Quaternion.Euler(0f, 180f, 0f));
            PlaceProp("Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Barn_01.prefab",
                new Vector3(-15f, 0f, 12f), Quaternion.Euler(0f, 90f, 0f));

            var autoGo = new GameObject("Autoplay");
            var auto = autoGo.AddComponent<AutoplayCinematicCamera>();
            var so = new SerializedObject(auto);
            so.FindProperty("playerTransform").objectReferenceValue = player.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveScene("Autoplay_INT004_CinematicCamera");
        }

        [MenuItem("FarmSim/Autoplay/INT-006 NPC Controller")]
        public static void BuildNPCScene()
        {
            EnsureDirectory();
            BeginScene();
            BuildCommonSceneConfig();
            BuildGround();
            BuildPlayer();
            BuildDialogueUI();

            new GameObject("Autoplay").AddComponent<AutoplayNPC>();

            SaveScene("Autoplay_INT006_NPC");
        }

        [MenuItem("FarmSim/Autoplay/INT-007 Mission Manager")]
        public static void BuildMissionScene()
        {
            EnsureDirectory();
            BeginScene();
            BuildCommonSceneConfig();
            BuildGround();
            BuildPlayer();
            BuildScreenEffectsUI();
            new GameObject("MissionManager").AddComponent<MissionManager>();
            new GameObject("SimpleAudioManager").AddComponent<SimpleAudioManager>();

            new GameObject("Autoplay").AddComponent<AutoplayMission>();

            SaveScene("Autoplay_INT007_Mission");
        }

        // ══════════════════════════════════════════════════════════
        // Shared build helpers
        // ══════════════════════════════════════════════════════════

        private static void EnsureDirectory()
        {
            if (!AssetDatabase.IsValidFolder(SceneDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
                AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Autoplay");
            }
        }

        private static void BeginScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        private static void SaveScene(string name)
        {
            string path = $"{SceneDir}/{name}.unity";
            EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);

            // Register in build settings
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in scenes) { if (s.path == path) { found = true; break; } }
            if (!found) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log($"[AutoplaySceneBuilder] {name}.unity saved.");
        }

        private static void BuildCommonSceneConfig()
        {
            // Remove default camera
            var defaultCam = Object.FindAnyObjectByType<Camera>();
            if (defaultCam != null) Object.DestroyImmediate(defaultCam.gameObject);

            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightGo.AddComponent<UniversalAdditionalLightData>();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.85f, 0.9f, 0.95f);
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 150f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.4f, 0.45f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.5f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.18f);

            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/SkyboxProcedural.mat")
                      ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/SkyboxProcedural.mat");
            if (skyMat != null) RenderSettings.skybox = skyMat;
        }

        private static void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.isStatic = true;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.35f, 0.55f, 0.25f));
            ground.GetComponent<Renderer>().material = mat;
        }

        private static GameObject BuildPlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1.5f, -5f);
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(player.transform);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<UniversalAdditionalCameraData>();

            foreach (var al in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                Object.DestroyImmediate(al);
            camGo.AddComponent<AudioListener>();

            return player;
        }

        private static void PlaceProp(string path, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.position = pos; inst.transform.rotation = rot;
            }
        }

        // ── ScreenEffects UI (mirrors WorldSceneBuilder) ─────────

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

            var fadeGo = new GameObject("FadeOverlay");
            fadeGo.transform.SetParent(root.transform, false);
            var fadeRect = fadeGo.AddComponent<RectTransform>();
            Stretch(fadeRect);
            var fadeImg = fadeGo.AddComponent<Image>();
            fadeImg.color = Color.black; fadeImg.raycastTarget = false;
            var fadeGrp = fadeGo.AddComponent<CanvasGroup>();
            fadeGrp.alpha = 0; fadeGrp.blocksRaycasts = false;

            var topBar = LetterboxBar("TopBar", root.transform, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1));
            var botBar = LetterboxBar("BottomBar", root.transform, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0));

            var objC = new GameObject("ObjectiveContainer");
            objC.transform.SetParent(root.transform, false);
            var objR = objC.AddComponent<RectTransform>();
            objR.anchorMin = new Vector2(0,0.5f); objR.anchorMax = new Vector2(1,0.5f);
            objR.sizeDelta = new Vector2(0,80);
            objC.AddComponent<Image>().color = new Color(0,0,0,0.6f);
            objC.SetActive(false);
            var objTGo = new GameObject("ObjectiveText");
            objTGo.transform.SetParent(objC.transform, false);
            Stretch(objTGo.AddComponent<RectTransform>());
            var objTmp = objTGo.AddComponent<TextMeshProUGUI>();
            objTmp.fontSize = 32; objTmp.color = Color.white; objTmp.alignment = TextAlignmentOptions.Center;

            var msGo = new GameObject("MissionPassed");
            msGo.transform.SetParent(root.transform, false);
            var msR = msGo.AddComponent<RectTransform>();
            msR.anchorMin = new Vector2(0,0.4f); msR.anchorMax = new Vector2(1,0.6f); msR.sizeDelta = Vector2.zero;
            msGo.AddComponent<Image>().color = new Color(0,0,0,0.7f);
            var msGrp = msGo.AddComponent<CanvasGroup>(); msGrp.alpha = 0;
            msGo.SetActive(false);
            var msTGo = new GameObject("MissionText");
            msTGo.transform.SetParent(msGo.transform, false);
            Stretch(msTGo.AddComponent<RectTransform>());
            var msTmp = msTGo.AddComponent<TextMeshProUGUI>();
            msTmp.fontSize = 48; msTmp.color = Color.white; msTmp.alignment = TextAlignmentOptions.Center;

            var fx = root.AddComponent<ScreenEffects>();
            var so = new SerializedObject(fx);
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeImg;
            so.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeGrp;
            so.FindProperty("topBar").objectReferenceValue = topBar;
            so.FindProperty("bottomBar").objectReferenceValue = botBar;
            so.FindProperty("objectiveContainer").objectReferenceValue = objR;
            so.FindProperty("objectiveText").objectReferenceValue = objTmp;
            so.FindProperty("missionPassedGroup").objectReferenceValue = msGrp;
            so.FindProperty("missionPassedText").objectReferenceValue = msTmp;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Dialogue UI (mirrors WorldSceneBuilder) ──────────────

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

            var panel = new GameObject("DialoguePanel");
            panel.transform.SetParent(root.transform, false);
            var pR = panel.AddComponent<RectTransform>();
            pR.anchorMin = new Vector2(0.1f,0); pR.anchorMax = new Vector2(0.9f,0);
            pR.pivot = new Vector2(0.5f,0); pR.sizeDelta = new Vector2(0,180);
            pR.anchoredPosition = new Vector2(0,30);
            var pBg = panel.AddComponent<Image>();
            pBg.color = new Color(0.05f,0.05f,0.1f,0.85f);

            var spkGo = new GameObject("SpeakerName");
            spkGo.transform.SetParent(panel.transform, false);
            var spkR = spkGo.AddComponent<RectTransform>();
            spkR.anchorMin = new Vector2(0,0.78f); spkR.anchorMax = new Vector2(1,1);
            spkR.sizeDelta = Vector2.zero; spkR.offsetMin = new Vector2(24,0); spkR.offsetMax = new Vector2(-24,-8);
            var spkTmp = spkGo.AddComponent<TextMeshProUGUI>();
            spkTmp.fontSize = 26; spkTmp.fontStyle = FontStyles.Bold;
            spkTmp.color = Color.yellow; spkTmp.alignment = TextAlignmentOptions.BottomLeft;

            var dlgGo = new GameObject("DialogueText");
            dlgGo.transform.SetParent(panel.transform, false);
            var dlgR = dlgGo.AddComponent<RectTransform>();
            dlgR.anchorMin = Vector2.zero; dlgR.anchorMax = new Vector2(1,0.78f);
            dlgR.sizeDelta = Vector2.zero; dlgR.offsetMin = new Vector2(24,12); dlgR.offsetMax = new Vector2(-24,-4);
            var dlgTmp = dlgGo.AddComponent<TextMeshProUGUI>();
            dlgTmp.fontSize = 22; dlgTmp.color = Color.white;
            dlgTmp.alignment = TextAlignmentOptions.TopLeft; dlgTmp.enableWordWrapping = true;

            var mgr = root.AddComponent<DialogueManager>();
            var so = new SerializedObject(mgr);
            so.FindProperty("dialogueCanvas").objectReferenceValue = canvas;
            so.FindProperty("speakerNameText").objectReferenceValue = spkTmp;
            so.FindProperty("dialogueText").objectReferenceValue = dlgTmp;
            so.FindProperty("panelBackground").objectReferenceValue = pBg;
            so.ApplyModifiedPropertiesWithoutUndo();
            // Don't deactivate the root — DialogueManager.Awake() needs to run
            // to set Instance. The Hide() call in Awake hides the panel visually.
        }

        // ── Cinematic Camera ─────────────────────────────────────

        private static void BuildCinematicCamera()
        {
            var camGo = new GameObject("CinematicCamera");
            camGo.transform.position = new Vector3(0f, 20f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.Skybox; cam.enabled = false;
            camGo.AddComponent<UniversalAdditionalCameraData>();

            Camera gameplayCam = null;
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.gameObject.CompareTag("MainCamera")) { gameplayCam = c; break; }

            var cine = camGo.AddComponent<CinematicCamera>();
            var so = new SerializedObject(cine);
            so.FindProperty("cinematicCam").objectReferenceValue = cam;
            if (gameplayCam != null)
                so.FindProperty("gameplayCam").objectReferenceValue = gameplayCam;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Helpers ──────────────────────────────────────────────

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.sizeDelta = Vector2.zero; r.anchoredPosition = Vector2.zero;
        }

        private static RectTransform LetterboxBar(string name, Transform parent,
            Vector2 aMin, Vector2 aMax, Vector2 pivot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot;
            r.sizeDelta = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = Color.black; img.raycastTarget = false;
            return r;
        }
    }
}
