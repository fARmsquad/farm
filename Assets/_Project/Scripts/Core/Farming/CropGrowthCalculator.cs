using System;
using FarmSimVR.Interfaces;

namespace FarmSimVR.Core.Farming
{
    public class CropGrowthCalculator : ICropGrowthCalculator
    {
        public GrowthResult CalculateGrowth(
            CropData cropData,
            GrowthConditions conditions,
            float currentGrowth,
            float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentException("deltaTime cannot be negative", nameof(deltaTime));

            if (currentGrowth >= cropData.MaxGrowth)
                return new GrowthResult(0f, true);

            float rate = cropData.BaseGrowthRate;

            // Rain doubles growth
            if (conditions.Weather == WeatherType.Rain)
                rate *= 2f;

            // Temperature outside 10-35 range halves growth
            if (conditions.Temperature < 10f || conditions.Temperature > 35f)
                rate *= 0.5f;

            // Soil quality multiplier
            rate *= GetSoilMultiplier(conditions.SoilQuality);

            float growth = rate * deltaTime;

            // Cap growth at max
            float remainingCapacity = cropData.MaxGrowth - currentGrowth;
            if (growth > remainingCapacity)
                growth = remainingCapacity;

            bool isFullyGrown = (currentGrowth + growth) >= cropData.MaxGrowth;

            return new GrowthResult(growth, isFullyGrown);
        }

        private static float GetSoilMultiplier(SoilQuality quality)
        {
            return quality switch
            {
                SoilQuality.Poor => 0.5f,
                SoilQuality.Normal => 1.0f,
                SoilQuality.Rich => 1.5f,
                _ => 1.0f
            };
        }
    }
}
