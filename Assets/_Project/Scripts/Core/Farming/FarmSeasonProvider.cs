using System;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Tracks the current farm season and transitions it every
    /// <see cref="DaysPerSeason"/> in-game days.
    /// Pure C# — no UnityEngine dependency.
    /// </summary>
    public sealed class FarmSeasonProvider
    {
        // ── Config ───────────────────────────────────────────────────────────

        /// <summary>How many in-game days each season lasts.</summary>
        public int DaysPerSeason { get; set; }

        // ── State ────────────────────────────────────────────────────────────

        public FarmSeason Current  { get; private set; }
        public int        DayOfSeason { get; private set; }  // 0-based within the season

        // ── Events ───────────────────────────────────────────────────────────

        public event Action<FarmSeason, FarmSeason> OnSeasonChanged;

        // ── Construction ─────────────────────────────────────────────────────

        /// <param name="daysPerSeason">In-game days per season. Default 7.</param>
        /// <param name="startSeason">Season to begin in.</param>
        public FarmSeasonProvider(int daysPerSeason = 7, FarmSeason startSeason = FarmSeason.Spring)
        {
            DaysPerSeason = daysPerSeason;
            Current       = startSeason;
            DayOfSeason   = 0;
        }

        // ── API ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Advance the season tracker by one in-game day.
        /// Call this from FarmDayClock.OnNewDay.
        /// </summary>
        public void OnDayElapsed()
        {
            DayOfSeason++;
            if (DayOfSeason >= DaysPerSeason)
            {
                DayOfSeason = 0;
                Advance();
            }
        }

        /// <summary>Force an immediate season change (debug / testbed use).</summary>
        public void Force(FarmSeason season)
        {
            var prev = Current;
            Current     = season;
            DayOfSeason = 0;
            if (season != prev) OnSeasonChanged?.Invoke(prev, season);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void Advance()
        {
            var prev = Current;
            Current = Current switch
            {
                FarmSeason.Spring => FarmSeason.Summer,
                FarmSeason.Summer => FarmSeason.Autumn,
                FarmSeason.Autumn => FarmSeason.Winter,
                FarmSeason.Winter => FarmSeason.Spring,
                _                 => FarmSeason.Spring,
            };
            OnSeasonChanged?.Invoke(prev, Current);
        }
    }
}
