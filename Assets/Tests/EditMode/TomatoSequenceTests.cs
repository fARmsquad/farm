using System.Collections.Generic;
using NUnit.Framework;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.Tests.EditMode
{
    /// <summary>
    /// Verifies the complete tomato growth sequence:
    ///   - Phase order is exactly Planted → Sprout → YoungPlant → Budding → Fruiting → Ready
    ///   - Each phase lasts ≈ 5 seconds under standard conditions (±20% tolerance for frame-step error)
    ///   - Wilt triggers correctly, recovery works, death is irreversible without ClearDead
    ///   - Soil type modifiers affect timing proportionally
    ///   - Moisture bonus (>0.6) shortens cycle correctly
    ///   - Milestones fire once per gate in the right order
    ///
    /// Standard conditions (the benchmark):
    ///   WeatherType.Sunny, 22°C, SoilQuality.Normal, seasonMultiplier=1
    ///   → weather×1, temp×1, soil×1 → effective rate = baseRate × 1.0 = 0.04/s
    ///   → each 0.2-unit gate = 0.2 / 0.04 = 5.0 s exactly
    /// </summary>
    [TestFixture]
    public class TomatoSequenceTests
    {
        // ── Canonical tomato crop data (mirrors FarmSimDriver) ─────────────
        private const float TomatoRate  = 0.04f;
        private const float TomatoMax   = 1.0f;
        private const float GateSize    = 0.2f;    // MaxGrowth / 5 phases
        private const float BenchmarkSeconds = GateSize / TomatoRate; // = 5.0s
        private const float TimingTolerance  = 0.20f;  // ±20%

        private static readonly CropData TomatoData =
            new CropData(TomatoRate, TomatoMax);

        // Standard = Sunny/22°C/Normal/1.0 → all multipliers = 1.0
        private static readonly GrowthConditions Standard =
            new GrowthConditions(WeatherType.Sunny, 22f, SoilQuality.Normal, 1f);

        // Well-watered bonus: moisture >0.6 → ×1.25 in CropPlotState
        // → effective rate = 0.04 × 1.25 = 0.05/s → gate = 0.2 / 0.05 = 4.0s
        private const float WateredGateSeconds = GateSize / (TomatoRate * 1.25f);

        // Rain weather multiplier ×2 → rate = 0.08/s → gate = 2.5s
        private static readonly GrowthConditions RainConditions =
            new GrowthConditions(WeatherType.Rain, 22f, SoilQuality.Normal, 1f);

        // Rich soil ×1.5 → rate = 0.06/s → gate = 0.2/0.06 ≈ 3.33s
        private static readonly GrowthConditions RichConditions =
            new GrowthConditions(WeatherType.Sunny, 22f, SoilQuality.Rich, 1f);

        // Cold temperature penalty ×0.5 → rate = 0.02/s → gate = 10.0s
        private static readonly GrowthConditions ColdConditions =
            new GrowthConditions(WeatherType.Sunny, 5f, SoilQuality.Normal, 1f);

        private CropPlotState MakePlot(float moisture = 1f)
        {
            var p = new CropPlotState(new CropGrowthCalculator());
            p.SetMoisture(moisture);
            return p;
        }

        private static void Germinate(CropPlotState plot, GrowthConditions conditions, float stepSize = 0.01f)
        {
            plot.NotifyWatered();
            plot.Tick(conditions, stepSize);
        }

        // ─────────────────────────────────────────────────────────────────
        // SECTION 1: Phase order
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Drives the simulation with a tiny fixed step and records the first tick
        /// each phase is observed in. Verifies the canonical order.
        /// </summary>
        [Test]
        public void TomatoSequence_PhaseOrder_IsExact()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            plot.NotifyWatered();

            var observedOrder = new List<PlotPhase>();
            PlotPhase prev = PlotPhase.Planted;
            observedOrder.Add(prev);

            // Step in 0.1s increments up to 60s (well past full cycle)
            for (int i = 0; i < 600; i++)
            {
                plot.SetMoisture(1f);
                plot.Tick(Standard, 0.1f);

                if (plot.Phase != prev)
                {
                    observedOrder.Add(plot.Phase);
                    prev = plot.Phase;
                }

                if (plot.Phase == PlotPhase.Ready)
                    break;
            }

            var expectedOrder = new List<PlotPhase>
            {
                PlotPhase.Planted,
                PlotPhase.Sprout,
                PlotPhase.YoungPlant,
                PlotPhase.Budding,
                PlotPhase.Fruiting,
                PlotPhase.Ready
            };

            Assert.AreEqual(expectedOrder, observedOrder,
                $"Expected {string.Join("→", expectedOrder)} but got {string.Join("→", observedOrder)}");
        }

        [Test]
        public void TomatoSequence_NoPhaseIsSkipped()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            plot.NotifyWatered();

            var seen = new HashSet<PlotPhase> { PlotPhase.Planted };
            for (int i = 0; i < 600; i++)
            {
                plot.SetMoisture(1f);
                plot.Tick(Standard, 0.1f);
                seen.Add(plot.Phase);
                if (plot.Phase == PlotPhase.Ready) break;
            }

            Assert.IsTrue(seen.Contains(PlotPhase.Sprout),     "Sprout was never observed");
            Assert.IsTrue(seen.Contains(PlotPhase.YoungPlant),  "YoungPlant was never observed");
            Assert.IsTrue(seen.Contains(PlotPhase.Budding),     "Budding was never observed");
            Assert.IsTrue(seen.Contains(PlotPhase.Fruiting),    "Fruiting was never observed");
            Assert.IsTrue(seen.Contains(PlotPhase.Ready),       "Ready was never observed");
        }

        // ─────────────────────────────────────────────────────────────────
        // SECTION 2: Phase timing benchmarks (Standard conditions)
        // ─────────────────────────────────────────────────────────────────

        private struct PhaseTimer
        {
            public PlotPhase Phase;
            public float EnterTime;
            public float ExitTime;
            public float DurationSeconds => ExitTime - EnterTime;
        }

        private List<PhaseTimer> RunFullCycleAndTimePhases(
            CropPlotState plot,
            GrowthConditions conditions,
            float stepSize = 0.1f,
            int maxSteps = 2000)
        {
            var timers = new List<PhaseTimer>();
            PlotPhase current = plot.Phase;
            float elapsed = 0f;
            float phaseEnter = 0f;
            timers.Add(new PhaseTimer { Phase = current, EnterTime = 0f });

            for (int i = 0; i < maxSteps; i++)
            {
                plot.SetMoisture(1f);
                plot.Tick(conditions, stepSize);
                elapsed += stepSize;

                if (plot.Phase != current)
                {
                    // Close out the previous timer
                    var t = timers[timers.Count - 1];
                    t.ExitTime = elapsed;
                    timers[timers.Count - 1] = t;

                    current = plot.Phase;
                    phaseEnter = elapsed;
                    timers.Add(new PhaseTimer { Phase = current, EnterTime = phaseEnter });
                }

                if (plot.Phase == PlotPhase.Ready)
                {
                    var t = timers[timers.Count - 1];
                    t.ExitTime = elapsed;
                    timers[timers.Count - 1] = t;
                    break;
                }
            }

            return timers;
        }

        [Test]
        public void Benchmark_Standard_EachGrowthPhase_Lasts_5s_PlusMinus20Pct()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            // Advance past Planted into Sprout with a tiny tick
            Germinate(plot, Standard);
            Assert.AreEqual(PlotPhase.Sprout, plot.Phase, "Expected Sprout after initial tick");

            var timers = RunFullCycleAndTimePhases(plot, Standard);

            // Growth phases to benchmark (Planted is a gate, not a timed grow phase)
            var growthPhases = new[] { PlotPhase.Sprout, PlotPhase.YoungPlant, PlotPhase.Budding, PlotPhase.Fruiting };
            float lo = BenchmarkSeconds * (1f - TimingTolerance);
            float hi = BenchmarkSeconds * (1f + TimingTolerance);

            foreach (var phase in growthPhases)
            {
                var timer = timers.Find(t => t.Phase == phase);
                Assert.IsNotNull(timer.Phase.ToString(),
                    $"Phase {phase} was not found in timer list — it was never entered");
                Assert.That(timer.DurationSeconds, Is.InRange(lo, hi),
                    $"Phase {phase} lasted {timer.DurationSeconds:F2}s — expected {BenchmarkSeconds:F2}s ±{TimingTolerance*100}%");
            }
        }

        [Test]
        public void Benchmark_Standard_TotalCycleTime_Is_25s_PlusMinus20Pct()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, Standard);

            float elapsed = 0.01f;
            for (int i = 0; i < 2000; i++)
            {
                plot.SetMoisture(1f);
                plot.Tick(Standard, 0.1f);
                elapsed += 0.1f;
                if (plot.Phase == PlotPhase.Ready) break;
            }

            // 5 gates × 5s = 25s
            float expectedTotal = 5f * BenchmarkSeconds;
            float lo = expectedTotal * (1f - TimingTolerance);
            float hi = expectedTotal * (1f + TimingTolerance);

            Assert.That(elapsed, Is.InRange(lo, hi),
                $"Total cycle = {elapsed:F2}s — expected {expectedTotal:F2}s ±{TimingTolerance*100}%");
        }

        // ─────────────────────────────────────────────────────────────────
        // SECTION 3: Condition modifiers scale timing correctly
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Benchmark_Rain_HalvesPhaseTime_Vs_Sunny()
        {
            // Rain = ×2 → gate should be ≈2.5s vs 5s
            float rainGate = GateSize / (TomatoRate * 2f); // 2.5s

            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, RainConditions);

            var timers = RunFullCycleAndTimePhases(plot, RainConditions);
            var sproutTimer = timers.Find(t => t.Phase == PlotPhase.Sprout);

            float lo = rainGate * (1f - TimingTolerance);
            float hi = rainGate * (1f + TimingTolerance);

            Assert.That(sproutTimer.DurationSeconds, Is.InRange(lo, hi),
                $"Sprout under Rain lasted {sproutTimer.DurationSeconds:F2}s — expected ≈{rainGate:F2}s");
        }

        [Test]
        public void Benchmark_ColdTemp_DoublesPhaseTime_Vs_Standard()
        {
            // Cold ×0.5 → gate = 10s
            float coldGate = GateSize / (TomatoRate * 0.5f); // 10s

            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, ColdConditions);

            var timers = RunFullCycleAndTimePhases(plot, ColdConditions, stepSize: 0.1f, maxSteps: 4000);
            var sproutTimer = timers.Find(t => t.Phase == PlotPhase.Sprout);

            float lo = coldGate * (1f - TimingTolerance);
            float hi = coldGate * (1f + TimingTolerance);

            Assert.That(sproutTimer.DurationSeconds, Is.InRange(lo, hi),
                $"Sprout under Cold lasted {sproutTimer.DurationSeconds:F2}s — expected ≈{coldGate:F2}s");
        }

        [Test]
        public void Benchmark_WellWatered_Moisture_ShortensGateBy25Pct()
        {
            // Moisture > 0.6 gives ×1.25 in Tick → gate = 4.0s
            float lo = WateredGateSeconds * (1f - TimingTolerance);
            float hi = WateredGateSeconds * (1f + TimingTolerance);

            var plot = MakePlot(0.8f); // well-watered
            plot.Plant(TomatoData);
            Germinate(plot, Standard);

            var timers = RunFullCycleAndTimePhases(plot, Standard);
            var sproutTimer = timers.Find(t => t.Phase == PlotPhase.Sprout);

            Assert.That(sproutTimer.DurationSeconds, Is.InRange(lo, hi),
                $"Well-watered Sprout lasted {sproutTimer.DurationSeconds:F2}s — expected ≈{WateredGateSeconds:F2}s");
        }

        // ─────────────────────────────────────────────────────────────────
        // SECTION 4: Wilt / recovery / death
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Wilt_OnlyTriggers_InGrowthPhases_Not_InPlanted()
        {
            // Planted phase should block growth but NOT wilt — no crop is invested yet
            var plot = MakePlot(0f);
            plot.Plant(TomatoData);
            plot.SetMoisture(0f);
            plot.Tick(Standard, 1f);
            Assert.AreEqual(PlotPhase.Planted, plot.Phase,
                "Planted phase should stay Planted on dry soil, not wilt");
        }

        [Test]
        public void Wilt_Fires_OnWilting_Event()
        {
            bool fired = false;
            var plot = MakePlot(1f);
            plot.OnWilting += () => fired = true;

            plot.Plant(TomatoData);
            Germinate(plot, Standard, 0.1f);  // Sprout
            plot.SetMoisture(0f);
            plot.Tick(Standard, 0.1f);

            Assert.IsTrue(fired, "OnWilting event should fire when moisture drops below threshold");
        }

        [Test]
        public void Wilt_GrowthPauses_DuringWilting()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, Standard, 0.1f);
            float growthBeforeWilt = plot.CurrentGrowth;

            plot.SetMoisture(0f);
            plot.Tick(Standard, 5f); // 5s in wilt — no growth
            Assert.AreEqual(growthBeforeWilt, plot.CurrentGrowth,
                "Growth should be frozen while wilting");
        }

        [Test]
        public void Wilt_Recovery_RestoresToSamePhase()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, Standard, 0.1f); // Sprout

            plot.SetMoisture(0f);
            plot.Tick(Standard, 0.1f); // wilt
            Assert.AreEqual(PlotPhase.Wilting, plot.Phase);

            plot.SetMoisture(1f);
            bool recovered = false;
            plot.OnRevived += () => recovered = true;
            plot.Tick(Standard, 0.1f);

            Assert.IsTrue(recovered, "OnRevived should fire on recovery");
            Assert.AreEqual(PlotPhase.Sprout, plot.Phase,
                "Plant should return to Sprout after recovery from Wilting");
        }

        [Test]
        public void Wilt_Recovery_ThenContinuesGrowth()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, Standard, 0.1f); // Sprout
            float growthAtWilt = plot.CurrentGrowth;

            plot.SetMoisture(0f);
            plot.Tick(Standard, 4f); // wilt — 4s dry (under death threshold)

            plot.SetMoisture(1f);
            plot.Tick(Standard, 0.1f); // recover
            Assert.AreEqual(PlotPhase.Sprout, plot.Phase);

            plot.Tick(Standard, 0.5f); // grow again
            Assert.Greater(plot.CurrentGrowth, growthAtWilt,
                "Growth should resume above pre-wilt value after recovery");
        }

        [Test]
        public void Death_FiresAfter_DeathTimeoutSeconds_Of_DryWilting()
        {
            bool died = false;
            var plot = MakePlot(1f);
            plot.OnDead += () => died = true;

            plot.Plant(TomatoData);
            Germinate(plot, Standard, 0.1f); // Sprout
            plot.SetMoisture(0f);
            plot.Tick(Standard, CropPlotState.DeathTimeoutSeconds + 0.5f);

            Assert.AreEqual(PlotPhase.Dead, plot.Phase);
            Assert.IsTrue(died, "OnDead event should have fired");
        }

        [Test]
        public void Death_JustBeforeTimeout_DoesNotDie()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, Standard, 0.1f);
            plot.SetMoisture(0f);
            plot.Tick(Standard, CropPlotState.DeathTimeoutSeconds - 0.5f);

            Assert.AreEqual(PlotPhase.Wilting, plot.Phase,
                "Should still be Wilting just before the death timeout");
        }

        [Test]
        public void Dead_Plot_DoesNotGrow_OrWiltAgain()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, Standard, 0.1f);
            plot.SetMoisture(0f);
            plot.Tick(Standard, CropPlotState.DeathTimeoutSeconds + 1f);
            Assert.AreEqual(PlotPhase.Dead, plot.Phase);

            // Tick with or without moisture — should stay Dead
            plot.SetMoisture(1f);
            plot.Tick(Standard, 10f);
            Assert.AreEqual(PlotPhase.Dead, plot.Phase);
        }

        [Test]
        public void ClearDead_AllowsReplanting()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            Germinate(plot, Standard, 0.1f);
            plot.SetMoisture(0f);
            plot.Tick(Standard, CropPlotState.DeathTimeoutSeconds + 1f);

            plot.ClearDead();
            Assert.AreEqual(PlotPhase.Empty, plot.Phase);
            Assert.DoesNotThrow(() => plot.Plant(TomatoData));
        }

        // ─────────────────────────────────────────────────────────────────
        // SECTION 5: Milestone event contracts
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Milestones_FiredExactly_5Times_InOrder_1Through5()
        {
            var gates = new List<int>();
            var plot = MakePlot(1f);
            plot.OnMilestone += g => gates.Add(g);

            plot.Plant(TomatoData);
            plot.SetMoisture(1f);
            plot.NotifyWatered();
            // Drive all the way through with small steps
            for (int i = 0; i < 2000; i++)
            {
                plot.SetMoisture(1f);
                plot.Tick(Standard, 0.1f);
                if (plot.Phase == PlotPhase.Ready) break;
            }

            Assert.AreEqual(5, gates.Count,
                $"Expected 5 milestones but got {gates.Count}: [{string.Join(", ", gates)}]");
            Assert.AreEqual(new List<int> { 1, 2, 3, 4, 5 }, gates,
                "Milestones must fire in gate order 1→5");
        }

        [Test]
        public void Milestones_DoNotFireDuplicate_OnMultipleTicks()
        {
            var gates = new List<int>();
            var plot = MakePlot(1f);
            plot.OnMilestone += g => gates.Add(g);

            plot.Plant(TomatoData);
            plot.NotifyWatered();
            // Drive past Sprout gate multiple times in tiny steps
            for (int i = 0; i < 500; i++)
            {
                plot.SetMoisture(1f);
                plot.Tick(Standard, 0.01f);
                if (plot.Phase == PlotPhase.YoungPlant) break;
            }

            int milestone1Count = gates.FindAll(g => g == 1).Count;
            int milestone2Count = gates.FindAll(g => g == 2).Count;

            Assert.AreEqual(1, milestone1Count, "Milestone 1 (Sprout) fired more than once");
            Assert.LessOrEqual(milestone2Count, 1, "Milestone 2 (YoungPlant) fired more than once");
        }

        // ─────────────────────────────────────────────────────────────────
        // SECTION 6: GrowthPercent contracts
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void GrowthPercent_IsAlways_Clamped_0_To_1()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            plot.NotifyWatered();

            Assert.AreEqual(0f, plot.GrowthPercent);

            for (int i = 0; i < 1000; i++)
            {
                plot.SetMoisture(1f);
                plot.Tick(Standard, 0.05f);
                Assert.GreaterOrEqual(plot.GrowthPercent, 0f,
                    $"GrowthPercent went below 0 at step {i}");
                Assert.LessOrEqual(plot.GrowthPercent, 1f,
                    $"GrowthPercent exceeded 1 at step {i}");
                if (plot.Phase == PlotPhase.Ready) break;
            }
        }

        [Test]
        public void GrowthPercent_IsMonotonicallyIncreasing_WithConstantMoisture()
        {
            var plot = MakePlot(1f);
            plot.Plant(TomatoData);
            plot.NotifyWatered();

            float prev = 0f;
            for (int i = 0; i < 1000; i++)
            {
                plot.SetMoisture(1f);
                plot.Tick(Standard, 0.05f);
                Assert.GreaterOrEqual(plot.GrowthPercent, prev,
                    $"GrowthPercent decreased from {prev:F4} to {plot.GrowthPercent:F4} at step {i}");
                prev = plot.GrowthPercent;
                if (plot.Phase == PlotPhase.Ready) break;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // SECTION 7: SoilTypeDefaults moisture drain benchmarks
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void Loam_DrainsBelowWiltThreshold_In_33s_To_44s()
        {
            var soil = new SoilState("plot_loam", SoilType.Loam);
            // initialMoisture=0.8, decayRate=0.018/s → dry in (0.8-0.2)/0.018 ≈ 33.3s
            float elapsed = 0f;
            while (soil.Moisture >= CropPlotState.WiltThreshold && elapsed < 120f)
            {
                soil.DeductMoisture(soil.MoistureDecayRate * 0.1f);
                elapsed += 0.1f;
            }

            Assert.That(elapsed, Is.InRange(33f, 44f),
                $"Loam drained to wilt in {elapsed:F1}s — expected 33–44s for one required watering mid-cycle");
        }

        [Test]
        public void Sandy_DrainsBelowWiltThreshold_In_18s_To_27s()
        {
            var soil = new SoilState("plot_sandy", SoilType.Sandy);
            // initialMoisture=0.8, decayRate=0.030/s → dry in (0.8-0.2)/0.030 = 20s
            float elapsed = 0f;
            while (soil.Moisture >= CropPlotState.WiltThreshold && elapsed < 120f)
            {
                soil.DeductMoisture(soil.MoistureDecayRate * 0.1f);
                elapsed += 0.1f;
            }

            Assert.That(elapsed, Is.InRange(18f, 27f),
                $"Sandy drained to wilt in {elapsed:F1}s — expected 18–27s (two waterings required per cycle)");
        }

        [Test]
        public void Clay_DrainsBelowWiltThreshold_In_54s_To_80s()
        {
            var soil = new SoilState("plot_clay", SoilType.Clay);
            // initialMoisture=0.8, decayRate=0.010/s → dry in (0.8-0.2)/0.010 = 60s
            float elapsed = 0f;
            while (soil.Moisture >= CropPlotState.WiltThreshold && elapsed < 180f)
            {
                soil.DeductMoisture(soil.MoistureDecayRate * 0.1f);
                elapsed += 0.1f;
            }

            Assert.That(elapsed, Is.InRange(54f, 80f),
                $"Clay drained to wilt in {elapsed:F1}s — expected 54–80s (forgiving, one watering sufficient)");
        }

        [Test]
        public void DeathTimeout_Is_8s_Constant()
        {
            Assert.AreEqual(8f, CropPlotState.DeathTimeoutSeconds,
                "DeathTimeoutSeconds must remain 8s — change this only with a deliberate design decision");
        }

        [Test]
        public void WiltThreshold_Is_20Pct()
        {
            Assert.AreEqual(0.20f, CropPlotState.WiltThreshold, 0.001f,
                "WiltThreshold must be 0.20 — change this only with a deliberate design decision");
        }
    }
}
