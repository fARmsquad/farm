using UnityEngine;
using TMPro;
using FarmSimVR.Core.Economy;

namespace FarmSimVR.MonoBehaviours.Economy
{
    /// <summary>
    /// HUD label showing the player's current Chicken Coin balance.
    /// Call Initialize(wallet) from EconomyManager after the wallet is created.
    /// </summary>
    public class WalletHUDController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI coinLabel;

        public void Initialize(WalletService wallet)
        {
            wallet.OnBalanceChanged += UpdateDisplay;
            UpdateDisplay(wallet.Balance);
        }

        private void UpdateDisplay(int balance)
        {
            if (coinLabel != null)
                coinLabel.text = $"Coins: {balance}";
        }
    }
}
