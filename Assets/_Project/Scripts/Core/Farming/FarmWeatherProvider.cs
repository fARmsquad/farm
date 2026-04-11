using System;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Owns the current weather state for the farm simulation.
    /// Supports manual forcing (testbed, debug) and optional automatic
    /// time-based transitions.  Pure C# — no UnityEngine dependency.
    /// </summary>
    public sealed class FarmWeatherProvider
    {
        // ── State ────────────────────────────────────────────────────────────

        public WeatherType Current { get; private set; } = WeatherType.Sunny;

        /// <summary>When true, Tick() will not change the weather automatically.</summary>
        public bool IsForced { get; private set; }

        /// <summary>Seconds remaining in the current weather window (when not forced).</summary>
        public float SecondsRemaining { get; private set; }

        // ── Config ───────────────────────────────────────────────────────────

        /// <summary>Minimum real seconds a weather state lasts before auto-transitioning.</summary>
        public float MinWeatherDuration { get; set; } = 60f;

        /// <summary>Maximum real seconds a weather state lasts before auto-transitioning.</summary>
        public float MaxWeatherDuration { get; set; } = 180f;

        // ── Events ───────────────────────────────────────────────────────────

        public event Action<WeatherType, WeatherType> OnWeatherChanged;

        // ── Rng ──────────────────────────────────────────────────────────────

        private readonly Random _rng;

        // ── Construction ─────────────────────────────────────────────────────

        public FarmWeatherProvider(WeatherType initial = WeatherType.Sunny, int seed = 0)
        {
            Current          = initial;
            _rng             = seed == 0 ? new Random() : new Random(seed);
            SecondsRemaining = NextDuration();
        }

        // ── API ──────────────────────────────────────────────────────────────

        /// <summary>Force a specific weather state. Disables automatic transitions.</summary>
        public void Force(WeatherType weather)
        {
            IsForced = true;
            SetWeather(weather);
        }

        /// <summary>Release forced state and resume automatic transitions.</summary>
        public void ReleaseForce()
        {
            IsForced         = false;
            SecondsRemaining = NextDuration();
        }

        /// <summary>
        /// Advance weather logic by <paramref name="realDeltaTime"/> real seconds.
        /// If forced, does nothing. Otherwise counts down and transitions when the
        /// current window expires.
        /// </summary>
        public void Tick(float realDeltaTime)
        {
            if (IsForced || realDeltaTime <= 0f) return;

            SecondsRemaining -= realDeltaTime;
            if (SecondsRemaining <= 0f)
            {
                SetWeather(NextWeather());
                SecondsRemaining = NextDuration();
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void SetWeather(WeatherType next)
        {
            if (next == Current) return;
            var prev = Current;
            Current = next;
            OnWeatherChanged?.Invoke(prev, next);
        }

        /// <summary>
        /// Simple weighted transition.
        /// Sunny → mostly stays Sunny, sometimes Cloudy.
        /// Cloudy → can go to Sunny or Rain equally.
        /// Rain   → mostly returns to Cloudy.
        /// </summary>
        private WeatherType NextWeather()
        {
            double roll = _rng.NextDouble();
            return Current switch
            {
                WeatherType.Sunny  => roll < 0.70 ? WeatherType.Sunny  : WeatherType.Cloudy,
                WeatherType.Cloudy => roll < 0.45 ? WeatherType.Sunny  : WeatherType.Rain,
                WeatherType.Rain   => roll < 0.20 ? WeatherType.Rain   : WeatherType.Cloudy,
                _                  => WeatherType.Sunny,
            };
        }

        private float NextDuration() =>
            (float)(_rng.NextDouble() * (MaxWeatherDuration - MinWeatherDuration) + MinWeatherDuration);
    }
}
