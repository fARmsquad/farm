using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public static class IntroSequenceBuilder
    {
        public static CinematicSequence Build()
        {
            var sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.steps = new[]
            {
                new CinematicStep
                {
                    type = CinematicStepType.Fade,
                    floatParam = 0f,
                    duration = 1.2f,
                    waitForCompletion = true
                },
                new CinematicStep
                {
                    type = CinematicStepType.Wait,
                    duration = 1.5f,
                    waitForCompletion = true
                },
                new CinematicStep
                {
                    type = CinematicStepType.ObjectivePopup,
                    stringParam = "El Pollo Loco is loose in the pen."
                },
                new CinematicStep
                {
                    type = CinematicStepType.MissionStart,
                    stringParam = "POLLO LOCO|Catch El Pollo Loco"
                },
                new CinematicStep
                {
                    type = CinematicStepType.Wait,
                    duration = 0.5f,
                    waitForCompletion = true
                }
            };

            return sequence;
        }
    }
}
