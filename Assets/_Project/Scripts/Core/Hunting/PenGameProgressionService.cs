namespace FarmSimVR.Core.Hunting
{
    public sealed class PenGameProgressionService
    {
        public PenGameProgressionState State { get; }

        public PenGameProgressionService()
            : this(new PenGameProgressionState())
        {
        }

        public PenGameProgressionService(PenGameProgressionState state)
        {
            State = state ?? new PenGameProgressionState();
        }

        public PenDepositReward ApplyDeposits(int animalCount)
        {
            if (animalCount <= 0)
                return PenDepositReward.Empty;

            var experience = animalCount * 20;
            var levelBefore = State.Level;
            State.AddExperience(experience);
            return new PenDepositReward(experience, State.Level - levelBefore);
        }

        public bool TrySpendAnimalHandlingPoint() => State.TrySpendAnimalHandlingPoint();

        public void GrantDebugExperience(int amount) => State.AddExperience(amount);

        public float GetCatchRadiusMultiplier() => 1f + (State.AnimalHandlingRank * 0.1f);

        public float GetFleeSpeedMultiplier() => 1f - (State.AnimalHandlingRank * 0.05f);

        public PenGameProgressionSnapshot CreateSnapshot() => State.CreateSnapshot();

        public void Restore(PenGameProgressionSnapshot snapshot) => State.Restore(snapshot);
    }

    public readonly struct PenDepositReward
    {
        public static PenDepositReward Empty => new PenDepositReward(0, 0);

        public int ExperienceEarned { get; }
        public int LevelsGained { get; }

        public PenDepositReward(int experienceEarned, int levelsGained)
        {
            ExperienceEarned = experienceEarned;
            LevelsGained = levelsGained;
        }
    }
}
