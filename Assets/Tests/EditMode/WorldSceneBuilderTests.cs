using NUnit.Framework;
using FarmSimVR.Editor;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class WorldSceneBuilderTests
    {
        [Test]
        public void WorldConstants_TerrainSize_Is400()
        {
            Assert.AreEqual(400f, WorldSceneBuilder.TerrainSize);
        }

        [Test]
        public void WorldConstants_TerrainHeight_Is20()
        {
            Assert.AreEqual(20f, WorldSceneBuilder.TerrainHeight);
        }

        [Test]
        public void WorldConstants_ZoneCount_Is9()
        {
            Assert.AreEqual(9, WorldSceneBuilder.ZoneNames.Length);
        }
    }
}
