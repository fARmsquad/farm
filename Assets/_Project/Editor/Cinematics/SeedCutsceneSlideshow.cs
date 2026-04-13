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
    /// Builds the illustrated slideshow for the SeedCutscene.
    /// Creates SlideshowPanel with RawImages (16:9), Intro-style bottom captions on
    /// TextOverlayCanvas + <see cref="SlideTextSync"/>, <see cref="SlideInFromRight"/> on
    /// each slide, and Timeline activation crossfades + narration aligned to voice lines.
    /// Each slide stays visible for the full VO clip plus a short hold (see <see cref="kPostSlidePauseSeconds"/>).
    ///
    /// Run via: FarmSimVR > SeedCutscene > Build Slideshow
    /// Or: FarmSimVR > SeedCutscene > Create Scene (new scene + build)
    /// </summary>
    public static class SeedCutsceneSlideshow
    {
        private const string kScenePath = "Assets/_Project/Scenes/SeedCutscene.unity";
        private const string kTimelinePath = "Assets/_Project/Data/Cinematics/SeedCutscene.playable";
        private const string kFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const double kSlideDuration = 5.0;
        /// <summary>Seconds of silence after each line ends before the next slide.</summary>
        private const double kPostSlidePauseSeconds = 1.0;
        private const double kMaxActivationBlend = 0.4;

        private static readonly string[] kImagePaths =
        {
            "Assets/_Project/Art/Cutscenes/SeedCutscene/SeedScene1.png",
            "Assets/_Project/Art/Cutscenes/SeedCutscene/SeedScene2.png",
            "Assets/_Project/Art/Cutscenes/SeedCutscene/SeedScene3.png",
            "Assets/_Project/Art/Cutscenes/SeedCutscene/SeedScene4.png",
            "Assets/_Project/Art/Cutscenes/SeedCutscene/SeedScene5.png",
        };

        /// <summary>Voice lines aligned with slides 1–5 (tools in hand → harvest).</summary>
        private static readonly string[] kAudioPaths =
        {
            "Assets/_Project/Art/Cutscenes/SeedCutscene/tools-in-hand.mp3",
            "Assets/_Project/Art/Cutscenes/SeedCutscene/wait-here.mp3",
            "Assets/_Project/Art/Cutscenes/SeedCutscene/chicken-emerges.mp3",
            "Assets/_Project/Art/Cutscenes/SeedCutscene/whatcha-got-there.mp3",
            "Assets/_Project/Art/Cutscenes/SeedCutscene/harvest-here-we-come.mp3",
        };

        /// <summary>Caption copy for each slide (Intro-style overlay; edit in scene as needed).</summary>
        private static readonly string[] kCaptions =
        {
            "Tools in hand — time to get to work.",
            "Wait here. I've got this.",
            "The chicken emerges…",
            "Whatcha got there?",
            "Harvest — here we come!",
        };

        [MenuItem("FarmSimVR/SeedCutscene/Create Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, kScenePath);
            Build();
            Debug.Log($"[SeedCutsceneSlideshow] ✓ Scene saved to {kScenePath}");
        }

        /// <summary>
        /// Unity CLI: <c>-executeMethod FarmSimVR.Editor.Cinematics.SeedCutsceneSlideshow.BatchCreateScene</c>
        /// </summary>
        public static void BatchCreateScene()
        {
            CreateScene();
        }

        /// <summary>
        /// Opens <see cref="kScenePath"/> and runs <see cref="Build"/> (for CLI batch import).
        /// </summary>
        public static void BatchRebuildSlideshowInScene()
        {
            if (!System.IO.File.Exists(kScenePath))
            {
                Debug.LogError($"[SeedCutsceneSlideshow] Scene not found: {kScenePath}");
                return;
            }

            EditorSceneManager.OpenScene(kScenePath);
            Build();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("FarmSimVR/SeedCutscene/Build Slideshow")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(kFontAssetPath);
            if (font == null)
                Debug.LogWarning($"[SeedCutsceneSlideshow] TMP font not found at {kFontAssetPath}");

            // ── 1. Find or create Canvas ──────────────────────────────────────────
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
                Debug.Log("[SeedCutsceneSlideshow] Canvas created.");
            }

            // ── 2. Remove stale panel + text overlay ──────────────────────────────
            var existing = canvas.transform.Find("SlideshowPanel");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            DestroyRootNamed(canvas.gameObject.scene, "TextOverlayCanvas");

            // ── 3. Create SlideshowPanel (black background, covers 3D content) ───
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

            // ── 4. Create one RawImage child per illustration ─────────────────────
            var slideGOs = new GameObject[kImagePaths.Length];
            for (int i = 0; i < kImagePaths.Length; i++)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(kImagePaths[i]);
                if (texture == null)
                {
                    Debug.LogError($"[SeedCutsceneSlideshow] Texture not found: {kImagePaths[i]}");
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

                slideGO.AddComponent<SlideInFromRight>();

                slideGO.SetActive(false); // Activation Track controls visibility
                slideGOs[i] = slideGO;
            }

            // ── 5. Intro-style caption overlay (TextOverlayCanvas + SlideTextSync) ─
            BuildTextOverlayRoot(canvas.gameObject.scene, slideGOs, font);

            // ── 6. Load or create Timeline ────────────────────────────────────────
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(kTimelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.editorSettings.frameRate = 30;
                timeline.durationMode = TimelineAsset.DurationMode.BasedOnClips;
                AssetDatabase.CreateAsset(timeline, kTimelinePath);
                AssetDatabase.SaveAssets();
            }

            // ── 7. Find or create PlayableDirector ────────────────────────────────
            var director = Object.FindFirstObjectByType<PlayableDirector>();
            if (director == null)
            {
                var directorGO = new GameObject("SlideshowDirector");
                director = directorGO.AddComponent<PlayableDirector>();
                Debug.Log("[SeedCutsceneSlideshow] PlayableDirector created on 'SlideshowDirector'.");
            }

            director.playableAsset = timeline;
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.None;

            // ── 8. Per-slide timing from narration clip lengths ───────────────────
            ComputeSlideSchedule(out var starts, out var durations);

            // ── 9. Remove stale Slide_ + Narration tracks ─────────────────────────
            var toDelete = new System.Collections.Generic.List<TrackAsset>();
            foreach (var track in timeline.GetRootTracks())
            {
                if (track.name.StartsWith("Slide_") || track.name == "Narration")
                    toDelete.Add(track);
            }

            foreach (var track in toDelete)
                timeline.DeleteTrack(track);

            // ── 10. Activation Tracks (crossfade between slides) ──────────────────
            for (int i = 0; i < slideGOs.Length; i++)
            {
                var track = timeline.CreateTrack<ActivationTrack>(null, $"Slide_{i + 1:00}");
                var clip = track.CreateDefaultClip();
                clip.start = starts[i];
                clip.duration = durations[i];
                var blend = System.Math.Min(kMaxActivationBlend, durations[i] * 0.12);
                if (blend > 0.02)
                {
                    clip.blendInDuration = blend;
                    clip.blendOutDuration = blend;
                }

                director.SetGenericBinding(track, slideGOs[i]);
            }

            // ── 11. Narration AudioTrack ──────────────────────────────────────────
            var audioSource = director.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = director.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            var narrationTrack = timeline.CreateTrack<AudioTrack>(null, "Narration");
            for (int i = 0; i < kAudioPaths.Length; i++)
            {
                var ac = AssetDatabase.LoadAssetAtPath<AudioClip>(kAudioPaths[i]);
                if (ac == null)
                {
                    Debug.LogError($"[SeedCutsceneSlideshow] AudioClip not found: {kAudioPaths[i]}");
                    continue;
                }

                var timelineAudioClip = narrationTrack.CreateClip(ac);
                if (timelineAudioClip == null)
                    continue;
                timelineAudioClip.start = starts[i];
                timelineAudioClip.duration = ac.length;
            }

            director.SetGenericBinding(narrationTrack, audioSource);

            // ── 12. Save ──────────────────────────────────────────────────────────
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(panelGO);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            AssetDatabase.Refresh();

            Debug.Log("[SeedCutsceneSlideshow] ✓ Slideshow built — captions, slide-in, activation crossfades, narration.");
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
                var ac = AssetDatabase.LoadAssetAtPath<AudioClip>(kAudioPaths[i]);
                double audioLen = ac != null && ac.length > 0.05 ? ac.length : kSlideDuration;
                durations[i] = audioLen + kPostSlidePauseSeconds;
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
