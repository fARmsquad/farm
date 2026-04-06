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
            throw new NotImplementedException();
        }
    }
}
