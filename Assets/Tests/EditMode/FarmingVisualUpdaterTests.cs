using FarmSimVR.Core.Farming;
using FarmSimVR.MonoBehaviours;
using NUnit.Framework;
using UnityEngine;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmingVisualUpdaterTests
    {
        private GameObject _plotObject;
        private GameObject _cropObject;
        private CropPlotController _controller;
        private PlotVisualUpdater _plotVisual;
        private CropVisualUpdater _cropVisual;
        private CropPlotState _cropState;
        private SoilState _soilState;
        private MaterialPropertyBlock _propBlock;

        [SetUp]
        public void SetUp()
        {
            _plotObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _plotObject.name = "CropPlot_Test";
            _plotObject.transform.localScale = new Vector3(1f, 0.1f, 1f);

            _cropObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cropObject.name = "CropVisual";
            _cropObject.transform.SetParent(_plotObject.transform, false);
            Object.DestroyImmediate(_cropObject.GetComponent<Collider>());

            _controller = _plotObject.AddComponent<CropPlotController>();
            _plotVisual = _plotObject.AddComponent<PlotVisualUpdater>();
            _cropVisual = _cropObject.AddComponent<CropVisualUpdater>();

            _cropState = new CropPlotState(new CropGrowthCalculator());
            _soilState = new SoilState("CropPlot_Test", SoilType.Loam);
            _controller.Initialize(_cropState, _soilState);

            _propBlock = new MaterialPropertyBlock();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_plotObject);
        }

        [Test]
        public void RefreshVisuals_PlantedPlot_ShowsImmediateCropAndChangesSoilColor()
        {
            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();
            var emptyColor = ReadPlotColor();

            _soilState.SetStatus(PlotStatus.Planted);
            _soilState.SetCropId("seed_tomato");
            _cropState.Plant(new CropData(10f, 100f));

            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();

            var plantedColor = ReadPlotColor();

            Assert.That(_cropObject.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(_cropObject.transform.localScale.y, Is.GreaterThan(0.05f));
            Assert.That(plantedColor, Is.Not.EqualTo(emptyColor));
        }

        [Test]
        public void RefreshVisuals_ReadyPlot_GrowsCropToFullHeight()
        {
            _soilState.SetStatus(PlotStatus.Planted);
            _soilState.SetCropId("seed_tomato");
            _cropState.Plant(new CropData(10f, 100f));

            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();
            var plantedHeight = _cropObject.transform.localScale.y;

            _cropState.Tick(new GrowthConditions(WeatherType.Rain, 25f, SoilQuality.Rich), 10f);
            _soilState.SetStatus(PlotStatus.Harvestable);

            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();

            Assert.That(_cropState.Phase, Is.EqualTo(PlotPhase.Ready));
            Assert.That(_cropObject.transform.localScale.y, Is.GreaterThan(plantedHeight));
        }

        [Test]
        public void RefreshVisuals_DepletedPlot_UsesDifferentSoilColorThanEmptyPlot()
        {
            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();
            var emptyColor = ReadPlotColor();

            _soilState.SetStatus(PlotStatus.Depleted);
            _soilState.DeductNutrients(1f);

            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();
            var depletedColor = ReadPlotColor();

            Assert.That(depletedColor, Is.Not.EqualTo(emptyColor));
            Assert.That(_cropObject.GetComponent<Renderer>().enabled, Is.False);
        }

        private Color ReadPlotColor()
        {
            var renderer = _plotObject.GetComponent<Renderer>();
            renderer.GetPropertyBlock(_propBlock);
            return _propBlock.GetColor("_Color");
        }
    }
}
