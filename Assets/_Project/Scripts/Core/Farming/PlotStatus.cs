namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Life-cycle status of a single crop plot.
    /// Transitions: Empty → Planted → Growing → Harvestable → Empty (or Depleted).
    /// Drought path: Growing → Dead → Empty (after ClearDead).
    /// </summary>
    public enum PlotStatus
    {
        /// <summary>No crop present. Ready to receive a seed.</summary>
        Empty,

        /// <summary>Seed placed but not yet actively growing (transient — advances on first Tick).</summary>
        Planted,

        /// <summary>Crop is actively growing toward harvestable.</summary>
        Growing,

        /// <summary>Crop has reached full maturity and is ready to harvest.</summary>
        Harvestable,

        /// <summary>Nutrients have been exhausted. Must be composted before planting again.</summary>
        Depleted,

        /// <summary>Crop died from drought. Must be cleared before replanting.</summary>
        Dead
    }
}
