using System;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.Core.Inventory
{
    /// <summary>
    /// Pure C# state tracker for the currently equipped tool.
    /// Owned by FarmSimDriver; UI controllers observe via <see cref="OnToolChanged"/>.
    /// </summary>
    public sealed class ToolEquipState
    {
        /// <summary>The currently equipped tool type.</summary>
        public FarmToolId EquippedTool { get; private set; } = FarmToolId.None;

        /// <summary>The hotbar slot index (0-4) that is currently selected, or -1 if none.</summary>
        public int EquippedHotbarSlot { get; private set; } = -1;

        /// <summary>Fired when the equipped tool changes.</summary>
        public event Action<FarmToolId> OnToolChanged;

        /// <summary>
        /// Equips the item in the given hotbar slot. If the slot is empty or holds a non-tool item,
        /// the equipped tool is set to <see cref="FarmToolId.None"/>.
        /// </summary>
        public void EquipSlot(int slotIndex, IInventorySystem inventory)
        {
            if (inventory == null)
                return;

            const int hotbarSlotCount = 5;
            if (slotIndex < 0 || slotIndex >= hotbarSlotCount)
                return;

            EquippedHotbarSlot = slotIndex;

            var slots = inventory.Slots;
            if (slotIndex >= slots.Count)
            {
                SetTool(FarmToolId.None);
                return;
            }

            var slot = slots[slotIndex];
            if (slot.IsEmpty)
            {
                SetTool(FarmToolId.None);
                return;
            }

            var toolId = FarmToolMap.FromItemId(slot.ItemId);
            SetTool(toolId);
        }

        private void SetTool(FarmToolId newTool)
        {
            if (newTool == EquippedTool)
                return;

            EquippedTool = newTool;
            OnToolChanged?.Invoke(newTool);
        }
    }
}
