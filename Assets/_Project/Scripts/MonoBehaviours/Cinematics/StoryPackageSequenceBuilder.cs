using System;
using FarmSimVR.Core.Story;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public static class StoryPackageSequenceBuilder
    {
        public static bool TryBuildCutsceneSequence(
            StoryBeatSnapshot beat,
            out CinematicSequence sequence,
            out string error)
        {
            sequence = null;
            error = string.Empty;

            if (beat == null)
            {
                error = "Beat is null.";
                return false;
            }

            if (!StoryBeatKindParser.TryParse(beat.Kind, out var kind) || kind != StoryBeatKind.Cutscene)
            {
                error = $"Beat '{beat.BeatId}' is not a cutscene beat.";
                return false;
            }

            if (beat.SequenceSteps == null || beat.SequenceSteps.Length == 0)
            {
                error = $"Beat '{beat.BeatId}' has no sequence steps.";
                return false;
            }

            var steps = new CinematicStep[beat.SequenceSteps.Length];
            for (int i = 0; i < beat.SequenceSteps.Length; i++)
            {
                if (!TryBuildStep(beat.SequenceSteps[i], out steps[i], out error))
                    return false;
            }

            sequence = ScriptableObject.CreateInstance<CinematicSequence>();
            sequence.name = string.IsNullOrWhiteSpace(beat.BeatId)
                ? "StoryPackageSequence"
                : $"{beat.BeatId}_Sequence";
            sequence.steps = steps;
            return true;
        }

        private static bool TryBuildStep(
            StorySequenceStepSnapshot snapshot,
            out CinematicStep step,
            out string error)
        {
            step = default;
            if (snapshot == null)
            {
                error = "Sequence step is null.";
                return false;
            }

            if (!Enum.TryParse(snapshot.StepType, true, out CinematicStepType stepType))
            {
                error = $"Unknown cinematic step type '{snapshot.StepType}'.";
                return false;
            }

            step = new CinematicStep
            {
                type = stepType,
                stringParam = snapshot.StringParam ?? string.Empty,
                floatParam = snapshot.FloatParam,
                intParam = snapshot.IntParam,
                duration = snapshot.Duration,
                waitForCompletion = snapshot.WaitForCompletion
            };
            error = string.Empty;
            return true;
        }
    }
}
