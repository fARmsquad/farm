using System;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Identifies a farm tool type. Maps 1:1 to tool item IDs in the inventory.
    /// </summary>
    public enum FarmToolId
    {
        None = 0,
        Hoe = 1,
        WateringCan = 2,
        SeedPouch = 3,
        HarvestBasket = 4
    }

    /// <summary>
    /// Utility class for mapping between <see cref="FarmToolId"/>, item IDs, and required farming actions.
    /// </summary>
    public static class FarmToolMap
    {
        private const string ItemIdHoe = "tool_hoe";
        private const string ItemIdWateringCan = "tool_watering_can";
        private const string ItemIdSeedPouch = "tool_seed_pouch";
        private const string ItemIdHarvestBasket = "tool_basket";

        /// <summary>
        /// Resolves an inventory item ID string to its corresponding <see cref="FarmToolId"/>.
        /// Returns <see cref="FarmToolId.None"/> for non-tool items.
        /// </summary>
        public static FarmToolId FromItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return FarmToolId.None;

            return itemId switch
            {
                ItemIdHoe => FarmToolId.Hoe,
                ItemIdWateringCan => FarmToolId.WateringCan,
                ItemIdSeedPouch => FarmToolId.SeedPouch,
                ItemIdHarvestBasket => FarmToolId.HarvestBasket,
                _ => FarmToolId.None
            };
        }

        /// <summary>
        /// Returns which tool is required to perform the given farming action.
        /// Returns <see cref="FarmToolId.None"/> for actions that don't require a specific tool.
        /// </summary>
        public static FarmToolId RequiredToolFor(FarmPlotAction action)
        {
            return action switch
            {
                FarmPlotAction.Till => FarmToolId.Hoe,
                FarmPlotAction.Water => FarmToolId.WateringCan,
                FarmPlotAction.PlantSelected => FarmToolId.SeedPouch,
                FarmPlotAction.PlantTomato => FarmToolId.SeedPouch,
                FarmPlotAction.PlantCarrot => FarmToolId.SeedPouch,
                FarmPlotAction.PlantLettuce => FarmToolId.SeedPouch,
                FarmPlotAction.Harvest => FarmToolId.HarvestBasket,
                _ => FarmToolId.None
            };
        }

        /// <summary>
        /// Converts a <see cref="FarmToolId"/> back to its inventory item ID string.
        /// Returns null for <see cref="FarmToolId.None"/>.
        /// </summary>
        public static string ToItemId(FarmToolId toolId)
        {
            return toolId switch
            {
                FarmToolId.Hoe => ItemIdHoe,
                FarmToolId.WateringCan => ItemIdWateringCan,
                FarmToolId.SeedPouch => ItemIdSeedPouch,
                FarmToolId.HarvestBasket => ItemIdHarvestBasket,
                _ => null
            };
        }
    }
}
