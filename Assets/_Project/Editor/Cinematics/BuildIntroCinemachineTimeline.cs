using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FarmSimVR.Editor.Cinematics
{
    /// <summary>
    /// One-click builder for the Intro cutscene Timeline asset.
    /// Opens the Intro scene, finds the VCam GameObjects created by Bezi Actions,
    /// creates Assets/_Project/Data/Cinematics/Intro_Timeline.playable, populates a
    /// CinemachineTrack with dolly-blend shot clips, then wires it to the
    /// PlayableDirector on CinematicRoot.
    ///
    /// Run via: FarmSimVR > Intro > Build Cinemachine Timeline
    /// </summary>
    public static class BuildIntroCinemachineTimeline
    {
        // ── Timing constants (seconds) — match Intro_Sequence.asset Wait steps ──────
        // Shot 1: camera is active from t=1.2 (after fade-in) for 5s
        private const double kShot1Start  = 1.2;
        private const double kShot1Dur    = 5.0;
        private const double kBlend1Dur   = kShot1Dur;          // full-length blend = dolly

        // Shot 2: orbit active from t=8.8 for 6.5s
        private const double kShot2Start  = 8.8;
        private const double kShot2Dur    = 6.5;
        private const double kBlend2Dur   = kShot2Dur;

        // Shot 3: active from t=21.8 for 5.5s
        private const double kShot3Start  = 21.8;
        private const double kShot3Dur    = 5.5;
        private const double kBlend3Dur   = kShot3Dur;

        // Total timeline duration (matches sequence fade-to-black at end)
        private const double kTotalDur    = 32.6;

        // ── VCam GameObject names — must match what Bezi Actions created ─────────
        private const string kVCam1Start  = "Intro_Shot1_Start_VCam";
        private const string kVCam1End    = "Intro_Shot1_End_VCam";
        private const string kVCam2Start  = "Intro_Shot2_OrbitA_VCam";
        private const string kVCam2End    = "Intro_Shot2_OrbitB_VCam";
        private const string kVCam3Start  = "Intro_Shot3_Start_VCam";
        private const string kVCam3End    = "Intro_Shot3_End_VCam";

        private const string kTimelinePath = "Assets/_Project/Data/Cinematics/Intro_Timeline.playable";
        private const string kDirectorPath = "/CinematicRoot";

        [MenuItem("FarmSimVR/Intro/Build Cinemachine Timeline")]
        public static void Build()
        {
            // ── 1. Find VCam GameObjects ──────────────────────────────────────────
            var vcam1Start = FindRequired(kVCam1Start);
            var vcam1End   = FindRequired(kVCam1End);
            var vcam2Start = FindRequired(kVCam2Start);
            var vcam2End   = FindRequired(kVCam2End);
            var vcam3Start = FindRequired(kVCam3Start);
            var vcam3End   = FindRequired(kVCam3End);

            if (vcam1Start == null || vcam1End   == null ||
                vcam2Start == null || vcam2End   == null ||
                vcam3Start == null || vcam3End   == null)
            {
                Debug.LogError("[BuildIntroCinemachineTimeline] One or more VCam GameObjects not found in the scene. " +
                               "Make sure the Intro scene is open and all VCam GameObjects exist.");
                return;
            }

            // ── 2. Create / overwrite Timeline asset ─────────────────────────────
            Directory.CreateDirectory(Path.GetDirectoryName(kTimelinePath)!);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.editorSettings.frameRate = 30;
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = kTotalDur;

            AssetDatabase.DeleteAsset(kTimelinePath);
            AssetDatabase.CreateAsset(timeline, kTimelinePath);

            // ── 3. CinemachineTrack ───────────────────────────────────────────────
            var cmTrack = timeline.CreateTrack<CinemachineTrack>(null, "Camera Shots");

            // Collect (exposedName, vcam) pairs — we bind them to the Director after it's found
            var pendingBindings = new System.Collections.Generic.List<(PropertyName, CinemachineVirtualCameraBase)>();

            // Helper: add a CinemachineShot clip; stash exposed binding for later
            TimelineClip AddShot(CinemachineVirtualCameraBase vcam, double start, double dur, double blendIn = 0)
            {
                var clip      = cmTrack.CreateClip<CinemachineShot>();
                clip.start    = start;
                clip.duration = dur;
                if (blendIn > 0)
                    clip.blendInDuration = blendIn;

                var shot          = (CinemachineShot)clip.asset;
                var exposedName   = new PropertyName(GUID.Generate().ToString());
                shot.VirtualCamera.exposedName = exposedName;

                pendingBindings.Add((exposedName, vcam));
                clip.displayName = vcam.name;
                return clip;
            }

            // Shot 1 — dolly via full-length blend from Start → End VCam
            AddShot(vcam1Start.GetComponent<CinemachineVirtualCameraBase>(), kShot1Start, 0.05);
            AddShot(vcam1End.GetComponent<CinemachineVirtualCameraBase>(),   kShot1Start + 0.05, kShot1Dur - 0.05, kBlend1Dur - 0.05);

            // Shot 2 — orbit sweep via full-length blend from OrbitA → OrbitB VCam
            AddShot(vcam2Start.GetComponent<CinemachineVirtualCameraBase>(), kShot2Start, 0.05);
            AddShot(vcam2End.GetComponent<CinemachineVirtualCameraBase>(),   kShot2Start + 0.05, kShot2Dur - 0.05, kBlend2Dur - 0.05);

            // Shot 3 — dolly via full-length blend from Start → End VCam
            AddShot(vcam3Start.GetComponent<CinemachineVirtualCameraBase>(), kShot3Start, 0.05);
            AddShot(vcam3End.GetComponent<CinemachineVirtualCameraBase>(),   kShot3Start + 0.05, kShot3Dur - 0.05, kBlend3Dur - 0.05);

            AssetDatabase.SaveAssets();

            // ── 4. Wire PlayableDirector on CinematicRoot ─────────────────────────
            var directorGO = GameObject.Find("CinematicRoot");
            if (directorGO == null)
            {
                Debug.LogError("[BuildIntroCinemachineTimeline] Could not find 'CinematicRoot' in the scene.");
                return;
            }

            var director = directorGO.GetComponent<PlayableDirector>();
            if (director == null)
                director = directorGO.AddComponent<PlayableDirector>();

            director.playableAsset   = timeline;
            director.playOnAwake     = false;
            director.timeUpdateMode  = DirectorUpdateMode.UnscaledGameTime;
            director.extrapolationMode = DirectorWrapMode.None;

            // Resolve ExposedReferences onto the Director (it is the IExposedPropertyTable)
            foreach (var (name, vcam) in pendingBindings)
                director.SetReferenceValue(name, vcam);

            // Bind CinemachineBrain as the track's output
            director.SetGenericBinding(cmTrack, FindOrAddBrain());

            EditorUtility.SetDirty(directorGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                directorGO.scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BuildIntroCinemachineTimeline] ✓ Intro_Timeline.playable built and wired. " +
                      "Open Window > Sequencing > Timeline to preview.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static GameObject FindRequired(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
                Debug.LogWarning($"[BuildIntroCinemachineTimeline] Could not find GameObject '{name}' in the open scene.");
            return go;
        }

        private static CinemachineBrain FindOrAddBrain()
        {
            var brain = Object.FindFirstObjectByType<CinemachineBrain>();
            if (brain != null)
                return brain;

            var camGO = GameObject.Find("Main Camera");
            if (camGO != null)
                return camGO.AddComponent<CinemachineBrain>();

            Debug.LogWarning("[BuildIntroCinemachineTimeline] CinemachineBrain not found and no 'Main Camera' to add it to.");
            return null;
        }
    }
}
