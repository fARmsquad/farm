using FarmSimVR.MonoBehaviours;
using FarmSimVR.Core.Farming;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public static class WorldFarmBootstrap
    {
        private const string FarmRootName = "Farm";
        private const string PlotsRootName = "Plots";
        private const float PlotSurfaceHeight = 0.08f;

        public static float RecommendedPlotSurfaceSizeMeters => CropArtCatalog.RecommendedPlotSurfaceSizeMeters;

        public static bool EnsureInstalled(GameObject host)
        {
            if (host == null)
                throw new System.ArgumentNullException(nameof(host));

            var plotsRoot = ResolvePlotsRoot();
            if (plotsRoot == null)
                return false;

            WorldPlayableSlicePruner.Apply(host);
            var farmRoot = ResolveFarmRoot(plotsRoot);
            WorldFarmZoneInstaller.Apply(farmRoot, plotsRoot);
            EnsurePlotAnchors(plotsRoot);
            EnsureLightingController();
            EnsureComponent<FarmSimDriver>(host);
            EnsureComponent<FarmDayClockDriver>(host);
            EnsureComponent<FarmWeatherDriver>(host);
            EnsureComponent<FarmSeasonDriver>(host);
            EnsureComponent<FarmPlotInteractionController>(host);
            EnsureComponent<FarmWeatherDebugShortcuts>(host);
            EnsureComponent<WorldFarmProgressionController>(host);
            EnsureComponent<WorldFarmAtmosphereController>(host);
            EnsureComponent<WorldFarmReferenceOverlay>(host);
            EnsureComponent<WorldFarmDevShortcuts>(host);
            return true;
        }

        private static Transform ResolvePlotsRoot()
        {
            var farm = GameObject.Find(FarmRootName);
            if (farm != null)
            {
                var plots = farm.transform.Find(PlotsRootName);
                if (plots != null)
                    return plots;
            }

            var fallback = GameObject.Find(PlotsRootName);
            return fallback != null ? fallback.transform : null;
        }

        private static Transform ResolveFarmRoot(Transform plotsRoot)
        {
            return plotsRoot.parent != null ? plotsRoot.parent : plotsRoot;
        }

        private static void EnsurePlotAnchors(Transform plotsRoot)
        {
            for (var i = 0; i < plotsRoot.childCount; i++)
                PreparePlotAnchor(plotsRoot.GetChild(i), i);
        }

        private static void PreparePlotAnchor(Transform anchor, int index)
        {
            var plot = anchor.gameObject;
            plot.name = $"CropPlot_{index}";
            plot.tag = "CropPlot";
            EnsureComponent<CropPlotController>(plot);
            var bounds = MeasureBounds(plot);
            DisableAnchorRenderers(plot);
            EnsurePlotSurface(plot, bounds);
            EnsureCropVisual(plot, bounds.center);
        }

        private static Bounds MeasureBounds(GameObject plot)
        {
            var renderers = plot.GetComponentsInChildren<Renderer>(true);
            var found = false;
            var bounds = new Bounds(plot.transform.position, Vector3.one);

            foreach (var renderer in renderers)
            {
                if (renderer.transform.name is "PlotSurface" or "CropVisual")
                    continue;

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return found ? bounds : new Bounds(plot.transform.position, new Vector3(1.5f, 0.2f, 1.5f));
        }

        private static void DisableAnchorRenderers(GameObject plot)
        {
            foreach (var renderer in plot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform.name is "PlotSurface" or "CropVisual")
                    continue;

                renderer.enabled = false;
            }
        }

        private static void EnsurePlotSurface(GameObject plot, Bounds bounds)
        {
            var surfaceTransform = plot.transform.Find("PlotSurface");
            var surface = surfaceTransform != null
                ? surfaceTransform.gameObject
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            surface.name = "PlotSurface";
            surface.transform.SetParent(plot.transform, false);
            surface.transform.localPosition = plot.transform.InverseTransformPoint(
                new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z));
            surface.transform.localScale = BuildLocalScale(
                plot.transform,
                new Vector3(
                    CropArtCatalog.RecommendedPlotSurfaceSizeMeters,
                    PlotSurfaceHeight,
                    CropArtCatalog.RecommendedPlotSurfaceSizeMeters));
            surface.layer = plot.layer;
            EnsureComponent<PlotVisualUpdater>(surface);
        }

        private static void EnsureCropVisual(GameObject plot, Vector3 worldCenter)
        {
            if (plot.transform.Find("CropVisual") != null)
                return;

            var crop = new GameObject("CropVisual");
            crop.transform.SetParent(plot.transform, false);
            crop.transform.localPosition = plot.transform.InverseTransformPoint(worldCenter);
            crop.transform.localScale = Vector3.one;
            crop.layer = plot.layer;
            EnsureComponent<CropVisualUpdater>(crop);
        }

        private static void EnsureLightingController()
        {
            var sun = ResolveSunLight();
            if (sun == null)
                return;

            sun.gameObject.SetActive(true);
            RenderSettings.sun = sun;
            EnsureComponent<FarmLightingController>(sun.gameObject).ApplyWeather(WeatherType.Sunny);
        }

        private static Light ResolveSunLight()
        {
            if (RenderSettings.sun != null)
                return RenderSettings.sun;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type == LightType.Directional)
                    return light;
            }

            return null;
        }

        private static T EnsureComponent<T>(GameObject host) where T : Component
        {
            var existing = host.GetComponent<T>();
            return existing != null ? existing : host.AddComponent<T>();
        }

        private static void DestroyObject(Object instance)
        {
            if (Application.isPlaying)
                Object.Destroy(instance);
            else
                Object.DestroyImmediate(instance);
        }

        private static Vector3 BuildLocalScale(Transform parent, Vector3 desiredWorldScale)
        {
            var lossy = parent.lossyScale;
            return new Vector3(
                Divide(desiredWorldScale.x, lossy.x),
                Divide(desiredWorldScale.y, lossy.y),
                Divide(desiredWorldScale.z, lossy.z));
        }

        private static float Divide(float value, float divisor)
        {
            return Mathf.Abs(divisor) < 0.0001f ? value : value / divisor;
        }
    }
}
