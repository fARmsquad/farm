using System.Collections;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Starts the intro cutscene on Start by playing the PlayableDirector,
    /// which drives all cinematic events (camera, fades, subtitles, audio)
    /// through custom Timeline tracks.
    /// </summary>
    public class IntroCinematicAutoPlay : MonoBehaviour
    {
        [Header("Legacy (no longer used — kept for migration reference)")]
        [SerializeField] private CinematicSequence sequence;
        [SerializeField] private string completionSceneName = TutorialSceneCatalog.ChickenGameSceneName;
        [SerializeField] private float playbackSpeed = TutorialDevTuning.IntroCutscenePlaybackSpeed;

        private PlayableDirector _director;
        private SceneLoader _sceneLoader;
        private bool _completionHandled;

        private IEnumerator Start()
        {
            yield return null;

            _director = GetComponent<PlayableDirector>();
            _sceneLoader = GetComponent<SceneLoader>();

            if (_director == null)
            {
                Debug.LogError("[IntroCinematicAutoPlay] No PlayableDirector found — cinematic will not play.");
                yield break;
            }

            if (_director.playableAsset == null)
            {
                Debug.LogError("[IntroCinematicAutoPlay] PlayableDirector has no TimelineAsset assigned.");
                yield break;
            }

            if (_sceneLoader == null)
                _sceneLoader = gameObject.AddComponent<SceneLoader>();

            _director.stopped += HandleDirectorStopped;

            _director.Play();
            ApplyPlaybackSpeed();
            GetComponent<SkipPrompt>()?.Activate();
        }

        private void OnDestroy()
        {
            if (_director != null)
                _director.stopped -= HandleDirectorStopped;
        }

        /// <summary>Skips the timeline and loads the same next scene as natural completion (UnityEvent / skip button).</summary>
        public void LoadNextScene()
        {
            if (_sceneLoader == null || _director == null)
                return;

            CompleteAndLoadNextScene(requestStopIfPlaying: true);
        }

        private void HandleDirectorStopped(PlayableDirector director)
        {
            CompleteAndLoadNextScene(requestStopIfPlaying: false);
        }

        private void CompleteAndLoadNextScene(bool requestStopIfPlaying)
        {
            if (_completionHandled || _sceneLoader == null)
                return;

            _completionHandled = true;

            if (_director != null)
            {
                _director.stopped -= HandleDirectorStopped;
                if (requestStopIfPlaying && _director.state == PlayState.Playing)
                    _director.Stop();
            }

            var nextScene = StoryPackageRuntimeCatalog.GetNextSceneOrNull(SceneManager.GetActiveScene().name);
            _sceneLoader.LoadScene(string.IsNullOrWhiteSpace(nextScene) ? completionSceneName : nextScene);
        }

        private void ApplyPlaybackSpeed()
        {
            if (_director == null || !_director.playableGraph.IsValid())
                return;

            var clampedSpeed = playbackSpeed <= 0f ? 1f : playbackSpeed;
            var rootCount = _director.playableGraph.GetRootPlayableCount();
            for (var i = 0; i < rootCount; i++)
                _director.playableGraph.GetRootPlayable(i).SetSpeed(clampedSpeed);
        }
    }
}
