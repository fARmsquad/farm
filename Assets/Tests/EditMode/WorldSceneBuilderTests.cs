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

        [Test]
        public void FogColor_IsWarmGolden()
        {
            var fog = WorldSceneBuilder.FogColor;
            Assert.That(fog.r, Is.GreaterThan(0.9f));
            Assert.That(fog.g, Is.InRange(0.75f, 0.9f));
            Assert.That(fog.b, Is.InRange(0.55f, 0.7f));
        }

        [Test]
        public void TerrainLayerPaths_AllExist()
        {
            foreach (var path in WorldSceneBuilder.TerrainTexturePaths)
            {
                var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
                Assert.IsNotNull(tex, $"Missing terrain texture: {path}");
            }
        }

        [Test]
        public void WaterPrefabPaths_AllExist()
        {
            foreach (var path in WorldSceneBuilder.WaterPrefabPaths)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
                Assert.IsNotNull(prefab, $"Missing water prefab: {path}");
            }
        }

        [Test]
        public void FarmBuildingPrefabPaths_AllExist()
        {
            string[] paths = {
                "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Farmhouse_01.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Barn_01.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Silo_01.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Silo_Small_01.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Buildings/SM_Bld_Greenhouse_01.prefab",
            };
            foreach (var p in paths)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p);
                Assert.IsNotNull(prefab, $"Missing farm building: {p}");
            }
        }
    }
}
