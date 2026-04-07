using FarmSimVR.Editor;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Hunting;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class SceneUnificationBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateAnchor("BarnPosition", new Vector3(0f, 0f, 8f));
            CreateAnchor("SpawnPoint", new Vector3(0f, 0f, -8f));

            for (var index = 0; index < 6; index++)
            {
                var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plot.name = $"CropPlot_{index}";
                plot.transform.position = new Vector3(index - 3, 0.05f, 0f);
                plot.transform.localScale = new Vector3(1f, 0.1f, 1f);
            }
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void ConfigureScene_AddsUnifiedGameplaySystems()
        {
            SceneUnificationBuilder.ConfigureScene(SceneManager.GetActiveScene());

            Assert.That(Object.FindAnyObjectByType<GameManager>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<SimulationManager>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<HuntingManager>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<WildAnimalSpawner>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<BarnDropOff>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<AnimalPen>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<HuntingHUD>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<PlayerMovement>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<KeyboardPlayerInput>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ThirdPersonCamera>(), Is.Not.Null);
        }

        [Test]
        public void ConfigureScene_WiresCropRuntimeIntoExistingGreyboxPlots()
        {
            SceneUnificationBuilder.ConfigureScene(SceneManager.GetActiveScene());

            for (var index = 0; index < 6; index++)
            {
                var plot = GameObject.Find($"CropPlot_{index}");
                Assert.That(plot, Is.Not.Null, $"Missing plot CropPlot_{index}");
                Assert.That(plot.GetComponent<CropPlotController>(), Is.Not.Null);

                var cropVisual = plot.transform.Find("CropVisual");
                Assert.That(cropVisual, Is.Not.Null, $"Missing CropVisual child for {plot.name}");
                Assert.That(cropVisual.GetComponent<CropVisualUpdater>(), Is.Not.Null);
            }
        }

        private static void CreateAnchor(string name, Vector3 position)
        {
            var anchor = new GameObject(name);
            anchor.transform.position = position;
        }
    }
}
