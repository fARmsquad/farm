using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace FarmSimVR.Editor.Cinematics
{
    /// <summary>
    /// Builds the illustrated slideshow for the Intro cutscene.
    /// Creates a full-screen SlideshowPanel under the existing Canvas, adds one
    /// RawImage per illustration, then wires Activation Tracks (8 s each) into
    /// Intro.playable so the timeline controls visibility.
    ///
    /// Run via: FarmSimVR > Intro > Build Slideshow
    /// </summary>
    public static class IntroSlideshowBuilder
    {
        private const string kTimelinePath  = "Assets/_Project/Data/Cinematics/Intro.playable";
        private const double kSlideDuration = 8.0;

        private static readonly string[] kImagePaths =
        {
            "Assets/_Project/Art/Cutscenes/Intro/intro_01_town.jpg",
            "Assets/_Project/Art/Cutscenes/Intro/intro_02_sleeping.jpg",
            "Assets/_Project/Art/Cutscenes/Intro/intro_03_wokenup.jpg",
            "Assets/_Project/Art/Cutscenes/Intro/intro_04_getsup.png",
            "Assets/_Project/Art/Cutscenes/Intro/intro_05_spots_chicken.png",
            "Assets/_Project/Art/Cutscenes/Intro/intro_06_chases_neighborhood.png",
            "Assets/_Project/Art/Cutscenes/Intro/intro_07_chases_farm.png",
            "Assets/_Project/Art/Cutscenes/Intro/intro_08_coop.png",
        };

        [MenuItem("FarmSimVR/Intro/Build Slideshow")]
        public static void Build()
        {
            // ── 1. Find Canvas ────────────────────────────────────────────────────
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[IntroSlideshowBuilder] No Canvas found in the open scene.");
                return;
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

            var bg       = panelGO.AddComponent<Image>();
            bg.color     = Color.black;
            bg.raycastTarget = false;

            // ── 4. Create one RawImage child per illustration ─────────────────────
            var slideGOs = new GameObject[kImagePaths.Length];
            for (int i = 0; i < kImagePaths.Length; i++)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(kImagePaths[i]);
                if (texture == null)
                {
                    Debug.LogError($"[IntroSlideshowBuilder] Texture not found: {kImagePaths[i]}");
                    Object.DestroyImmediate(panelGO);
                    return;
                }

                var slideGO  = new GameObject($"Slide_{i + 1:00}");
                slideGO.transform.SetParent(panelGO.transform, false);
                var rect     = slideGO.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var rawImage         = slideGO.AddComponent<RawImage>();
                rawImage.texture     = texture;
                rawImage.color       = Color.white;
                rawImage.raycastTarget = false;

                slideGO.SetActive(false); // Activation Track controls visibility
                slideGOs[i] = slideGO;
            }

            // ── 5. Load Timeline ──────────────────────────────────────────────────
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(kTimelinePath);
            if (timeline == null)
            {
                Debug.LogError($"[IntroSlideshowBuilder] Timeline not found: {kTimelinePath}");
                Object.DestroyImmediate(panelGO);
                return;
            }

            // ── 6. Find PlayableDirector ──────────────────────────────────────────
            var director = Object.FindFirstObjectByType<PlayableDirector>();
            if (director == null)
            {
                Debug.LogError("[IntroSlideshowBuilder] No PlayableDirector found in scene.");
                Object.DestroyImmediate(panelGO);
                return;
            }

            // ── 7. Remove stale Slide_ tracks ────────────────────────────────────
            var toDelete = new System.Collections.Generic.List<TrackAsset>();
            foreach (var track in timeline.GetRootTracks())
            {
                if (track.name.StartsWith("Slide_"))
                    toDelete.Add(track);
            }
            foreach (var track in toDelete)
                timeline.DeleteTrack(track);

            // ── 8. Add Activation Tracks (one per slide) ──────────────────────────
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

            Debug.Log("[IntroSlideshowBuilder] ✓ Slideshow built — 8 slides × 8 s wired into Intro.playable.");
        }
    }
}
