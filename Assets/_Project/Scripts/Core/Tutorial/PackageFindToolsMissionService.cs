namespace FarmSimVR.Core.Tutorial
{
    public sealed class PackageFindToolsMissionService
    {
        private static readonly string[] StarterTools = { "Hoe", "Watering Can", "Seed Pouch" };
        private static readonly string[] WateringTools = { "Watering Can", "Bucket", "Hose Reel" };
        private static readonly string[] PlantingTools = { "Hoe", "Seed Pouch", "Harvest Basket" };

        private string _baseObjective = "Recover the tools.";

        public string CurrentObjective { get; private set; } = string.Empty;
        public string TargetToolSet { get; private set; } = "starter";
        public string SearchZone { get; private set; } = "yard";
        public string HintStrength { get; private set; } = "strong";
        public string[] ToolDisplayNames { get; private set; } = System.Array.Empty<string>();
        public int RequiredCount { get; private set; }
        public int CurrentCount { get; private set; }
        public float TimeRemainingSeconds { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsFailed { get; private set; }

        public void Configure(
            string objectiveText,
            string targetToolSet,
            int requiredCount,
            string searchZone,
            string hintStrength,
            float timeLimitSeconds)
        {
            RequiredCount = requiredCount < 1 ? 1 : requiredCount;
            TargetToolSet = NormalizeOrDefault(targetToolSet, "starter");
            SearchZone = NormalizeOrDefault(searchZone, "yard");
            HintStrength = NormalizeOrDefault(hintStrength, "strong");
            ToolDisplayNames = ResolveTools(TargetToolSet, RequiredCount);
            TimeRemainingSeconds = timeLimitSeconds <= 0f ? 240f : timeLimitSeconds;
            CurrentCount = 0;
            IsComplete = false;
            IsFailed = false;
            _baseObjective = string.IsNullOrWhiteSpace(objectiveText)
                ? "Recover the tools."
                : objectiveText.Trim();
            CurrentObjective = BuildObjective(_baseObjective, CurrentCount, RequiredCount, TimeRemainingSeconds);
        }

        public void Observe(int collectedCount, float deltaTime)
        {
            if (IsComplete || IsFailed)
                return;

            CurrentCount = collectedCount < 0 ? 0 : collectedCount;
            if (CurrentCount >= RequiredCount)
            {
                IsComplete = true;
                CurrentObjective = "Tools recovered.";
                return;
            }

            TimeRemainingSeconds -= deltaTime;
            if (TimeRemainingSeconds <= 0f)
            {
                TimeRemainingSeconds = 0f;
                IsFailed = true;
                CurrentObjective = "Time ran out. Reload the slice and try again.";
                return;
            }

            CurrentObjective = BuildObjective(_baseObjective, CurrentCount, RequiredCount, TimeRemainingSeconds);
        }

        private static string[] ResolveTools(string targetToolSet, int requiredCount)
        {
            var source = targetToolSet switch
            {
                "watering" => WateringTools,
                "planting" => PlantingTools,
                _ => StarterTools,
            };

            var resolved = new string[requiredCount];
            for (var i = 0; i < requiredCount; i++)
                resolved[i] = source[i % source.Length];
            return resolved;
        }

        private static string BuildObjective(string baseObjective, int collectedCount, int requiredCount, float timeRemainingSeconds)
        {
            var safeBase = string.IsNullOrWhiteSpace(baseObjective)
                ? "Recover the tools."
                : baseObjective.Trim();
            var wholeSeconds = timeRemainingSeconds < 0f ? 0 : (int)timeRemainingSeconds;
            var minutes = wholeSeconds / 60;
            var seconds = wholeSeconds % 60;
            return $"{safeBase}  {collectedCount}/{requiredCount} found.  {minutes:00}:{seconds:00} remaining.";
        }

        private static string NormalizeOrDefault(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim().ToLowerInvariant();
        }
    }
}
