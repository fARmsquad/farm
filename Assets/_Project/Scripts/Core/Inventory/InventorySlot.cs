namespace FarmSimVR.Core.Inventory
{
    /// <summary>
    /// A single inventory slot holding a quantity of one item type.
    /// Managed exclusively by InventorySystem.
    /// </summary>
    public sealed class InventorySlot
    {
        public string ItemId { get; private set; }
        public int Quantity { get; private set; }
        public int MaxStack { get; private set; }

        public bool IsEmpty => string.IsNullOrEmpty(ItemId);
        public bool IsFull => !IsEmpty && Quantity >= MaxStack;
        public int RemainingCapacity => IsEmpty ? 0 : MaxStack - Quantity;

        public InventorySlot() { }

        /// <summary>
        /// Assign an item type to an empty slot and add an initial quantity.
        /// Returns the amount that could not fit (overflow).
        /// Intended to be called only by InventorySystem.
        /// </summary>
        public int Assign(string itemId, int maxStack, int quantity)
        {
            ItemId = itemId;
            MaxStack = maxStack;
            Quantity = 0;
            return Add(quantity);
        }

        /// <summary>
        /// Add quantity to an already-assigned slot. Returns overflow.
        /// Intended to be called only by InventorySystem.
        /// </summary>
        public int Add(int quantity)
        {
            int canFit = MaxStack - Quantity;
            int added = quantity < canFit ? quantity : canFit;
            Quantity += added;
            return quantity - added;
        }

        /// <summary>
        /// Remove quantity from this slot. Returns false if insufficient (slot unchanged).
        /// Intended to be called only by InventorySystem.
        /// </summary>
        public bool Remove(int quantity)
        {
            if (Quantity < quantity)
                return false;

            Quantity -= quantity;
            if (Quantity == 0)
                Clear();

            return true;
        }

        public void Clear()
        {
            ItemId = null;
            Quantity = 0;
            MaxStack = 0;
        }
    }
}
