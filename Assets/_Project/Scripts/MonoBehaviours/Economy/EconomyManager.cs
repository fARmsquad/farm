using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.Core.Economy;
using FarmSimVR.Core.Inventory;
using FarmSimVR.MonoBehaviours.Farming;

namespace FarmSimVR.MonoBehaviours.Economy
{
    /// <summary>
    /// Central coordinator for the farm economy loop.
    /// Owns the player's persistent inventory (survives scene transitions).
    /// Eggs are produced each morning and sold in town by talking to Mira the Baker.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WalletHUDController walletHUD;

        public const int EggSellPrice = 5;

        public static EconomyManager Instance { get; private set; }

        private EggService _eggService;
        private WalletService _walletService;
        private InventorySystem _inventory;

        public WalletService Wallet       => _walletService;
        public InventorySystem Inventory  => _inventory;

        private void Awake()
        {
            Instance      = this;
            _eggService   = new EggService();
            _walletService = new WalletService();
            _inventory    = new InventorySystem(ItemDatabase.CreateStarterDatabase(), 24);
        }

        private void Start()
        {
            walletHUD?.Initialize(_walletService);

            if (FarmDayClockDriver.Instance?.Clock != null)
                FarmDayClockDriver.Instance.Clock.OnNewDay += HandleNewDay;
        }

        private void OnDestroy()
        {
            if (FarmDayClockDriver.Instance?.Clock != null)
                FarmDayClockDriver.Instance.Clock.OnNewDay -= HandleNewDay;

            if (Instance == this) Instance = null;
        }

        private void HandleNewDay(int dayCount)
        {
            _eggService.ProduceMorningEggs(1, _inventory);
        }

        /// <summary>
        /// Sells every egg in the player's inventory at EggSellPrice coins each.
        /// Returns true and a success message, or false with a reason.
        /// </summary>
        public bool TrySellAllEggs(out string message)
        {
            int eggCount = _inventory.GetCount("egg");

            if (eggCount <= 0)
            {
                message = "You don't have any eggs to sell.";
                return false;
            }

            int coins = _eggService.SellAllEggs(_inventory, _walletService, EggSellPrice);
            message = $"Sold {eggCount} egg{(eggCount == 1 ? "" : "s")} for {coins} Chicken Coins!";
            return true;
        }

#if UNITY_EDITOR
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame)
                FarmDayClockDriver.Instance?.Clock.SkipTo(0.01f);
        }
#endif
    }
}
