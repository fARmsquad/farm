using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Farming
{
    public sealed class CropInventory
    {
        private readonly Dictionary<string, int> _items = new(StringComparer.Ordinal);

        public void AddItem(string itemId, int quantity)
        {
            ValidateItemId(itemId);
            ValidateQuantity(quantity);

            _items.TryGetValue(itemId, out var existingCount);
            _items[itemId] = existingCount + quantity;
        }

        public int GetCount(string itemId)
        {
            ValidateItemId(itemId);
            return _items.TryGetValue(itemId, out var count) ? count : 0;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            ValidateItemId(itemId);
            ValidateQuantity(quantity);
            return GetCount(itemId) >= quantity;
        }

        public bool TryRemoveItem(string itemId, int quantity)
        {
            ValidateItemId(itemId);
            ValidateQuantity(quantity);

            if (!HasItem(itemId, quantity))
                return false;

            var remaining = _items[itemId] - quantity;
            if (remaining == 0)
            {
                _items.Remove(itemId);
                return true;
            }

            _items[itemId] = remaining;
            return true;
        }

        private static void ValidateItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        private static void ValidateQuantity(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity));
        }
    }
}
