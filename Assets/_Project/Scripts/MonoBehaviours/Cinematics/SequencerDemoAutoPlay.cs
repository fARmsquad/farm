using System.Collections;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Auto-plays a demo cinematic sequence on Start.
    /// Shows: fade to black, letterbox, objective popup, screen shake,
    /// fade from black, mission passed, and player control toggle.
    /// Total runtime: ~18 seconds. Just hit Play and watch.
    /// </summary>
    public class SequencerDemoAutoPlay : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(RunDemo());
        }

        private IEnumerator RunDemo()
        {
            // Wait one frame for all Awake/Start to finish
            yield return null;

            var sequencer = GetComponent<CinematicSequencer>();
            if (sequencer == null)
            {
                Debug.LogError("[SequencerDemo] No CinematicSequencer found on this GameObject.");
                yield break;
            }

            // Build a sequence programmatically
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                // Act 1: Fade in from black (scene starts visible, we first go black then come back)
                Step(CinematicStepType.Fade, floatParam: 1f, duration: 1f, wait: true),       // fade to black
                Step(CinematicStepType.Wait, duration: 0.5f, wait: true),                      // hold black

                // Show letterbox bars
                Step(CinematicStepType.Letterbox, floatParam: 1f, duration: 0.8f, wait: true), // letterbox in
                Step(CinematicStepType.Fade, floatParam: -1f, duration: 1.5f, wait: true),     // fade from black

                // Act 2: Show an objective
                Step(CinematicStepType.Wait, duration: 1f, wait: true),
                Step(CinematicStepType.ObjectivePopup, stringParam: "Find the farmhouse before dawn."),
                Step(CinematicStepType.Wait, duration: 3f, wait: true),

                // Act 3: Screen shake!
                Step(CinematicStepType.Shake, floatParam: 0.4f, duration: 0.5f, wait: true),
                Step(CinematicStepType.Wait, duration: 1f, wait: true),

                // Act 4: Disable player, show mission passed
                Step(CinematicStepType.DisablePlayerControl),
                Step(CinematicStepType.ObjectivePopup, stringParam: "MISSION: POLLO LOCO"),
                Step(CinematicStepType.Wait, duration: 3f, wait: true),

                // Act 5: Hide letterbox, re-enable player
                Step(CinematicStepType.Letterbox, floatParam: 0f, duration: 0.8f, wait: true), // letterbox out
                Step(CinematicStepType.EnablePlayerControl),
                Step(CinematicStepType.Wait, duration: 1f, wait: true),

                // Finale: fade to black and show mission passed
                Step(CinematicStepType.Fade, floatParam: 1f, duration: 1f, wait: true),
                Step(CinematicStepType.MissionComplete),
                Step(CinematicStepType.Wait, duration: 0.5f, wait: true),
                Step(CinematicStepType.Fade, floatParam: -1f, duration: 1f, wait: true),
            };

            sequencer.OnSequenceComplete.AddListener(() =>
                Debug.Log("[SequencerDemo] Sequence complete!"));

            Debug.Log("[SequencerDemo] Starting demo sequence (~18 seconds)...");
            sequencer.Play(sequence);
        }

        private static CinematicStep Step(
            CinematicStepType type,
            string stringParam = "",
            float floatParam = 0f,
            int intParam = 0,
            float duration = 0f,
            bool wait = false)
        {
            return new CinematicStep
            {
                type = type,
                stringParam = stringParam,
                floatParam = floatParam,
                intParam = intParam,
                duration = duration,
                waitForCompletion = wait
            };
        }
    }
}
