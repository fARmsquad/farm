namespace FarmSimVR.Core.Inventory
{
    /// <summary>
    /// Immutable definition of a single item type registered in the ItemDatabase.
    /// </summary>
    public sealed class ItemData
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public ItemCategory Category { get; }
        public int MaxStack { get; }
        public int SellValue { get; }
        public string Description { get; }

        public ItemData(
            string itemId,
            string displayName,
            ItemCategory category,
            int maxStack,
            int sellValue,
            string description)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Category = category;
            MaxStack = maxStack > 0 ? maxStack : 1;
            SellValue = sellValue >= 0 ? sellValue : 0;
            Description = description ?? string.Empty;
        }
    }
}
