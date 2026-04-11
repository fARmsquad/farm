using FarmSimVR.Core.Farming;
using FarmSimVR.MonoBehaviours.Audio;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed class WorldFarmAtmosphereController : MonoBehaviour
    {
        private const string AudioLibraryResource = "FarmWorld_AudioLibrary";

        private AudioLibrary _library;
        private ZoneTracker _zoneTracker;
        private AudioSource _farmLoop;
        private AudioSource _houseLoop;
        private AudioSource _coopLoop;
        private AudioSource _coopOneShot;
        private ParticleController _plotPollen;
        private ParticleController _houseSmoke;
        private ParticleController _coopDust;
        private ParticleController _rain;
        private float _nextCoopCallTime;

        private void Awake()
        {
            _library = Resources.Load<AudioLibrary>(AudioLibraryResource);
            CreateAudioSources();
            CreateParticles();
        }

        private void Update()
        {
            if (_zoneTracker == null)
                _zoneTracker = FindAnyObjectByType<ZoneTracker>();

            UpdateAudio();
            UpdateParticles();
        }

        private void CreateAudioSources()
        {
            _farmLoop = CreateAudioSource("FarmAmbientLoop", true);
            _houseLoop = CreateAudioSource("HouseAmbientLoop", true);
            _coopLoop = CreateAudioSource("CoopAmbientLoop", true);
            _coopOneShot = CreateAudioSource("CoopOneShot", false);
        }

        private void CreateParticles()
        {
            _plotPollen = CreateParticles("PlotPollen", new Vector3(0f, 2f, 0f));
            ConfigurePlotPollen(_plotPollen);

            _houseSmoke = CreateParticles("HouseSmoke", new Vector3(0f, 2f, 0f));
            ConfigureHouseSmoke(_houseSmoke);

            _coopDust = CreateParticles("CoopDust", new Vector3(0f, 1.5f, 0f));
            ConfigureCoopDust(_coopDust);

            _rain = CreateParticles("RainCurtain", new Vector3(0f, 4f, 0f));
            ConfigureRain(_rain);
        }

        private void UpdateAudio()
        {
            if (_library == null)
                return;

            var zone = _zoneTracker?.CurrentZone ?? string.Empty;
            var weather = FarmWeatherDriver.Instance?.Provider.Current;
            var phase = FarmDayClockDriver.Instance?.Clock.Phase;

            AssignClipIfChanged(_farmLoop, phase == DayPhase.Dusk || phase == DayPhase.Night
                ? _library.GetClip("crickets")
                : _library.GetClip("morning-wind-birds-crickets"));
            AssignClipIfChanged(_houseLoop, _library.GetClip("wind"));
            AssignClipIfChanged(_coopLoop, _library.GetClip("distant-chickens"));

            FadeTo(_farmLoop, zone == "Farm Plots" ? 0.32f : 0.08f);
            FadeTo(_houseLoop, zone == "Farm House" ? 0.22f : 0.03f);
            FadeTo(_coopLoop, zone == "Chicken Coop" ? 0.32f : 0.05f);

            if (weather == WeatherType.Rain)
            {
                FadeTo(_farmLoop, zone == "Farm Plots" ? 0.18f : 0.04f);
                FadeTo(_houseLoop, zone == "Farm House" ? 0.28f : 0.06f);
            }

            if (zone == "Chicken Coop" && Time.time >= _nextCoopCallTime)
            {
                var clip = Random.value > 0.5f ? _library.GetClip("bawk-bawk") : _library.GetClip("chicklets");
                if (clip != null)
                    _coopOneShot.PlayOneShot(clip, 0.35f);

                _nextCoopCallTime = Time.time + Random.Range(8f, 14f);
            }
        }

        private void UpdateParticles()
        {
            var zone = _zoneTracker?.CurrentZone ?? string.Empty;
            var weather = FarmWeatherDriver.Instance?.Provider.Current ?? WeatherType.Sunny;

            RepositionParticle(_plotPollen, ResolveZoneCenter("FarmPlotsZone"));
            RepositionParticle(_houseSmoke, ResolveZoneCenter("FarmHouseZone"));
            RepositionParticle(_coopDust, ResolveZoneCenter("ChickenCoopZone"));
            RepositionRain();

            _plotPollen.SetIntensity(weather == WeatherType.Rain ? 0f : zone == "Farm Plots" ? 0.8f : 0.2f);
            _houseSmoke.SetIntensity(0.45f);
            _coopDust.SetIntensity(zone == "Chicken Coop" ? 0.75f : 0.2f);
            _rain.SetIntensity(weather == WeatherType.Rain ? 1f : 0f);
        }

        private AudioSource CreateAudioSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private ParticleController CreateParticles(string name, Vector3 localOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffset;
            var system = go.AddComponent<ParticleSystem>();
            var controller = go.AddComponent<ParticleController>();
            system.Play();
            return controller;
        }

        private static void ConfigurePlotPollen(ParticleController controller)
        {
            var system = controller.GetComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = 2.2f;
            main.startSpeed = 0.1f;
            main.startSize = 0.08f;
            main.maxParticles = 40;
            main.startColor = new Color(1f, 0.95f, 0.72f, 0.55f);

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 1f, 12f);

            var emission = system.emission;
            emission.rateOverTime = 20f;
        }

        private static void ConfigureHouseSmoke(ParticleController controller)
        {
            var system = controller.GetComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.35f;
            main.startSize = 0.25f;
            main.maxParticles = 24;
            main.startColor = new Color(0.7f, 0.7f, 0.7f, 0.45f);

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.2f;
            shape.angle = 8f;

            var emission = system.emission;
            emission.rateOverTime = 6f;
        }

        private static void ConfigureCoopDust(ParticleController controller)
        {
            var system = controller.GetComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = 1.4f;
            main.startSpeed = 0.08f;
            main.startSize = 0.06f;
            main.maxParticles = 30;
            main.startColor = new Color(0.82f, 0.73f, 0.58f, 0.4f);

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 2f, 8f);

            var emission = system.emission;
            emission.rateOverTime = 14f;
        }

        private static void ConfigureRain(ParticleController controller)
        {
            var system = controller.GetComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = 0.65f;
            main.startSpeed = 8f;
            main.startSize = 0.05f;
            main.maxParticles = 120;
            main.startColor = new Color(0.72f, 0.84f, 1f, 0.55f);

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 1f, 10f);

            var emission = system.emission;
            emission.rateOverTime = 90f;
        }

        private static void FadeTo(AudioSource source, float targetVolume)
        {
            if (source == null)
                return;

            targetVolume = Mathf.Clamp01(targetVolume);
            if (source.clip != null && !source.isPlaying)
                source.Play();

            source.volume = Mathf.MoveTowards(source.volume, targetVolume, Time.deltaTime * 0.4f);
            if (source.volume <= 0.001f && source.isPlaying && targetVolume <= 0f)
                source.Stop();
        }

        private static void AssignClipIfChanged(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null || source.clip == clip)
                return;

            var wasPlaying = source.isPlaying;
            source.clip = clip;
            if (wasPlaying)
                source.Play();
        }

        private Vector3 ResolveZoneCenter(string zoneObjectName)
        {
            var farm = GameObject.Find("Farm");
            if (farm == null)
                return transform.position;

            var zone = farm.transform.Find($"Zones/{zoneObjectName}");
            return zone != null ? zone.position : farm.transform.position;
        }

        private static void RepositionParticle(ParticleController controller, Vector3 worldPosition)
        {
            if (controller == null)
                return;

            controller.transform.position = worldPosition;
        }

        private void RepositionRain()
        {
            if (_rain == null)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            _rain.transform.position = camera.transform.position + new Vector3(0f, 4f, 0f);
        }
    }
}
