using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours.Farming
{
    /// <summary>
    /// Drives the farm's directional light, moon light, ambient light, fog,
    /// skybox, and star particles continuously based on normalised time-of-day
    /// (0-1). Uses Gradient fields for colors and AnimationCurve fields for
    /// floats, giving smooth transitions throughout the entire day cycle.
    /// </summary>
    public sealed class FarmLightingController : MonoBehaviour
    {
        // ── Shader property IDs ────────────────────────────────────────────
        private static readonly int SkyTintId            = Shader.PropertyToID("_SkyTint");
        private static readonly int ExposureId           = Shader.PropertyToID("_Exposure");
        private static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int GroundColorId        = Shader.PropertyToID("_GroundColor");

        private static readonly Color WEATHER_OVERCAST_TINT = new Color(0.55f, 0.60f, 0.70f);

        // ── Night thresholds ───────────────────────────────────────────────
        private const float DAWN_START = 0.18f;
        private const float DAWN_END   = 0.28f;
        private const float DUSK_START = 0.72f;
        private const float DUSK_END   = 0.82f;

        [Header("References")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private ParticleSystem starParticles;

        // ── Sun ────────────────────────────────────────────────────────────
        [Header("Sun")]
        [SerializeField] private Gradient sunColor = DefaultSunColorGradient();
        [SerializeField] private AnimationCurve sunIntensity = DefaultSunIntensityCurve();

        // ── Moon ───────────────────────────────────────────────────────────
        [Header("Moon")]
        [SerializeField] private Color moonColor = new Color(0.40f, 0.45f, 0.65f);
        [SerializeField] private AnimationCurve moonIntensity = DefaultMoonIntensityCurve();

        // ── Stars ──────────────────────────────────────────────────────────
        [Header("Stars")]
        [SerializeField] private AnimationCurve starVisibility = DefaultStarVisibilityCurve();

        // ── Ambient ────────────────────────────────────────────────────────
        [Header("Ambient")]
        [SerializeField] private Gradient ambientColor = DefaultAmbientColorGradient();

        // ── Fog ────────────────────────────────────────────────────────────
        [Header("Fog")]
        [SerializeField] private Gradient fogColor = DefaultFogColorGradient();
        [SerializeField] private AnimationCurve fogDensity = DefaultFogDensityCurve();

        // ── Skybox ─────────────────────────────────────────────────────────
        [Header("Skybox")]
        [SerializeField] private Gradient skyTint = DefaultSkyTintGradient();
        [SerializeField] private AnimationCurve skyExposure = DefaultSkyExposureCurve();
        [SerializeField] private AnimationCurve atmosphereThickness = DefaultAtmosphereThicknessCurve();
        [SerializeField] private Gradient groundColor = DefaultGroundColorGradient();

        // ── Weather / Season modifiers ─────────────────────────────────────
        private float _weatherDim;
        private Color _seasonTint = Color.white;

        // ── Star particle cache ────────────────────────────────────────────
        private ParticleSystem.EmissionModule _starEmission;
        private float _starBaseRate;
        private bool  _starsCached;

        private void Awake()
        {
            TryResolveSunLight();
            TryResolveSkybox();
            CacheStarParticles();
        }

        /// <summary>
        /// Evaluates all gradients and curves at the given normalised time
        /// and applies the results directly. Called every frame by FarmDayClockDriver.
        /// </summary>
        public void ApplyTime(float normalisedTime)
        {
            TryResolveSunLight();

            // ── Evaluate gradients / curves ────────────────────────────────
            Color baseSunColor   = sunColor.Evaluate(normalisedTime);
            float baseSunInt     = sunIntensity.Evaluate(normalisedTime);
            float baseMoonInt    = moonIntensity.Evaluate(normalisedTime);
            float baseStarVis   = starVisibility.Evaluate(normalisedTime);
            Color baseAmbient    = ambientColor.Evaluate(normalisedTime);
            Color baseFogColor   = fogColor.Evaluate(normalisedTime);
            float baseFogDensity = fogDensity.Evaluate(normalisedTime);
            Color baseSkyTint    = skyTint.Evaluate(normalisedTime);
            float baseSkyExp     = skyExposure.Evaluate(normalisedTime);
            float baseAtmoThick  = atmosphereThickness.Evaluate(normalisedTime);
            Color baseGroundCol  = groundColor.Evaluate(normalisedTime);

            // ── Apply weather dimming + season tint to sun ─────────────────
            Color weatheredSun  = Color.Lerp(baseSunColor, WEATHER_OVERCAST_TINT, _weatherDim);
            Color finalSunColor = weatheredSun * _seasonTint;
            float finalSunInt   = Mathf.Lerp(baseSunInt, baseSunInt * 0.45f, _weatherDim);

            // ── Sun arc: map 0.25 (dawn) -> 0 deg to 0.75 (dusk) -> 180 deg
            float sunArc = (normalisedTime - 0.25f) / 0.5f;
            float sunPitch = Mathf.Sin(Mathf.Clamp01(sunArc) * Mathf.PI) * 80f - 10f;

            // ── Moon arc: opposite of sun, active 0.75 -> 0.25 (through midnight)
            float moonArc = (normalisedTime < 0.5f)
                ? (normalisedTime + 0.5f)
                : (normalisedTime - 0.5f);
            float moonPitch = Mathf.Sin(Mathf.Clamp01((moonArc - 0.25f) / 0.5f) * Mathf.PI) * 60f - 5f;

            // ── Write sun ──────────────────────────────────────────────────
            if (sunLight != null)
            {
                bool sunActive = finalSunInt > 0.01f;
                if (sunLight.enabled != sunActive)
                    sunLight.enabled = sunActive;

                if (sunActive)
                {
                    sunLight.color     = finalSunColor;
                    sunLight.intensity = finalSunInt;
                    var eu = sunLight.transform.eulerAngles;
                    sunLight.transform.eulerAngles = new Vector3(sunPitch, eu.y, eu.z);
                }
            }

            // ── Write moon ─────────────────────────────────────────────────
            if (moonLight != null)
            {
                bool moonActive = baseMoonInt > 0.01f;
                if (moonLight.enabled != moonActive)
                    moonLight.enabled = moonActive;

                if (moonActive)
                {
                    moonLight.color     = moonColor;
                    moonLight.intensity = Mathf.Lerp(baseMoonInt, baseMoonInt * 0.3f, _weatherDim);
                    var eu = moonLight.transform.eulerAngles;
                    moonLight.transform.eulerAngles = new Vector3(moonPitch, eu.y, eu.z);
                }
            }

            // ── Write stars ────────────────────────────────────────────────
            if (_starsCached)
            {
                float weatheredStars = Mathf.Lerp(baseStarVis, 0f, _weatherDim);
                _starEmission.rateOverTime = _starBaseRate * weatheredStars;

                // Fade star particle alpha
                var main = starParticles.main;
                var startColor = main.startColor;
                startColor.color = new Color(1f, 1f, 1f, weatheredStars);
                main.startColor = startColor;
            }

            // ── Write ambient ──────────────────────────────────────────────
            RenderSettings.ambientLight = baseAmbient;

            // ── Write fog ──────────────────────────────────────────────────
            RenderSettings.fog        = true;
            RenderSettings.fogColor   = baseFogColor;
            RenderSettings.fogDensity = baseFogDensity;

            // ── Write skybox ───────────────────────────────────────────────
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                skyboxMaterial.SetColor(SkyTintId, baseSkyTint);
                skyboxMaterial.SetFloat(ExposureId, baseSkyExp);
                skyboxMaterial.SetFloat(AtmosphereThicknessId, baseAtmoThick);
                skyboxMaterial.SetColor(GroundColorId, baseGroundCol);
            }
        }

        /// <summary>
        /// Instantly applies lighting for the given normalised time.
        /// Call after scene transitions to prevent baked lighting bleed.
        /// </summary>
        public void ForceApply(float normalisedTime)
        {
            ApplyTime(normalisedTime);
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
            _seasonTint = season switch
            {
                FarmSeason.Spring => new Color(0.95f, 1.00f, 0.95f),
                FarmSeason.Summer => new Color(1.00f, 1.00f, 0.90f),
                FarmSeason.Autumn => new Color(1.00f, 0.90f, 0.75f),
                FarmSeason.Winter => new Color(0.85f, 0.90f, 1.00f),
                _                 => Color.white,
            };
        }

        // ── Resolve helpers ────────────────────────────────────────────────

        private void TryResolveSunLight()
        {
            if (sunLight != null) return;

            if (TryGetComponent(out Light ownLight) && ownLight.type == LightType.Directional)
            {
                sunLight = ownLight;
                return;
            }

            if (RenderSettings.sun != null)
                sunLight = RenderSettings.sun;
        }

        private void TryResolveSkybox()
        {
            if (skyboxMaterial != null) return;

            if (RenderSettings.skybox != null)
                skyboxMaterial = RenderSettings.skybox;
        }

        private void CacheStarParticles()
        {
            if (starParticles == null) return;

            _starEmission = starParticles.emission;
            _starBaseRate  = _starEmission.rateOverTime.constant;
            _starsCached   = true;
        }

        // ── Default gradient / curve factories ─────────────────────────────
        // Timeline: 0.0 = midnight, 0.20 = dawn start, 0.30 = morning,
        //           0.50 = noon, 0.70 = afternoon end, 0.80 = dusk end, 1.0 = midnight

        private static Gradient DefaultSunColorGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.00f),
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.18f),
                    new GradientColorKey(new Color(1.00f, 0.60f, 0.30f), 0.25f),
                    new GradientColorKey(new Color(1.00f, 0.90f, 0.70f), 0.35f),
                    new GradientColorKey(new Color(1.00f, 1.00f, 0.95f), 0.50f),
                    new GradientColorKey(new Color(1.00f, 0.85f, 0.60f), 0.62f),
                    new GradientColorKey(new Color(0.90f, 0.40f, 0.20f), 0.75f),
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.82f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static AnimationCurve DefaultSunIntensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.18f, 0.00f),
                new Keyframe(0.22f, 0.10f),
                new Keyframe(0.30f, 0.60f),
                new Keyframe(0.35f, 0.80f),
                new Keyframe(0.50f, 1.10f),
                new Keyframe(0.62f, 0.90f),
                new Keyframe(0.70f, 0.60f),
                new Keyframe(0.78f, 0.10f),
                new Keyframe(0.82f, 0.00f),
                new Keyframe(1.00f, 0.00f));
        }

        private static AnimationCurve DefaultMoonIntensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0.00f, 0.25f),   // midnight - bright moon
                new Keyframe(0.15f, 0.25f),   // pre-dawn - still bright
                new Keyframe(0.22f, 0.05f),   // dawn transition - fading
                new Keyframe(0.28f, 0.00f),   // gone by morning
                new Keyframe(0.72f, 0.00f),   // no moon during day
                new Keyframe(0.78f, 0.05f),   // dusk transition - appearing
                new Keyframe(0.85f, 0.25f),   // night - bright
                new Keyframe(1.00f, 0.25f));  // midnight wrap
        }

        private static AnimationCurve DefaultStarVisibilityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0.00f, 1.00f),   // midnight - full stars
                new Keyframe(0.15f, 1.00f),   // pre-dawn
                new Keyframe(0.22f, 0.30f),   // dawn - fading
                new Keyframe(0.28f, 0.00f),   // gone by morning
                new Keyframe(0.72f, 0.00f),   // no stars during day
                new Keyframe(0.78f, 0.30f),   // dusk - appearing
                new Keyframe(0.85f, 1.00f),   // night - full
                new Keyframe(1.00f, 1.00f));  // midnight wrap
        }

        private static Gradient DefaultAmbientColorGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.12f), 0.00f),
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.12f), 0.18f),
                    new GradientColorKey(new Color(0.40f, 0.30f, 0.25f), 0.25f),
                    new GradientColorKey(new Color(0.55f, 0.55f, 0.50f), 0.35f),
                    new GradientColorKey(new Color(0.65f, 0.65f, 0.60f), 0.50f),
                    new GradientColorKey(new Color(0.55f, 0.50f, 0.45f), 0.62f),
                    new GradientColorKey(new Color(0.35f, 0.20f, 0.18f), 0.75f),
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.12f), 0.82f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static Gradient DefaultFogColorGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.04f, 0.04f, 0.10f), 0.00f),
                    new GradientColorKey(new Color(0.04f, 0.04f, 0.10f), 0.18f),
                    new GradientColorKey(new Color(0.70f, 0.45f, 0.30f), 0.25f),
                    new GradientColorKey(new Color(0.75f, 0.75f, 0.70f), 0.35f),
                    new GradientColorKey(new Color(0.80f, 0.80f, 0.78f), 0.50f),
                    new GradientColorKey(new Color(0.75f, 0.70f, 0.55f), 0.62f),
                    new GradientColorKey(new Color(0.60f, 0.35f, 0.20f), 0.75f),
                    new GradientColorKey(new Color(0.04f, 0.04f, 0.10f), 0.82f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static AnimationCurve DefaultFogDensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0.00f, 0.020f),
                new Keyframe(0.18f, 0.020f),
                new Keyframe(0.25f, 0.012f),
                new Keyframe(0.35f, 0.005f),
                new Keyframe(0.50f, 0.003f),
                new Keyframe(0.62f, 0.006f),
                new Keyframe(0.75f, 0.015f),
                new Keyframe(0.82f, 0.020f),
                new Keyframe(1.00f, 0.020f));
        }

        private static Gradient DefaultSkyTintGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.10f, 0.10f, 0.20f), 0.00f),
                    new GradientColorKey(new Color(0.10f, 0.10f, 0.20f), 0.18f),
                    new GradientColorKey(new Color(0.60f, 0.40f, 0.35f), 0.25f),
                    new GradientColorKey(new Color(0.45f, 0.55f, 0.70f), 0.35f),
                    new GradientColorKey(new Color(0.45f, 0.55f, 0.70f), 0.50f),
                    new GradientColorKey(new Color(0.50f, 0.50f, 0.60f), 0.62f),
                    new GradientColorKey(new Color(0.65f, 0.35f, 0.25f), 0.75f),
                    new GradientColorKey(new Color(0.10f, 0.10f, 0.20f), 0.82f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static AnimationCurve DefaultSkyExposureCurve()
        {
            return new AnimationCurve(
                new Keyframe(0.00f, 0.30f),
                new Keyframe(0.18f, 0.30f),
                new Keyframe(0.25f, 1.00f),
                new Keyframe(0.35f, 1.30f),
                new Keyframe(0.50f, 1.45f),
                new Keyframe(0.62f, 1.20f),
                new Keyframe(0.75f, 0.80f),
                new Keyframe(0.82f, 0.30f),
                new Keyframe(1.00f, 0.30f));
        }

        private static AnimationCurve DefaultAtmosphereThicknessCurve()
        {
            // Low = crisp blue sky, High = warm/orange scattering
            return new AnimationCurve(
                new Keyframe(0.00f, 0.40f),   // midnight - thin
                new Keyframe(0.18f, 0.40f),   // pre-dawn
                new Keyframe(0.23f, 1.80f),   // dawn - thick for warm horizon
                new Keyframe(0.30f, 0.80f),   // morning - slightly thick, warm tone
                new Keyframe(0.40f, 0.55f),   // late morning - clearing up
                new Keyframe(0.50f, 0.45f),   // noon - thin, crisp blue
                new Keyframe(0.60f, 0.65f),   // early afternoon - slight warmth
                new Keyframe(0.68f, 0.90f),   // afternoon - warmer golden
                new Keyframe(0.75f, 1.80f),   // dusk - thick for warm sunset
                new Keyframe(0.83f, 0.40f),   // post-dusk
                new Keyframe(1.00f, 0.40f));  // midnight wrap
        }

        private static Gradient DefaultGroundColorGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.08f), 0.00f),  // midnight - near black
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.08f), 0.18f),  // pre-dawn
                    new GradientColorKey(new Color(0.50f, 0.35f, 0.25f), 0.25f),  // dawn - warm brown
                    new GradientColorKey(new Color(0.40f, 0.45f, 0.35f), 0.35f),  // morning - green-grey
                    new GradientColorKey(new Color(0.35f, 0.40f, 0.30f), 0.50f),  // noon - natural green
                    new GradientColorKey(new Color(0.45f, 0.40f, 0.30f), 0.62f),  // afternoon - warm
                    new GradientColorKey(new Color(0.50f, 0.30f, 0.15f), 0.75f),  // dusk - orange-brown
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.08f), 0.83f),  // post-dusk
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }
    }
}
