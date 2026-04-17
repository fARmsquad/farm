using FarmSimVR.Core.Hunting;

namespace FarmSimVR.Core.Economy
{
    public static class LivestockPrices
    {
        public static int Get(AnimalType type) => type switch
        {
            AnimalType.Chicken => 45,
            AnimalType.Pig     => 45,
            AnimalType.Horse   => 75,
            _                  => 50,
        };
    }
}
