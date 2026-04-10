namespace FarmSimVR.Core.Farming
{
    public sealed class FarmProgressionState
    {
        public int Coins { get; private set; }
        public int Experience { get; private set; }
        public int Level { get; private set; } = 1;
        public int SkillPoints { get; private set; }
        public int WateringCanTier { get; private set; } = 1;
        public int ExpansionLevel { get; private set; }
        public int GreenThumbRank { get; private set; }
        public int MerchantRank { get; private set; }
        public int RainTenderRank { get; private set; }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;

            Coins += amount;
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0 || Coins < amount)
                return false;

            Coins -= amount;
            return true;
        }

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

        public bool TryUpgradeWateringCan()
        {
            if (WateringCanTier >= 4)
                return false;

            WateringCanTier += 1;
            return true;
        }

        public bool TryUnlockExpansion()
        {
            if (ExpansionLevel >= 3)
                return false;

            ExpansionLevel += 1;
            return true;
        }

        public bool TrySpendSkillPoint(FarmSkillType skill)
        {
            if (SkillPoints <= 0)
                return false;

            if (!CanIncrease(skill))
                return false;

            SkillPoints -= 1;
            IncreaseSkill(skill);
            return true;
        }

        public FarmProgressionSnapshot CreateSnapshot()
        {
            return new FarmProgressionSnapshot
            {
                Coins = Coins,
                Experience = Experience,
                Level = Level,
                SkillPoints = SkillPoints,
                WateringCanTier = WateringCanTier,
                ExpansionLevel = ExpansionLevel,
                GreenThumbRank = GreenThumbRank,
                MerchantRank = MerchantRank,
                RainTenderRank = RainTenderRank,
            };
        }

        public void Restore(FarmProgressionSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            Coins = snapshot.Coins < 0 ? 0 : snapshot.Coins;
            Experience = snapshot.Experience < 0 ? 0 : snapshot.Experience;
            Level = snapshot.Level <= 0 ? 1 : snapshot.Level;
            SkillPoints = snapshot.SkillPoints < 0 ? 0 : snapshot.SkillPoints;
            WateringCanTier = Clamp(snapshot.WateringCanTier, 1, 4);
            ExpansionLevel = Clamp(snapshot.ExpansionLevel, 0, 3);
            GreenThumbRank = Clamp(snapshot.GreenThumbRank, 0, 3);
            MerchantRank = Clamp(snapshot.MerchantRank, 0, 3);
            RainTenderRank = Clamp(snapshot.RainTenderRank, 0, 3);
        }

        private bool CanIncrease(FarmSkillType skill)
        {
            return skill switch
            {
                FarmSkillType.GreenThumb => GreenThumbRank < 3,
                FarmSkillType.Merchant => MerchantRank < 3,
                FarmSkillType.RainTender => RainTenderRank < 3,
                _ => false
            };
        }

        private void IncreaseSkill(FarmSkillType skill)
        {
            switch (skill)
            {
                case FarmSkillType.GreenThumb:
                    GreenThumbRank += 1;
                    break;
                case FarmSkillType.Merchant:
                    MerchantRank += 1;
                    break;
                case FarmSkillType.RainTender:
                    RainTenderRank += 1;
                    break;
            }
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
