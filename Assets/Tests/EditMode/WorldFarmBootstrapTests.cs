using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Farming;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class WorldFarmBootstrapTests
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
        public void EnsureInstalled_WithWorldFarm_AddsRuntimeSystemsAndPreparesPlots()
        {
            var host = new GameObject("WorldSceneBootstrap");
            CreateDirectionalLight();
            CreatePlayer();
            var plotsRoot = CreatePlotsRoot();
            var farmRoot = plotsRoot.parent;
            CreateFarmHouse(farmRoot);
            CreatePen(farmRoot);
            var terrainRoot = CreateTerrainRoot();
            var town = CreateClutterRoot("Town");
            var meadow = CreateClutterRoot("Meadow");
            var sandyShores = CreateClutterRoot("SandyShores");
            var wildflowerHills = CreateClutterRoot("WildflowerHills");
            CreatePlotAnchor(plotsRoot, "Row_A");
            CreatePlotAnchor(plotsRoot, "Row_B");

            var installed = WorldFarmBootstrap.EnsureInstalled(host);

            Assert.That(installed, Is.True);
            Assert.That(host.GetComponent<FarmSimDriver>(), Is.Not.Null);
            Assert.That(host.GetComponent<FarmDayClockDriver>(), Is.Not.Null);
            Assert.That(host.GetComponent<FarmWeatherDriver>(), Is.Not.Null);
            Assert.That(host.GetComponent<FarmSeasonDriver>(), Is.Not.Null);
            Assert.That(host.GetComponent<FarmPlotInteractionController>(), Is.Not.Null);
            Assert.That(host.GetComponent<FarmWeatherDebugShortcuts>(), Is.Not.Null);
            Assert.That(HasComponent(host, "FarmSimVR.MonoBehaviours.Farming.WorldFarmProgressionController"), Is.True);
            Assert.That(HasComponent(host, "FarmSimVR.MonoBehaviours.Farming.WorldFarmAtmosphereController"), Is.True);
            Assert.That(HasComponent(host, "FarmSimVR.MonoBehaviours.Farming.WorldFarmReferenceOverlay"), Is.True);
            Assert.That(HasComponent(host, "FarmSimVR.MonoBehaviours.Farming.WorldFarmDevShortcuts"), Is.True);
            Assert.That(Object.FindAnyObjectByType<FarmLightingController>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ZoneTracker>(), Is.Not.Null);
            Assert.That(terrainRoot.activeSelf, Is.True);
            Assert.That(town.activeSelf, Is.False);
            Assert.That(meadow.activeSelf, Is.False);
            Assert.That(sandyShores.activeSelf, Is.False);
            Assert.That(wildflowerHills.activeSelf, Is.False);
            var installedFarmRoot = GameObject.Find("Farm").transform;
            Assert.That(installedFarmRoot.Find("FarmPaths").gameObject.activeSelf, Is.True);
            Assert.That(installedFarmRoot.Find("GroundCover").gameObject.activeSelf, Is.True);
            Assert.That(installedFarmRoot.Find("Props").gameObject.activeSelf, Is.False);
            Assert.That(installedFarmRoot.Find("Pasture").gameObject.activeSelf, Is.False);
            Assert.That(installedFarmRoot.Find("Trees").gameObject.activeSelf, Is.False);
            Assert.That(installedFarmRoot.Find("FX").gameObject.activeSelf, Is.False);

            AssertZoneExists(installedFarmRoot, "FarmPlotsZone", "Farm Plots");
            AssertZoneExists(installedFarmRoot, "FarmHouseZone", "Farm House");
            AssertZoneExists(installedFarmRoot, "ChickenCoopZone", "Chicken Coop");

            for (var i = 0; i < plotsRoot.childCount; i++)
            {
                var plot = plotsRoot.GetChild(i);
                Assert.That(plot.CompareTag("CropPlot"), Is.True);
                Assert.That(plot.name, Is.EqualTo($"CropPlot_{i}"));
                Assert.That(plot.GetComponent<CropPlotController>(), Is.Not.Null);

                var surface = plot.Find("PlotSurface");
                Assert.That(surface, Is.Not.Null);
                Assert.That(surface.GetComponent<PlotVisualUpdater>(), Is.Not.Null);
                Assert.That(surface.GetComponent<Collider>(), Is.Not.Null);
                Assert.That(surface.lossyScale.x, Is.EqualTo(WorldFarmBootstrap.RecommendedPlotSurfaceSizeMeters).Within(0.001f));
                Assert.That(surface.lossyScale.y, Is.EqualTo(0.08f).Within(0.001f));
                Assert.That(surface.lossyScale.z, Is.EqualTo(WorldFarmBootstrap.RecommendedPlotSurfaceSizeMeters).Within(0.001f));

                var cropVisual = plot.Find("CropVisual");
                Assert.That(cropVisual, Is.Not.Null);
                Assert.That(cropVisual.GetComponent<CropVisualUpdater>(), Is.Not.Null);
                Assert.That(cropVisual.position.x, Is.EqualTo(surface.position.x).Within(0.001f));
                Assert.That(cropVisual.position.z, Is.EqualTo(surface.position.z).Within(0.001f));
            }
        }

        [Test]
        public void EnsureInstalled_IsIdempotent()
        {
            var host = new GameObject("WorldSceneBootstrap");
            CreateDirectionalLight();
            CreatePlayer();
            var plotsRoot = CreatePlotsRoot();
            var farmRoot = plotsRoot.parent;
            CreateFarmHouse(farmRoot);
            CreatePen(farmRoot);
            CreateTerrainRoot();
            CreatePlotAnchor(plotsRoot, "Row_A");

            WorldFarmBootstrap.EnsureInstalled(host);
            WorldFarmBootstrap.EnsureInstalled(host);

            Assert.That(host.GetComponents<FarmSimDriver>().Length, Is.EqualTo(1));
            Assert.That(host.GetComponents<FarmDayClockDriver>().Length, Is.EqualTo(1));
            Assert.That(host.GetComponents<FarmWeatherDriver>().Length, Is.EqualTo(1));
            Assert.That(host.GetComponents<FarmSeasonDriver>().Length, Is.EqualTo(1));
            Assert.That(host.GetComponents<FarmPlotInteractionController>().Length, Is.EqualTo(1));
            Assert.That(host.GetComponents<FarmWeatherDebugShortcuts>().Length, Is.EqualTo(1));
            Assert.That(CountComponents(host, "FarmSimVR.MonoBehaviours.Farming.WorldFarmProgressionController"), Is.EqualTo(1));
            Assert.That(CountComponents(host, "FarmSimVR.MonoBehaviours.Farming.WorldFarmAtmosphereController"), Is.EqualTo(1));
            Assert.That(CountComponents(host, "FarmSimVR.MonoBehaviours.Farming.WorldFarmReferenceOverlay"), Is.EqualTo(1));
            Assert.That(CountComponents(host, "FarmSimVR.MonoBehaviours.Farming.WorldFarmDevShortcuts"), Is.EqualTo(1));

            var plot = plotsRoot.GetChild(0);
            Assert.That(plot.GetComponentsInChildren<PlotVisualUpdater>(true).Length, Is.EqualTo(1));
            Assert.That(plot.GetComponentsInChildren<CropVisualUpdater>(true).Length, Is.EqualTo(1));
            Assert.That(GameObject.Find("Farm").transform.Find("Zones").childCount, Is.EqualTo(3));
        }

        [Test]
        public void EnsureInstalled_WithoutPlotsRoot_ReturnsFalse()
        {
            var host = new GameObject("WorldSceneBootstrap");

            var installed = WorldFarmBootstrap.EnsureInstalled(host);

            Assert.That(installed, Is.False);
            Assert.That(host.GetComponent<FarmSimDriver>(), Is.Null);
        }

        private static void CreateDirectionalLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            RenderSettings.sun = light;
        }

        private static Transform CreatePlotsRoot()
        {
            var farm = new GameObject("Farm").transform;
            new GameObject("Buildings").transform.SetParent(farm, false);
            new GameObject("FarmPaths").transform.SetParent(farm, false);
            new GameObject("GroundCover").transform.SetParent(farm, false);
            new GameObject("Props").transform.SetParent(farm, false);
            var plots = new GameObject("Plots").transform;
            plots.SetParent(farm, false);
            new GameObject("Pasture").transform.SetParent(farm, false);
            new GameObject("Trees").transform.SetParent(farm, false);
            new GameObject("FX").transform.SetParent(farm, false);
            return plots;
        }

        private static GameObject CreateTerrainRoot()
        {
            var terrain = new GameObject("Terrain");
            var terrainChild = GameObject.CreatePrimitive(PrimitiveType.Plane);
            terrainChild.name = "Willowbrook_Terrain";
            terrainChild.transform.SetParent(terrain.transform, false);
            return terrain;
        }

        private static void CreatePlotAnchor(Transform parent, string name)
        {
            var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plot.name = name;
            plot.transform.SetParent(parent, false);
            plot.transform.localScale = new Vector3(2f, 0.2f, 2f);
        }

        private static void CreatePlayer()
        {
            var player = new GameObject("ExplorationPlayer");
            player.tag = "Player";
            player.AddComponent<CharacterController>();
        }

        private static void CreateFarmHouse(Transform farm)
        {
            var buildings = farm.Find("Buildings");
            var house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "FarmHouse";
            house.transform.SetParent(buildings, false);
            house.transform.localPosition = new Vector3(5f, 0.5f, 0f);
            house.transform.localScale = new Vector3(6f, 2f, 6f);
        }

        private static void CreatePen(Transform farm)
        {
            var pen = new GameObject("Pen").transform;
            pen.SetParent(farm, false);
            pen.localPosition = new Vector3(-5f, 0f, 0f);
        }

        private static GameObject CreateClutterRoot(string name)
        {
            return new GameObject(name);
        }

        private static void AssertZoneExists(Transform farmRoot, string zoneObjectName, string expectedZoneName)
        {
            var zone = farmRoot.Find($"Zones/{zoneObjectName}");
            Assert.That(zone, Is.Not.Null, $"Missing zone object: {zoneObjectName}");

            var marker = zone.GetComponent<ZoneMarker>();
            Assert.That(marker, Is.Not.Null, $"Missing ZoneMarker on {zoneObjectName}");
            Assert.That(marker.ZoneName, Is.EqualTo(expectedZoneName));

            var collider = zone.GetComponent<BoxCollider>();
            Assert.That(collider, Is.Not.Null, $"Missing BoxCollider on {zoneObjectName}");
            Assert.That(collider.isTrigger, Is.True);
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
    }
}
