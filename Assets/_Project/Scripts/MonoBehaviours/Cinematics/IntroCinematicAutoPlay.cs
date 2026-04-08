using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Starts the intro cutscene on Start by simultaneously playing:
    ///   1. The PlayableDirector (Cinemachine Timeline — camera shots)
    ///   2. The CinematicSequencer (fades, subtitles, audio)
    /// Also activates SkipPrompt if present.
    /// </summary>
    public class IntroCinematicAutoPlay : MonoBehaviour
    {
        [SerializeField] private CinematicSequence sequence;

        private IEnumerator Start()
        {
            // Wait one frame so all Awake/Start callbacks finish first
            yield return null;

            var director = GetComponent<PlayableDirector>();
            var sequencer = GetComponent<CinematicSequencer>();
            var skipPrompt = GetComponent<SkipPrompt>();

            if (sequencer == null)
                Debug.LogWarning("[IntroCinematicAutoPlay] No CinematicSequencer found — non-camera events will not play.");

            if (director == null)
                Debug.LogWarning("[IntroCinematicAutoPlay] No PlayableDirector found — camera shots will not play.");

            skipPrompt?.Activate();

            // Start both simultaneously so Timeline camera and sequence events stay in sync
            director?.Play();

            if (sequencer != null && sequence != null)
                sequencer.Play(sequence);
            else if (sequence == null)
                Debug.LogWarning("[IntroCinematicAutoPlay] No CinematicSequence asset assigned.");
        }
    }
}
