using System;

namespace FarmSimVR.Core.Farming
{
    public enum WeatherType
    {
        Sunny,
        Cloudy,
        Rain,
        Snow,
        Heatwave,
        Blizzard
    }

    public enum SoilQuality
    {
        Poor,
        Normal,
        Rich
    }

    public enum FarmSeason
    {
        Spring,
        Summer,
        Autumn,
        Winter,
    }

    public readonly struct CropData
    {
        public float BaseGrowthRate { get; }
        public float MaxGrowth { get; }

        public CropData(float baseGrowthRate, float maxGrowth)
        {
            BaseGrowthRate = baseGrowthRate;
            MaxGrowth = maxGrowth;
        }
    }

    public readonly struct GrowthConditions
    {
        public WeatherType Weather { get; }
        public float Temperature { get; }
        public SoilQuality SoilQuality { get; }
        /// <summary>Season suitability multiplier. 1 = ideal, 0.5 = tolerated, 0 = not plantable.</summary>
        public float SeasonMultiplier { get; }

        public GrowthConditions(WeatherType weather, float temperature, SoilQuality soilQuality, float seasonMultiplier = 1f)
        {
            Weather = weather;
            Temperature = temperature;
            SoilQuality = soilQuality;
            SeasonMultiplier = seasonMultiplier;
        }
    }

    public readonly struct GrowthResult
    {
        public float GrowthAmount { get; }
        public bool IsFullyGrown { get; }

        public GrowthResult(float growthAmount, bool isFullyGrown)
        {
            GrowthAmount = growthAmount;
            IsFullyGrown = isFullyGrown;
        }
    }
}
