using FarmSimVR.MonoBehaviours.Cinematics;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace FarmSimVR.Editor.Cinematics
{
    /// <summary>
    /// Builds the illustrated slideshow for CaughtChickenCutscene (images 1–8, stationary slides,
    /// short Timeline activation blends, Bangers captions on TextOverlayCanvas).
    /// No bundled VO — each slide uses a fixed hold; add narration later via Timeline if needed.
    ///
    /// Run: FarmSimVR &gt; CaughtChickenCutscene &gt; Build Slideshow
    /// Or: FarmSimVR &gt; CaughtChickenCutscene &gt; Create Scene
    /// </summary>
    public static class CaughtChickenCutsceneSlideshow
    {
        private const string kScenePath = "Assets/_Project/Scenes/CaughtChickenCutscene.unity";
        private const string kTimelinePath = "Assets/_Project/Data/Cinematics/CaughtChickenCutscene.playable";
        private const string kFontAssetPath =
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Bangers SDF.asset";
        private const double kSlideDuration = 5.0;
        private const double kPostSlidePauseSeconds = 1.0;
        /// <summary>Seconds of activation crossfade between slides (no slide-in motion).</summary>
        private const double kActivationBlendSeconds = 0.18;

        private static readonly string[] kImagePaths =
        {
            "Assets/_Project/Art/Cutscenes/CaughtChickenCutscene/1.png",
            "Assets/_Project/Art/Cutscenes/CaughtChickenCutscene/2.png",
            "Assets/_Project/Art/Cutscenes/CaughtChickenCutscene/3.png",
            "Assets/_Project/Art/Cutscenes/CaughtChickenCutscene/4.png",
            "Assets/_Project/Art/Cutscenes/CaughtChickenCutscene/5.png",
            "Assets/_Project/Art/Cutscenes/CaughtChickenCutscene/6.png",
            "Assets/_Project/Art/Cutscenes/CaughtChickenCutscene/7.png",
            "Assets/_Project/Art/Cutscenes/CaughtChickenCutscene/8.png",
        };

        /// <summary>Placeholder captions for OverlayText_01–08 (replace with final copy).</summary>
        private static readonly string[] kCaptions =
        {
            "PLACEHOLDER 01 — Caught red-handed!",
            "PLACEHOLDER 02 — The chicken blinks.",
            "PLACEHOLDER 03 — Nobody moves.",
            "PLACEHOLDER 04 — Feathers settle…",
            "PLACEHOLDER 05 — This is fine. (It is not fine.)",
            "PLACEHOLDER 06 — Stare-down at high noon.",
            "PLACEHOLDER 07 — Something's about to pop off.",
            "PLACEHOLDER 08 — To be continued…",
        };

        [MenuItem("FarmSimVR/CaughtChickenCutscene/Create Scene")]
        public static void CreateScene()
        {
            if (System.IO.File.Exists(kScenePath))
            {
                Debug.LogWarning($"[CaughtChickenCutsceneSlideshow] Scene already exists at {kScenePath} — opening and running Build.");
                BatchRebuildSlideshowInScene();
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, kScenePath);
            Build();
            Debug.Log($"[CaughtChickenCutsceneSlideshow] Scene saved to {kScenePath}");
        }

        /// <summary>Unity CLI: <c>-executeMethod FarmSimVR.Editor.Cinematics.CaughtChickenCutsceneSlideshow.BatchCreateScene</c></summary>
        public static void BatchCreateScene()
        {
            CreateScene();
        }

        /// <summary>Opens <see cref="kScenePath"/> and runs <see cref="Build"/> (batch / CI).</summary>
        public static void BatchRebuildSlideshowInScene()
        {
            if (!System.IO.File.Exists(kScenePath))
            {
                Debug.LogError($"[CaughtChickenCutsceneSlideshow] Scene not found: {kScenePath}");
                return;
            }

            EditorSceneManager.OpenScene(kScenePath);
            Build();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("FarmSimVR/CaughtChickenCutscene/Build Slideshow")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(kFontAssetPath);
            if (font == null)
                Debug.LogWarning($"[CaughtChickenCutsceneSlideshow] TMP font not found at {kFontAssetPath}");

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;

                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasGO.AddComponent<GraphicRaycaster>();
                Debug.Log("[CaughtChickenCutsceneSlideshow] Canvas created.");
            }

            var existing = canvas.transform.Find("SlideshowPanel");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            DestroyRootNamed(canvas.gameObject.scene, "TextOverlayCanvas");

            var panelGO = new GameObject("SlideshowPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var bg = panelGO.AddComponent<Image>();
            bg.color = Color.black;
            bg.raycastTarget = false;

            var slideGOs = new GameObject[kImagePaths.Length];
            for (int i = 0; i < kImagePaths.Length; i++)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(kImagePaths[i]);
                if (texture == null)
                {
                    Debug.LogError($"[CaughtChickenCutsceneSlideshow] Texture not found: {kImagePaths[i]}");
                    Object.DestroyImmediate(panelGO);
                    return;
                }

                var slideGO = new GameObject($"Slide_{i + 1:00}");
                slideGO.transform.SetParent(panelGO.transform, false);
                var rect = slideGO.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var aspect = slideGO.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspect.aspectRatio = 16f / 9f;

                var rawImage = slideGO.AddComponent<RawImage>();
                rawImage.texture = texture;
                rawImage.color = Color.white;
                rawImage.raycastTarget = false;

                slideGO.SetActive(false);
                slideGOs[i] = slideGO;
            }

            BuildTextOverlayRoot(canvas.gameObject.scene, slideGOs, font);

            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(kTimelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.editorSettings.frameRate = 30;
                timeline.durationMode = TimelineAsset.DurationMode.BasedOnClips;
                AssetDatabase.CreateAsset(timeline, kTimelinePath);
                AssetDatabase.SaveAssets();
            }

            var director = Object.FindFirstObjectByType<PlayableDirector>();
            if (director == null)
            {
                var directorGO = new GameObject("SlideshowDirector");
                director = directorGO.AddComponent<PlayableDirector>();
                Debug.Log("[CaughtChickenCutsceneSlideshow] PlayableDirector created on 'SlideshowDirector'.");
            }

            director.playableAsset = timeline;
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.None;

            ComputeSlideSchedule(out var starts, out var durations);

            var toDelete = new System.Collections.Generic.List<TrackAsset>();
            foreach (var track in timeline.GetRootTracks())
            {
                if (track.name.StartsWith("Slide_") || track.name == "Narration")
                    toDelete.Add(track);
            }

            foreach (var track in toDelete)
                timeline.DeleteTrack(track);

            for (int i = 0; i < slideGOs.Length; i++)
            {
                var track = timeline.CreateTrack<ActivationTrack>(null, $"Slide_{i + 1:00}");
                var clip = track.CreateDefaultClip();
                clip.start = starts[i];
                clip.duration = durations[i];
                var blend = System.Math.Min(kActivationBlendSeconds, durations[i] * 0.15);
                if (blend > 0.02)
                {
                    clip.blendInDuration = blend;
                    clip.blendOutDuration = blend;
                }

                director.SetGenericBinding(track, slideGOs[i]);
            }

            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(panelGO);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            AssetDatabase.Refresh();

            Debug.Log("[CaughtChickenCutsceneSlideshow] Slideshow built — 8 slides, quick activation blends, no Narration track.");
        }

        private static void ComputeSlideSchedule(out double[] starts, out double[] durations)
        {
            int n = kImagePaths.Length;
            starts = new double[n];
            durations = new double[n];
            double t = 0;
            for (int i = 0; i < n; i++)
            {
                starts[i] = t;
                durations[i] = kSlideDuration + kPostSlidePauseSeconds;
                t += durations[i];
            }
        }

        private static void DestroyRootNamed(UnityEngine.SceneManagement.Scene scene, string objectName)
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    Object.DestroyImmediate(roots[i]);
            }
        }

        private static void BuildTextOverlayRoot(UnityEngine.SceneManagement.Scene scene, GameObject[] slideGOs, TMP_FontAsset font)
        {
            var overlayGO = new GameObject("TextOverlayCanvas");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlayGO, scene);

            var overlayCanvas = overlayGO.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 20;
            overlayCanvas.additionalShaderChannels = (AdditionalCanvasShaderChannels)25;

            var scaler = overlayGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            overlayGO.AddComponent<GraphicRaycaster>();

            var textGOs = new GameObject[slideGOs.Length];
            for (int i = 0; i < slideGOs.Length; i++)
            {
                var tgo = new GameObject($"OverlayText_{i + 1:00}");
                tgo.transform.SetParent(overlayGO.transform, false);
                var rt = tgo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(0f, 80f);
                rt.sizeDelta = new Vector2(-160f, 140f);
                rt.pivot = new Vector2(0.5f, 0f);

                var tmp = tgo.AddComponent<TextMeshProUGUI>();
                if (font != null)
                    tmp.font = font;
                tmp.text = i < kCaptions.Length ? kCaptions[i] : string.Empty;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 36;
                tmp.fontSizeMax = 64;
                tmp.fontSize = 64;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Bottom;
                tmp.textWrappingMode = TextWrappingModes.Normal;

                tgo.SetActive(false);
                textGOs[i] = tgo;
            }

            var sync = overlayGO.AddComponent<SlideTextSync>();
            var so = new SerializedObject(sync);
            var pairs = so.FindProperty("pairs");
            pairs.arraySize = slideGOs.Length;
            for (int i = 0; i < slideGOs.Length; i++)
            {
                var el = pairs.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("slide").objectReferenceValue = slideGOs[i];
                el.FindPropertyRelative("text").objectReferenceValue = textGOs[i];
                el.FindPropertyRelative("hideWhenThisFadesIn").objectReferenceValue = null;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(overlayGO);
        }
    }
}
