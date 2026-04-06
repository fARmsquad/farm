namespace FarmSimVR.Core.Farming
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
