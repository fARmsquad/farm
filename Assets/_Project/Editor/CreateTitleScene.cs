using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using FarmSimVR.Core.Tutorial;
using System.Collections.Generic;

namespace FarmSimVR.Editor
{
    public static class CreateTitleScene
    {
        [MenuItem("FarmSimVR/Create Title Screen Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.08f, 0.06f);
            cam.orthographic = true;

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Background — full screen
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Textures/fARm.png");
            var bgGO = CreateImage("Background", canvasGO.transform, bgSprite);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            bgGO.GetComponent<Image>().preserveAspect = false;

            // Character (goggles) — bottom-left
            var charSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Textures/fARm-goggles.png");
            var charGO = CreateImage("CharacterGoggles", canvasGO.transform, charSprite);
            var charRect = charGO.GetComponent<RectTransform>();
            charRect.anchorMin = new Vector2(0f, 0f);
            charRect.anchorMax = new Vector2(0f, 0f);
            charRect.pivot = new Vector2(0f, 0f);
            charRect.sizeDelta = new Vector2(320, 430);
            charRect.anchoredPosition = new Vector2(40, 40);
            charGO.GetComponent<Image>().preserveAspect = true;

            // Start Game button — bottom center
            var btnGO = new GameObject("StartGameButton");
            btnGO.transform.SetParent(canvasGO.transform, false);
            var btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.sizeDelta = new Vector2(360, 80);
            btnRect.anchoredPosition = new Vector2(0, 80);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.13f, 0.55f, 0.13f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var colors = btn.colors;
            colors.normalColor    = new Color(0.13f, 0.55f, 0.13f);
            colors.highlightedColor = new Color(0.18f, 0.72f, 0.18f);
            colors.pressedColor   = new Color(0.09f, 0.38f, 0.09f);
            btn.colors = colors;

            // Button label (legacy Text — no extra assembly needed)
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            var lblRect = lblGO.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.sizeDelta = Vector2.zero;
            lblRect.anchoredPosition = Vector2.zero;
            var txt = lblGO.AddComponent<Text>();
            txt.text = "START GAME";
            txt.fontSize = 36;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // TitleScreenManager + music
            var mgrGO = new GameObject("TitleScreenManager");
            var mgr = mgrGO.AddComponent<FarmSimVR.MonoBehaviours.TitleScreenManager>();

            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Sounds/title.mp3");
            var audioSource = mgrGO.AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 1f;

            var mgrSO = new SerializedObject(mgr);
            mgrSO.FindProperty("targetSceneName").stringValue = SceneWorkCatalog.FirstTutorialSceneName;
            mgrSO.FindProperty("musicSource").objectReferenceValue = audioSource;
            mgrSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire button -> StartGame
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                btn.onClick,
                mgr.StartGame);

            // --- START MY STORY SIBLING BUTTON ---
            // Shift the existing StartGameButton to the LEFT of bottom-center so the two
            // buttons live side by side at matching offsets.
            btnRect.anchoredPosition = new Vector2(-200f, 80f);

            // Create StartMyStoryButton as a sibling to the RIGHT of bottom-center.
            var smsGO = new GameObject("StartMyStoryButton");
            smsGO.transform.SetParent(canvasGO.transform, false);
            var smsRect = smsGO.AddComponent<RectTransform>();
            smsRect.anchorMin = new Vector2(0.5f, 0f);
            smsRect.anchorMax = new Vector2(0.5f, 0f);
            smsRect.pivot = new Vector2(0.5f, 0.5f);
            smsRect.sizeDelta = new Vector2(360f, 80f);
            smsRect.anchoredPosition = new Vector2(200f, 80f);

            var smsImg = smsGO.AddComponent<Image>();
            smsImg.color = new Color(0.13f, 0.55f, 0.13f);
            var smsBtn = smsGO.AddComponent<Button>();
            smsBtn.targetGraphic = smsImg;
            var smsColors = smsBtn.colors;
            smsColors.normalColor = new Color(0.13f, 0.55f, 0.13f);
            smsColors.highlightedColor = new Color(0.18f, 0.72f, 0.18f);
            smsColors.pressedColor = new Color(0.09f, 0.38f, 0.09f);
            smsBtn.colors = smsColors;

            // Label child
            var smsLblGO = new GameObject("Label");
            smsLblGO.transform.SetParent(smsGO.transform, false);
            var smsLblRect = smsLblGO.AddComponent<RectTransform>();
            smsLblRect.anchorMin = Vector2.zero;
            smsLblRect.anchorMax = Vector2.one;
            smsLblRect.sizeDelta = Vector2.zero;
            smsLblRect.anchoredPosition = Vector2.zero;
            var smsTxt = smsLblGO.AddComponent<Text>();
            smsTxt.text = "START MY STORY";
            smsTxt.fontSize = 36;
            smsTxt.fontStyle = FontStyle.Bold;
            smsTxt.color = Color.white;
            smsTxt.alignment = TextAnchor.MiddleCenter;
            smsTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Wire OnClick to TitleScreenManager.StartGameStorySlice
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                smsBtn.onClick,
                mgr.StartGameStorySlice);

            // EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();

            // Save
            string scenePath = SceneWorkCatalog.TitleScreenScenePath;
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            SyncBuildSettings();
            Debug.Log("[TitleScene] Done! TitleScreen.unity created.");
        }

        [MenuItem("FarmSimVR/Sync Title Screen Build Settings")]
        public static void SyncBuildSettings()
        {
            CreateGenerativePlaythroughMenuScene.CreateIfMissing();
            AddScenesToBuild(GetOrderedBuildScenePaths());
        }

        public static IReadOnlyList<string> GetOrderedBuildScenePaths()
        {
            return SceneWorkCatalog.TitleScreenBuildScenePaths;
        }

        static GameObject CreateImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            if (sprite != null)
                img.sprite = sprite;
            else
                Debug.LogWarning($"[TitleScene] Sprite not found for {name} — check texture import settings.");
            return go;
        }

        static void AddScenesToBuild(IReadOnlyList<string> scenePaths)
        {
            var existingScenes = EditorBuildSettings.scenes;
            var existingByPath = new Dictionary<string, EditorBuildSettingsScene>();
            foreach (var scene in existingScenes)
                existingByPath[scene.path] = scene;

            var ordered = new List<EditorBuildSettingsScene>(scenePaths.Count + existingByPath.Count);
            for (int i = 0; i < scenePaths.Count; i++)
            {
                string path = scenePaths[i];
                if (existingByPath.TryGetValue(path, out var existing))
                {
                    ordered.Add(new EditorBuildSettingsScene(existing.path, true));
                    existingByPath.Remove(path);
                    continue;
                }

                ordered.Add(new EditorBuildSettingsScene(path, true));
            }

            var configuredPaths = new HashSet<string>(scenePaths);
            foreach (var remaining in existingScenes)
            {
                if (configuredPaths.Contains(remaining.path))
                    continue;

                ordered.Add(remaining);
            }

            EditorBuildSettings.scenes = ordered.ToArray();
            Debug.Log("[TitleScene] Build Settings updated.");
        }
    }
}
