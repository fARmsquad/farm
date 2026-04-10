using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Inventory
{
    /// <summary>
    /// Slot-based inventory with stack limits per item type.
    /// Zero Unity dependencies — pure C# data system.
    /// </summary>
    public sealed class InventorySystem : IInventorySystem
    {
        private readonly IItemDatabase _database;
        private readonly List<InventorySlot> _slots;

        public IReadOnlyList<InventorySlot> Slots => _slots;

        public InventorySystem(IItemDatabase database, int slotCount = 24)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            if (slotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotCount), "Slot count must be positive.");

            _slots = new List<InventorySlot>(slotCount);
            for (int i = 0; i < slotCount; i++)
                _slots.Add(new InventorySlot());
        }

        // ── AddItem ──────────────────────────────────────────────────

        public AddResult AddItem(string itemId, int quantity)
        {
            if (quantity == 0)
                return AddResult.Ok();

            ValidateItemId(itemId);
            if (quantity < 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be >= 0.");

            var data = _database.GetItem(itemId);
            int remaining = quantity;

            // First pass: fill existing slots that hold this item
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty || slot.ItemId != itemId)
                    continue;
                if (slot.IsFull)
                    continue;

                remaining = slot.Add(remaining);
                if (remaining == 0)
                    return AddResult.Ok();
            }

            // Second pass: assign empty slots
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty)
                    continue;

                remaining = slot.Assign(itemId, data.MaxStack, remaining);
                if (remaining == 0)
                    return AddResult.Ok();
            }

            // Still overflow remaining — partial success if some was stored
            bool stored = remaining < quantity;
            return stored ? AddResult.Partial(remaining) : AddResult.Full(remaining);
        }

        // ── RemoveItem ───────────────────────────────────────────────

        public RemoveResult RemoveItem(string itemId, int quantity)
        {
            ValidateItemId(itemId);
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be > 0.");

            if (GetCount(itemId) < quantity)
                return RemoveResult.Fail($"Insufficient '{itemId}': need {quantity}, have {GetCount(itemId)}.");

            int remaining = quantity;
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty || slot.ItemId != itemId)
                    continue;

                int take = remaining <= slot.Quantity ? remaining : slot.Quantity;
                slot.Remove(take);
                remaining -= take;

                if (remaining == 0)
                    break;
            }

            return RemoveResult.Ok();
        }

        // ── Query ────────────────────────────────────────────────────

        public bool HasItem(string itemId, int quantity = 1)
        {
            ValidateItemId(itemId);
            return GetCount(itemId) >= quantity;
        }

        public int GetCount(string itemId)
        {
            ValidateItemId(itemId);
            int total = 0;
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty && slot.ItemId == itemId)
                    total += slot.Quantity;
            }
            return total;
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static void ValidateItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Item id must not be null or empty.", nameof(itemId));
        }
    }
}
