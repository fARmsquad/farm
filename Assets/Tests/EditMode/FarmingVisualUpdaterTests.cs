using System.Reflection;
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
        private GameObject _fallbackObject;
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

            _cropObject = new GameObject("CropVisual");
            _cropObject.name = "CropVisual";
            _cropObject.transform.SetParent(_plotObject.transform, false);

            _fallbackObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _fallbackObject.name = "FallbackVisual";
            _fallbackObject.transform.SetParent(_cropObject.transform, false);
            Object.DestroyImmediate(_fallbackObject.GetComponent<Collider>());

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
            _cropState.NotifyWatered();

            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();

            var plantedColor = ReadPlotColor();

            Assert.That(_fallbackObject.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(_fallbackObject.transform.localScale.y, Is.GreaterThan(0.05f));
            Assert.That(plantedColor, Is.Not.EqualTo(emptyColor));
        }

        [Test]
        public void RefreshVisuals_ReadyPlot_GrowsCropToFullHeight()
        {
            _soilState.SetStatus(PlotStatus.Planted);
            _soilState.SetCropId("seed_tomato");
            _cropState.Plant(new CropData(10f, 100f));
            _cropState.NotifyWatered();

            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();
            var plantedHeight = _fallbackObject.transform.localScale.y;

            _cropState.Tick(new GrowthConditions(WeatherType.Rain, 25f, SoilQuality.Rich), 10f);
            _soilState.SetStatus(PlotStatus.Harvestable);

            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();

            Assert.That(_cropState.Phase, Is.EqualTo(PlotPhase.Ready));
            Assert.That(_fallbackObject.transform.localScale.y, Is.GreaterThan(plantedHeight));
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
            Assert.That(_fallbackObject.GetComponent<Renderer>().enabled, Is.False);
        }

        [Test]
        public void RefreshVisuals_WithImportedStages_ActivatesMatchingStageAndHidesFallback()
        {
            var tomatoStage1 = CreateImportedStage("tomato", 1);
            var tomatoStage2 = CreateImportedStage("tomato", 2);
            var tomatoStage3 = CreateImportedStage("tomato", 3);

            _soilState.SetStatus(PlotStatus.Planted);
            _soilState.SetCropId("seed_tomato");
            _cropState.Plant(new CropData(10f, 100f));
            _cropState.NotifyWatered();

            _cropVisual.RefreshVisuals();

            Assert.That(tomatoStage1.activeSelf, Is.True);
            Assert.That(tomatoStage2.activeSelf, Is.False);
            Assert.That(tomatoStage3.activeSelf, Is.False);
            Assert.That(_fallbackObject.GetComponent<Renderer>().enabled, Is.False);

            _cropState.Tick(new GrowthConditions(WeatherType.Sunny, 25f, SoilQuality.Normal), 2f);
            _soilState.SetStatus(PlotStatus.Growing);

            _cropVisual.RefreshVisuals();

            Assert.That(tomatoStage1.activeSelf, Is.False);
            Assert.That(tomatoStage2.activeSelf, Is.True);
            Assert.That(tomatoStage3.activeSelf, Is.False);

            _cropState.Tick(new GrowthConditions(WeatherType.Rain, 25f, SoilQuality.Rich), 20f);
            _soilState.SetStatus(PlotStatus.Harvestable);

            _cropVisual.RefreshVisuals();

            Assert.That(_cropState.Phase, Is.EqualTo(PlotPhase.Ready));
            Assert.That(tomatoStage1.activeSelf, Is.False);
            Assert.That(tomatoStage2.activeSelf, Is.False);
            Assert.That(tomatoStage3.activeSelf, Is.True);
        }

        [Test]
        public void RefreshVisuals_WithUnsupportedCrop_KeepsFallbackVisible()
        {
            CreateImportedStage("tomato", 1);
            CreateImportedStage("tomato", 2);
            CreateImportedStage("tomato", 3);

            _soilState.SetStatus(PlotStatus.Planted);
            _soilState.SetCropId("seed_lettuce");
            _cropState.Plant(new CropData(10f, 100f));
            _cropState.NotifyWatered();

            _cropVisual.RefreshVisuals();

            Assert.That(_fallbackObject.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(FindStage("tomato", 1).activeSelf, Is.False);
            Assert.That(FindStage("tomato", 2).activeSelf, Is.False);
            Assert.That(FindStage("tomato", 3).activeSelf, Is.False);
        }

        [Test]
        public void RefreshVisuals_WithImportedSoilVariant_TintsSoilToMatchPlotState()
        {
            var soilVariant = CreateImportedSoilVariant(0);

            _soilState.SetStatus(PlotStatus.Growing);
            _soilState.SetCropId("seed_tomato");
            _cropState.Plant(new CropData(10f, 100f));
            _cropState.NotifyWatered();

            _plotVisual.RefreshVisuals();
            _cropVisual.RefreshVisuals();

            var soilRenderer = soilVariant.GetComponentInChildren<Renderer>();
            soilRenderer.GetPropertyBlock(_propBlock);
            var soilColor = _propBlock.GetColor("_Color");

            Assert.That(soilVariant.activeSelf, Is.True);
            Assert.That(soilColor, Is.EqualTo(ReadPlotColor()));
        }

        [Test]
        public void RefreshVisuals_WithPhaseExtras_KeepsSupportAssetVisibleAfterPlanting()
        {
            var plantedStage = new GameObject("StagePlanted");
            plantedStage.transform.SetParent(_cropObject.transform, false);
            var ripeStage = new GameObject("StageRipe");
            ripeStage.transform.SetParent(_cropObject.transform, false);
            var support = new GameObject("StageSupport");
            support.transform.SetParent(_cropObject.transform, false);

            SetPhaseStages(
                new CropVisualUpdater.PhaseStageEntry { phase = PlotPhase.Planted, stageRoot = plantedStage },
                new CropVisualUpdater.PhaseStageEntry { phase = PlotPhase.Ready, stageRoot = ripeStage });
            SetPhaseExtras(
                new CropVisualUpdater.PhaseExtraEntry { minPhase = PlotPhase.Planted, maxPhase = PlotPhase.Dead, root = support });

            _soilState.SetStatus(PlotStatus.Planted);
            _soilState.SetCropId("seed_tomato");
            _cropState.Plant(new CropData(0.04f, 1f));
            _cropVisual.RefreshVisuals();

            Assert.That(plantedStage.activeSelf, Is.True);
            Assert.That(support.activeSelf, Is.True);

            _cropState.NotifyWatered();
            AdvanceToPhase(PlotPhase.Ready, new GrowthConditions(WeatherType.Rain, 25f, SoilQuality.Rich, 1f));
            _soilState.SetStatus(PlotStatus.Harvestable);
            _cropVisual.RefreshVisuals();

            Assert.That(_cropState.Phase, Is.EqualTo(PlotPhase.Ready));
            Assert.That(ripeStage.activeSelf, Is.True);
            Assert.That(support.activeSelf, Is.True);

            _cropState.Harvest();
            _soilState.SetStatus(PlotStatus.Empty);
            _soilState.SetCropId(null);
            _cropVisual.RefreshVisuals();

            Assert.That(support.activeSelf, Is.False);
        }

        [Test]
        public void RefreshVisuals_WithTutorialLifecycleStages_UsesStageIndexWithinSharedPhase()
        {
            var stage0 = new GameObject("TutorialStage0");
            stage0.transform.SetParent(_cropObject.transform, false);
            var stage1 = new GameObject("TutorialStage1");
            stage1.transform.SetParent(_cropObject.transform, false);
            var stage2 = new GameObject("TutorialStage2");
            stage2.transform.SetParent(_cropObject.transform, false);
            var stage3 = new GameObject("TutorialStage3");
            stage3.transform.SetParent(_cropObject.transform, false);
            var stage4 = new GameObject("TutorialStage4");
            stage4.transform.SetParent(_cropObject.transform, false);

            _cropVisual.SetTutorialLifecycleStages(stage0, stage1, stage2, stage3, stage4);
            _cropState.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);
            _soilState.SetStatus(PlotStatus.Planted);
            _soilState.SetCropId("seed_tomato");
            _cropState.Plant(new CropData(0.04f, 1f));

            _cropVisual.RefreshVisuals();
            Assert.That(stage0.activeSelf, Is.True);

            Assert.That(_cropState.TryCompleteTask(CropTaskId.PatSoil), Is.True);
            Assert.That(_cropState.TryCompleteTask(CropTaskId.ClearWeeds), Is.True);
            Assert.That(_cropState.TryCompleteTask(CropTaskId.TieVine), Is.True);
            _cropVisual.RefreshVisuals();

            Assert.That(_cropState.Phase, Is.EqualTo(PlotPhase.Budding));
            Assert.That(stage3.activeSelf, Is.True);
            Assert.That(stage4.activeSelf, Is.False);
            Assert.That(_fallbackObject.GetComponent<Renderer>().enabled, Is.False);

            Assert.That(_cropState.TryCompleteTask(CropTaskId.PinchSuckers), Is.True);
            _cropVisual.RefreshVisuals();

            Assert.That(_cropState.Phase, Is.EqualTo(PlotPhase.Budding));
            Assert.That(stage3.activeSelf, Is.False);
            Assert.That(stage4.activeSelf, Is.True);
        }

        private void AdvanceToPhase(
            PlotPhase targetPhase,
            GrowthConditions conditions,
            float stepSize = 0.1f,
            int maxSteps = 256)
        {
            for (var step = 0; step < maxSteps && _cropState.Phase != targetPhase; step++)
            {
                _cropState.SetMoisture(1f);
                _cropState.Tick(conditions, stepSize);
            }

            Assert.That(_cropState.Phase, Is.EqualTo(targetPhase));
        }

        private Color ReadPlotColor()
        {
            var renderer = _plotObject.GetComponent<Renderer>();
            renderer.GetPropertyBlock(_propBlock);
            return _propBlock.GetColor("_Color");
        }

        private GameObject CreateImportedStage(string cropKey, int stage)
        {
            var stageRoot = new GameObject($"CropStage_{cropKey}_{stage}");
            stageRoot.transform.SetParent(_cropObject.transform, false);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(stageRoot.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            return stageRoot;
        }

        private GameObject FindStage(string cropKey, int stage)
        {
            return _cropObject.transform.Find($"CropStage_{cropKey}_{stage}")?.gameObject;
        }

        private GameObject CreateImportedSoilVariant(int index)
        {
            var soilRoot = new GameObject($"SoilVariant_{index}");
            soilRoot.transform.SetParent(_cropObject.transform, false);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(soilRoot.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            return soilRoot;
        }

        private void SetPhaseStages(params CropVisualUpdater.PhaseStageEntry[] entries)
        {
            typeof(CropVisualUpdater)
                .GetField("phaseStages", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_cropVisual, entries);
        }

        private void SetPhaseExtras(params CropVisualUpdater.PhaseExtraEntry[] entries)
        {
            typeof(CropVisualUpdater)
                .GetField("phaseExtras", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_cropVisual, entries);
        }

    }
}
