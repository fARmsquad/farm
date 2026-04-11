namespace FarmSimVR.Core.Farming
{
    [System.Serializable]
    public sealed class FarmProgressionSnapshot
    {
        public int Coins;
        public int Experience;
        public int Level = 1;
        public int SkillPoints;
        public int WateringCanTier = 1;
        public int ExpansionLevel;
        public int GreenThumbRank;
        public int MerchantRank;
        public int RainTenderRank;
    }
}
