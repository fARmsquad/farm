using System.Collections.Generic;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Manages the state of all crop plots on the farm.
    /// This is the authoritative hub that planting, watering, growth, and harvesting
    /// all read from and write to.
    /// </summary>
    public interface ISoilManager
    {
        /// <summary>Returns the SoilState for plotId. Throws if not found.</summary>
        SoilState GetPlot(string plotId);

        /// <summary>All registered plots in registration order.</summary>
        IReadOnlyList<SoilState> AllPlots { get; }

        /// <summary>
        /// Place a crop seed in an Empty plot.
        /// Returns true on success, false if the plot is not Empty or does not exist.
        /// </summary>
        bool Plant(string plotId, string cropId);

        /// <summary>Add moisture to a plot, clamped at 1.0.</summary>
        void Water(string plotId, float amount);

        /// <summary>
        /// Harvest a Harvestable plot: depletes nutrients, clears the crop id,
        /// and transitions to Empty (or Depleted if nutrients reach 0).
        /// Throws if the plot is not in Harvestable status.
        /// </summary>
        void Harvest(string plotId);

        /// <summary>Restore nutrients to any plot, clamped at 1.0.</summary>
        void Compost(string plotId, float amount);

        /// <summary>
        /// Called once per simulation frame.
        /// Decays moisture on all plots and advances Planted → Growing.
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Signal from the growth system (Block 5) that a Growing plot has reached
        /// full maturity and is ready to harvest.
        /// No-ops if the plot is not in Growing status.
        /// </summary>
        void MarkHarvestable(string plotId);

        /// <summary>
        /// Signal from the growth system that a plot's crop has died from drought.
        /// Clears the crop id and sets status back to Empty so it can be replanted.
        /// </summary>
        void MarkDead(string plotId);

        /// <summary>Remove a dead plant from the plot, returning it to Empty status.</summary>
        void ClearDead(string plotId);
    }
}
