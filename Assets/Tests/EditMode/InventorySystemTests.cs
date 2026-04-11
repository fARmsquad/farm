using System;
using System.Collections.Generic;
using NUnit.Framework;
using FarmSimVR.Core.Inventory;

namespace FarmSimVR.Tests.EditMode
{
    // ─────────────────────────────────────────────────────────────────────────
    // ItemDatabase Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class ItemDatabaseTests
    {
        private IItemDatabase _db;

        [SetUp]
        public void SetUp()
        {
            _db = ItemDatabase.CreateStarterDatabase();
        }

        [Test]
        public void StarterDatabase_ContainsAllStarterSeeds()
        {
            Assert.IsTrue(_db.Exists("seed_tomato"),  "seed_tomato missing");
            Assert.IsTrue(_db.Exists("seed_carrot"),  "seed_carrot missing");
            Assert.IsTrue(_db.Exists("seed_lettuce"), "seed_lettuce missing");
        }

        [Test]
        public void StarterDatabase_ContainsAllStarterCrops()
        {
            Assert.IsTrue(_db.Exists("crop_tomato"),  "crop_tomato missing");
            Assert.IsTrue(_db.Exists("crop_carrot"),  "crop_carrot missing");
            Assert.IsTrue(_db.Exists("crop_lettuce"), "crop_lettuce missing");
        }

        [Test]
        public void StarterDatabase_ContainsStarterTools()
        {
            Assert.IsTrue(_db.Exists("tool_watering_can"), "tool_watering_can missing");
            Assert.IsTrue(_db.Exists("tool_basket"),       "tool_basket missing");
        }

        [Test]
        public void GetItem_KnownId_ReturnsCorrectData()
        {
            var data = _db.GetItem("seed_tomato");
            Assert.AreEqual("seed_tomato",   data.ItemId);
            Assert.AreEqual(ItemCategory.Seed, data.Category);
            Assert.Greater(data.MaxStack, 0);
        }

        [Test]
        public void GetItem_UnknownId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _db.GetItem("not_a_real_item"));
        }

        [Test]
        public void GetItem_NullId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _db.GetItem(null));
        }

        [Test]
        public void Exists_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_db.Exists("nothing_here"));
        }

        [Test]
        public void Constructor_DuplicateId_ThrowsArgumentException()
        {
            var items = new[]
            {
                new ItemData("dup", "Dup A", ItemCategory.Seed, 10, 1, ""),
                new ItemData("dup", "Dup B", ItemCategory.Seed, 10, 1, ""),
            };
            Assert.Throws<ArgumentException>(() => new ItemDatabase(items));
        }

        [Test]
        public void ToolItem_HasMaxStackOfOne()
        {
            var can = _db.GetItem("tool_watering_can");
            Assert.AreEqual(1, can.MaxStack);
        }

        [Test]
        public void SeedItem_HasSellValueGreaterThanZero()
        {
            var seed = _db.GetItem("seed_tomato");
            Assert.Greater(seed.SellValue, 0);
        }

        [Test]
        public void CropItem_HasHigherSellValueThanCorrespondingSeed()
        {
            var seed = _db.GetItem("seed_tomato");
            var crop = _db.GetItem("crop_tomato");
            Assert.Greater(crop.SellValue, seed.SellValue);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InventorySlot Tests (via InventorySystem)
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class InventorySlotTests
    {
        [Test]
        public void NewSlot_IsEmpty()
        {
            var slot = new InventorySlot();
            Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void AssignedSlot_IsNotEmpty()
        {
            var slot = new InventorySlot();
            slot.Assign("seed_tomato", 20, 5);
            Assert.IsFalse(slot.IsEmpty);
        }

        [Test]
        public void Assign_SetsQuantityAndReturnsZeroOverflowWhenFits()
        {
            var slot = new InventorySlot();
            int overflow = slot.Assign("seed_tomato", 20, 10);
            Assert.AreEqual(10, slot.Quantity);
            Assert.AreEqual(0, overflow);
        }

        [Test]
        public void Assign_ReturnsOverflowWhenExceedsMaxStack()
        {
            var slot = new InventorySlot();
            int overflow = slot.Assign("seed_tomato", 20, 25);
            Assert.AreEqual(20, slot.Quantity);
            Assert.AreEqual(5, overflow);
        }

        [Test]
        public void IsFull_WhenAtMaxStack_ReturnsTrue()
        {
            var slot = new InventorySlot();
            slot.Assign("seed_tomato", 5, 5);
            Assert.IsTrue(slot.IsFull);
        }

        [Test]
        public void Remove_DecreasesQuantity()
        {
            var slot = new InventorySlot();
            slot.Assign("seed_tomato", 20, 10);
            bool ok = slot.Remove(4);
            Assert.IsTrue(ok);
            Assert.AreEqual(6, slot.Quantity);
        }

        [Test]
        public void Remove_ExactTotal_ClearsSlot()
        {
            var slot = new InventorySlot();
            slot.Assign("seed_tomato", 20, 5);
            slot.Remove(5);
            Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void Remove_MoreThanAvailable_ReturnsFalse()
        {
            var slot = new InventorySlot();
            slot.Assign("seed_tomato", 20, 3);
            bool ok = slot.Remove(5);
            Assert.IsFalse(ok);
            Assert.AreEqual(3, slot.Quantity); // unchanged
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InventorySystem Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class InventorySystemTests
    {
        private IItemDatabase _db;
        private InventorySystem _inventory;

        [SetUp]
        public void SetUp()
        {
            _db = ItemDatabase.CreateStarterDatabase();
            _inventory = new InventorySystem(_db, slotCount: 8);
        }

        // ── AddItem ──────────────────────────────────────────────────

        [Test]
        public void AddItem_ToEmptyInventory_Succeeds()
        {
            var result = _inventory.AddItem("seed_tomato", 4);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.Overflow);
            Assert.AreEqual(4, _inventory.GetCount("seed_tomato"));
        }

        [Test]
        public void AddItem_ZeroQuantity_SucceedsWithNoChange()
        {
            var result = _inventory.AddItem("seed_tomato", 0);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, _inventory.GetCount("seed_tomato"));
        }

        [Test]
        public void AddItem_StacksWithExistingSlot()
        {
            _inventory.AddItem("seed_tomato", 5);
            _inventory.AddItem("seed_tomato", 5);
            Assert.AreEqual(10, _inventory.GetCount("seed_tomato"));
        }

        [Test]
        public void AddItem_OverflowsToNextEmptySlot()
        {
            // seed_tomato maxStack = 20; add 25 → first slot fills to 20, next slot gets 5
            var result = _inventory.AddItem("seed_tomato", 25);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.Overflow);
            Assert.AreEqual(25, _inventory.GetCount("seed_tomato"));
        }

        [Test]
        public void AddItem_WhenInventoryFull_ReturnsOverflow()
        {
            // Fill all 8 slots with tool_basket (maxStack=1) so each takes one slot
            // Use 8 different items to saturate. We'll use a tiny db for this test.
            var tinyDb = new ItemDatabase(new[]
            {
                new ItemData("item_a", "A", ItemCategory.Seed, 1, 0, ""),
            });
            var tiny = new InventorySystem(tinyDb, slotCount: 2);

            tiny.AddItem("item_a", 1);
            tiny.AddItem("item_a", 1); // fills both slots (maxStack=1)

            var result = tiny.AddItem("item_a", 1);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(1, result.Overflow);
        }

        [Test]
        public void AddItem_UnknownId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _inventory.AddItem("unknown_id", 1));
        }

        [Test]
        public void AddItem_NullId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _inventory.AddItem(null, 1));
        }

        [Test]
        public void AddItem_NegativeQuantity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _inventory.AddItem("seed_tomato", -1));
        }

        // ── RemoveItem ───────────────────────────────────────────────

        [Test]
        public void RemoveItem_WhenEnough_SucceedsAndDecrements()
        {
            _inventory.AddItem("seed_tomato", 5);
            var result = _inventory.RemoveItem("seed_tomato", 3);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, _inventory.GetCount("seed_tomato"));
        }

        [Test]
        public void RemoveItem_ExactAmount_LeavesZero()
        {
            _inventory.AddItem("seed_carrot", 3);
            _inventory.RemoveItem("seed_carrot", 3);
            Assert.AreEqual(0, _inventory.GetCount("seed_carrot"));
        }

        [Test]
        public void RemoveItem_MoreThanAvailable_Fails()
        {
            _inventory.AddItem("seed_tomato", 2);
            var result = _inventory.RemoveItem("seed_tomato", 5);
            Assert.IsFalse(result.Success);
            Assert.IsNotEmpty(result.FailReason);
            Assert.AreEqual(2, _inventory.GetCount("seed_tomato")); // unchanged
        }

        [Test]
        public void RemoveItem_FromEmptyInventory_Fails()
        {
            var result = _inventory.RemoveItem("seed_tomato", 1);
            Assert.IsFalse(result.Success);
        }

        [Test]
        public void RemoveItem_AcrossMultipleSlots_RemovesCorrectTotal()
        {
            // seed_tomato maxStack=20; add 25 => 2 slots (20 + 5); remove 22
            _inventory.AddItem("seed_tomato", 25);
            var result = _inventory.RemoveItem("seed_tomato", 22);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, _inventory.GetCount("seed_tomato"));
        }

        // ── HasItem / GetCount ────────────────────────────────────────

        [Test]
        public void HasItem_WhenEnoughPresent_ReturnsTrue()
        {
            _inventory.AddItem("seed_lettuce", 3);
            Assert.IsTrue(_inventory.HasItem("seed_lettuce", 3));
        }

        [Test]
        public void HasItem_WhenNotEnough_ReturnsFalse()
        {
            _inventory.AddItem("seed_lettuce", 2);
            Assert.IsFalse(_inventory.HasItem("seed_lettuce", 3));
        }

        [Test]
        public void HasItem_WhenAbsent_ReturnsFalse()
        {
            Assert.IsFalse(_inventory.HasItem("seed_carrot"));
        }

        [Test]
        public void GetCount_DifferentItemTypes_DoNotStack()
        {
            _inventory.AddItem("seed_tomato", 3);
            _inventory.AddItem("seed_carrot", 5);
            Assert.AreEqual(3, _inventory.GetCount("seed_tomato"));
            Assert.AreEqual(5, _inventory.GetCount("seed_carrot"));
        }

        [Test]
        public void GetCount_AcrossMultipleSlots_SumsCorrectly()
        {
            // Force two slots by adding exactly maxStack (20) then 5 more
            _inventory.AddItem("seed_tomato", 20);
            _inventory.AddItem("seed_tomato", 5);
            Assert.AreEqual(25, _inventory.GetCount("seed_tomato"));
        }

        // ── Slots ────────────────────────────────────────────────────

        [Test]
        public void Slots_CountMatchesConstructedSlotCount()
        {
            Assert.AreEqual(8, _inventory.Slots.Count);
        }

        // ── Starter Loadout ──────────────────────────────────────────

        [Test]
        public void StarterLoadout_AddsSeedsForAllThreeStarterCrops()
        {
            _inventory.AddItem("seed_tomato",  3);
            _inventory.AddItem("seed_carrot",  5);
            _inventory.AddItem("seed_lettuce", 5);

            Assert.AreEqual(3, _inventory.GetCount("seed_tomato"));
            Assert.AreEqual(5, _inventory.GetCount("seed_carrot"));
            Assert.AreEqual(5, _inventory.GetCount("seed_lettuce"));
        }
    }
}
