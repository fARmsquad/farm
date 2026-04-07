namespace FarmSimVR.Core.Hunting
{
    public readonly struct CaughtAnimalRecord
    {
        public AnimalType Type { get; }
        public float CatchTime { get; }

        public CaughtAnimalRecord(AnimalType type, float catchTime)
        {
            Type = type;
            CatchTime = catchTime;
        }
    }
}
