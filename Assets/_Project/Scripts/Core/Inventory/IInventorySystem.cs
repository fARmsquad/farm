using System.Collections.Generic;

namespace FarmSimVR.Core.Inventory
{
    public interface IInventorySystem
    {
        /// <summary>Attempt to add quantity of itemId. Returns result with any overflow.</summary>
        AddResult AddItem(string itemId, int quantity);

        /// <summary>Attempt to remove quantity of itemId. Returns result with failure reason.</summary>
        RemoveResult RemoveItem(string itemId, int quantity);

        /// <summary>True if at least quantity of itemId is present.</summary>
        bool HasItem(string itemId, int quantity = 1);

        /// <summary>Total count of itemId across all slots.</summary>
        int GetCount(string itemId);

        IReadOnlyList<InventorySlot> Slots { get; }
    }
}
