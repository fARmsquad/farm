namespace FarmSimVR.Core.Tutorial
{
    public readonly struct HorseTrainingSnapshot
    {
        public HorseTrainingSnapshot(
            HorseTrainingStep step,
            HorseTrainingFailureReason failureReason,
            int treatMarkersCleared,
            int requiredTreatMarkers,
            int jumpRailsCleared,
            int requiredJumpRails,
            int slalomGatesCleared,
            int requiredSlalomGates,
            float balance,
            float maxBalance)
        {
            Step = step;
            FailureReason = failureReason;
            TreatMarkersCleared = treatMarkersCleared;
            RequiredTreatMarkers = requiredTreatMarkers;
            JumpRailsCleared = jumpRailsCleared;
            RequiredJumpRails = requiredJumpRails;
            SlalomGatesCleared = slalomGatesCleared;
            RequiredSlalomGates = requiredSlalomGates;
            Balance = balance;
            MaxBalance = maxBalance;
        }

        public HorseTrainingStep Step { get; }
        public HorseTrainingFailureReason FailureReason { get; }
        public int TreatMarkersCleared { get; }
        public int RequiredTreatMarkers { get; }
        public int JumpRailsCleared { get; }
        public int RequiredJumpRails { get; }
        public int SlalomGatesCleared { get; }
        public int RequiredSlalomGates { get; }
        public float Balance { get; }
        public float MaxBalance { get; }

        public bool IsComplete => Step == HorseTrainingStep.Success || Step == HorseTrainingStep.Failure;
        public float BalanceNormalized => MaxBalance <= 0f ? 0f : Balance / MaxBalance;
    }
}
