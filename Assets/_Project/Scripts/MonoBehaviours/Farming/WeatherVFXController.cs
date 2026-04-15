using FarmSimVR.Core.Farming;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Instantiates artist-made PolygonNature VFX prefabs for weather effects
    /// and manages their emission at runtime.
    /// </summary>
    public sealed class WeatherVFXController : MonoBehaviour
    {
        // ── Temperature tint endpoints ────────────────────────────────────
        private static readonly Color COLD_TINT = new Color(0.75f, 0.85f, 1.00f, 0.65f);
        private static readonly Color WARM_TINT = new Color(1.00f, 0.90f, 0.70f, 0.65f);
        private static readonly Color SNOW_DEFAULT_COLOR = new Color(0.95f, 0.95f, 1.00f, 0.70f);
        private static readonly Color BLIZZARD_DEFAULT_COLOR = new Color(0.90f, 0.92f, 1.00f, 0.80f);
        private static readonly Color HEAT_HAZE_DEFAULT_COLOR = new Color(1.00f, 0.85f, 0.55f, 0.20f);
        private static readonly Color RAIN_DEFAULT_COLOR = new Color(0.72f, 0.84f, 1.00f, 0.55f);

        private static readonly Vector3 SNOW_OFFSET = new Vector3(0f, 5f, 0f);
        private static readonly Vector3 RAIN_OFFSET = new Vector3(0f, 5f, 0f);
        private static readonly Vector3 BLIZZARD_OFFSET = new Vector3(0f, 5f, 0f);
        private static readonly Vector3 HEAT_HAZE_OFFSET = new Vector3(0f, 0.5f, 0f);

        // ── Serialized prefab references ──────────────────────────────────
        [Header("PolygonNature VFX Prefabs")]
        [SerializeField] private GameObject _snowPrefab;
        [SerializeField] private GameObject _rainPrefab;
        [SerializeField] private GameObject _blizzardPrefab;
        [SerializeField] private GameObject _heatHazePrefab;

        // ── Runtime instances ─────────────────────────────────────────────
        private ParticleController _snow;
        private ParticleController _rain;
        private ParticleController _rainChild;
        private ParticleController _blizzard;
        private ParticleController _heatHaze;

        private ParticleSystem _snowSystem;
        private ParticleSystem _rainSystem;
        private ParticleSystem _blizzardSystem;
        private ParticleSystem _heatHazeSystem;

        private WeatherType _currentWeather = WeatherType.Sunny;

        private void Awake()
        {
            CreateParticles();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Enables/disables the relevant particle groups based on weather.
        /// </summary>
        public void SetWeather(WeatherType weather)
        {
            _currentWeather = weather;

            _snow.SetIntensity(weather == WeatherType.Snow ? 1f : 0f);

            float rainIntensity = weather == WeatherType.Rain ? 1f : 0f;
            _rain.SetIntensity(rainIntensity);
            if (_rainChild != null)
                _rainChild.SetIntensity(rainIntensity);

            _blizzard.SetIntensity(weather == WeatherType.Blizzard ? 1f : 0f);
            _heatHaze.SetIntensity(weather == WeatherType.Heatwave ? 1f : 0f);
        }

        /// <summary>
        /// Tints particle colors based on normalised temperature (0 = cold blue, 1 = warm amber).
        /// </summary>
        public void SetTemperature(float normalisedTemp)
        {
            normalisedTemp = Mathf.Clamp01(normalisedTemp);
            Color tint = Color.Lerp(COLD_TINT, WARM_TINT, normalisedTemp);

            ApplyColorTint(_snowSystem, SNOW_DEFAULT_COLOR, tint);
            ApplyColorTint(_rainSystem, RAIN_DEFAULT_COLOR, tint);
            ApplyColorTint(_blizzardSystem, BLIZZARD_DEFAULT_COLOR, tint);
            ApplyColorTint(_heatHazeSystem, HEAT_HAZE_DEFAULT_COLOR, tint);
        }

        private void Update()
        {
            RepositionToCamera();
        }

        // ── Particle creation ─────────────────────────────────────────────

        private void CreateParticles()
        {
            // Snow
            _snow = InstantiatePrefab(_snowPrefab, "Snow_VFX", SNOW_OFFSET);
            _snowSystem = _snow.GetComponent<ParticleSystem>();
            ConfigureSnow(_snowSystem);

            // Rain (two-layer: root = floor splashes, child = falling rain)
            _rain = InstantiatePrefab(_rainPrefab, "Rain_VFX", RAIN_OFFSET);
            _rainSystem = _rain.GetComponent<ParticleSystem>();
            ConfigureRain(_rainSystem);

            var rainChildSystem = _rain.GetComponentInChildren<ParticleSystem>(true);
            if (rainChildSystem != null && rainChildSystem != _rainSystem)
            {
                _rainChild = rainChildSystem.gameObject.GetComponent<ParticleController>();
                if (_rainChild == null)
                    _rainChild = rainChildSystem.gameObject.AddComponent<ParticleController>();
            }

            // Blizzard
            _blizzard = InstantiatePrefab(_blizzardPrefab, "Blizzard_VFX", BLIZZARD_OFFSET);
            _blizzardSystem = _blizzard.GetComponent<ParticleSystem>();
            ConfigureBlizzard(_blizzardSystem);

            // Heat Haze
            _heatHaze = InstantiatePrefab(_heatHazePrefab, "HeatHaze_VFX", HEAT_HAZE_OFFSET);
            _heatHazeSystem = _heatHaze.GetComponent<ParticleSystem>();
            ConfigureHeatHaze(_heatHazeSystem);

            // Start with all off
            _snow.SetIntensity(0f);
            _rain.SetIntensity(0f);
            if (_rainChild != null) _rainChild.SetIntensity(0f);
            _blizzard.SetIntensity(0f);
            _heatHaze.SetIntensity(0f);
        }

        /// <summary>
        /// Instantiates a VFX prefab as a child of this transform, adding a
        /// <see cref="ParticleController"/> wrapper if the prefab doesn't have one.
        /// </summary>
        private ParticleController InstantiatePrefab(GameObject prefab, string name, Vector3 localOffset)
        {
            var instance = Instantiate(prefab, transform);
            instance.name = name;
            instance.transform.localPosition = localOffset;

            var controller = instance.GetComponent<ParticleController>();
            if (controller == null)
                controller = instance.AddComponent<ParticleController>();

            var system = instance.GetComponent<ParticleSystem>();
            if (system != null)
                system.Play();

            return controller;
        }

        // ── Configure methods ─────────────────────────────────────────────

        private static void ConfigureSnow(ParticleSystem system)
        {
            var main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = system.shape;
            shape.scale = new Vector3(12f, 1f, 12f);
        }

        private static void ConfigureRain(ParticleSystem system)
        {
            var main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        private static void ConfigureBlizzard(ParticleSystem system)
        {
            var main = system.main;
            main.startColor = BLIZZARD_DEFAULT_COLOR;
            main.startSpeed = 6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = system.shape;
            shape.rotation = new Vector3(-30f, 0f, 0f);

            var emission = system.emission;
            emission.rateOverTime = 150f;

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 1.5f;
        }

        private static void ConfigureHeatHaze(ParticleSystem system)
        {
            var main = system.main;
            main.startColor = HEAT_HAZE_DEFAULT_COLOR;
            main.startSpeed = 0.3f;
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.rateOverTime = 4f;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static void ApplyColorTint(ParticleSystem system, Color baseColor, Color tint)
        {
            if (system == null) return;
            var main = system.main;
            main.startColor = baseColor * tint;
        }

        private void RepositionToCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            _snow.transform.position = camPos + SNOW_OFFSET;
            _rain.transform.position = camPos + RAIN_OFFSET;
            _blizzard.transform.position = camPos + BLIZZARD_OFFSET;
            _heatHaze.transform.position = camPos + HEAT_HAZE_OFFSET;
        }
    }
}
