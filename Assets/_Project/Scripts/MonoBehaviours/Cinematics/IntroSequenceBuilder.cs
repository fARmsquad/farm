using System.Collections.Generic;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Builds the 5-panel intro cinematic sequence at runtime.
    /// Returns a <see cref="CinematicSequence"/> ScriptableObject instance
    /// wired with all the steps described in the INT-008 spec.
    ///
    /// All audio and dialogue keys reference assets registered
    /// in <see cref="CinematicSequencer"/>'s named registries.
    /// Missing assets are handled gracefully by the sequencer (logged, skipped).
    /// </summary>
    public static class IntroSequenceBuilder
    {
        /// <summary>
        /// Creates and returns the master intro cinematic sequence.
        /// </summary>
        public static CinematicSequence Build()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.name = "MasterIntroSequence";

            var steps = new List<CinematicStep>();

            // ──────────────────────────────────────────────
            // Panel 1: Sleeping Soundly (~8s)
            // ──────────────────────────────────────────────
            steps.Add(Step(CinematicStepType.Letterbox, floatParam: 1f, duration: 0.5f));
            steps.Add(Step(CinematicStepType.SetLighting, stringParam: "Night"));
            steps.Add(Step(CinematicStepType.CameraMove, stringParam: "Panel1_Bedroom", duration: 8f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.PlayMusic, stringParam: "intro_guitar"));
            steps.Add(Step(CinematicStepType.PlaySFX, stringParam: "crickets_wind"));
            steps.Add(Step(CinematicStepType.Dialogue, stringParam: "panel1_text", duration: 4f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.Wait, duration: 2f));

            // ──────────────────────────────────────────────
            // Panel 2: Town Sleeps (~6s)
            // ──────────────────────────────────────────────
            steps.Add(Step(CinematicStepType.CameraMove, stringParam: "Panel2_TownAerial", duration: 6f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.Dialogue, stringParam: "panel2_text", duration: 3f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.PlaySFX, stringParam: "cat_meow"));
            steps.Add(Step(CinematicStepType.ActivateNPC, stringParam: "RooftopCat"));
            steps.Add(Step(CinematicStepType.Wait, duration: 1f));

            // ──────────────────────────────────────────────
            // Panel 3: The Disruption (~8s)
            // ──────────────────────────────────────────────
            steps.Add(Step(CinematicStepType.Fade, floatParam: 0f, duration: 0.1f)); // hard cut
            steps.Add(Step(CinematicStepType.CameraMove, stringParam: "Panel3_Rooftop", duration: 8f));
            steps.Add(Step(CinematicStepType.Wait, duration: 1f));
            steps.Add(Step(CinematicStepType.PlaySFX, stringParam: "guitar_snap"));
            steps.Add(Step(CinematicStepType.Wait, duration: 0.5f));
            steps.Add(Step(CinematicStepType.PlaySFX, stringParam: "rooster_crow"));
            steps.Add(Step(CinematicStepType.Shake, floatParam: 4f, duration: 0.3f));
            steps.Add(Step(CinematicStepType.PlaySFX, stringParam: "dogs_barking"));
            steps.Add(Step(CinematicStepType.PlaySFX, stringParam: "not_again_yell"));
            steps.Add(Step(CinematicStepType.Dialogue, stringParam: "big_smoke_vo", duration: 2f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.Dialogue, stringParam: "niko_vo", duration: 2f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.Wait, duration: 1f));

            // ──────────────────────────────────────────────
            // Panel 4: The Walk (~15s)
            // ──────────────────────────────────────────────
            steps.Add(Step(CinematicStepType.Fade, floatParam: 1f, duration: 0.5f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.SetLighting, stringParam: "PreDawn"));
            steps.Add(Step(CinematicStepType.Fade, floatParam: 0f, duration: 0.5f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.CameraMove, stringParam: "Panel4_Walk", duration: 15f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.StopMusic));
            steps.Add(Step(CinematicStepType.PlayMusic, stringParam: "tension_track"));

            // ──────────────────────────────────────────────
            // Panel 5: The Pen — Transition to Gameplay (~10s)
            // ──────────────────────────────────────────────
            steps.Add(Step(CinematicStepType.Fade, floatParam: 1f, duration: 0.5f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.SetLighting, stringParam: "Dawn"));
            steps.Add(Step(CinematicStepType.Fade, floatParam: 0f, duration: 0.5f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.CameraMove, stringParam: "Panel5_Farm", duration: 6f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.PlaySFX, stringParam: "morning_birds"));
            steps.Add(Step(CinematicStepType.PlaySFX, stringParam: "gate_creak"));
            steps.Add(Step(CinematicStepType.StopMusic));
            steps.Add(Step(CinematicStepType.PlayMusic, stringParam: "kazoo_standoff"));
            steps.Add(Step(CinematicStepType.MissionStart, stringParam: "POLLO LOCO|Capture El Pollo Loco. Don't harm the chicks.", duration: 3f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.Letterbox, floatParam: 0f, duration: 0.5f, waitForCompletion: true));
            steps.Add(Step(CinematicStepType.EnablePlayerControl));

            sequence.steps = steps.ToArray();
            return sequence;
        }

        private static CinematicStep Step(
            CinematicStepType type,
            string stringParam = "",
            float floatParam = 0f,
            int intParam = 0,
            float duration = 0f,
            bool waitForCompletion = false)
        {
            return new CinematicStep
            {
                type = type,
                stringParam = stringParam ?? "",
                floatParam = floatParam,
                intParam = intParam,
                duration = duration,
                waitForCompletion = waitForCompletion
            };
        }
    }
}
