using System.Collections;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Tutorial;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Plays the sibling <see cref="PlayableDirector"/> on startup (like <see cref="IntroCinematicAutoPlay"/>),
    /// enables <see cref="SkipPrompt"/>, then loads the next scene when the timeline ends, is skipped, or a
    /// duration failsafe elapses (guards against a missing <c>stopped</c> callback).
    /// </summary>
    public sealed class PlayableDirectorCompleteTutorialFlow : MonoBehaviour
    {
        private const float TimelineEndFailsafePadSeconds = 0.35f;

        private PlayableDirector _director;
        private SceneLoader _sceneLoader;
        private bool _completionHandled;

        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            _sceneLoader = GetComponent<SceneLoader>();
            if (_sceneLoader == null)
                _sceneLoader = gameObject.AddComponent<SceneLoader>();
        }

        private IEnumerator Start()
        {
            yield return null;

            if (_director == null || _director.playableAsset == null)
                yield break;

            _director.stopped += OnDirectorStopped;

            var skip = GetComponent<SkipPrompt>();
            if (skip == null)
                skip = gameObject.AddComponent<SkipPrompt>();
            if (skip.OnSkipRequested == null)
                skip.OnSkipRequested = new UnityEvent();
            skip.OnSkipRequested.RemoveListener(OnSkipPressed);
            skip.OnSkipRequested.AddListener(OnSkipPressed);
            skip.Activate();

            if (_director.state != PlayState.Playing)
                _director.Play();

            var timelineDuration = ComputeTimelineDurationSeconds();
            if (timelineDuration > 0f)
                StartCoroutine(CoFailsafeAdvance(timelineDuration + TimelineEndFailsafePadSeconds));
        }

        private void OnDestroy()
        {
            if (_director != null)
                _director.stopped -= OnDirectorStopped;

            var skip = GetComponent<SkipPrompt>();
            if (skip != null && skip.OnSkipRequested != null)
                skip.OnSkipRequested.RemoveListener(OnSkipPressed);
        }

        /// <summary>Skip control (same contract as <see cref="IntroCinematicAutoPlay.LoadNextScene"/>).</summary>
        public void LoadNextScene()
        {
            AdvanceToNextScene(requestStopIfPlaying: true);
        }

        private void OnSkipPressed()
        {
            AdvanceToNextScene(requestStopIfPlaying: true);
        }

        private void OnDirectorStopped(PlayableDirector director)
        {
            AdvanceToNextScene(requestStopIfPlaying: false);
        }

        private IEnumerator CoFailsafeAdvance(float waitSeconds)
        {
            yield return new WaitForSecondsRealtime(waitSeconds);
            AdvanceToNextScene(requestStopIfPlaying: false);
        }

        private float ComputeTimelineDurationSeconds()
        {
            if (_director == null || _director.playableAsset == null)
                return 0f;

            var d = (float)_director.duration;
            return d > 0f ? d : 0f;
        }

        private void AdvanceToNextScene(bool requestStopIfPlaying)
        {
            if (_completionHandled)
                return;

            _completionHandled = true;

            GetComponent<SkipPrompt>()?.Deactivate();

            if (_director != null)
            {
                _director.stopped -= OnDirectorStopped;

                if (requestStopIfPlaying && _director.state == PlayState.Playing)
                    _director.Stop();
            }

            if (TutorialFlowController.Instance != null)
            {
                TutorialFlowController.Instance.CompleteCurrentSceneAndLoadNext();
                return;
            }

            var next = StoryPackageRuntimeCatalog.GetNextSceneOrNull(SceneManager.GetActiveScene().name);
            if (string.IsNullOrWhiteSpace(next))
                next = TutorialSceneCatalog.GetSceneName(
                    TutorialSceneCatalog.GetNextStep(
                        TutorialSceneCatalog.GetStepForScene(SceneManager.GetActiveScene().name)));

            _sceneLoader.LoadScene(next);
        }
    }
}
