using System;
using NUnit.Framework;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.Tests.EditMode
{
    // ─────────────────────────────────────────────────────────────────────────
    // SoilTypeDefaults Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilTypeDefaultsTests
    {
        [Test]
        public void Sandy_HasBelowAverageGrowthMultiplier()
        {
            var d = SoilTypeDefaults.For(SoilType.Sandy);
            Assert.Less(d.GrowthMultiplier, 1.0f);
        }

        [Test]
        public void Loam_HasBaselineGrowthMultiplier()
        {
            var d = SoilTypeDefaults.For(SoilType.Loam);
            Assert.AreEqual(1.0f, d.GrowthMultiplier, 0.001f);
        }

        [Test]
        public void Clay_HasBelowAverageGrowthMultiplier()
        {
            var d = SoilTypeDefaults.For(SoilType.Clay);
            Assert.Less(d.GrowthMultiplier, 1.0f);
        }

        [Test]
        public void Rich_HasAboveAverageGrowthMultiplier()
        {
            var d = SoilTypeDefaults.For(SoilType.Rich);
            Assert.Greater(d.GrowthMultiplier, 1.0f);
        }

        [Test]
        public void AllTypes_HaveMoistureDecayRateGreaterThanZero()
        {
            foreach (SoilType type in Enum.GetValues(typeof(SoilType)))
            {
                var d = SoilTypeDefaults.For(type);
                Assert.Greater(d.MoistureDecayRate, 0f, $"{type} decay must be > 0");
            }
        }

        [Test]
        public void AllTypes_HaveInitialMoistureAndNutrientsInRange()
        {
            foreach (SoilType type in Enum.GetValues(typeof(SoilType)))
            {
                var d = SoilTypeDefaults.For(type);
                Assert.GreaterOrEqual(d.InitialMoisture,  0f);
                Assert.LessOrEqual(d.InitialMoisture,     1f);
                Assert.GreaterOrEqual(d.InitialNutrients, 0f);
                Assert.LessOrEqual(d.InitialNutrients,    1f);
            }
        }

        [Test]
        public void Sandy_HasHigherDecayRateThanRich()
        {
            var sandy = SoilTypeDefaults.For(SoilType.Sandy);
            var rich  = SoilTypeDefaults.For(SoilType.Rich);
            Assert.Greater(sandy.MoistureDecayRate, rich.MoistureDecayRate);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoilState Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilStateTests
    {
        [Test]
        public void NewSoilState_IsEmpty()
        {
            var state = new SoilState("plot_0", SoilType.Loam);
            Assert.AreEqual(PlotStatus.Empty, state.Status);
            Assert.IsNull(state.CurrentCropId);
        }

        [Test]
        public void NewSoilState_LoamDefaults_MatchExpectedValues()
        {
            var state = new SoilState("plot_0", SoilType.Loam);
            var d = SoilTypeDefaults.For(SoilType.Loam);
            Assert.AreEqual(d.InitialMoisture,  state.Moisture,  0.001f);
            Assert.AreEqual(d.InitialNutrients, state.Nutrients, 0.001f);
            Assert.AreEqual(d.GrowthMultiplier, state.GrowthMultiplier, 0.001f);
        }

        [Test]
        public void NewSoilState_SandyDefaults_MatchExpectedValues()
        {
            var state = new SoilState("plot_0", SoilType.Sandy);
            var d = SoilTypeDefaults.For(SoilType.Sandy);
            Assert.AreEqual(d.InitialMoisture,  state.Moisture,  0.001f);
            Assert.AreEqual(d.InitialNutrients, state.Nutrients, 0.001f);
        }

        [Test]
        public void Constructor_EmptyPlotId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SoilState("", SoilType.Loam));
        }

        [Test]
        public void GrowthMultiplier_WhenDepleted_IsReducedByHalf()
        {
            var state = new SoilState("plot_0", SoilType.Loam);
            var d = SoilTypeDefaults.For(SoilType.Loam);

            state.SetStatus(PlotStatus.Depleted);

            Assert.AreEqual(d.GrowthMultiplier * 0.5f, state.GrowthMultiplier, 0.001f);
        }

        [Test]
        public void PlotId_IsPreservedExactly()
        {
            var state = new SoilState("CropPlot_3", SoilType.Clay);
            Assert.AreEqual("CropPlot_3", state.PlotId);
        }

        [Test]
        public void Type_IsPreservedExactly()
        {
            var state = new SoilState("plot_0", SoilType.Rich);
            Assert.AreEqual(SoilType.Rich, state.Type);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoilManager.Plant Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilManagerPlantTests
    {
        private SoilManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SoilManager();
            _manager.AddPlot("plot_0", SoilType.Loam);
        }

        [Test]
        public void Plant_InEmptyPlot_ReturnsTrue()
        {
            bool ok = _manager.Plant("plot_0", "seed_tomato");
            Assert.IsTrue(ok);
        }

        [Test]
        public void Plant_SetsStatusToPlanted()
        {
            _manager.Plant("plot_0", "seed_tomato");
            Assert.AreEqual(PlotStatus.Planted, _manager.GetPlot("plot_0").Status);
        }

        [Test]
        public void Plant_SetsCropId()
        {
            _manager.Plant("plot_0", "seed_tomato");
            Assert.AreEqual("seed_tomato", _manager.GetPlot("plot_0").CurrentCropId);
        }

        [Test]
        public void Plant_InNonEmptyPlot_ReturnsFalse()
        {
            _manager.Plant("plot_0", "seed_tomato");
            bool second = _manager.Plant("plot_0", "seed_carrot");
            Assert.IsFalse(second);
        }

        [Test]
        public void Plant_InNonEmptyPlot_DoesNotChangeCropId()
        {
            _manager.Plant("plot_0", "seed_tomato");
            _manager.Plant("plot_0", "seed_carrot");
            Assert.AreEqual("seed_tomato", _manager.GetPlot("plot_0").CurrentCropId);
        }

        [Test]
        public void Plant_UnknownPlotId_ReturnsFalse()
        {
            bool ok = _manager.Plant("does_not_exist", "seed_tomato");
            Assert.IsFalse(ok);
        }

        [Test]
        public void Plant_EmptyCropId_ReturnsFalse()
        {
            bool ok = _manager.Plant("plot_0", "");
            Assert.IsFalse(ok);
        }

        [Test]
        public void Plant_InDepletedPlot_ReturnsFalse()
        {
            // Force depleted status
            var state = _manager.GetPlot("plot_0");
            state.SetStatus(PlotStatus.Depleted);
            bool ok = _manager.Plant("plot_0", "seed_tomato");
            Assert.IsFalse(ok);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoilManager.Water Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilManagerWaterTests
    {
        private SoilManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SoilManager();
            _manager.AddPlot("plot_0", SoilType.Sandy); // Sandy starts low moisture
        }

        [Test]
        public void Water_IncreasesMoisture()
        {
            float before = _manager.GetPlot("plot_0").Moisture;
            _manager.Water("plot_0", 0.3f);
            Assert.Greater(_manager.GetPlot("plot_0").Moisture, before);
        }

        [Test]
        public void Water_ClampsAtOne()
        {
            _manager.Water("plot_0", 5.0f);  // way more than 1
            Assert.AreEqual(1.0f, _manager.GetPlot("plot_0").Moisture, 0.001f);
        }

        [Test]
        public void Water_UnknownPlotId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.Water("ghost_plot", 0.5f));
        }

        [Test]
        public void Water_ZeroAmount_NoChange()
        {
            float before = _manager.GetPlot("plot_0").Moisture;
            _manager.Water("plot_0", 0f);
            Assert.AreEqual(before, _manager.GetPlot("plot_0").Moisture, 0.001f);
        }

        [Test]
        public void Water_ExactlyFillingToOne_NoClamping()
        {
            // Sandy starts at 0.3; water 0.7 should reach exactly 1.0
            float start = _manager.GetPlot("plot_0").Moisture;
            _manager.Water("plot_0", 1.0f - start);
            Assert.AreEqual(1.0f, _manager.GetPlot("plot_0").Moisture, 0.001f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoilManager.Harvest Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilManagerHarvestTests
    {
        private SoilManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SoilManager();
            _manager.AddPlot("plot_0", SoilType.Loam);
        }

        private void BringToHarvestable()
        {
            _manager.Plant("plot_0", "seed_tomato");
            _manager.Tick(0.1f);                         // Planted → Growing
            _manager.MarkHarvestable("plot_0");          // Growing → Harvestable
        }

        [Test]
        public void Harvest_FromHarvestable_TransitionsToEmpty()
        {
            BringToHarvestable();
            _manager.Harvest("plot_0");
            Assert.AreEqual(PlotStatus.Empty, _manager.GetPlot("plot_0").Status);
        }

        [Test]
        public void Harvest_ClearsCropId()
        {
            BringToHarvestable();
            _manager.Harvest("plot_0");
            Assert.IsNull(_manager.GetPlot("plot_0").CurrentCropId);
        }

        [Test]
        public void Harvest_DepletesNutrients()
        {
            BringToHarvestable();
            float before = _manager.GetPlot("plot_0").Nutrients;
            _manager.Harvest("plot_0");
            Assert.Less(_manager.GetPlot("plot_0").Nutrients, before);
        }

        [Test]
        public void Harvest_WhenNotHarvestable_ThrowsInvalidOperationException()
        {
            // Plot is Empty — cannot harvest
            Assert.Throws<InvalidOperationException>(() => _manager.Harvest("plot_0"));
        }

        [Test]
        public void Harvest_WhenPlanted_ThrowsInvalidOperationException()
        {
            _manager.Plant("plot_0", "seed_tomato");
            Assert.Throws<InvalidOperationException>(() => _manager.Harvest("plot_0"));
        }

        [Test]
        public void Harvest_WhenNutrientsReachZero_TransitionsToDepleted()
        {
            // Sandy soil: initial nutrients 0.60, depletion 0.25 each harvest
            _manager.AddPlot("sandy", SoilType.Sandy);

            // Force nutrients to near zero so one harvest depletes them completely.
            var state = _manager.GetPlot("sandy");
            // Drain nutrients manually via internal: we need to exhaust them via repeated harvest.
            // Use Loam as base and force a very low-nutrient scenario by composting a scratch plot
            // then harvesting enough times. Instead, test by forcing state directly.
            state.SetStatus(PlotStatus.Harvestable);
            // Use Sandy's depletion (0.25). Force nutrients to 0.10 so harvest (0.25) → 0.
            // We reach this by applying internal method — accessible from test assembly since public.
            // SoilState.DeductNutrients is internal; we drive it via manager instead.
            // Create a fresh manager with a plot that has been harvested enough times.
            var m2 = new SoilManager();
            m2.AddPlot("p", SoilType.Sandy); // nutrients=0.60, depletion=0.25
            // Harvest 1: 0.60 - 0.25 = 0.35  → Empty
            // Harvest 2: 0.35 - 0.25 = 0.10  → Empty
            // Harvest 3: 0.10 - 0.25 → 0     → Depleted
            for (int i = 0; i < 3; i++)
            {
                m2.Plant("p", "seed_tomato");
                m2.Tick(0.1f);
                m2.MarkHarvestable("p");
                m2.Harvest("p");
                if (m2.GetPlot("p").Status == PlotStatus.Depleted) break;
            }

            Assert.AreEqual(PlotStatus.Depleted, m2.GetPlot("p").Status);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoilManager.Compost Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilManagerCompostTests
    {
        private SoilManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SoilManager();
            _manager.AddPlot("plot_0", SoilType.Sandy); // Sandy: nutrients=0.6
        }

        [Test]
        public void Compost_IncreasesNutrients()
        {
            float before = _manager.GetPlot("plot_0").Nutrients;
            _manager.Compost("plot_0", 0.2f);
            Assert.Greater(_manager.GetPlot("plot_0").Nutrients, before);
        }

        [Test]
        public void Compost_ClampsAtOne()
        {
            _manager.Compost("plot_0", 5.0f);
            Assert.AreEqual(1.0f, _manager.GetPlot("plot_0").Nutrients, 0.001f);
        }

        [Test]
        public void Compost_WorksOnAnyStatus()
        {
            // Growing plot can be composted
            _manager.Plant("plot_0", "seed_tomato");
            _manager.Tick(0.1f);  // → Growing
            float before = _manager.GetPlot("plot_0").Nutrients;
            _manager.Compost("plot_0", 0.1f);
            Assert.Greater(_manager.GetPlot("plot_0").Nutrients, before);
        }

        [Test]
        public void Compost_OnDepletedPlot_RestoresStatusToEmpty()
        {
            var state = _manager.GetPlot("plot_0");
            state.SetStatus(PlotStatus.Depleted);
            // Force nutrients to 0 by draining them
            state.DeductNutrients(1.0f); // drain all

            _manager.Compost("plot_0", 0.3f);
            Assert.AreEqual(PlotStatus.Empty, _manager.GetPlot("plot_0").Status);
        }

        [Test]
        public void Compost_UnknownPlotId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.Compost("ghost", 0.5f));
        }

        [Test]
        public void Compost_ZeroAmount_NoChange()
        {
            float before = _manager.GetPlot("plot_0").Nutrients;
            _manager.Compost("plot_0", 0f);
            Assert.AreEqual(before, _manager.GetPlot("plot_0").Nutrients, 0.001f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoilManager.Tick Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilManagerTickTests
    {
        private SoilManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SoilManager();
            _manager.AddPlot("plot_0", SoilType.Loam); // decayRate=0.01/s
        }

        [Test]
        public void Tick_DecaysMoisture()
        {
            float before = _manager.GetPlot("plot_0").Moisture;
            _manager.Tick(1.0f);
            Assert.Less(_manager.GetPlot("plot_0").Moisture, before);
        }

        [Test]
        public void Tick_DecayAmount_MatchesDecayRateTimesDeltaTime()
        {
            var state  = _manager.GetPlot("plot_0");
            float before = state.Moisture;
            float rate = state.MoistureDecayRate; // 0.01 for Loam

            _manager.Tick(10.0f);

            float expected = before - rate * 10.0f;
            Assert.AreEqual(expected, state.Moisture, 0.001f);
        }

        [Test]
        public void Tick_MoistureClampsAtZero()
        {
            _manager.Tick(9999f);
            Assert.AreEqual(0f, _manager.GetPlot("plot_0").Moisture, 0.001f);
        }

        [Test]
        public void Tick_PlantedStatus_AdvancesToGrowing()
        {
            _manager.Plant("plot_0", "seed_tomato");
            Assert.AreEqual(PlotStatus.Planted, _manager.GetPlot("plot_0").Status);

            _manager.Tick(0.1f);
            Assert.AreEqual(PlotStatus.Growing, _manager.GetPlot("plot_0").Status);
        }

        [Test]
        public void Tick_GrowingStatus_DoesNotChangeStatus()
        {
            _manager.Plant("plot_0", "seed_tomato");
            _manager.Tick(0.1f); // → Growing
            _manager.Tick(1.0f);
            Assert.AreEqual(PlotStatus.Growing, _manager.GetPlot("plot_0").Status);
        }

        [Test]
        public void Tick_ZeroDeltaTime_NoChange()
        {
            float moisture = _manager.GetPlot("plot_0").Moisture;
            _manager.Tick(0f);
            Assert.AreEqual(moisture, _manager.GetPlot("plot_0").Moisture, 0.001f);
        }

        [Test]
        public void Tick_NegativeDeltaTime_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _manager.Tick(-1f));
        }

        [Test]
        public void Tick_MultiplePlots_AllDecay()
        {
            _manager.AddPlot("plot_1", SoilType.Sandy);
            _manager.AddPlot("plot_2", SoilType.Rich);

            float m0 = _manager.GetPlot("plot_0").Moisture;
            float m1 = _manager.GetPlot("plot_1").Moisture;
            float m2 = _manager.GetPlot("plot_2").Moisture;

            _manager.Tick(1.0f);

            Assert.Less(_manager.GetPlot("plot_0").Moisture, m0);
            Assert.Less(_manager.GetPlot("plot_1").Moisture, m1);
            Assert.Less(_manager.GetPlot("plot_2").Moisture, m2);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoilManager.MarkHarvestable + AllPlots Tests
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilManagerHarvestableTests
    {
        private SoilManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SoilManager();
            _manager.AddPlot("plot_0", SoilType.Loam);
        }

        [Test]
        public void MarkHarvestable_FromGrowing_TransitionsToHarvestable()
        {
            _manager.Plant("plot_0", "seed_tomato");
            _manager.Tick(0.1f); // → Growing
            _manager.MarkHarvestable("plot_0");
            Assert.AreEqual(PlotStatus.Harvestable, _manager.GetPlot("plot_0").Status);
        }

        [Test]
        public void MarkHarvestable_FromPlanted_IsNoOp()
        {
            _manager.Plant("plot_0", "seed_tomato");
            _manager.MarkHarvestable("plot_0"); // still Planted, not Growing
            Assert.AreEqual(PlotStatus.Planted, _manager.GetPlot("plot_0").Status);
        }

        [Test]
        public void MarkHarvestable_UnknownPlotId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.MarkHarvestable("ghost"));
        }

        [Test]
        public void AllPlots_ReturnsAllRegisteredPlots()
        {
            _manager.AddPlot("plot_1", SoilType.Sandy);
            _manager.AddPlot("plot_2", SoilType.Rich);
            Assert.AreEqual(3, _manager.AllPlots.Count);
        }

        [Test]
        public void AddPlot_DuplicateId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _manager.AddPlot("plot_0", SoilType.Clay));
        }

        [Test]
        public void GetPlot_UnknownId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _manager.GetPlot("unknown"));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Full Loop Integration Test
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class SoilManagerIntegrationTests
    {
        [Test]
        public void FullLoop_Plant_Tick_Harvestable_Harvest_Empty()
        {
            var manager = new SoilManager();
            manager.AddPlot("plot_0", SoilType.Loam);

            // 1. Empty → Planted
            bool planted = manager.Plant("plot_0", "seed_tomato");
            Assert.IsTrue(planted);
            Assert.AreEqual(PlotStatus.Planted, manager.GetPlot("plot_0").Status);

            // 2. Planted → Growing (first Tick)
            manager.Tick(0.1f);
            Assert.AreEqual(PlotStatus.Growing, manager.GetPlot("plot_0").Status);

            // 3. Growing → Harvestable (growth system signals)
            manager.MarkHarvestable("plot_0");
            Assert.AreEqual(PlotStatus.Harvestable, manager.GetPlot("plot_0").Status);

            // 4. Harvestable → Empty (or Depleted)
            float nutrientsBefore = manager.GetPlot("plot_0").Nutrients;
            manager.Harvest("plot_0");

            var plot = manager.GetPlot("plot_0");
            Assert.IsTrue(plot.Status == PlotStatus.Empty || plot.Status == PlotStatus.Depleted);
            Assert.IsNull(plot.CurrentCropId);
            Assert.Less(plot.Nutrients, nutrientsBefore);
        }

        [Test]
        public void SixPlots_AllCycleCorrectly()
        {
            var manager = new SoilManager();
            for (int i = 0; i < 6; i++)
                manager.AddPlot($"plot_{i}", SoilType.Loam);

            Assert.AreEqual(6, manager.AllPlots.Count);

            // Plant all
            foreach (var plot in manager.AllPlots)
                Assert.IsTrue(manager.Plant(plot.PlotId, "seed_carrot"));

            // Tick → Growing
            manager.Tick(0.1f);
            foreach (var plot in manager.AllPlots)
                Assert.AreEqual(PlotStatus.Growing, plot.Status);

            // Mark harvestable
            foreach (var plot in manager.AllPlots)
                manager.MarkHarvestable(plot.PlotId);

            // Harvest all
            foreach (var plot in manager.AllPlots)
                manager.Harvest(plot.PlotId);

            // All should now be Empty or Depleted with no crop id
            foreach (var plot in manager.AllPlots)
            {
                Assert.IsTrue(plot.Status == PlotStatus.Empty || plot.Status == PlotStatus.Depleted);
                Assert.IsNull(plot.CurrentCropId);
            }
        }
    }
}
