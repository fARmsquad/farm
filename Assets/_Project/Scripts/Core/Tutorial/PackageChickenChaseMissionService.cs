namespace FarmSimVR.Core.Tutorial
{
    public sealed class PackageChickenChaseMissionService
    {
        private const string DefaultObjective = "Catch the chicken and drop it in the coop.";
        private const string DefaultArenaPresetId = "tutorial_pen_small";
        private const string DefaultGuidanceLevel = "high";

        private string _baseObjective = DefaultObjective;

        public string CurrentObjective { get; private set; } = DefaultObjective;
        public int RequiredCaptureCount { get; private set; } = 1;
        public int ConfiguredChickenCount { get; private set; } = 1;
        public int CapturedCount { get; private set; }
        public string ArenaPresetId { get; private set; } = DefaultArenaPresetId;
        public string GuidanceLevel { get; private set; } = DefaultGuidanceLevel;
        public bool IsComplete { get; private set; }

        public void Configure(
            string objectiveText,
            int requiredCaptureCount,
            int chickenCount,
            string arenaPresetId,
            string guidanceLevel)
        {
            RequiredCaptureCount = requiredCaptureCount < 1 ? 1 : requiredCaptureCount;
            ConfiguredChickenCount = chickenCount < RequiredCaptureCount
                ? RequiredCaptureCount
                : chickenCount;
            ArenaPresetId = NormalizeOrDefault(arenaPresetId, DefaultArenaPresetId);
            GuidanceLevel = NormalizeOrDefault(guidanceLevel, DefaultGuidanceLevel);
            _baseObjective = string.IsNullOrWhiteSpace(objectiveText)
                ? DefaultObjective
                : objectiveText.Trim();
            ResetProgress();
        }

        public bool RegisterSuccessfulCapture()
        {
            if (IsComplete)
                return true;

            CapturedCount++;
            if (CapturedCount >= RequiredCaptureCount)
            {
                CapturedCount = RequiredCaptureCount;
                IsComplete = true;
                CurrentObjective = "Chicken secured.";
                return true;
            }

            CurrentObjective = BuildObjective(_baseObjective, CapturedCount, RequiredCaptureCount);
            return false;
        }

        public void ResetProgress()
        {
            CapturedCount = 0;
            IsComplete = false;
            CurrentObjective = BuildObjective(_baseObjective, CapturedCount, RequiredCaptureCount);
        }

        private static string BuildObjective(string baseObjective, int capturedCount, int requiredCaptureCount)
        {
            var safeBase = string.IsNullOrWhiteSpace(baseObjective)
                ? DefaultObjective
                : baseObjective.Trim();

            if (requiredCaptureCount <= 1)
                return safeBase;

            return $"{safeBase}  {capturedCount}/{requiredCaptureCount} secured.";
        }

        private static string NormalizeOrDefault(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim().ToLowerInvariant();
        }
    }
}
