using System;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Pure C# state tracker for the watering can's water level.
    /// Owned by FarmSimDriver; UI controllers observe via <see cref="OnWaterLevelChanged"/>.
    /// </summary>
    public sealed class WateringCanState
    {
        /// <summary>Normalized maximum water level.</summary>
        public const float MaxWater = 1.0f;

        /// <summary>Amount of water drained per single use (20% = 5 pours per full can).</summary>
        public const float DrainPerUse = 0.2f;

        /// <summary>Current water level as 0.0-1.0.</summary>
        public float WaterLevel { get; private set; }

        /// <summary>True when the can has no water remaining.</summary>
        public bool IsEmpty => WaterLevel <= 0f;

        /// <summary>True when the can is completely full.</summary>
        public bool IsFull => WaterLevel >= MaxWater;

        /// <summary>Fired with the new water level after any change.</summary>
        public event Action<float> OnWaterLevelChanged;

        /// <summary>
        /// Creates a new watering can state. The can starts empty — the player must visit the well first.
        /// </summary>
        public WateringCanState()
        {
            WaterLevel = 0f;
        }

        /// <summary>
        /// Attempts to drain the specified amount from the can.
        /// Returns false if the can is already empty. <paramref name="actualDrained"/> is the clamped amount removed.
        /// </summary>
        public bool TryDrain(float amount, out float actualDrained)
        {
            if (amount <= 0f || WaterLevel <= 0f)
            {
                actualDrained = 0f;
                return WaterLevel > 0f;
            }

            actualDrained = Math.Min(amount, WaterLevel);
            WaterLevel = Math.Max(0f, WaterLevel - actualDrained);
            OnWaterLevelChanged?.Invoke(WaterLevel);
            return true;
        }

        /// <summary>
        /// Adds water to the can, clamped to <see cref="MaxWater"/>. Negative amounts are ignored.
        /// </summary>
        public void Fill(float amount)
        {
            if (amount <= 0f)
                return;

            WaterLevel = Math.Min(MaxWater, WaterLevel + amount);
            OnWaterLevelChanged?.Invoke(WaterLevel);
        }

        /// <summary>
        /// Fills the can to maximum capacity.
        /// </summary>
        public void FillToMax()
        {
            WaterLevel = MaxWater;
            OnWaterLevelChanged?.Invoke(WaterLevel);
        }
    }
}
