using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Drives the farm's directional light to match the current day phase.
    /// Assign the scene's Sun directional light in the Inspector.
    /// This is a pure presentation layer — it reads from FarmDayClock and
    /// never writes back to it.
    /// </summary>
    public sealed class FarmLightingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Light sunLight;

        [Header("Sun Colours Per Phase")]
        [SerializeField] private Color nightColour     = new Color(0.05f, 0.05f, 0.15f);
        [SerializeField] private Color dawnColour      = new Color(1.00f, 0.60f, 0.30f);
        [SerializeField] private Color morningColour   = new Color(1.00f, 0.90f, 0.70f);
        [SerializeField] private Color noonColour      = new Color(1.00f, 1.00f, 0.95f);
        [SerializeField] private Color afternoonColour = new Color(1.00f, 0.85f, 0.60f);
        [SerializeField] private Color duskColour      = new Color(0.90f, 0.40f, 0.20f);

        [Header("Sun Intensities Per Phase")]
        [SerializeField] private float nightIntensity     = 0.05f;
        [SerializeField] private float dawnIntensity      = 0.40f;
        [SerializeField] private float morningIntensity   = 0.80f;
        [SerializeField] private float noonIntensity      = 1.10f;
        [SerializeField] private float afternoonIntensity = 0.90f;
        [SerializeField] private float duskIntensity      = 0.45f;

        [Header("Transition")]
        [SerializeField] private float transitionSpeed = 1.5f;

        private Color  _targetColour;
        private float  _targetIntensity;
        private float  _targetPitch; // X rotation (sun elevation)

        // Weather dims the phase colour and intensity (0 = no dimming, 1 = full).
        private float _weatherDim;

        // Season tints the final light colour additively.
        private Color _seasonTint = Color.white;

        private void Awake()
        {
            TryResolveSunLight();
        }

        public void ApplyPhase(DayPhase phase, float sunAngleDegrees)
        {
            (Color baseCol, float baseInt) = phase switch
            {
                DayPhase.Dawn      => (dawnColour,      dawnIntensity),
                DayPhase.Morning   => (morningColour,   morningIntensity),
                DayPhase.Noon      => (noonColour,      noonIntensity),
                DayPhase.Afternoon => (afternoonColour, afternoonIntensity),
                DayPhase.Dusk      => (duskColour,      duskIntensity),
                _                  => (nightColour,     nightIntensity),
            };

            // Apply weather dimming then season tint.
            Color weathered = Color.Lerp(baseCol, new Color(0.55f, 0.60f, 0.70f), _weatherDim);
            _targetColour    = weathered * _seasonTint;
            _targetIntensity = Mathf.Lerp(baseInt, baseInt * 0.45f, _weatherDim);

            // Sun pitch: arc from -10° (below horizon) to 80° (high noon) to -10° again.
            _targetPitch = Mathf.Sin(Mathf.Clamp01(sunAngleDegrees / 180f) * Mathf.PI) * 80f - 10f;
        }

        /// <summary>Called by FarmWeatherDriver when weather changes.</summary>
        public void ApplyWeather(WeatherType weather)
        {
            _weatherDim = weather switch
            {
                WeatherType.Sunny  => 0.00f,
                WeatherType.Cloudy => 0.40f,
                WeatherType.Rain   => 0.75f,
                _                  => 0.00f,
            };
        }

        /// <summary>Called by FarmSeasonDriver when the season changes.</summary>
        public void ApplySeason(FarmSeason season)
        {
            // Each season shifts the colour temperature of daylight slightly.
            _seasonTint = season switch
            {
                FarmSeason.Spring => new Color(0.95f, 1.00f, 0.95f), // fresh green-white
                FarmSeason.Summer => new Color(1.00f, 1.00f, 0.90f), // warm yellow-white
                FarmSeason.Autumn => new Color(1.00f, 0.90f, 0.75f), // golden amber
                FarmSeason.Winter => new Color(0.85f, 0.90f, 1.00f), // cool blue-white
                _                 => Color.white,
            };
        }

        private void Update()
        {
            TryResolveSunLight();
            if (sunLight == null) return;
            float t = Time.deltaTime * transitionSpeed;
            sunLight.color     = Color.Lerp(sunLight.color, _targetColour, t);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, _targetIntensity, t);
            var eu = sunLight.transform.eulerAngles;
            float current = eu.x > 180f ? eu.x - 360f : eu.x;
            sunLight.transform.eulerAngles = new Vector3(
                Mathf.LerpAngle(current, _targetPitch, t), eu.y, eu.z);
        }

        private void TryResolveSunLight()
        {
            if (sunLight != null)
                return;

            if (TryGetComponent(out Light ownLight) && ownLight.type == LightType.Directional)
            {
                sunLight = ownLight;
                return;
            }

            if (RenderSettings.sun != null)
                sunLight = RenderSettings.sun;
        }
    }
}
