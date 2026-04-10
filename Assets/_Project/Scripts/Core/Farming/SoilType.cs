namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Physical soil type for a crop plot. Determines initial moisture, nutrient levels,
    /// decay rates, and the base growth multiplier.
    /// </summary>
    public enum SoilType
    {
        /// <summary>Low water retention, quick drain, below-average growth.</summary>
        Sandy,

        /// <summary>Balanced soil. The baseline for all growth multipliers.</summary>
        Loam,

        /// <summary>High water retention, slower drain, slightly below-average growth.</summary>
        Clay,

        /// <summary>Nutrient-dense, low decay, above-average growth.</summary>
        Rich
    }
}
