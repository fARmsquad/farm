using System;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Tracks a normalised temperature value (0.0 = freezing, 1.0 = scorching).
    /// Automatically adjusts based on weather type and season.
    /// Visual only — does not influence gameplay mechanics.
    /// Pure C# — no UnityEngine dependency.
    /// </summary>
    public sealed class TemperatureProvider
    {
        // ── Config ───────────────────────────────────────────────────────────

        private const float DEFAULT_LERP_RATE = 0.3f;
        private const float CHANGE_THRESHOLD = 0.05f;

        // ── State ────────────────────────────────────────────────────────────

        /// <summary>Current normalised temperature 0..1.</summary>
        public float NormalisedTemperature { get; private set; } = 0.5f;

        /// <summary>When true, Tick() will not lerp toward target.</summary>
        public bool IsForced { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Fires when temperature shifts significantly (delta > 0.05).</summary>
        public event Action<float> OnTemperatureChanged;

        // ── Internal ─────────────────────────────────────────────────────────

        private float _target = 0.5f;
        private float _lastReportedValue = 0.5f;
        private float _lerpRate;

        // ── Construction ─────────────────────────────────────────────────────

        /// <param name="lerpRate">Rate at which temperature lerps toward target per second.</param>
        public TemperatureProvider(float lerpRate = DEFAULT_LERP_RATE)
        {
            _lerpRate = lerpRate;
        }

        // ── API ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Recalculates the target temperature based on weather and season.
        /// </summary>
        public void SetFromWeather(WeatherType weather, FarmSeason season)
        {
            float seasonBase = season switch
            {
                FarmSeason.Spring => 0.45f,
                FarmSeason.Summer => 0.70f,
                FarmSeason.Autumn => 0.40f,
                FarmSeason.Winter => 0.20f,
                _                => 0.50f,
            };

            float weatherOffset = weather switch
            {
                WeatherType.Sunny    =>  0.15f,
                WeatherType.Cloudy   => -0.05f,
                WeatherType.Rain     => -0.10f,
                _                    =>  0.00f,
            };

            _target = Clamp01(seasonBase + weatherOffset);
        }

        /// <summary>
        /// Smoothly lerps current temperature toward the target.
        /// Call once per frame with deltaTime.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (IsForced || deltaTime <= 0f) return;

            float diff = _target - NormalisedTemperature;
            float step = _lerpRate * deltaTime;
            NormalisedTemperature = Math.Abs(diff) < step
                ? _target
                : NormalisedTemperature + Math.Sign(diff) * step;

            NormalisedTemperature = Clamp01(NormalisedTemperature);
            CheckAndReport();
        }

        /// <summary>
        /// Debug override — immediately sets temperature to the given value.
        /// </summary>
        public void Force(float value)
        {
            IsForced = true;
            NormalisedTemperature = Clamp01(value);
            _target = NormalisedTemperature;
            CheckAndReport();
        }

        /// <summary>
        /// Release forced state and resume automatic temperature lerping.
        /// </summary>
        public void ReleaseForce()
        {
            IsForced = false;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void CheckAndReport()
        {
            if (Math.Abs(NormalisedTemperature - _lastReportedValue) >= CHANGE_THRESHOLD)
            {
                _lastReportedValue = NormalisedTemperature;
                OnTemperatureChanged?.Invoke(NormalisedTemperature);
            }
        }

        private static float Clamp01(float value) =>
            value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
