using FarmSimVR.Core;

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
            GenerativePlaythroughController runtimeController)
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

            if (runtimeController != null && runtimeController.HasPreparedSequence)
            {
                var envelope = GenerativeTurnRuntimeState.PreparedTurn;
                if (envelope != null)
                {
                    packageLabel = "runtime/v1";
                    beatLabel = Sanitize(envelope.cutscene?.display_name, "none");

                    if (envelope.cutscene?.shots != null && envelope.cutscene.shots.Length > 0)
                    {
                        shotCount = envelope.cutscene.shots.Length.ToString();
                        var firstShot = envelope.cutscene.shots[0];
                        firstSubtitle = Sanitize(firstShot?.subtitle_text, "none");
                        firstImage = Sanitize(firstShot?.image_asset_id, "none");
                        firstAudio = Sanitize(firstShot?.audio_asset_id, "none");
                    }
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
            GenerativePlaythroughController runtimeController)
        {
            if (runtimeController != null && runtimeController.HasActiveSession)
                return Sanitize(runtimeController.ActiveSessionId, "none");

            return lifecycleState == "Generating" ? "pending" : "none";
        }

        private static string ResolveEntrySceneName(
            string lifecycleState,
            GenerativePlaythroughController runtimeController)
        {
            if (runtimeController != null && runtimeController.HasPreparedSequence)
                return Sanitize(runtimeController.PreparedEntrySceneName, "none");

            return lifecycleState == "Generating" ? "pending" : "none";
        }

        private static string ResolveNextStep(string lifecycleState)
        {
            return lifecycleState switch
            {
                "Generating" => "Backend is writing the story, generating cutscene art, then narration audio.",
                "Ready" => "Press Play Unique Playthrough to load the generated run.",
                "Loading" => "Unity is fading into the generated entry scene.",
                "Busy" => "A previous generation request is still running.",
                "Failed" => $"Check the Unity Console, {TownVoiceTokenServiceEndpointResolver.ProductionBaseUrl}, the FARMSIM_STORY_ORCHESTRATOR_URL override, or the local launcher log at /tmp/story-orchestrator-8012.log.",
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
