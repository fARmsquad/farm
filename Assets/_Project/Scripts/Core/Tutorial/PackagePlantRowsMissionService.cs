using FarmSimVR.Core.Farming;

namespace FarmSimVR.Core.Tutorial
{
    public sealed class PackagePlantRowsMissionService
    {
        private string _baseObjective = "Plant the target crop.";

        public string CurrentObjective { get; private set; } = string.Empty;
        public string TargetSeedId { get; private set; } = "seed_carrot";
        public int RequiredCount { get; private set; }
        public int DesiredPlotCount { get; private set; }
        public int CurrentCount { get; private set; }
        public float TimeRemainingSeconds { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsFailed { get; private set; }

        public void Configure(string objectiveText, string cropType, int requiredCount, int rowCount, float timeLimitSeconds)
        {
            RequiredCount = requiredCount < 1 ? 1 : requiredCount;
            var safeRowCount = rowCount < 1 ? 1 : rowCount;
            DesiredPlotCount = RequiredCount * safeRowCount;
            TimeRemainingSeconds = timeLimitSeconds <= 0f ? 300f : timeLimitSeconds;
            TargetSeedId = ToSeedId(cropType);
            CurrentCount = 0;
            IsComplete = false;
            IsFailed = false;
            _baseObjective = string.IsNullOrWhiteSpace(objectiveText)
                ? "Plant the target crop."
                : objectiveText.Trim();
            CurrentObjective = BuildObjective(_baseObjective, CurrentCount, RequiredCount, TimeRemainingSeconds);
        }

        public void Observe(int plantedCount, float deltaTime)
        {
            if (IsComplete || IsFailed)
                return;

            CurrentCount = plantedCount < 0 ? 0 : plantedCount;
            if (CurrentCount >= RequiredCount)
            {
                IsComplete = true;
                CurrentObjective = "Planting complete.";
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

        public bool IsActionAllowed(FarmPlotAction action, PlotStatus soilStatus, string cropId)
        {
            if (IsComplete || IsFailed)
                return false;

            if (soilStatus == PlotStatus.Untilled)
                return action == FarmPlotAction.Till;

            if (soilStatus == PlotStatus.Empty && string.IsNullOrWhiteSpace(cropId))
                return action == FarmPlotAction.PlantSelected;

            return false;
        }

        public FarmPlotAction? GetPrimaryAction(PlotStatus soilStatus, string cropId)
        {
            if (IsComplete || IsFailed)
                return null;

            if (soilStatus == PlotStatus.Untilled)
                return FarmPlotAction.Till;

            if (soilStatus == PlotStatus.Empty && string.IsNullOrWhiteSpace(cropId))
                return FarmPlotAction.PlantSelected;

            return null;
        }

        private static string BuildObjective(string baseObjective, int plantedCount, int requiredCount, float timeRemainingSeconds)
        {
            var safeBase = string.IsNullOrWhiteSpace(baseObjective)
                ? "Plant the target crop."
                : baseObjective.Trim();
            var wholeSeconds = timeRemainingSeconds < 0f ? 0 : (int)timeRemainingSeconds;
            var minutes = wholeSeconds / 60;
            var seconds = wholeSeconds % 60;
            return $"{safeBase}  {plantedCount}/{requiredCount} planted.  {minutes:00}:{seconds:00} remaining.";
        }

        private static string ToSeedId(string cropType)
        {
            return cropType?.Trim().ToLowerInvariant() switch
            {
                "tomato" => "seed_tomato",
                "lettuce" => "seed_lettuce",
                _ => "seed_carrot",
            };
        }
    }
}
