namespace FarmSimVR.Core.Inventory
{
    /// <summary>Result returned by IInventorySystem.RemoveItem.</summary>
    public sealed class RemoveResult
    {
        public bool Success { get; }
        public string FailReason { get; }

        public RemoveResult(bool success, string failReason = null)
        {
            Success = success;
            FailReason = failReason ?? string.Empty;
        }

        public static RemoveResult Ok() => new RemoveResult(true);
        public static RemoveResult Fail(string reason) => new RemoveResult(false, reason);
    }
}
