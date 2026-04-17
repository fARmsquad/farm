using NUnit.Framework;
using FarmSimVR.Core.Economy;
using FarmSimVR.Core.Hunting;

namespace FarmSimVR.Tests.EditMode
{
    public class LivestockRegistryTests
    {
        [Test]
        public void Add_SingleAnimal_IncreasesTotal()
        {
            var registry = new LivestockRegistry();
            registry.Add(AnimalType.Chicken);
            Assert.AreEqual(1, registry.Total);
        }

        [Test]
        public void Add_MultipleAnimals_TracksTotalCorrectly()
        {
            var registry = new LivestockRegistry();
            registry.Add(AnimalType.Chicken);
            registry.Add(AnimalType.Pig);
            registry.Add(AnimalType.Horse);
            Assert.AreEqual(3, registry.Total);
        }

        [Test]
        public void Count_ByType_ReturnsMatchingCount()
        {
            var registry = new LivestockRegistry();
            registry.Add(AnimalType.Chicken);
            registry.Add(AnimalType.Chicken);
            registry.Add(AnimalType.Pig);
            Assert.AreEqual(2, registry.Count(AnimalType.Chicken));
            Assert.AreEqual(1, registry.Count(AnimalType.Pig));
            Assert.AreEqual(0, registry.Count(AnimalType.Horse));
        }

        [Test]
        public void Animals_ReturnsAllAdded()
        {
            var registry = new LivestockRegistry();
            registry.Add(AnimalType.Chicken);
            registry.Add(AnimalType.Horse);
            Assert.AreEqual(2, registry.Animals.Count);
        }

        [Test]
        public void Add_FiresOnChanged()
        {
            var registry = new LivestockRegistry();
            bool fired = false;
            registry.OnChanged += () => fired = true;
            registry.Add(AnimalType.Pig);
            Assert.IsTrue(fired);
        }

        [Test]
        public void NewRegistry_HasZeroTotal()
        {
            var registry = new LivestockRegistry();
            Assert.AreEqual(0, registry.Total);
        }
    }

    public class LivestockPricesTests
    {
        [TestCase(AnimalType.Chicken, 45)]
        [TestCase(AnimalType.Pig, 45)]
        [TestCase(AnimalType.Horse, 75)]
        public void Get_KnownAnimal_ReturnsCorrectPrice(AnimalType type, int expectedPrice)
        {
            Assert.AreEqual(expectedPrice, LivestockPrices.Get(type));
        }
    }
}
