using System.Reflection;
using FarmSimVR.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmSceneBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void BuildFarmLayoutCore_RebuildsWithoutDuplicatingFarmRoot()
        {
            InvokePrivateStatic("BuildFarmLayoutCore");
            InvokePrivateStatic("BuildFarmLayoutCore");

            var farmRoots = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var farmCount = 0;
            foreach (var transform in farmRoots)
            {
                if (transform.name == "Farm" && transform.parent == null)
                    farmCount++;
            }

            var farm = GameObject.Find("Farm");

            Assert.That(farmCount, Is.EqualTo(1));
            Assert.That(farm, Is.Not.Null);
            Assert.That(farm.transform.Find("Plots"), Is.Not.Null);
            Assert.That(farm.transform.Find("Markers/SpawnPoint"), Is.Not.Null);
        }

        [Test]
        public void BuildSkyAndLightingCore_ReusesExistingSkyboxAsset()
        {
            InvokePrivateStatic("BuildSkyAndLightingCore");
            var first = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Materials/SkyboxProcedural.mat");

            InvokePrivateStatic("BuildSkyAndLightingCore");
            var second = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Materials/SkyboxProcedural.mat");

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
            Assert.That(RenderSettings.skybox, Is.SameAs(first));
        }

        [Test]
        public void BuildFarmLayoutCore_UsesShadersCompatibleWithActiveRenderPipeline()
        {
            InvokePrivateStatic("BuildFarmLayoutCore");

            var plot = GameObject.Find("CropPlot_0");
            var expansionZone = GameObject.Find("ExpansionZone_East");

            Assert.That(plot, Is.Not.Null);
            Assert.That(expansionZone, Is.Not.Null);

            var plotShader = plot.GetComponent<Renderer>().sharedMaterial.shader;
            var expansionShader = expansionZone.GetComponent<Renderer>().sharedMaterial.shader;

            Assert.That(plotShader, Is.Not.Null);
            Assert.That(expansionShader, Is.Not.Null);

            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Assert.That(plotShader.name.StartsWith("Universal Render Pipeline/"), Is.False,
                    "Farm greybox materials should not use URP shaders when no render pipeline asset is assigned.");
                Assert.That(expansionShader.name.StartsWith("Universal Render Pipeline/"), Is.False,
                    "Transparent expansion-zone materials should not use URP shaders when no render pipeline asset is assigned.");
            }
        }

        private static void InvokePrivateStatic(string methodName)
        {
            var method = typeof(FarmSceneBuilder).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, $"Missing private method '{methodName}'.");
            method.Invoke(null, null);
        }
    }
}
