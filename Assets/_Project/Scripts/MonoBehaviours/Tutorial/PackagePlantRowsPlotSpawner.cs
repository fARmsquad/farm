using FarmSimVR.MonoBehaviours;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public static class PackagePlantRowsPlotSpawner
    {
        private const string RuntimeRootName = "PackagePlantRowsRuntimePlots";
        private const string RuntimePlotPrefix = "CropPlot_PackageRows_";
        private const float PlotSpacingMeters = 1.6f;

        public static void EnsurePlots(GameObject anchorPlot, int desiredPlotCount, int rowCount)
        {
            if (anchorPlot == null || desiredPlotCount <= 1)
                return;

            var safeRowCount = rowCount < 1 ? 1 : rowCount;
            var runtimeRoot = ResolveRuntimeRoot(anchorPlot);
            var existingCount = runtimeRoot.childCount;
            var plotsToCreate = desiredPlotCount - 1 - existingCount;
            if (plotsToCreate <= 0)
                return;

            var columns = Mathf.CeilToInt((float)desiredPlotCount / safeRowCount);
            for (var i = existingCount + 1; i < desiredPlotCount; i++)
            {
                var plot = CreatePlotFromAnchor(anchorPlot, $"{RuntimePlotPrefix}{i:00}");
                plot.transform.SetParent(runtimeRoot, true);
                plot.transform.position = ResolveWorldPosition(anchorPlot.transform, i, columns);
            }
        }

        private static Transform ResolveRuntimeRoot(GameObject anchorPlot)
        {
            var parent = anchorPlot.transform.parent;
            var existing = parent != null ? parent.Find(RuntimeRootName) : null;
            if (existing != null)
                return existing;

            var root = new GameObject(RuntimeRootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root.transform;
        }

        private static Vector3 ResolveWorldPosition(Transform anchor, int plotIndex, int columns)
        {
            var column = plotIndex % columns;
            var row = plotIndex / columns;
            var right = anchor.right;
            var forward = anchor.forward;
            return anchor.position + right * (column * PlotSpacingMeters) + forward * (row * PlotSpacingMeters);
        }

        private static GameObject CreatePlotFromAnchor(GameObject anchorPlot, string plotName)
        {
            var plot = new GameObject(plotName);
            plot.tag = "CropPlot";
            plot.layer = anchorPlot.layer;
            plot.transform.rotation = anchorPlot.transform.rotation;
            plot.transform.localScale = anchorPlot.transform.localScale;

            EnsureRootCollider(plot, anchorPlot.GetComponent<BoxCollider>());
            plot.AddComponent<CropPlotController>();

            CreatePlotSurface(plot.transform, anchorPlot.transform.Find("PlotSurface"));
            CreateCropVisual(plot.transform, anchorPlot.transform.Find("CropVisual"));
            return plot;
        }

        private static void EnsureRootCollider(GameObject plot, BoxCollider template)
        {
            var collider = plot.AddComponent<BoxCollider>();
            if (template == null)
            {
                collider.center = new Vector3(0f, 0.15f, 0f);
                collider.size = new Vector3(0.9f, 0.35f, 0.9f);
                return;
            }

            collider.center = template.center;
            collider.size = template.size;
        }

        private static void CreatePlotSurface(Transform plotRoot, Transform templateSurface)
        {
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "PlotSurface";
            surface.transform.SetParent(plotRoot, false);
            surface.layer = plotRoot.gameObject.layer;

            var surfaceTransform = surface.transform;
            if (templateSurface != null)
            {
                surfaceTransform.localPosition = templateSurface.localPosition;
                surfaceTransform.localRotation = templateSurface.localRotation;
                surfaceTransform.localScale = templateSurface.localScale;

                var templateRenderer = templateSurface.GetComponent<Renderer>();
                var renderer = surface.GetComponent<Renderer>();
                if (templateRenderer != null && renderer != null)
                    renderer.sharedMaterials = templateRenderer.sharedMaterials;
            }
            else
            {
                surfaceTransform.localPosition = new Vector3(0f, 0.05f, 0f);
                surfaceTransform.localRotation = Quaternion.identity;
                surfaceTransform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            }

            DestroyCollider(surface.GetComponent<Collider>());
            surface.AddComponent<PlotVisualUpdater>();
        }

        private static void CreateCropVisual(Transform plotRoot, Transform templateCropVisual)
        {
            var crop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crop.name = "CropVisual";
            crop.transform.SetParent(plotRoot, false);
            crop.layer = plotRoot.gameObject.layer;

            var cropTransform = crop.transform;
            if (templateCropVisual != null)
            {
                cropTransform.localPosition = templateCropVisual.localPosition;
                cropTransform.localRotation = templateCropVisual.localRotation;
                cropTransform.localScale = templateCropVisual.localScale;
            }
            else
            {
                cropTransform.localPosition = new Vector3(0f, 0.05f, 0f);
                cropTransform.localRotation = Quaternion.identity;
                cropTransform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            }

            var renderer = crop.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;

            DestroyCollider(crop.GetComponent<Collider>());
            crop.AddComponent<CropVisualUpdater>();
        }

        private static void DestroyCollider(Collider collider)
        {
            if (collider == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
