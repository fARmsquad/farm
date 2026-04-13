using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FarmSimVR.Core.Farming;
using GLTFast;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    public sealed class CropVisualUpdater : MonoBehaviour
    {
        private const string FallbackVisualName = "FallbackVisual";
        private const string ImportedSourceRootName = "_ImportedCropSource";
        private const string SeedVisualName = "SeedVisual";
        private const string UntilledMarkerPrefix = "UntilledMarker_";
        private const int UntilledMarkerCount = 4;
        private const float CropYOffset = 0.01f;
        private const float SoilYOffset = 0.005f;
        private static readonly Quaternion ImportedModelCorrection = Quaternion.Euler(90f, 0f, 0f);
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color PlantedColor = new(0.48f, 0.66f, 0.24f);
        private static readonly Color SeedlingColor = new(0.28f, 0.78f, 0.24f);
        private static readonly Color MatureColor = new(0.92f, 0.2f, 0.15f);
        private static readonly Color SeedColor = new(0.45f, 0.3f, 0.12f);
        private static readonly Color StakeColor = new(0.55f, 0.42f, 0.22f);

        [SerializeField] private GameObject soilPrefab;

        /// <summary>
        /// Direct phase → GameObject mapping using asset pack prefab children.
        /// When any entry is wired, the phase-stage system takes full priority over the GLB importer.
        /// </summary>
        [SerializeField] private PhaseStageEntry[] phaseStages = Array.Empty<PhaseStageEntry>();
        [SerializeField] private PhaseExtraEntry[] phaseExtras = Array.Empty<PhaseExtraEntry>();

        [Serializable]
        public sealed class PhaseStageEntry
        {
            public PlotPhase phase;
            public GameObject stageRoot;
        }

        [Serializable]
        public sealed class PhaseExtraEntry
        {
            public PlotPhase minPhase = PlotPhase.Planted;
            public PlotPhase maxPhase = PlotPhase.Ready;
            public GameObject root;
        }

        private CropPlotController _controller;
        private Renderer _fallbackRenderer;
        private Transform _fallbackTransform;
        private MaterialPropertyBlock _propBlock;
        private Vector3 _fallbackBaseLocalPosition;
        private Vector3 _fallbackBaseLocalScale;
        private bool _artLoadStarted;
        private int _indexedChildCount = -1;
        private readonly Dictionary<string, Transform[]> _stageVariants = new(StringComparer.Ordinal);
        private readonly List<Transform> _soilVariants = new();
        private GameObject[] _tutorialStageRoots = Array.Empty<GameObject>();
        private GameObject _seedVisualRoot;
        private readonly GameObject[] _untilledMarkers = new GameObject[UntilledMarkerCount];

        private void Awake()
        {
            _controller = GetComponentInParent<CropPlotController>();
            _propBlock = new MaterialPropertyBlock();
            EnsureFallbackVisual();
            ReindexImportedArt();
        }
        private void Start()
        {
            BeginImportedArtLoadIfNeeded();
            BuildPrefabSoilVariant();
        }
        private void Update()
        {
            RefreshVisuals();
        }

        public void SetTutorialLifecycleStages(params GameObject[] stageRoots)
        {
            _tutorialStageRoots = stageRoots ?? Array.Empty<GameObject>();
        }

        public void RefreshVisuals()
        {
            if (_controller == null)
                _controller = GetComponentInParent<CropPlotController>();

            if (_controller?.State == null)
                return;

            ApplySoilVariant();
            ApplyUntilledMarkers();
            ApplySeedVisual();

            var phase = _controller.State.Phase;

            if (_controller.State.IsTutorialTaskMode && _tutorialStageRoots.Length > 0)
            {
                ApplyTutorialStages(_controller.State.CurrentStageIndex);
                ApplyPhaseExtras(phase);
                HideFallbackVisual();
                return;
            }

            // Phase-stage system (asset pack prefabs) takes full priority over GLB importer
            if (phaseStages != null && phaseStages.Length > 0)
            {
                ApplyPhaseStages(phase);
                ApplyPhaseExtras(phase);
                HideFallbackVisual();
                return;
            }

            // Legacy GLB importer path
            EnsureFallbackVisual();
            BeginImportedArtLoadIfNeeded();
            if (_indexedChildCount != transform.childCount)
                ReindexImportedArt();

            var growthPercent = _controller.State.GrowthPercent;
            var cropId = _controller.SoilState?.CurrentCropId;
            if (TryApplyImportedStage(cropId, phase))
                return;
            ApplyFallbackVisual(phase, growthPercent);
        }

        private void ApplyTutorialStages(int activeStageIndex)
        {
            if (phaseStages != null)
            {
                foreach (var entry in phaseStages)
                {
                    if (entry?.stageRoot != null)
                        entry.stageRoot.SetActive(false);
                }
            }

            for (var i = 0; i < _tutorialStageRoots.Length; i++)
            {
                var root = _tutorialStageRoots[i];
                if (root != null)
                    root.SetActive(i == activeStageIndex);
            }
        }

        private void ApplyPhaseStages(PlotPhase activePhase)
        {
            foreach (var entry in phaseStages)
            {
                if (entry?.stageRoot == null)
                    continue;
                entry.stageRoot.SetActive(entry.phase == activePhase);
            }
        }

        private void ApplyPhaseExtras(PlotPhase activePhase)
        {
            foreach (var entry in phaseExtras)
            {
                if (entry?.root == null)
                    continue;

                entry.root.SetActive(activePhase >= entry.minPhase && activePhase <= entry.maxPhase);
            }
        }

        private void HideFallbackVisual()
        {
            if (_fallbackRenderer != null)
                _fallbackRenderer.enabled = false;
        }

        private void BeginImportedArtLoadIfNeeded()
        {
            if (!Application.isPlaying || _artLoadStarted)
                return;

            _artLoadStarted = true;
            _ = LoadImportedArtAsync();
        }
        private async Task LoadImportedArtAsync()
        {
            var glbPath = Path.Combine(Application.streamingAssetsPath, CropArtCatalog.StreamingAssetRelativePath);
            if (!File.Exists(glbPath))
                return;

            var sourceRoot = new GameObject(ImportedSourceRootName);
            sourceRoot.hideFlags = HideFlags.HideAndDontSave;
            sourceRoot.transform.SetParent(transform, false);
            try
            {
                var gltfImport = new GltfImport();
                var loaded = await gltfImport.LoadFile(glbPath);
                if (!loaded)
                    return;

                var instantiated = await gltfImport.InstantiateMainSceneAsync(sourceRoot.transform);
                if (!instantiated)
                    return;

                BuildImportedVariants(sourceRoot.transform);
            }
            finally
            {
                DestroyRuntimeObject(sourceRoot);
                _indexedChildCount = -1;
                ReindexImportedArt();
            }
        }

        private void BuildImportedVariants(Transform sourceRoot)
        {
            foreach (var cropDefinition in CropArtCatalog.StageDefinitions)
            {
                if (!CropArtCatalog.TryGetDisplayProfile(cropDefinition.Key, out var profile))
                    continue;

                var matureSource = FindDescendantByName(sourceRoot, cropDefinition.Value[cropDefinition.Value.Length - 1]);
                if (matureSource == null)
                    continue;

                var scaleFactor = ResolveUniformCropScale(matureSource, profile);
                if (scaleFactor <= 0f)
                    continue;

                for (var i = 0; i < cropDefinition.Value.Length; i++)
                {
                    var outputName = BuildStageObjectName(cropDefinition.Key, i + 1);
                    if (transform.Find(outputName) != null)
                        continue;

                    var source = FindDescendantByName(sourceRoot, cropDefinition.Value[i]);
                    if (source == null)
                        continue;

                    CreateScaledClone(source, outputName, scaleFactor, CropYOffset);
                }
            }
            BuildPrefabSoilVariant();
        }
        private void BuildPrefabSoilVariant()
        {
            var outputName = BuildSoilObjectName(0);
            if (transform.Find(outputName) != null)
                return;

            if (soilPrefab == null)
                return;

            var instance = Instantiate(soilPrefab, transform, false);
            instance.name = outputName;
            RemoveColliders(instance.transform);
            SetLayerRecursively(instance.transform, gameObject.layer);

            var soilTargetWidth = ResolveSoilFootprintWidth();
            var bounds = MeasureBounds(instance.transform);
            if (bounds.size.sqrMagnitude > 0f)
            {
                var scaleX = soilTargetWidth / Mathf.Max(bounds.size.x, 0.001f);
                var scaleZ = soilTargetWidth / Mathf.Max(bounds.size.z, 0.001f);
                var scaleY = Mathf.Min(scaleX, scaleZ);
                instance.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }

            AlignWrapperToGround(instance.transform, SoilYOffset);
            instance.SetActive(false);
            _soilVariants.Add(instance.transform);
        }

        private static float ResolveUniformCropScale(
            Transform matureSource,
            CropArtCatalog.CropDisplayProfile profile)
        {
            var bounds = MeasureBounds(matureSource);
            var heightScale = profile.MatureHeightMeters / Mathf.Max(bounds.size.z, 0.001f);
            var footprintScale = profile.MaxFootprintMeters / Mathf.Max(bounds.size.x, bounds.size.y, 0.001f);
            return Mathf.Min(heightScale, footprintScale);
        }
        private void CreateScaledClone(
            Transform source,
            string outputName,
            float scaleFactor,
            float yOffset)
        {
            var wrapper = CreateWrappedClone(source, outputName);
            wrapper.transform.localScale = Vector3.one * scaleFactor;
            AlignWrapperToGround(wrapper.transform, yOffset);
            wrapper.SetActive(false);
        }
        private void CreateFootprintClone(
            Transform source,
            string outputName,
            float targetWidth,
            float yOffset)
        {
            var wrapper = CreateWrappedClone(source, outputName);
            var bounds = MeasureBounds(wrapper.transform);
            if (bounds.size.sqrMagnitude > 0f)
            {
                var scaleX = targetWidth / Mathf.Max(bounds.size.x, 0.001f);
                var scaleZ = targetWidth / Mathf.Max(bounds.size.z, 0.001f);
                var scaleY = Mathf.Min(scaleX, scaleZ);
                wrapper.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
            AlignWrapperToGround(wrapper.transform, yOffset);
            wrapper.SetActive(false);
        }
        private GameObject CreateWrappedClone(Transform source, string outputName)
        {
            var wrapper = new GameObject(outputName);
            wrapper.transform.SetParent(transform, false);
            wrapper.layer = gameObject.layer;
            var clone = Instantiate(source.gameObject, wrapper.transform, false);
            clone.name = source.gameObject.name;
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = ImportedModelCorrection * source.localRotation;
            clone.transform.localScale = source.localScale;
            clone.SetActive(true);
            RemoveColliders(wrapper.transform);
            SetLayerRecursively(wrapper.transform, gameObject.layer);
            return wrapper;
        }
        private void AlignWrapperToGround(Transform wrapper, float yOffset)
        {
            var bounds = MeasureBounds(wrapper);
            var targetBaseWorld = ResolvePlotAnchorWorldPoint(yOffset);
            var deltaWorld = new Vector3(
                targetBaseWorld.x - bounds.center.x,
                targetBaseWorld.y - bounds.min.y,
                targetBaseWorld.z - bounds.center.z);
            wrapper.position += deltaWorld;
        }
        private float ResolveSoilFootprintWidth()
        {
            if (!TryGetPlotSurfaceBounds(out var plotSurfaceBounds))
                return CropArtCatalog.RecommendedPlotSurfaceSizeMeters * 0.92f;
            return Mathf.Max(Mathf.Min(plotSurfaceBounds.size.x, plotSurfaceBounds.size.z) * 0.92f, 0.1f);
        }
        private Vector3 ResolvePlotAnchorWorldPoint(float yOffset)
        {
            if (!TryGetPlotSurfaceBounds(out var plotSurfaceBounds))
                return transform.TransformPoint(new Vector3(0f, yOffset, 0f));
            return new Vector3(
                plotSurfaceBounds.center.x,
                plotSurfaceBounds.max.y + yOffset,
                plotSurfaceBounds.center.z);
        }
        private bool TryGetPlotSurfaceBounds(out Bounds plotSurfaceBounds)
        {
            plotSurfaceBounds = default;
            var plotSurface = transform.parent != null ? transform.parent.Find("PlotSurface") : null;
            var renderer = plotSurface != null
                ? plotSurface.GetComponent<Renderer>()
                : transform.parent != null ? transform.parent.GetComponent<Renderer>() : null;
            if (renderer == null)
                return false;

            plotSurfaceBounds = renderer.bounds;
            return true;
        }
        private static Bounds MeasureBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
        private static void RemoveColliders(Transform root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                DestroyRuntimeObject(collider);
        }
        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }
        private bool TryApplyImportedStage(string cropId, PlotPhase phase)
        {
            var stageIndex = CropArtCatalog.ResolveStageIndex(phase);
            string cropKey = null;
            var hasCrop = stageIndex > 0 && CropArtCatalog.TryGetCropKey(cropId, out cropKey);
            foreach (var entry in _stageVariants)
            {
                var shouldShowEntry = hasCrop && string.Equals(entry.Key, cropKey, StringComparison.Ordinal);
                for (var i = 0; i < entry.Value.Length; i++)
                {
                    var stage = entry.Value[i];
                    if (stage == null)
                        continue;
                    stage.gameObject.SetActive(shouldShowEntry && (i + 1) == stageIndex);
                }
            }
            if (!hasCrop || !_stageVariants.TryGetValue(cropKey, out var stages))
                return false;
            var activeStage = stageIndex - 1 < stages.Length ? stages[stageIndex - 1] : null;
            if (activeStage == null)
                return false;
            ResetFallbackTransform();
            if (_fallbackRenderer != null)
                _fallbackRenderer.enabled = false;
            return true;
        }
        private void ApplySoilVariant()
        {
            if (_soilVariants.Count == 0)
                return;

            // Hide soil mesh when plot is untilled — raw ground only
            var isUntilled = _controller?.SoilState?.Status == PlotStatus.Untilled;
            if (isUntilled)
            {
                foreach (var soil in _soilVariants)
                {
                    if (soil != null)
                        soil.gameObject.SetActive(false);
                }
                return;
            }

            var activeIndex = ResolveSoilVariantIndex();
            for (var i = 0; i < _soilVariants.Count; i++)
            {
                var soil = _soilVariants[i];
                if (soil != null)
                    soil.gameObject.SetActive(i == activeIndex);
            }

            if (activeIndex >= 0 && activeIndex < _soilVariants.Count && _soilVariants[activeIndex] != null)
                ApplySoilTint(_soilVariants[activeIndex]);
        }
        private int ResolveSoilVariantIndex()
        {
            if (_soilVariants.Count == 0)
                return -1;
            var plotName = _controller != null ? _controller.gameObject.name : gameObject.name;
            var hash = plotName != null ? Mathf.Abs(plotName.GetHashCode()) : 0;
            return hash % _soilVariants.Count;
        }
        private void ApplySoilTint(Transform soilRoot)
        {
            var targetColor = PlotVisualUpdater.ResolveSoilColor(
                _controller?.SoilState,
                _controller?.State?.Phase ?? PlotPhase.Empty);

            foreach (var renderer in soilRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(ColorId, targetColor);
                _propBlock.SetColor(BaseColorId, targetColor);
                renderer.SetPropertyBlock(_propBlock);
            }
        }
        /// <summary>
        /// Shows small brown spheres on the soil during the Planted phase to represent seeds.
        /// </summary>
        private void ApplySeedVisual()
        {
            var showSeeds = _controller?.State?.Phase == PlotPhase.Planted;

            if (showSeeds && _seedVisualRoot == null)
            {
                _seedVisualRoot = new GameObject(SeedVisualName);
                _seedVisualRoot.transform.SetParent(transform, false);
                _seedVisualRoot.layer = gameObject.layer;

                var offsets = new[]
                {
                    new Vector3(-0.12f, CropYOffset + 0.02f, 0.08f),
                    new Vector3(0.10f, CropYOffset + 0.02f, -0.06f),
                    new Vector3(-0.02f, CropYOffset + 0.02f, -0.12f),
                };

                foreach (var offset in offsets)
                {
                    var seed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    seed.name = "Seed";
                    seed.transform.SetParent(_seedVisualRoot.transform, false);
                    seed.transform.localPosition = offset;
                    seed.transform.localScale = new Vector3(0.04f, 0.03f, 0.04f);
                    seed.layer = gameObject.layer;

                    var col = seed.GetComponent<Collider>();
                    if (col != null) DestroyRuntimeObject(col);

                    var rend = seed.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.GetPropertyBlock(_propBlock);
                        _propBlock.SetColor(ColorId, SeedColor);
                        _propBlock.SetColor(BaseColorId, SeedColor);
                        rend.SetPropertyBlock(_propBlock);
                    }
                }
            }

            if (_seedVisualRoot != null)
                _seedVisualRoot.SetActive(showSeeds);
        }

        /// <summary>
        /// Shows subtle corner stake markers when the plot is untilled so players can find plots in the grass.
        /// </summary>
        private void ApplyUntilledMarkers()
        {
            var isUntilled = _controller?.SoilState?.Status == PlotStatus.Untilled;

            if (isUntilled && _untilledMarkers[0] == null)
            {
                var halfSize = 0.45f;
                var corners = new[]
                {
                    new Vector3(-halfSize, 0f, -halfSize),
                    new Vector3(halfSize, 0f, -halfSize),
                    new Vector3(halfSize, 0f, halfSize),
                    new Vector3(-halfSize, 0f, halfSize),
                };

                for (var i = 0; i < UntilledMarkerCount; i++)
                {
                    var stake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stake.name = UntilledMarkerPrefix + i;
                    stake.transform.SetParent(transform, false);
                    stake.transform.localPosition = corners[i] + new Vector3(0f, 0.06f, 0f);
                    stake.transform.localScale = new Vector3(0.03f, 0.06f, 0.03f);
                    stake.layer = gameObject.layer;

                    var col = stake.GetComponent<Collider>();
                    if (col != null) DestroyRuntimeObject(col);

                    var rend = stake.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.GetPropertyBlock(_propBlock);
                        _propBlock.SetColor(ColorId, StakeColor);
                        _propBlock.SetColor(BaseColorId, StakeColor);
                        rend.SetPropertyBlock(_propBlock);
                    }

                    _untilledMarkers[i] = stake;
                }
            }

            for (var i = 0; i < UntilledMarkerCount; i++)
            {
                if (_untilledMarkers[i] != null)
                    _untilledMarkers[i].SetActive(isUntilled);
            }
        }

        private void ApplyFallbackVisual(PlotPhase phase, float growthPercent)
        {
            if (_fallbackRenderer == null || _fallbackTransform == null)
                return;
            // Hide fallback when in Planted phase (seed visual is shown instead)
            var visible = phase != PlotPhase.Empty && phase != PlotPhase.Planted;
            if (_fallbackRenderer.enabled != visible)
                _fallbackRenderer.enabled = visible;
            if (!visible)
                return;
            var displayT = phase switch
            {
                PlotPhase.Planted    => 0.08f,
                PlotPhase.Sprout     => Mathf.Lerp(0.10f, 0.30f, Mathf.Clamp01(growthPercent)),
                PlotPhase.YoungPlant => Mathf.Lerp(0.30f, 0.55f, Mathf.Clamp01(growthPercent)),
                PlotPhase.Budding    => Mathf.Lerp(0.55f, 0.75f, Mathf.Clamp01(growthPercent)),
                PlotPhase.Fruiting   => Mathf.Lerp(0.75f, 0.92f, Mathf.Clamp01(growthPercent)),
                PlotPhase.Ready      => 1f,
                PlotPhase.Wilting    => 0.5f,
                PlotPhase.Dead       => 0.2f,
                _                    => Mathf.Clamp01(growthPercent)
            };
            var scaleY = Mathf.Lerp(0.12f, 1.0f, displayT);
            var width = Mathf.Lerp(0.18f, 0.8f, displayT);
            _fallbackTransform.localScale = new Vector3(width, scaleY, width);
            _fallbackTransform.localPosition = new Vector3(
                _fallbackBaseLocalPosition.x,
                _fallbackBaseLocalPosition.y + scaleY * 0.5f,
                _fallbackBaseLocalPosition.z);
            var fromColor = phase == PlotPhase.Planted ? PlantedColor : SeedlingColor;
            var color = Color.Lerp(fromColor, MatureColor, Mathf.Clamp01(growthPercent));
            _fallbackRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorId, color);
            _propBlock.SetColor(BaseColorId, color);
            _fallbackRenderer.SetPropertyBlock(_propBlock);
        }
        private void EnsureFallbackVisual()
        {
            if (_fallbackRenderer != null && _fallbackTransform != null)
                return;
            var fallback = transform.Find(FallbackVisualName);
            if (fallback != null)
            {
                _fallbackTransform = fallback;
                _fallbackRenderer = fallback.GetComponent<Renderer>();
            }
            if (_fallbackRenderer == null)
            {
                var selfRenderer = GetComponent<Renderer>();
                if (selfRenderer != null)
                {
                    _fallbackTransform = transform;
                    _fallbackRenderer = selfRenderer;
                }
            }
            if (_fallbackRenderer == null)
            {
                var fallbackObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallbackObject.name = FallbackVisualName;
                fallbackObject.transform.SetParent(transform, false);
                fallbackObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                fallbackObject.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
                fallbackObject.layer = gameObject.layer;
                var collider = fallbackObject.GetComponent<Collider>();
                if (collider != null)
                    DestroyRuntimeObject(collider);
                _fallbackTransform = fallbackObject.transform;
                _fallbackRenderer = fallbackObject.GetComponent<Renderer>();
            }
            _fallbackBaseLocalPosition = _fallbackTransform.localPosition;
            _fallbackBaseLocalScale = _fallbackTransform.localScale;
        }
        private void ResetFallbackTransform()
        {
            if (_fallbackTransform == null)
                return;
            _fallbackTransform.localPosition = _fallbackBaseLocalPosition;
            _fallbackTransform.localScale = _fallbackBaseLocalScale;
        }
        private void ReindexImportedArt()
        {
            _indexedChildCount = transform.childCount;
            _stageVariants.Clear();
            _soilVariants.Clear();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (TryParseStageObjectName(child.name, out var cropKey, out var stageIndex))
                {
                    if (!_stageVariants.TryGetValue(cropKey, out var stages))
                    {
                        stages = new Transform[3];
                        _stageVariants[cropKey] = stages;
                    }
                    if (stageIndex >= 1 && stageIndex <= stages.Length)
                        stages[stageIndex - 1] = child;
                    continue;
                }
                if (TryParseSoilObjectName(child.name, out _))
                    _soilVariants.Add(child);
            }
        }
        private static string BuildStageObjectName(string cropKey, int stageIndex)
        {
            return $"CropStage_{cropKey}_{stageIndex}";
        }
        private static string BuildSoilObjectName(int soilIndex)
        {
            return $"SoilVariant_{soilIndex}";
        }
        private static bool TryParseStageObjectName(string objectName, out string cropKey, out int stageIndex)
        {
            cropKey = null;
            stageIndex = 0;
            const string prefix = "CropStage_";
            if (!objectName.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            var suffix = objectName.Substring(prefix.Length);
            var separatorIndex = suffix.LastIndexOf('_');
            if (separatorIndex <= 0)
                return false;
            cropKey = suffix.Substring(0, separatorIndex);
            return int.TryParse(suffix.Substring(separatorIndex + 1), out stageIndex);
        }
        private static bool TryParseSoilObjectName(string objectName, out int soilIndex)
        {
            const string prefix = "SoilVariant_";
            soilIndex = -1;
            return objectName.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(objectName.Substring(prefix.Length), out soilIndex);
        }
        private static Transform FindDescendantByName(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var match = FindDescendantByName(root.GetChild(i), name);
                if (match != null)
                    return match;
            }
            return null;
        }
        private static void DestroyRuntimeObject(UnityEngine.Object instance)
        {
            if (instance == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(instance);
            else
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
