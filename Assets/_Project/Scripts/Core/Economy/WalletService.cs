using System;

namespace FarmSimVR.Core.Economy
{
    /// <summary>
    /// Tracks the player's Chicken Coin balance. Earn-only for now.
    /// Zero Unity dependencies — pure C# ledger.
    /// </summary>
    public class WalletService
    {
        public int Balance { get; private set; }

        public event Action<int> OnBalanceChanged;

        public void AddCoins(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0.");

            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0.");

            if (Balance < amount)
                return false;

            Balance -= amount;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }
    }
}
