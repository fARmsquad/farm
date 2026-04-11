using FarmSimVR.Core.Hunting;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Hunting;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class WorldPenBootstrapTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void EnsureInstalled_WithPenRoot_AddsWorldPenSystemsAndWiresPlayerInput()
        {
            var host = new GameObject("WorldSceneBootstrap");
            var player = CreatePlayer();
            var farm = new GameObject("Farm").transform;
            var pen = CreatePenRoot(farm);
            CreateChickenCoopZone(farm);

            var installed = WorldPenBootstrap.EnsureInstalled(host);

            Assert.That(installed, Is.True);
            Assert.That(player.GetComponent<KeyboardPlayerInput>(), Is.Not.Null);
            Assert.That(HasComponent(host, "FarmSimVR.MonoBehaviours.Hunting.WorldPenGameController"), Is.True);
            Assert.That(HasComponent(host, "FarmSimVR.MonoBehaviours.Hunting.WorldPenProgressionController"), Is.True);
            Assert.That(HasComponent(host, "FarmSimVR.MonoBehaviours.Hunting.WorldPenDevShortcuts"), Is.True);
            Assert.That(HasComponent(host, "FarmSimVR.MonoBehaviours.Hunting.WorldPenOverlay"), Is.True);

            var runtime = pen.Find("PenGameRuntime");
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.GetComponent<WildAnimalSpawner>(), Is.Not.Null);
            Assert.That(runtime.GetComponent<AnimalPen>(), Is.Not.Null);

            var dropOff = runtime.Find("PenDropOff");
            Assert.That(dropOff, Is.Not.Null);
            Assert.That(dropOff.GetComponent<BarnDropOff>(), Is.Not.Null);
            Assert.That(dropOff.GetComponent<BoxCollider>(), Is.Not.Null);
        }

        [Test]
        public void EnsureInstalled_IsIdempotent()
        {
            var host = new GameObject("WorldSceneBootstrap");
            CreatePlayer();
            var farm = new GameObject("Farm").transform;
            CreatePenRoot(farm);
            CreateChickenCoopZone(farm);

            WorldPenBootstrap.EnsureInstalled(host);
            WorldPenBootstrap.EnsureInstalled(host);

            Assert.That(CountComponents(host, "FarmSimVR.MonoBehaviours.Hunting.WorldPenGameController"), Is.EqualTo(1));
            Assert.That(CountComponents(host, "FarmSimVR.MonoBehaviours.Hunting.WorldPenProgressionController"), Is.EqualTo(1));
            Assert.That(CountComponents(host, "FarmSimVR.MonoBehaviours.Hunting.WorldPenDevShortcuts"), Is.EqualTo(1));
            Assert.That(CountComponents(host, "FarmSimVR.MonoBehaviours.Hunting.WorldPenOverlay"), Is.EqualTo(1));
        }

        [Test]
        public void TrySpawnNow_UsesConfiguredPenCenterInsteadOfWorldOrigin()
        {
            var center = new GameObject("SpawnCenter").transform;
            center.position = new Vector3(-72f, 0f, -24f);

            var player = CreatePlayer().transform;
            player.position = new Vector3(-68f, 0f, -20f);
            var input = player.gameObject.AddComponent<TestPlayerInput>();

            var config = ScriptableObject.CreateInstance<HuntingConfig>();
            config.spawnRadius = 6f;
            config.maxWildAnimals = 1;

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prefab.name = "Chicken";
            prefab.SetActive(false);

            var spawnerGo = new GameObject("Spawner");
            var spawner = spawnerGo.AddComponent<WildAnimalSpawner>();
            spawner.Configure(config, new[] { prefab }, player, center);
            spawner.Initialize(input, new CaughtAnimalTracker());

            var spawned = spawner.TrySpawnNow();

            Assert.That(spawned, Is.Not.Null);
            Assert.That(Vector3.Distance(spawned.transform.position, center.position), Is.LessThanOrEqualTo(config.spawnRadius + 0.25f));
            Assert.That(Vector3.Distance(spawned.transform.position, Vector3.zero), Is.GreaterThan(20f));
        }

        [Test]
        public void TrySpawnNow_WithOnlyMissingPrefabEntries_ReturnsNullWithoutThrowing()
        {
            var center = new GameObject("SpawnCenter").transform;
            center.position = new Vector3(-72f, 0f, -24f);

            var player = CreatePlayer().transform;
            var input = player.gameObject.AddComponent<TestPlayerInput>();

            var config = ScriptableObject.CreateInstance<HuntingConfig>();
            config.spawnRadius = 6f;
            config.maxWildAnimals = 1;

            var spawnerGo = new GameObject("Spawner");
            var spawner = spawnerGo.AddComponent<WildAnimalSpawner>();
            spawner.Configure(config, new GameObject[] { null }, player, center);
            spawner.Initialize(input, new CaughtAnimalTracker());

            GameObject spawned = null;

            Assert.That(() => spawned = spawner.TrySpawnNow(), Throws.Nothing);
            Assert.That(spawned, Is.Null);
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [Test]
        public void TrySpawnNow_WithWorldPenCatalogPrefabs_DoesNotThrow()
        {
            var catalog = Resources.Load<WorldPenGameCatalog>("WorldPenGameCatalog");
            Assert.That(catalog, Is.Not.Null, "World pen catalog should be available from Resources.");
            Assert.That(catalog.WildAnimalPrefabs, Is.Not.Null.And.Not.Empty);

            var center = new GameObject("SpawnCenter").transform;
            center.position = new Vector3(-72f, 0f, -24f);

            var player = CreatePlayer().transform;
            var input = player.gameObject.AddComponent<TestPlayerInput>();

            var spawnerGo = new GameObject("Spawner");
            var spawner = spawnerGo.AddComponent<WildAnimalSpawner>();
            spawner.Configure(catalog.HuntingConfig, catalog.WildAnimalPrefabs, player, center);
            spawner.Initialize(input, new CaughtAnimalTracker());

            GameObject spawned = null;

            Assert.That(() => spawned = spawner.TrySpawnNow(), Throws.Nothing);
            Assert.That(spawned, Is.Not.Null);
        }

        private static GameObject CreatePlayer()
        {
            var player = new GameObject("ExplorationPlayer");
            player.tag = "Player";
            player.AddComponent<CharacterController>();
            return player;
        }

        private static Transform CreatePenRoot(Transform farm)
        {
            var pen = new GameObject("Pen").transform;
            pen.SetParent(farm, false);
            pen.localPosition = new Vector3(-72f, 0f, -24f);

            var bounds = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bounds.name = "PenBounds";
            bounds.transform.SetParent(pen, false);
            bounds.transform.localScale = new Vector3(12f, 2f, 12f);
            return pen;
        }

        private static void CreateChickenCoopZone(Transform farm)
        {
            var zones = new GameObject("Zones").transform;
            zones.SetParent(farm, false);
            var zone = new GameObject("ChickenCoopZone");
            zone.transform.SetParent(zones, false);
            var collider = zone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            var marker = zone.AddComponent<ZoneMarker>();
            marker.SetZoneName("Chicken Coop");
        }

        private static bool HasComponent(GameObject host, string typeName)
        {
            return CountComponents(host, typeName) > 0;
        }

        private static int CountComponents(GameObject host, string typeName)
        {
            var type = System.Type.GetType(typeName + ", FarmSimVR.MonoBehaviours");
            Assert.That(type, Is.Not.Null, $"Missing runtime type: {typeName}");
            return host.GetComponents(type).Length;
        }

        private sealed class TestPlayerInput : MonoBehaviour, IPlayerInput
        {
            public bool CatchPressed => false;
            public Vector3 Position => transform.position;
        }
    }
}
