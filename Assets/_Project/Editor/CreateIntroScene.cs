using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.Core.Tutorial;

namespace FarmSimVR.Editor
{
    public static class CreateIntroScene
    {
        [MenuItem("FarmSimVR/Create Intro Cutscene Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Camera ---
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 60f;
            camGO.AddComponent<AudioListener>();

            // --- Shot 1: flat rectangle at origin ---
            var rect1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rect1.name = "Shot1_Rectangle";
            rect1.transform.position = new Vector3(0f, 0f, 0f);
            rect1.transform.localScale = new Vector3(5f, 0.4f, 3f);
            ApplyColor(rect1, new Color(0.3f, 0.25f, 0.2f));

            // --- Shot 2: big rectangle at offset ---
            var rect2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rect2.name = "Shot2_BigRectangle";
            rect2.transform.position = new Vector3(40f, 1.5f, 0f);
            rect2.transform.localScale = new Vector3(7f, 4f, 5f);
            ApplyColor(rect2, new Color(0.2f, 0.22f, 0.35f));

            // --- Shot 3: circle (cylinder) at further offset ---
            var circ = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circ.name = "Shot3_Circle";
            circ.transform.position = new Vector3(80f, 0f, 0f);
            circ.transform.localScale = new Vector3(4f, 0.15f, 4f);
            ApplyColor(circ, new Color(0.15f, 0.35f, 0.15f));

            // --- Canvas ---
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Black fade overlay
            var fadeGO = new GameObject("FadeOverlay");
            fadeGO.transform.SetParent(canvasGO.transform, false);
            var fadeRect = fadeGO.AddComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.sizeDelta = Vector2.zero;
            var fadeImg = fadeGO.AddComponent<Image>();
            fadeImg.color = Color.black;
            var fadeGroup = fadeGO.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;

            // Subtitle text — centered bottom
            var txtGO = new GameObject("SubtitleText");
            txtGO.transform.SetParent(canvasGO.transform, false);
            var txtRect = txtGO.AddComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0.1f, 0.05f);
            txtRect.anchorMax = new Vector2(0.9f, 0.22f);
            txtRect.sizeDelta = Vector2.zero;
            var txt = txtGO.AddComponent<Text>();
            txt.text = "";
            txt.fontSize = 52;
            txt.fontStyle = FontStyle.Italic;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Outline effect for readability
            var outline = txtGO.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            // --- EventSystem ---
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();

            // --- CinematicCamera on Main Camera ---
            camGO.AddComponent<CinematicCamera>();

            // --- CinematicRoot ---
            var mgrGO = new GameObject("CinematicRoot");

            var screenEffects = mgrGO.AddComponent<ScreenEffects>();
            var soEffects = new SerializedObject(screenEffects);
            soEffects.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeGroup;
            soEffects.FindProperty("targetCamera").objectReferenceValue = cam;
            soEffects.ApplyModifiedPropertiesWithoutUndo();

            mgrGO.AddComponent<SkipPrompt>();
            mgrGO.AddComponent<SceneLoader>();
            mgrGO.AddComponent<CinematicSequencer>();
            var autoPlay = mgrGO.AddComponent<IntroCinematicAutoPlay>();
            var autoPlaySo = new SerializedObject(autoPlay);
            autoPlaySo.FindProperty("completionSceneName").stringValue = TutorialSceneCatalog.ChickenGameSceneName;
            autoPlaySo.FindProperty("playbackSpeed").floatValue = TutorialDevTuning.IntroCutscenePlaybackSpeed;
            autoPlaySo.ApplyModifiedPropertiesWithoutUndo();

            // --- Save ---
            string scenePath = "Assets/_Project/Scenes/Intro.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            AddSceneToBuild(scenePath);
            Debug.Log("[IntroScene] Intro.unity created and added to Build Settings.");
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        static void AddSceneToBuild(string introPath)
        {
            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            bool hasIntro = false;
            int farmMainIdx = -1;
            for (int i = 0; i < existing.Count; i++)
            {
                if (existing[i].path == introPath) hasIntro = true;
                if (existing[i].path == "Assets/_Project/Scenes/FarmMain.unity") farmMainIdx = i;
            }

            if (!hasIntro)
            {
                // Insert Intro right before FarmMain (or at index 1 after TitleScreen)
                int insertAt = farmMainIdx >= 0 ? farmMainIdx : 1;
                if (insertAt > existing.Count) insertAt = existing.Count;
                existing.Insert(insertAt, new EditorBuildSettingsScene(introPath, true));
            }

            EditorBuildSettings.scenes = existing.ToArray();
        }
    }
}
