namespace FarmSimVR.Core.Inventory
{
    /// <summary>Result returned by IInventorySystem.AddItem.</summary>
    public sealed class AddResult
    {
        /// <summary>True if at least some quantity was added.</summary>
        public bool Success { get; }

        /// <summary>Quantity that could not fit into the inventory.</summary>
        public int Overflow { get; }

        public AddResult(bool success, int overflow)
        {
            Success = success;
            Overflow = overflow < 0 ? 0 : overflow;
        }

        public static AddResult Ok() => new AddResult(true, 0);
        public static AddResult Partial(int overflow) => new AddResult(true, overflow);
        public static AddResult Full(int overflow) => new AddResult(false, overflow);
    }
}
