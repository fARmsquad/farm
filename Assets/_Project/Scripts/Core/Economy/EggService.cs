using System;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.Core.Economy
{
    /// <summary>
    /// Calculates and deposits morning egg production into the player's inventory.
    /// Fixed yield: 3 eggs per chicken. Zero Unity dependencies.
    /// </summary>
    public class EggService
    {
        private const int EggsPerChicken = 3;
        private const string EggItemId   = "egg";

        public int GenerateMorningEggs(int chickenCount)
        {
            if (chickenCount < 0)
                throw new ArgumentOutOfRangeException(nameof(chickenCount), "Chicken count must be >= 0.");
            return chickenCount * EggsPerChicken;
        }

        public void ProduceMorningEggs(int chickenCount, IInventorySystem inventory)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            int eggs = GenerateMorningEggs(chickenCount);
            if (eggs > 0)
                inventory.AddItem(EggItemId, eggs);
        }

        /// <summary>
        /// Removes all eggs from inventory and credits the wallet.
        /// Returns the number of coins earned (0 if no eggs).
        /// </summary>
        public int SellAllEggs(IInventorySystem inventory, WalletService wallet, int pricePerEgg)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));

            int count = inventory.GetCount(EggItemId);
            if (count <= 0) return 0;

            inventory.RemoveItem(EggItemId, count);
            int coins = count * pricePerEgg;
            wallet.AddCoins(coins);
            return coins;
        }
    }
}
