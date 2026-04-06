using System;

namespace FarmSimVR.Core.Farming
{
    public enum WeatherType
    {
        Sunny,
        Cloudy,
        Rain
    }

    public enum SoilQuality
    {
        Poor,
        Normal,
        Rich
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

        public GrowthConditions(WeatherType weather, float temperature, SoilQuality soilQuality)
        {
            Weather = weather;
            Temperature = temperature;
            SoilQuality = soilQuality;
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
