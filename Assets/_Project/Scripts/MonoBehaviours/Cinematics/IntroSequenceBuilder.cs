using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Builds the runtime CinematicSequence for the intro cutscene.
    /// Full 5-panel wiring is implemented as part of INT-008.
    /// This stub returns an empty sequence so the project compiles
    /// and AutoplayIntroScene can be iterated on independently.
    /// </summary>
    public static class IntroSequenceBuilder
    {
        /// <summary>Creates and returns the intro CinematicSequence asset.</summary>
        public static CinematicSequence Build()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.name = "IntroSequence_Runtime";
            sequence.steps = System.Array.Empty<CinematicStep>();

            Debug.LogWarning("[IntroSequenceBuilder] Returning empty sequence — full INT-008 wiring not yet implemented.");
            return sequence;
        }
    }
}
