namespace FarmSimVR.Core.Inventory
{
    public interface IItemDatabase
    {
        /// <summary>Returns the ItemData for itemId. Throws ArgumentException if not found.</summary>
        ItemData GetItem(string itemId);

        /// <summary>True if itemId is registered in the database.</summary>
        bool Exists(string itemId);
    }
}
