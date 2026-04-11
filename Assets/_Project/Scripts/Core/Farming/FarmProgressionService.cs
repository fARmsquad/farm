namespace FarmSimVR.Core.Farming
{
    public sealed class FarmProgressionService
    {
        private static readonly int[] WateringUpgradeCosts = { 75, 150, 300 };
        private static readonly int[] ExpansionCosts = { 120, 240, 480 };

        public FarmProgressionState State { get; }

        public FarmProgressionService()
            : this(new FarmProgressionState())
        {
        }

        public FarmProgressionService(FarmProgressionState state)
        {
            State = state ?? new FarmProgressionState();
        }

        public FarmSaleReward ApplySale(int sellValuePerItem, int quantity)
        {
            if (sellValuePerItem <= 0 || quantity <= 0)
                return FarmSaleReward.Empty;

            var gross = sellValuePerItem * quantity;
            var bonus = gross * (GetSaleMultiplier() - 1f);
            var totalCoins = gross + (int)System.MathF.Round(bonus);
            var levelsBefore = State.Level;

            State.AddCoins(totalCoins);
            State.AddExperience(quantity * 12);

            return new FarmSaleReward(totalCoins, State.Level - levelsBefore);
        }

        public void GrantDebugCoins(int amount) => State.AddCoins(amount);

        public void GrantDebugExperience(int amount) => State.AddExperience(amount);

        public bool TryBuyWateringUpgrade()
        {
            var nextIndex = State.WateringCanTier - 1;
            if (nextIndex < 0 || nextIndex >= WateringUpgradeCosts.Length)
                return false;

            var cost = WateringUpgradeCosts[nextIndex];
            return State.SpendCoins(cost) && State.TryUpgradeWateringCan();
        }

        public bool TryUnlockNextExpansion()
        {
            var nextIndex = State.ExpansionLevel;
            if (nextIndex < 0 || nextIndex >= ExpansionCosts.Length)
                return false;

            var cost = ExpansionCosts[nextIndex];
            return State.SpendCoins(cost) && State.TryUnlockExpansion();
        }

        public bool TrySpendSkillPoint(FarmSkillType skill) => State.TrySpendSkillPoint(skill);

        public float GetWateringMultiplier() => 1f + ((State.WateringCanTier - 1) * 0.2f);

        public float GetGrowthMultiplier() => 1f + (State.GreenThumbRank * 0.05f);

        public float GetSaleMultiplier() => 1f + (State.MerchantRank * 0.1f);

        public float GetRainMultiplier() => 1f + (State.RainTenderRank * 0.15f);

        public FarmProgressionSnapshot CreateSnapshot() => State.CreateSnapshot();

        public void Restore(FarmProgressionSnapshot snapshot) => State.Restore(snapshot);

        public int GetNextWateringUpgradeCost()
        {
            var index = State.WateringCanTier - 1;
            if (index < 0 || index >= WateringUpgradeCosts.Length)
                return -1;

            return WateringUpgradeCosts[index];
        }

        public int GetNextExpansionCost()
        {
            var index = State.ExpansionLevel;
            if (index < 0 || index >= ExpansionCosts.Length)
                return -1;

            return ExpansionCosts[index];
        }

        public string GetCurrentExpansionLabel()
        {
            return State.ExpansionLevel switch
            {
                0 => "Starter Farm",
                1 => "Expanded Plots",
                2 => "House Workbench",
                3 => "Coop Upgrade",
                _ => "Starter Farm"
            };
        }
    }

    public readonly struct FarmSaleReward
    {
        public static FarmSaleReward Empty => new FarmSaleReward(0, 0);

        public int CoinsEarned { get; }
        public int LevelsGained { get; }

        public FarmSaleReward(int coinsEarned, int levelsGained)
        {
            CoinsEarned = coinsEarned;
            LevelsGained = levelsGained;
        }
    }
}
