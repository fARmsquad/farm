using System;
using System.Collections.Generic;
using System.Text;
using FarmSimVR.Core;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    internal readonly struct GenerativeProgressStageView
    {
        public GenerativeProgressStageView(string key, string label, string state)
        {
            Key = key ?? string.Empty;
            Label = label ?? string.Empty;
            State = state ?? "pending";
        }

        public string Key { get; }
        public string Label { get; }
        public string State { get; }
    }

    internal static class GenerativePlaythroughMenuFormatter
    {
        private static readonly (string Key, string Label)[] StageDefinitions =
        {
            ("queued", "Queued"),
            ("planning", "Story"),
            ("generating_images", "Images"),
            ("generating_audio", "Audio"),
            ("assembling_contract", "Package"),
            ("validating", "Validate"),
            ("ready", "Ready"),
        };

        public static IReadOnlyList<GenerativeProgressStageView> BuildStages(GenerativeRuntimeTrackerSessionDetail detail)
        {
            var stepByName = new Dictionary<string, string>(StringComparer.Ordinal);
            if (detail?.active_job?.steps != null)
            {
                foreach (var step in detail.active_job.steps)
                {
                    if (step == null || string.IsNullOrWhiteSpace(step.step_name))
                        continue;

                    stepByName[step.step_name] = Sanitize(step.status, "pending");
                }
            }

            var currentStage = detail == null ? string.Empty : Sanitize(detail.current_stage, string.Empty);
            var stages = new GenerativeProgressStageView[StageDefinitions.Length];
            for (int index = 0; index < StageDefinitions.Length; index++)
            {
                var definition = StageDefinitions[index];
                var state = ResolveStageState(definition.Key, currentStage, stepByName);
                stages[index] = new GenerativeProgressStageView(definition.Key, definition.Label, state);
            }

            return stages;
        }

        public static string BuildSessionRow(GenerativeRuntimeTrackerSessionSummary summary, bool selected)
        {
            if (summary == null)
                return selected ? "> Missing session" : "Missing session";

            var prefix = selected ? "> " : string.Empty;
            var turnProgress = $"{summary.ready_turn_count}/{summary.max_turns}";
            return prefix + $"{turnProgress}  {Sanitize(summary.status, "unknown")}  {Sanitize(summary.current_stage, "queued")}";
        }

        public static string BuildStatus(
            string note,
            GenerativePlaythroughController controller,
            GenerativeRuntimeTrackerSessionDetail detail)
        {
            var builder = new StringBuilder();
            builder.AppendLine(Sanitize(note, "Select a playthrough or create a new one."));
            builder.Append("Local Session: ");
            builder.AppendLine(controller == null || !controller.HasActiveSession ? "none" : controller.ActiveSessionId);
            builder.Append("Prepared Scene: ");
            builder.AppendLine(controller == null || !controller.HasPreparedSequence ? "not ready" : Sanitize(controller.PreparedEntrySceneName, "not ready"));

            if (detail == null)
            {
                builder.Append("Selected Session: none");
                return builder.ToString();
            }

            builder.Append("Selected Session: ");
            builder.AppendLine(Sanitize(detail.session_id, "none"));
            builder.Append("Stage: ");
            builder.AppendLine(Sanitize(detail.current_stage, "unknown"));
            builder.Append("Turns Ready: ");
            builder.Append(detail.ready_turn_count);
            builder.Append('/');
            builder.Append(detail.max_turns);
            return builder.ToString();
        }

        public static string BuildDetail(GenerativeRuntimeTrackerSessionDetail detail)
        {
            if (detail == null)
                return "No playthrough selected.";

            var builder = new StringBuilder();
            builder.AppendLine(Sanitize(detail.package_display_name, "Generated Playthrough"));
            builder.Append("Status: ");
            builder.AppendLine(Sanitize(detail.status, "unknown"));
            builder.Append("Stage: ");
            builder.AppendLine(Sanitize(detail.current_stage, "unknown"));
            builder.Append("Turns Ready: ");
            builder.Append(detail.ready_turn_count);
            builder.Append('/');
            builder.AppendLine(detail.max_turns.ToString());

            if (detail.turns == null || detail.turns.Length == 0)
            {
                builder.Append("Latest Turn: not ready yet");
                return builder.ToString();
            }

            var latestTurn = detail.turns[detail.turns.Length - 1];
            builder.Append("Latest Turn: ");
            builder.AppendLine($"#{latestTurn.turn_index + 1} {Sanitize(latestTurn.generator_id, "generator")}");
            builder.Append("Character: ");
            builder.AppendLine(Sanitize(latestTurn.character_name, "unknown"));
            builder.Append("Scene: ");
            builder.AppendLine(Sanitize(latestTurn.scene_name, "unknown"));
            builder.Append("Objective: ");
            builder.AppendLine(Sanitize(latestTurn.objective_text, "unknown"));
            builder.Append("Artifacts: ");
            builder.Append(latestTurn.artifact_count);
            builder.Append(" total");
            if (latestTurn.fallback_artifact_count > 0)
            {
                builder.Append(" (");
                builder.Append(latestTurn.fallback_artifact_count);
                builder.Append(" fallback)");
            }
            builder.AppendLine();
            builder.Append("Summary: ");
            builder.Append(Sanitize(latestTurn.summary, "none"));
            return builder.ToString();
        }

        private static string ResolveStageState(
            string stageKey,
            string currentStage,
            IReadOnlyDictionary<string, string> stepByName)
        {
            if (stepByName.TryGetValue(stageKey, out var explicitState))
                return explicitState;

            if (string.Equals(currentStage, "completed", StringComparison.Ordinal))
                return "completed";
            if (string.Equals(currentStage, "failed", StringComparison.Ordinal) && string.Equals(stageKey, "ready", StringComparison.Ordinal))
                return "failed";

            var currentIndex = FindStageIndex(currentStage);
            var stageIndex = FindStageIndex(stageKey);
            if (currentIndex < 0 || stageIndex < 0)
                return "pending";
            if (stageIndex < currentIndex)
                return "completed";
            if (stageIndex == currentIndex)
                return "running";
            return "pending";
        }

        private static int FindStageIndex(string stageKey)
        {
            for (int index = 0; index < StageDefinitions.Length; index++)
            {
                if (string.Equals(StageDefinitions[index].Key, stageKey, StringComparison.Ordinal))
                    return index;
            }

            return -1;
        }

        private static string Sanitize(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }
    }
}
