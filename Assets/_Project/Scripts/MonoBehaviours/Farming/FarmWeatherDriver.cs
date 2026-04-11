using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Owns the FarmWeatherProvider, ticks it each frame, applies rain
    /// moisture passively to all soil plots, and notifies the lighting
    /// controller of weather changes.
    ///
    /// Attach to the same GameObject as FarmDayClockDriver.
    /// </summary>
    public sealed class FarmWeatherDriver : MonoBehaviour
    {
        [Header("Auto Transition Durations (real seconds)")]
        [SerializeField] private float minWeatherDuration = 60f;
        [SerializeField] private float maxWeatherDuration = 180f;

        [Header("Rain Effect")]
        [Tooltip("Moisture added to each plot per real second while raining.")]
        [SerializeField] private float rainMoisturePerSecond = 0.05f;

        public static FarmWeatherDriver Instance { get; private set; }
        public FarmWeatherProvider Provider { get; private set; }

        // Set by FarmSimDriver after it builds the SoilManager.
        private SoilManager _soil;
        private FarmLightingController _lighting;
        private WorldFarmProgressionController _progression;

        public void Initialize(SoilManager soil, FarmLightingController lighting)
        {
            _soil    = soil;
            _lighting = lighting;
        }

        private void Awake()
        {
            Instance = this;
            Provider = new FarmWeatherProvider(WeatherType.Sunny)
            {
                MinWeatherDuration = minWeatherDuration,
                MaxWeatherDuration = maxWeatherDuration,
            };

            Provider.OnWeatherChanged += (prev, next) =>
            {
                Debug.Log($"[FarmWeather] {prev} → {next}");
                _lighting?.ApplyWeather(next);
            };
        }

        private void Update()
        {
            if (_progression == null)
                _progression = GetComponent<WorldFarmProgressionController>() ?? FindAnyObjectByType<WorldFarmProgressionController>();

            Provider.Tick(Time.deltaTime);

            // Passive rain watering — applies to every registered plot.
            if (Provider.Current == WeatherType.Rain && _soil != null)
            {
                var multiplier = _progression != null ? _progression.RainMultiplier : 1f;
                float amount = rainMoisturePerSecond * multiplier * Time.deltaTime;
                foreach (var plot in _soil.AllPlots)
                    _soil.Water(plot.PlotId, amount);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
