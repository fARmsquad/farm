using FarmSimVR.MonoBehaviours.Tutorial;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// When the sibling <see cref="PlayableDirector"/> stops after natural playback,
    /// advances the tutorial flow (loads next scene per story package / flow state).
    /// </summary>
    public sealed class PlayableDirectorCompleteTutorialFlow : MonoBehaviour
    {
        private PlayableDirector _director;
        private bool _completionHandled;

        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
        }

        private void OnEnable()
        {
            if (_director != null)
                _director.stopped += OnDirectorStopped;
        }

        private void OnDisable()
        {
            if (_director != null)
                _director.stopped -= OnDirectorStopped;
        }

        private void OnDirectorStopped(PlayableDirector director)
        {
            if (_completionHandled)
                return;

            _completionHandled = true;
            TutorialFlowController.Instance?.CompleteCurrentSceneAndLoadNext();
        }
    }
}
