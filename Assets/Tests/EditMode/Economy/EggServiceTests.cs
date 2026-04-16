using NUnit.Framework;
using FarmSimVR.Core.Economy;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class EggServiceTests
    {
        private EggService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new EggService();
        }

        [Test]
        public void GenerateMorningEggs_OneChicken_ReturnsThree()
        {
            int eggs = _service.GenerateMorningEggs(1);
            Assert.AreEqual(3, eggs);
        }

        [Test]
        public void GenerateMorningEggs_ThreeChickens_ReturnsNine()
        {
            int eggs = _service.GenerateMorningEggs(3);
            Assert.AreEqual(9, eggs);
        }

        [Test]
        public void GenerateMorningEggs_ZeroChickens_ReturnsZero()
        {
            int eggs = _service.GenerateMorningEggs(0);
            Assert.AreEqual(0, eggs);
        }

        [Test]
        public void GenerateMorningEggs_NegativeChickens_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => _service.GenerateMorningEggs(-1));
        }

        [Test]
        public void ProduceMorningEggs_AddsEggsToInventory()
        {
            var db = ItemDatabase.CreateStarterDatabase();
            var inventory = new InventorySystem(db);

            _service.ProduceMorningEggs(1, inventory);

            Assert.AreEqual(3, inventory.GetCount("egg"));
        }

        [Test]
        public void ProduceMorningEggs_FullInventory_DoesNotThrow()
        {
            var db = ItemDatabase.CreateStarterDatabase();
            var inventory = new InventorySystem(db, slotCount: 1);
            // Fill the single slot completely
            inventory.AddItem("egg", 24);

            Assert.DoesNotThrow(() => _service.ProduceMorningEggs(1, inventory));
        }

        [Test]
        public void ProduceMorningEggs_NullInventory_ThrowsArgumentNull()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => _service.ProduceMorningEggs(1, null));
        }

        // ── SellAllEggs ──────────────────────────────────────────────────────

        [Test]
        public void SellAllEggs_WithEggs_RemovesAllEggsAndReturnsCoinsEarned()
        {
            var db        = ItemDatabase.CreateStarterDatabase();
            var inventory = new InventorySystem(db);
            var wallet    = new WalletService();
            inventory.AddItem("egg", 3);

            int coins = _service.SellAllEggs(inventory, wallet, pricePerEgg: 5);

            Assert.AreEqual(15, coins);
            Assert.AreEqual(15, wallet.Balance);
            Assert.AreEqual(0,  inventory.GetCount("egg"));
        }

        [Test]
        public void SellAllEggs_WithNoEggs_ReturnsZeroAndWalletUnchanged()
        {
            var db        = ItemDatabase.CreateStarterDatabase();
            var inventory = new InventorySystem(db);
            var wallet    = new WalletService();

            int coins = _service.SellAllEggs(inventory, wallet, pricePerEgg: 5);

            Assert.AreEqual(0, coins);
            Assert.AreEqual(0, wallet.Balance);
        }

        [Test]
        public void SellAllEggs_NullInventory_ThrowsArgumentNull()
        {
            var wallet = new WalletService();
            Assert.Throws<System.ArgumentNullException>(
                () => _service.SellAllEggs(null, wallet, 5));
        }

        [Test]
        public void SellAllEggs_NullWallet_ThrowsArgumentNull()
        {
            var db        = ItemDatabase.CreateStarterDatabase();
            var inventory = new InventorySystem(db);
            Assert.Throws<System.ArgumentNullException>(
                () => _service.SellAllEggs(inventory, null, 5));
        }
    }
}
