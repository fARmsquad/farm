namespace FarmSimVR.Core.Hunting
{
    public sealed class PenGameProgressionState
    {
        public int Experience { get; private set; }
        public int Level { get; private set; } = 1;
        public int SkillPoints { get; private set; }
        public int AnimalHandlingRank { get; private set; }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
                return;

            Experience += amount;
            while (Experience >= RequiredExperienceForNextLevel())
            {
                Experience -= RequiredExperienceForNextLevel();
                Level += 1;
                SkillPoints += 1;
            }
        }

        public bool TrySpendAnimalHandlingPoint()
        {
            if (SkillPoints <= 0 || AnimalHandlingRank >= 5)
                return false;

            SkillPoints -= 1;
            AnimalHandlingRank += 1;
            return true;
        }

        public PenGameProgressionSnapshot CreateSnapshot()
        {
            return new PenGameProgressionSnapshot
            {
                Experience = Experience,
                Level = Level,
                SkillPoints = SkillPoints,
                AnimalHandlingRank = AnimalHandlingRank,
            };
        }

        public void Restore(PenGameProgressionSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            Experience = snapshot.Experience < 0 ? 0 : snapshot.Experience;
            Level = snapshot.Level <= 0 ? 1 : snapshot.Level;
            SkillPoints = snapshot.SkillPoints < 0 ? 0 : snapshot.SkillPoints;
            AnimalHandlingRank = Clamp(snapshot.AnimalHandlingRank, 0, 5);
        }

        private static int RequiredExperienceForNextLevel() => 100;

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;

            return value > max ? max : value;
        }
    }
}
