using FarmSimVR.Core;
using FarmSimVR.Core.Story;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    internal static class GenerativeMinigameContractReader
    {
        public static bool TryGetStringParameter(
            GenerativeMinigameContract contract,
            string parameterName,
            out string value)
        {
            value = string.Empty;
            var entry = FindParameter(contract, parameterName);
            if (entry == null || !string.Equals(entry.ValueType, "String", System.StringComparison.OrdinalIgnoreCase))
                return false;

            value = entry.StringValue ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        public static bool TryGetIntParameter(
            GenerativeMinigameContract contract,
            string parameterName,
            out int value)
        {
            value = 0;
            var entry = FindParameter(contract, parameterName);
            if (entry == null || !string.Equals(entry.ValueType, "Int", System.StringComparison.OrdinalIgnoreCase))
                return false;

            value = entry.IntValue;
            return true;
        }

        public static bool TryGetFloatParameter(
            GenerativeMinigameContract contract,
            string parameterName,
            out float value)
        {
            value = 0f;
            var entry = FindParameter(contract, parameterName);
            if (entry == null || !string.Equals(entry.ValueType, "Float", System.StringComparison.OrdinalIgnoreCase))
                return false;

            value = entry.FloatValue;
            return true;
        }

        public static StoryMinigameConfigSnapshot ToLegacySnapshot(GenerativeMinigameContract contract)
        {
            if (contract == null)
                return null;

            var resolvedEntries = contract.resolved_parameter_entries;
            var legacyEntries = resolvedEntries == null
                ? System.Array.Empty<StoryMinigameParameterSnapshot>()
                : new StoryMinigameParameterSnapshot[resolvedEntries.Length];
            if (resolvedEntries != null)
            {
                for (int i = 0; i < resolvedEntries.Length; i++)
                {
                    var entry = resolvedEntries[i];
                    legacyEntries[i] = new StoryMinigameParameterSnapshot
                    {
                        Name = entry?.Name,
                        ValueType = entry?.ValueType,
                        StringValue = entry?.StringValue,
                        IntValue = entry == null ? 0 : entry.IntValue,
                        FloatValue = entry == null ? 0f : entry.FloatValue,
                        BoolValue = entry != null && entry.BoolValue,
                    };
                }
            }

            return new StoryMinigameConfigSnapshot
            {
                AdapterId = contract.adapter_id,
                ObjectiveText = contract.objective_text,
                RequiredCount = contract.required_count,
                TimeLimitSeconds = contract.time_limit_seconds,
                GeneratorId = contract.generator_id,
                MinigameId = contract.minigame_id,
                FallbackGeneratorIds = contract.fallback_generator_ids ?? System.Array.Empty<string>(),
                ResolvedParameterEntries = legacyEntries,
            };
        }

        private static GenerativeMinigameParameterEntry FindParameter(
            GenerativeMinigameContract contract,
            string parameterName)
        {
            if (contract == null || contract.resolved_parameter_entries == null || string.IsNullOrWhiteSpace(parameterName))
                return null;

            for (int i = 0; i < contract.resolved_parameter_entries.Length; i++)
            {
                var entry = contract.resolved_parameter_entries[i];
                if (entry == null)
                    continue;

                if (string.Equals(entry.Name, parameterName, System.StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }
    }
}
