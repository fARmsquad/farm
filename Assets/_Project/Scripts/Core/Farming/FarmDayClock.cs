using System;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Tracks normalised time-of-day (0–1) and emits DayPhase transitions.
    /// Pure C# — no UnityEngine dependency.
    /// One full cycle = <see cref="RealSecondsPerDay"/> real seconds.
    /// </summary>
    public class FarmDayClock
    {
        // ── Config ──────────────────────────────────────────────────────────

        /// <summary>How many real seconds make one full in-game day.</summary>
        public float RealSecondsPerDay { get; set; }

        // ── State ───────────────────────────────────────────────────────────

        /// <summary>Normalised time within the current day. 0 = midnight, 0.25 = dawn, 0.5 = noon, 0.75 = dusk.</summary>
        public float NormalisedTime { get; private set; }

        /// <summary>Total number of fully elapsed days since simulation start.</summary>
        public int   DayCount       { get; private set; }

        public DayPhase Phase       { get; private set; } = DayPhase.Night;

        // ── Events ──────────────────────────────────────────────────────────

        /// <summary>Fired whenever the phase changes. Passes (previous, next).</summary>
        public event Action<DayPhase, DayPhase> OnPhaseChanged;

        /// <summary>Fired once at the start of each new day (NormalisedTime wraps past 0).</summary>
        public event Action<int> OnNewDay;

        // ── Construction ────────────────────────────────────────────────────

        /// <param name="realSecondsPerDay">Real seconds per full in-game day. Default 120 s = 2 min day.</param>
        /// <param name="startNormalisedTime">Starting time fraction. 0.25 = dawn.</param>
        public FarmDayClock(float realSecondsPerDay = 120f, float startNormalisedTime = 0.25f)
        {
            RealSecondsPerDay = realSecondsPerDay;
            NormalisedTime    = startNormalisedTime;
            Phase             = PhaseFor(NormalisedTime);
        }

        // ── Tick ────────────────────────────────────────────────────────────

        public void Tick(float realDeltaTime)
        {
            float prev = NormalisedTime;
            NormalisedTime += realDeltaTime / RealSecondsPerDay;

            if (NormalisedTime >= 1f)
            {
                NormalisedTime -= 1f;
                DayCount++;
                OnNewDay?.Invoke(DayCount);
            }

            var newPhase = PhaseFor(NormalisedTime);
            if (newPhase != Phase)
            {
                var old = Phase;
                Phase = newPhase;
                OnPhaseChanged?.Invoke(old, newPhase);
            }
        }

        // ── Time Skip ───────────────────────────────────────────────────────

        /// <summary>
        /// Jumps the clock to a target normalised time.
        /// If the target is at or before the current time, a new day is triggered first.
        /// Fires <see cref="OnNewDay"/> and <see cref="OnPhaseChanged"/> as appropriate.
        /// </summary>
        public void SkipTo(float targetNormalisedTime)
        {
            targetNormalisedTime = Math.Clamp(targetNormalisedTime, 0f, 1f);

            if (targetNormalisedTime <= NormalisedTime)
            {
                DayCount++;
                OnNewDay?.Invoke(DayCount);
            }

            NormalisedTime = targetNormalisedTime;

            var newPhase = PhaseFor(NormalisedTime);
            if (newPhase != Phase)
            {
                var old = Phase;
                Phase = newPhase;
                OnPhaseChanged?.Invoke(old, newPhase);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Map normalised time to a DayPhase.
        /// Dawn 0.20–0.30 | Morning 0.30–0.45 | Noon 0.45–0.55 | Afternoon 0.55–0.70 | Dusk 0.70–0.80 | Night otherwise.
        /// </summary>
        public static DayPhase PhaseFor(float t)
        {
            if (t >= 0.20f && t < 0.30f) return DayPhase.Dawn;
            if (t >= 0.30f && t < 0.45f) return DayPhase.Morning;
            if (t >= 0.45f && t < 0.55f) return DayPhase.Noon;
            if (t >= 0.55f && t < 0.70f) return DayPhase.Afternoon;
            if (t >= 0.70f && t < 0.80f) return DayPhase.Dusk;
            return DayPhase.Night;
        }

        /// <summary>Sun angle in degrees. 0 = horizon east, 90 = overhead, 180 = horizon west.</summary>
        public float SunAngleDegrees()
        {
            // Map 0.25 (dawn) → 0° and 0.75 (dusk) → 180°
            float t = (NormalisedTime - 0.25f) / 0.5f;
            return t * 180f;
        }
    }

    public enum DayPhase
    {
        Night,
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Dusk,
    }
}
