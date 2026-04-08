using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Starts the intro cutscene on Start by playing the PlayableDirector,
    /// which drives all cinematic events (camera, fades, subtitles, audio)
    /// through custom Timeline tracks.
    /// Also activates SkipPrompt if present.
    /// </summary>
    public class IntroCinematicAutoPlay : MonoBehaviour
    {
        [Header("Legacy (no longer used — kept for migration reference)")]
        [SerializeField] private CinematicSequence sequence;

        private IEnumerator Start()
        {
            // Wait one frame so all Awake/Start callbacks finish first
            yield return null;

            var director = GetComponent<PlayableDirector>();
            var skipPrompt = GetComponent<SkipPrompt>();

            if (director == null)
            {
                Debug.LogError("[IntroCinematicAutoPlay] No PlayableDirector found — cinematic will not play.");
                yield break;
            }

            if (director.playableAsset == null)
            {
                Debug.LogError("[IntroCinematicAutoPlay] PlayableDirector has no TimelineAsset assigned.");
                yield break;
            }

            skipPrompt?.Activate();
            director.Play();
        }
    }
}
