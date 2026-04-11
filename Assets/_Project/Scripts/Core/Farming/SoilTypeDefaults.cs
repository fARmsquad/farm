namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Immutable configuration values for a given SoilType.
    /// All rate values are expressed per real second; callers apply a time scale.
    /// </summary>
    public sealed class SoilTypeDefaults
    {
        /// <summary>Starting moisture level (0-1).</summary>
        public float InitialMoisture { get; }

        /// <summary>Starting nutrient level (0-1).</summary>
        public float InitialNutrients { get; }

        /// <summary>Moisture lost per second when not watered.</summary>
        public float MoistureDecayRate { get; }

        /// <summary>Nutrient fraction removed per harvest.</summary>
        public float NutrientDepletionPerHarvest { get; }

        /// <summary>
        /// Base growth multiplier applied to crop growth calculations.
        /// Sandy=0.7, Loam=1.0, Clay=0.8, Rich=1.3
        /// </summary>
        public float GrowthMultiplier { get; }

        public SoilTypeDefaults(
            float initialMoisture,
            float initialNutrients,
            float moistureDecayRate,
            float nutrientDepletionPerHarvest,
            float growthMultiplier)
        {
            InitialMoisture = Clamp01(initialMoisture);
            InitialNutrients = Clamp01(initialNutrients);
            MoistureDecayRate = moistureDecayRate >= 0f ? moistureDecayRate : 0f;
            NutrientDepletionPerHarvest = Clamp01(nutrientDepletionPerHarvest);
            GrowthMultiplier = growthMultiplier > 0f ? growthMultiplier : 0.1f;
        }

        /// <summary>Returns the canonical defaults for the given SoilType.</summary>
        public static SoilTypeDefaults For(SoilType type)
        {
            return type switch
            {
                SoilType.Sandy => new SoilTypeDefaults(
                    initialMoisture: 0.30f,
                    initialNutrients: 0.60f,
                    moistureDecayRate: 0.020f,
                    nutrientDepletionPerHarvest: 0.25f,
                    growthMultiplier: 0.70f),

                SoilType.Loam => new SoilTypeDefaults(
                    initialMoisture: 0.60f,
                    initialNutrients: 0.80f,
                    moistureDecayRate: 0.010f,
                    nutrientDepletionPerHarvest: 0.20f,
                    growthMultiplier: 1.00f),

                SoilType.Clay => new SoilTypeDefaults(
                    initialMoisture: 0.70f,
                    initialNutrients: 0.70f,
                    moistureDecayRate: 0.008f,
                    nutrientDepletionPerHarvest: 0.20f,
                    growthMultiplier: 0.80f),

                SoilType.Rich => new SoilTypeDefaults(
                    initialMoisture: 0.70f,
                    initialNutrients: 1.00f,
                    moistureDecayRate: 0.005f,
                    nutrientDepletionPerHarvest: 0.15f,
                    growthMultiplier: 1.30f),

                _ => new SoilTypeDefaults(0.50f, 0.70f, 0.010f, 0.20f, 1.00f)
            };
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
