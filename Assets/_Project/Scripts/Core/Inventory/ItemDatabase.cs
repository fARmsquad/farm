using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Inventory
{
    /// <summary>
    /// Registry of all item definitions. Built once at startup and treated as immutable.
    /// </summary>
    public sealed class ItemDatabase : IItemDatabase
    {
        private readonly Dictionary<string, ItemData> _items =
            new Dictionary<string, ItemData>(StringComparer.Ordinal);

        public ItemDatabase(IEnumerable<ItemData> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                if (item == null)
                    throw new ArgumentException("ItemDatabase received a null ItemData entry.");
                if (_items.ContainsKey(item.ItemId))
                    throw new ArgumentException($"Duplicate item id: '{item.ItemId}'.");
                _items[item.ItemId] = item;
            }
        }

        public ItemData GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("itemId must not be null or empty.", nameof(itemId));

            if (!_items.TryGetValue(itemId, out var data))
                throw new ArgumentException($"Unknown item id: '{itemId}'.", nameof(itemId));

            return data;
        }

        public bool Exists(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            return _items.ContainsKey(itemId);
        }

        /// <summary>
        /// Creates a pre-seeded database containing the three starter crops
        /// (tomato, carrot, lettuce) and a watering can tool.
        /// </summary>
        public static ItemDatabase CreateStarterDatabase()
        {
            var items = new[]
            {
                // Seeds
                new ItemData("seed_tomato",   "Tomato Seed",  ItemCategory.Seed, 20, 5,  "A tomato seed. Medium growth, clear harvest payoff."),
                new ItemData("seed_carrot",   "Carrot Seed",  ItemCategory.Seed, 20, 3,  "A carrot seed. Fast and forgiving."),
                new ItemData("seed_lettuce",  "Lettuce Seed", ItemCategory.Seed, 20, 2,  "A lettuce seed. Very fast, great for early satisfaction."),

                // Harvested crops
                new ItemData("crop_tomato",   "Tomato",       ItemCategory.Crop, 10, 15, "A ripe tomato."),
                new ItemData("crop_carrot",   "Carrot",       ItemCategory.Crop, 10, 10, "A fresh carrot."),
                new ItemData("crop_lettuce",  "Lettuce",      ItemCategory.Crop, 10, 8,  "A crisp lettuce head."),

                // Tools (stack of 1 each)
                new ItemData("tool_watering_can", "Watering Can", ItemCategory.Tool, 1, 0, "Waters crop plots to boost growth."),
                new ItemData("tool_basket",        "Harvest Basket", ItemCategory.Tool, 1, 0, "Used to collect harvested crops."),
            };

            return new ItemDatabase(items);
        }
    }
}
