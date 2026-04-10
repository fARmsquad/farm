using System.Reflection;
using NUnit.Framework;
using FarmSimVR.Editor;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class ChickenGameSceneBuilderTests
    {
        [TestCase("Player", true)]
        [TestCase("MainCamera", true)]
        [TestCase("SpawnPoint", false)]
        [TestCase("CustomChickenTag", false)]
        public void IsBuiltInTag_ReturnsExpectedValue(string tag, bool expected)
        {
            MethodInfo method = typeof(ChickenGameSceneBuilder).GetMethod(
                "IsBuiltInTag",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);

            bool result = (bool)method.Invoke(null, new object[] { tag });

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
