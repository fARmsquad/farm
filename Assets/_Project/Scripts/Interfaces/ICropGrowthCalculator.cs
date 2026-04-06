using FarmSimVR.Core.Farming;

namespace FarmSimVR.Interfaces
{
    public interface ICropGrowthCalculator
    {
        GrowthResult CalculateGrowth(
            CropData cropData,
            GrowthConditions conditions,
            float currentGrowth,
            float deltaTime);
    }
}
