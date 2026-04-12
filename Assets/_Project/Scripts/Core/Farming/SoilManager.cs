using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Concrete implementation of ISoilManager.
    /// Manages per-plot soil state: moisture decay, nutrient depletion, and
    /// the status state machine that planting, watering, growth, and harvesting share.
    ///
    /// Zero Unity dependencies — pure C# simulation logic.
    /// </summary>
    public sealed class SoilManager : ISoilManager
    {
        private readonly Dictionary<string, SoilState> _plotsById;
        private readonly List<SoilState> _plotsOrdered;

        public IReadOnlyList<SoilState> AllPlots => _plotsOrdered;

        // ── Construction ─────────────────────────────────────────────────────

        public SoilManager()
        {
            _plotsById = new Dictionary<string, SoilState>(StringComparer.Ordinal);
            _plotsOrdered = new List<SoilState>();
        }

        /// <summary>
        /// Register a new plot. Must be called before any operation on that plotId.
        /// Throws if the plotId is already registered.
        /// </summary>
        public void AddPlot(string plotId, SoilType type = SoilType.Loam)
        {
            if (string.IsNullOrWhiteSpace(plotId))
                throw new ArgumentException("plotId must not be null or empty.", nameof(plotId));
            if (_plotsById.ContainsKey(plotId))
                throw new ArgumentException($"Plot '{plotId}' is already registered.", nameof(plotId));

            var state = new SoilState(plotId, type);
            _plotsById[plotId] = state;
            _plotsOrdered.Add(state);
        }

        // ── ISoilManager ─────────────────────────────────────────────────────

        public SoilState GetPlot(string plotId)
        {
            RequirePlot(plotId, out var state);
            return state;
        }

        // ── Plant ─────────────────────────────────────────────────────────────

        public bool Plant(string plotId, string cropId)
        {
            if (!_plotsById.TryGetValue(plotId, out var state))
                return false;
            if (string.IsNullOrWhiteSpace(cropId))
                return false;
            if (state.Status != PlotStatus.Empty)
                return false;

            state.SetCropId(cropId);
            state.SetStatus(PlotStatus.Planted);
            return true;
        }

        // ── Water ─────────────────────────────────────────────────────────────

        public void Water(string plotId, float amount)
        {
            if (!_plotsById.TryGetValue(plotId, out var state))
                return;
            if (amount <= 0f)
                return;

            state.AddMoisture(amount);
        }

        // ── Harvest ───────────────────────────────────────────────────────────

        public void Harvest(string plotId)
        {
            RequirePlot(plotId, out var state);

            if (state.Status != PlotStatus.Harvestable)
                throw new InvalidOperationException(
                    $"Cannot harvest plot '{plotId}': status is {state.Status}, expected Harvestable.");

            state.DeductNutrients(state.NutrientDepletionPerHarvest);
            state.SetCropId(null);

            // If nutrients are exhausted, the plot becomes Depleted; otherwise Empty.
            state.SetStatus(state.Nutrients <= 0f ? PlotStatus.Depleted : PlotStatus.Empty);
        }

        // ── Compost ───────────────────────────────────────────────────────────

        public void Compost(string plotId, float amount)
        {
            if (!_plotsById.TryGetValue(plotId, out var state))
                return;
            if (amount <= 0f)
                return;

            state.AddNutrients(amount);

            // Restore Depleted → Empty once nutrients are above zero again.
            if (state.Status == PlotStatus.Depleted && state.Nutrients > 0f)
                state.SetStatus(PlotStatus.Empty);
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentException("deltaTime cannot be negative.", nameof(deltaTime));
            if (deltaTime == 0f)
                return;

            foreach (var state in _plotsOrdered)
            {
                // Decay moisture on all active plots (not Depleted/Empty that have no crop).
                // Decay applies to every plot — even empty soil dries out.
                state.DeductMoisture(state.MoistureDecayRate * deltaTime);

                // Planted is a transient state — advance to Growing on the first Tick.
                if (state.Status == PlotStatus.Planted)
                    state.SetStatus(PlotStatus.Growing);
            }
        }

        // ── MarkHarvestable (called by Block 5 growth system) ─────────────────

        public void MarkHarvestable(string plotId)
        {
            if (!_plotsById.TryGetValue(plotId, out var state))
                return;
            if (state.Status != PlotStatus.Growing)
                return;

            state.SetStatus(PlotStatus.Harvestable);
        }

        // ── MarkDead / ClearDead ──────────────────────────────────────────────

        public void MarkDead(string plotId)
        {
            if (!_plotsById.TryGetValue(plotId, out var state))
                return;
            // Mark as dead: keep crop id visible but flag as non-growing
            state.SetStatus(PlotStatus.Dead);
        }

        public void ClearDead(string plotId)
        {
            if (!_plotsById.TryGetValue(plotId, out var state))
                return;
            if (state.Status != PlotStatus.Dead)
                return;

            state.DeductNutrients(state.NutrientDepletionPerHarvest * 0.5f);
            state.SetCropId(null);
            state.SetStatus(state.Nutrients <= 0f ? PlotStatus.Depleted : PlotStatus.Empty);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RequirePlot(string plotId, out SoilState state)
        {
            if (string.IsNullOrWhiteSpace(plotId))
                throw new ArgumentException("plotId must not be null or empty.", nameof(plotId));
            if (!_plotsById.TryGetValue(plotId, out state))
                throw new ArgumentException($"Plot '{plotId}' is not registered.", nameof(plotId));
        }
    }
}
