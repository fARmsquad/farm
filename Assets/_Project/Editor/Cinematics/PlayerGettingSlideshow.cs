using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace FarmSimVR.Editor.Cinematics
{
    /// <summary>
    /// Builds the illustrated slideshow for the PlayerGettingSeeds cutscene.
    /// Creates a full-screen SlideshowPanel under the existing Canvas, adds one
    /// RawImage per illustration (16:9 via AspectRatioFitter; RawImage has no preserveAspect),
    /// then wires Activation Tracks (6 s each) into
    /// PlayerGettingSeeds.playable so the timeline controls visibility.
    ///
    /// Run via: FarmSimVR > PlayerGettingSeeds > Build Slideshow
    /// </summary>
    public static class PlayerGettingSlideshow
    {
        private const string kTimelinePath  = "Assets/_Project/Data/Cinematics/PlayerGettingSeeds.playable";
        private const double kSlideDuration = 6.0;

        private static readonly string[] kImagePaths =
        {
            "Assets/_Project/Art/Cutscenes/PlayerGettingSeeds/SeedScene1.png",
            "Assets/_Project/Art/Cutscenes/PlayerGettingSeeds/SeedScene2.png",
            "Assets/_Project/Art/Cutscenes/PlayerGettingSeeds/SeedScene3.png",
            "Assets/_Project/Art/Cutscenes/PlayerGettingSeeds/SeedScene4.png",
            "Assets/_Project/Art/Cutscenes/PlayerGettingSeeds/SeedScene5.png",
        };

        [MenuItem("FarmSimVR/PlayerGettingSeeds/Build Slideshow")]
        public static void Build()
        {
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
                Debug.Log("[PlayerGettingSlideshow] Canvas created.");
            }

            // ── 2. Remove stale panel ─────────────────────────────────────────────
            var existing = canvas.transform.Find("SlideshowPanel");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            // ── 3. Create SlideshowPanel (black background, covers 3D content) ───
            var panelGO   = new GameObject("SlideshowPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var bg           = panelGO.AddComponent<Image>();
            bg.color         = Color.black;
            bg.raycastTarget = false;

            // ── 4. Create one RawImage child per illustration ─────────────────────
            var slideGOs = new GameObject[kImagePaths.Length];
            for (int i = 0; i < kImagePaths.Length; i++)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(kImagePaths[i]);
                if (texture == null)
                {
                    Debug.LogError($"[PlayerGettingSlideshow] Texture not found: {kImagePaths[i]}");
                    Object.DestroyImmediate(panelGO);
                    return;
                }

                var slideGO = new GameObject($"Slide_{i + 1:00}");
                slideGO.transform.SetParent(panelGO.transform, false);
                var rect       = slideGO.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var aspect = slideGO.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspect.aspectRatio = 16f / 9f;

                var rawImage         = slideGO.AddComponent<RawImage>();
                rawImage.texture     = texture;
                rawImage.color       = Color.white;
                rawImage.raycastTarget = false;

                slideGO.SetActive(false); // Activation Track controls visibility
                slideGOs[i] = slideGO;
            }

            // ── 5. Load or create Timeline ────────────────────────────────────────
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(kTimelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.editorSettings.frameRate = 30;
                timeline.durationMode  = TimelineAsset.DurationMode.BasedOnClips;
                AssetDatabase.CreateAsset(timeline, kTimelinePath);
                AssetDatabase.SaveAssets();
            }

            // ── 6. Find or create PlayableDirector ────────────────────────────────
            var director = Object.FindFirstObjectByType<PlayableDirector>();
            if (director == null)
            {
                var directorGO = new GameObject("SlideshowDirector");
                director = directorGO.AddComponent<PlayableDirector>();
                Debug.Log("[PlayerGettingSlideshow] PlayableDirector created on 'SlideshowDirector'.");
            }

            director.playableAsset  = timeline;
            director.playOnAwake    = true;
            director.extrapolationMode = DirectorWrapMode.None;

            // ── 7. Remove stale Slide_ tracks ────────────────────────────────────
            var toDelete = new System.Collections.Generic.List<TrackAsset>();
            foreach (var track in timeline.GetRootTracks())
            {
                if (track.name.StartsWith("Slide_"))
                    toDelete.Add(track);
            }
            foreach (var track in toDelete)
                timeline.DeleteTrack(track);

            // ── 8. Add Activation Tracks (one per slide, sequential) ──────────────
            for (int i = 0; i < slideGOs.Length; i++)
            {
                var track = timeline.CreateTrack<ActivationTrack>(null, $"Slide_{i + 1:00}");
                var clip  = track.CreateDefaultClip();
                clip.start    = i * kSlideDuration;
                clip.duration = kSlideDuration;
                director.SetGenericBinding(track, slideGOs[i]);
            }

            // ── 9. Save ───────────────────────────────────────────────────────────
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(panelGO);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            AssetDatabase.Refresh();

            Debug.Log("[PlayerGettingSlideshow] ✓ Slideshow built — 5 slides × 6 s wired into PlayerGettingSeeds.playable.");
        }
    }
}
