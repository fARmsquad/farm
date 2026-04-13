using System;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Mutable per-plot soil state. Read freely; mutate only through SoilManager.
    /// Owns: status, moisture, nutrients, soil type, and current crop id.
    /// Does NOT own growth progress — that belongs to Block 5 (crop growth system).
    /// </summary>
    public sealed class SoilState
    {
        // ── Identity ─────────────────────────────────────────────────────────

        public string PlotId { get; }
        public SoilType Type { get; }

        // ── Status ───────────────────────────────────────────────────────────

        public PlotStatus Status { get; private set; }

        public string CurrentCropId { get; private set; }

        // ── Resources ────────────────────────────────────────────────────────

        /// <summary>Current moisture level, 0-1. Decays over time; increased by watering.</summary>
        public float Moisture { get; private set; }

        /// <summary>Current nutrient level, 0-1. Depleted per harvest; restored by composting.</summary>
        public float Nutrients { get; private set; }

        // ── Soil type configuration (read-only after construction) ────────────

        public float MoistureDecayRate { get; }
        public float NutrientDepletionPerHarvest { get; }

        /// <summary>
        /// Base growth multiplier for this soil type. When soil is Depleted, returns a
        /// reduced penalty value instead. Block 5 will factor this into growth calculations.
        /// </summary>
        public float GrowthMultiplier =>
            Status == PlotStatus.Depleted ? _defaults.GrowthMultiplier * DepletedGrowthPenalty
                                          : _defaults.GrowthMultiplier;

        private const float DepletedGrowthPenalty = 0.5f;

        private readonly SoilTypeDefaults _defaults;

        // ── Construction ─────────────────────────────────────────────────────

        public SoilState(string plotId, SoilType type)
        {
            if (string.IsNullOrWhiteSpace(plotId))
                throw new ArgumentException("plotId must not be null or empty.", nameof(plotId));

            PlotId = plotId;
            Type = type;
            _defaults = SoilTypeDefaults.For(type);

            MoistureDecayRate = _defaults.MoistureDecayRate;
            NutrientDepletionPerHarvest = _defaults.NutrientDepletionPerHarvest;

            Moisture = _defaults.InitialMoisture;
            Nutrients = _defaults.InitialNutrients;
            Status = PlotStatus.Untilled;
        }

        // ── Mutations — intended for SoilManager use only ──────────────────────

        /// <summary>
        /// Transitions from Untilled to Empty (tilled). No-op if already tilled.
        /// </summary>
        public bool Till()
        {
            if (Status != PlotStatus.Untilled)
                return false;

            Status = PlotStatus.Empty;
            return true;
        }

        public void SetStatus(PlotStatus status) => Status = status;

        public void SetCropId(string cropId) => CurrentCropId = cropId;

        public void AddMoisture(float amount)
        {
            Moisture = Clamp01(Moisture + amount);
        }

        public void DeductMoisture(float amount)
        {
            Moisture = Clamp01(Moisture - amount);
        }

        public void AddNutrients(float amount)
        {
            Nutrients = Clamp01(Nutrients + amount);
        }

        public void DeductNutrients(float amount)
        {
            Nutrients = Clamp01(Nutrients - amount);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
