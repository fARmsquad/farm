using System;

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

            float rate = cropData.BaseGrowthRate
                * GetWeatherMultiplier(conditions.Weather)
                * GetTemperatureMultiplier(conditions.Temperature)
                * GetSoilMultiplier(conditions.SoilQuality)
                * conditions.SeasonMultiplier;

            float growth = rate * deltaTime;

            // Cap growth at max
            float remainingCapacity = cropData.MaxGrowth - currentGrowth;
            if (growth > remainingCapacity)
                growth = remainingCapacity;

            bool isFullyGrown = (currentGrowth + growth) >= cropData.MaxGrowth;

            return new GrowthResult(growth, isFullyGrown);
        }

        private static float GetWeatherMultiplier(WeatherType weather)
        {
            return weather == WeatherType.Rain ? 2.0f : 1.0f;
        }

        private static float GetTemperatureMultiplier(float temperature)
        {
            return (temperature < 10f || temperature > 35f) ? 0.5f : 1.0f;
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
