namespace FarmSimVR.Core.Story
{
    [System.Serializable]
    public sealed class StoryMinigameConfigSnapshot
    {
        public string AdapterId;
        public string ObjectiveText;
        public int RequiredCount;
        public float TimeLimitSeconds;
        public string GeneratorId;
        public string MinigameId;
        public string[] FallbackGeneratorIds = System.Array.Empty<string>();
        public StoryMinigameParameterSnapshot[] ResolvedParameterEntries = System.Array.Empty<StoryMinigameParameterSnapshot>();

        public bool TryGetStringParameter(string parameterName, out string value)
        {
            value = string.Empty;
            var entry = FindParameter(parameterName);
            if (entry == null || !string.Equals(entry.ValueType, "String", System.StringComparison.OrdinalIgnoreCase))
                return false;

            value = entry.StringValue ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        public bool TryGetIntParameter(string parameterName, out int value)
        {
            value = 0;
            var entry = FindParameter(parameterName);
            if (entry == null || !string.Equals(entry.ValueType, "Int", System.StringComparison.OrdinalIgnoreCase))
                return false;

            value = entry.IntValue;
            return true;
        }

        public bool TryGetFloatParameter(string parameterName, out float value)
        {
            value = 0f;
            var entry = FindParameter(parameterName);
            if (entry == null || !string.Equals(entry.ValueType, "Float", System.StringComparison.OrdinalIgnoreCase))
                return false;

            value = entry.FloatValue;
            return true;
        }

        private StoryMinigameParameterSnapshot FindParameter(string parameterName)
        {
            if (ResolvedParameterEntries == null || string.IsNullOrWhiteSpace(parameterName))
                return null;

            for (int i = 0; i < ResolvedParameterEntries.Length; i++)
            {
                var entry = ResolvedParameterEntries[i];
                if (entry == null)
                    continue;

                if (string.Equals(entry.Name, parameterName, System.StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }
    }
}
