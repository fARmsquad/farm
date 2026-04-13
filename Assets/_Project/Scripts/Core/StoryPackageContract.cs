using System.Collections.Generic;

namespace FarmSimVR.Core.Story
{
    public static class StoryPackageContract
    {
        public static StoryPackageValidationResult Validate(StoryPackageSnapshot package)
        {
            var errors = new List<string>();
            if (package == null)
            {
                errors.Add("Package is null.");
                return new StoryPackageValidationResult(false, errors.ToArray());
            }

            ValidateHeader(package, errors);
            ValidateBeats(package, errors);
            return new StoryPackageValidationResult(errors.Count == 0, errors.ToArray());
        }

        private static void ValidateHeader(StoryPackageSnapshot package, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(package.PackageId))
                errors.Add("PackageId is required.");

            if (package.SchemaVersion <= 0)
                errors.Add("SchemaVersion must be greater than 0.");

            if (package.PackageVersion <= 0)
                errors.Add("PackageVersion must be greater than 0.");
        }

        private static void ValidateBeats(StoryPackageSnapshot package, List<string> errors)
        {
            if (package.Beats == null || package.Beats.Length == 0)
            {
                errors.Add("At least one beat is required.");
                return;
            }

            var beatIds = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < package.Beats.Length; i++)
                ValidateBeat(package.Beats[i], i, beatIds, errors);
        }

        private static void ValidateBeat(
            StoryBeatSnapshot beat,
            int index,
            ISet<string> beatIds,
            List<string> errors)
        {
            if (beat == null)
            {
                errors.Add($"Beat {index} is null.");
                return;
            }

            ValidateBeatIdentity(beat, index, beatIds, errors);
            ValidateBeatKind(beat, index, errors);
        }

        private static void ValidateBeatIdentity(
            StoryBeatSnapshot beat,
            int index,
            ISet<string> beatIds,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(beat.BeatId))
            {
                errors.Add($"Beat {index} is missing BeatId.");
            }
            else if (!beatIds.Add(beat.BeatId))
            {
                errors.Add($"Beat {index} has duplicate BeatId '{beat.BeatId}'.");
            }

            if (string.IsNullOrWhiteSpace(beat.SceneName))
                errors.Add($"Beat {index} is missing SceneName.");
        }

        private static void ValidateBeatKind(StoryBeatSnapshot beat, int index, List<string> errors)
        {
            if (!StoryBeatKindParser.TryParse(beat.Kind, out var kind))
            {
                errors.Add($"Beat {index} has unknown Kind '{beat.Kind}'.");
                return;
            }

            switch (kind)
            {
                case StoryBeatKind.Cutscene:
                    ValidateCutsceneBeat(beat, index, errors);
                    break;
                case StoryBeatKind.Minigame:
                    ValidateMinigameBeat(beat, index, errors);
                    break;
            }
        }

        private static void ValidateCutsceneBeat(StoryBeatSnapshot beat, int index, List<string> errors)
        {
            if (beat.SequenceSteps == null || beat.SequenceSteps.Length == 0)
            {
                errors.Add($"Beat {index} cutscene requires at least one SequenceStep.");
                return;
            }

            for (int i = 0; i < beat.SequenceSteps.Length; i++)
            {
                if (beat.SequenceSteps[i] == null || string.IsNullOrWhiteSpace(beat.SequenceSteps[i].StepType))
                    errors.Add($"Beat {index} cutscene step {i} is missing StepType.");
            }
        }

        private static void ValidateMinigameBeat(StoryBeatSnapshot beat, int index, List<string> errors)
        {
            if (beat.Minigame == null)
            {
                errors.Add($"Beat {index} minigame requires config.");
                return;
            }

            if (string.IsNullOrWhiteSpace(beat.Minigame.AdapterId))
                errors.Add($"Beat {index} minigame requires AdapterId.");
        }
    }
}
