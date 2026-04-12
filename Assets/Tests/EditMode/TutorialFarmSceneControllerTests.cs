using System.Reflection;
using FarmSimVR.Core.Farming;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class TutorialFarmSceneControllerTests
    {
        private GameObject _sceneRoot;
        private GameObject _heroPlot;
        private TutorialFarmSceneController _controller;
        private CropPlotController _plotController;

        [SetUp]
        public void SetUp()
        {
            _sceneRoot = new GameObject("TutorialSceneRoot");
            _heroPlot = new GameObject("CropPlot_0");
            _controller = _sceneRoot.AddComponent<TutorialFarmSceneController>();
            _plotController = _heroPlot.AddComponent<CropPlotController>();

            typeof(TutorialFarmSceneController)
                .GetField("_heroCropPlot", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_controller, _heroPlot);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sceneRoot);
            Object.DestroyImmediate(_heroPlot);
        }

        [Test]
        public void EnsureHeroPlotConfigured_WhenStateAppearsLater_ConfiguresTutorialLifecycle()
        {
            _controller.EnsureHeroPlotConfigured();

            var state = new CropPlotState(new CropGrowthCalculator());
            _plotController.Initialize(state, new SoilState("CropPlot_0", SoilType.Loam));

            _controller.EnsureHeroPlotConfigured();

            Assert.IsTrue(state.IsTutorialTaskMode);
            Assert.IsFalse(state.RequireWateringPerPhase);
            Assert.AreEqual(CropTaskId.None, state.CurrentTaskId);
        }
    }
}
