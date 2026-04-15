using FarmSimVR.Core.Story;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    internal static class GeneratedPlaythroughStatusFormatter
    {
        private const string DefaultNote = "Generate a unique playthrough to inspect the next run.";

        public static string Build(
            string lifecycleState,
            string note,
            string error,
            string preparedAtUtc,
            StorySequenceRuntimeController runtimeController)
        {
            var state = Sanitize(lifecycleState, "Idle");
            var sessionId = ResolveSessionId(state, runtimeController);
            var entrySceneName = ResolveEntrySceneName(state, runtimeController);
            var packageLabel = "none";
            var beatLabel = "none";
            var shotCount = "0";
            var firstSubtitle = "none";
            var firstImage = "none";
            var firstAudio = "none";

            if (runtimeController != null && runtimeController.HasActiveSession &&
                StoryPackageRuntimeCatalog.TryGetPackage(out var package, out _))
            {
                packageLabel = ResolvePackageLabel(package);

                if (!string.IsNullOrWhiteSpace(runtimeController.PreparedEntrySceneName) &&
                    StoryPackageRuntimeCatalog.TryGetBeat(runtimeController.PreparedEntrySceneName, out var beat) &&
                    beat != null)
                {
                    beatLabel = ResolveBeatLabel(beat);
                }

                if (!string.IsNullOrWhiteSpace(runtimeController.PreparedEntrySceneName) &&
                    StoryPackageRuntimeCatalog.TryGetStoryboard(runtimeController.PreparedEntrySceneName, out _, out var storyboard) &&
                    storyboard?.Shots != null &&
                    storyboard.Shots.Length > 0)
                {
                    shotCount = storyboard.Shots.Length.ToString();
                    var firstShot = storyboard.Shots[0];
                    firstSubtitle = Sanitize(firstShot?.SubtitleText, "none");
                    firstImage = Sanitize(firstShot?.ImageResourcePath, "none");
                    firstAudio = Sanitize(firstShot?.AudioResourcePath, "none");
                }
            }

            var preparedAt = Sanitize(preparedAtUtc, "not yet");
            var detailNote = Sanitize(note, DefaultNote);
            var lastError = Sanitize(error, "none");
            var nextStep = ResolveNextStep(state);

            return string.Join("\n", new[]
            {
                detailNote,
                $"State: {state}",
                $"Next Step: {nextStep}",
                $"Session: {sessionId}",
                $"Entry Scene: {entrySceneName}",
                $"Package: {packageLabel}",
                $"Beat: {beatLabel}",
                $"Shots: {shotCount}",
                $"First Subtitle: {firstSubtitle}",
                $"First Image: {firstImage}",
                $"First Audio: {firstAudio}",
                $"Prepared At (UTC): {preparedAt}",
                $"Last Error: {lastError}",
            });
        }

        private static string ResolveSessionId(
            string lifecycleState,
            StorySequenceRuntimeController runtimeController)
        {
            if (runtimeController != null && runtimeController.HasActiveSession)
                return Sanitize(runtimeController.ActiveSessionId, "none");

            return lifecycleState == "Generating" ? "pending" : "none";
        }

        private static string ResolveEntrySceneName(
            string lifecycleState,
            StorySequenceRuntimeController runtimeController)
        {
            if (runtimeController != null && runtimeController.HasPreparedSequence)
                return Sanitize(runtimeController.PreparedEntrySceneName, "none");

            return lifecycleState == "Generating" ? "pending" : "none";
        }

        private static string ResolvePackageLabel(StoryPackageSnapshot package)
        {
            if (package == null)
                return "none";

            if (!string.IsNullOrWhiteSpace(package.DisplayName))
                return Sanitize(package.DisplayName, "unnamed package");

            return Sanitize(package.PackageId, "unnamed package");
        }

        private static string ResolveBeatLabel(StoryBeatSnapshot beat)
        {
            if (beat == null)
                return "none";

            if (!string.IsNullOrWhiteSpace(beat.DisplayName))
                return Sanitize(beat.DisplayName, "unnamed beat");

            return Sanitize(beat.BeatId, "unnamed beat");
        }

        private static string ResolveNextStep(string lifecycleState)
        {
            return lifecycleState switch
            {
                "Generating" => "Backend is writing the story, generating cutscene art, then narration audio.",
                "Ready" => "Press Play Unique Playthrough to load the generated run.",
                "Loading" => "Unity is fading into the generated entry scene.",
                "Busy" => "A previous generation request is still running.",
                "Failed" => "Check the Unity Console, /tmp/story-orchestrator-8012.log, and backend/story-orchestrator/.env.local.",
                _ => "Press Generate Unique Playthrough to request a fresh run.",
            };
        }

        private static string Sanitize(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }
    }
}
