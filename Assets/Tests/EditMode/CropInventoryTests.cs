using NUnit.Framework;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class CropInventoryTests
    {
        private CropInventory _inventory;

        [SetUp]
        public void SetUp()
        {
            _inventory = new CropInventory();
        }

        [Test]
        public void AddItem_NewItem_StoresCount()
        {
            _inventory.AddItem("tomato_seed", 4);
            Assert.AreEqual(4, _inventory.GetCount("tomato_seed"));
        }

        [Test]
        public void AddItem_ExistingItem_IncrementsCount()
        {
            _inventory.AddItem("tomato_seed", 2);
            _inventory.AddItem("tomato_seed", 3);
            Assert.AreEqual(5, _inventory.GetCount("tomato_seed"));
        }

        [Test]
        public void HasItem_WhenEnoughItemsExist_ReturnsTrue()
        {
            _inventory.AddItem("tomato_seed", 2);
            Assert.IsTrue(_inventory.HasItem("tomato_seed", 2));
        }

        [Test]
        public void TryRemoveItem_WhenEnoughItemsExist_ReturnsTrueAndDecrements()
        {
            _inventory.AddItem("tomato_seed", 3);
            var removed = _inventory.TryRemoveItem("tomato_seed", 2);

            Assert.IsTrue(removed);
            Assert.AreEqual(1, _inventory.GetCount("tomato_seed"));
        }

        [Test]
        public void TryRemoveItem_WhenItemsAreInsufficient_ReturnsFalseAndLeavesCount()
        {
            _inventory.AddItem("tomato_seed", 1);
            var removed = _inventory.TryRemoveItem("tomato_seed", 2);

            Assert.IsFalse(removed);
            Assert.AreEqual(1, _inventory.GetCount("tomato_seed"));
        }
    }
}
