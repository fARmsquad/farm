using FarmSimVR.Core.Farming;
using FarmSimVR.Core.Story;
using FarmSimVR.Core.Tutorial;
using FarmSimVR.MonoBehaviours;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Farming;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class TutorialFarmSceneController : MonoBehaviour
    {
        [Header("Core Systems")]
        [SerializeField] private FarmSimDriver _driver;

        [Header("Hero Plot")]
        [SerializeField] private GameObject _heroCropPlot;

        [Header("Crop Stage Prefabs (TomatoSeed_01 -> Tomato_07)")]
        [SerializeField] private GameObject _stageSeed;
        [SerializeField] private GameObject _stageSprout;
        [SerializeField] private GameObject _stageGrowing;
        [SerializeField] private GameObject _stageFruiting;
        [SerializeField] private GameObject _stageRipe;
        [SerializeField] private GameObject _tomatoStick;

        [Header("Objective UI")]
        [SerializeField] private string _objectivePrefix = string.Empty;

        [Header("Completion")]
        [SerializeField] private float _completionBannerDuration = 4f;

        private readonly FarmTutorialMissionService _missionService = new();
        private readonly PackagePlantRowsMissionService _packagePlantRowsService = new();

        private CropPlotController _heroCropController;
        private CropPlotController[] _allPlotControllers = System.Array.Empty<CropPlotController>();
        private bool _showCompletion;
        private float _completionTimer;
        private GUIStyle _objectiveStyle;
        private GUIStyle _completionStyle;
        private bool _stylesBuilt;
        private bool _heroPlotConfigured;
        private bool _usePackagePlantRowsMode;
        private bool _packagePlotsRegistered;
        private string _currentObjective = string.Empty;
        private int _packageRowCount = 1;

        public bool IsComplete => _usePackagePlantRowsMode
            ? _packagePlantRowsService.IsComplete
            : _missionService.IsComplete;
        public string CurrentObjectiveText => string.IsNullOrEmpty(_objectivePrefix)
            ? _currentObjective
            : $"{_objectivePrefix}{_currentObjective}";

        private void Start()
        {
            if (_driver == null)
                _driver = FindAnyObjectByType<FarmSimDriver>();

            CacheHeroPlotController();
            CacheAllPlotControllers();
            if (TryConfigurePackagePlantRowsMode())
            {
                EnsurePackagePlantRowsPlots();
                CacheAllPlotControllers();
                _currentObjective = _packagePlantRowsService.CurrentObjective;
                return;
            }

            EnsureHeroPlotConfigured();
            _missionService.Reset();
            _currentObjective = _missionService.CurrentObjective;
        }

        private void Update()
        {
            if (_usePackagePlantRowsMode)
            {
                EnsurePackagePlantRowsPlots();
                TickPackagePlantRowsMode();
                return;
            }

            EnsureHeroPlotConfigured();

            if (_showCompletion)
            {
                _completionTimer -= Time.deltaTime;
                if (_completionTimer <= 0f)
                    _showCompletion = false;
                return;
            }

            TickMissionStep();
        }

        public bool IsActionAllowed(FarmPlotAction action, CropPlotController plotController)
        {
            if (plotController == null)
                return false;

            if (_usePackagePlantRowsMode)
            {
                var packageSoilStatus = plotController.SoilState?.Status ?? PlotStatus.Untilled;
                var packageCropId = plotController.SoilState?.CurrentCropId;
                return _packagePlantRowsService.IsActionAllowed(action, packageSoilStatus, packageCropId);
            }

            EnsureHeroPlotConfigured();

            if (!IsHeroPlot(plotController) && !IsComplete)
                return false;

            var soilState = plotController.SoilState;
            var soilStatus = soilState?.Status ?? PlotStatus.Untilled;
            var cropId = soilState?.CurrentCropId;
            var taskId = plotController.State?.CurrentTaskId ?? CropTaskId.None;
            return _missionService.IsActionAllowed(action, cropId, soilStatus, taskId);
        }

        public FarmPlotAction? GetPrimaryAction(CropPlotController plotController)
        {
            if (plotController == null)
                return null;

            if (_usePackagePlantRowsMode)
            {
                var packageSoilStatus = plotController.SoilState?.Status ?? PlotStatus.Untilled;
                var packageCropId = plotController.SoilState?.CurrentCropId;
                return _packagePlantRowsService.GetPrimaryAction(packageSoilStatus, packageCropId);
            }

            EnsureHeroPlotConfigured();

            if (!IsHeroPlot(plotController) && !IsComplete)
                return null;

            var soilState = plotController.SoilState;
            var soilStatus = soilState?.Status ?? PlotStatus.Untilled;
            var cropId = soilState?.CurrentCropId;
            var taskId = plotController.State?.CurrentTaskId ?? CropTaskId.None;
            return _missionService.GetPrimaryAction(cropId, soilStatus, taskId);
        }

        public void EnsureHeroPlotConfigured()
        {
            if (_usePackagePlantRowsMode)
                return;

            if (_heroPlotConfigured)
                return;

            CacheHeroPlotController();

            if (_heroCropController?.State == null)
                return;

            _heroCropController.State.RequireWateringPerPhase = false;
            _heroCropController.State.ConfigureTutorialLifecycle(CropLifecycleProfiles.TomatoTutorial);

            var cropVisual = _heroCropPlot != null ? _heroCropPlot.GetComponent<CropVisualUpdater>() : null;
            if (cropVisual != null)
            {
                cropVisual.SetTutorialLifecycleStages(
                    _stageSeed,
                    _stageSprout,
                    FindStageRoot("Stage_Tied", "Tomato_02a"),
                    _stageGrowing,
                    FindStageRoot("Stage_Blossoms", "Stage_Budding", "Tomato_04a"),
                    FindStageRoot("Stage_LeafStrip", "Tomato_05"),
                    _stageFruiting,
                    _stageRipe);
            }

            _heroPlotConfigured = true;
        }

        private void CacheHeroPlotController()
        {
            if (_heroCropController == null && _heroCropPlot != null)
                _heroCropController = _heroCropPlot.GetComponent<CropPlotController>();
        }

        private void CacheAllPlotControllers()
        {
            _allPlotControllers = FindObjectsByType<CropPlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private GameObject FindStageRoot(params string[] names)
        {
            if (_heroCropPlot == null)
                return null;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var child = _heroCropPlot.transform.Find(name);
                if (child != null)
                    return child.gameObject;
            }

            return null;
        }

        private void TickMissionStep()
        {
            if (_heroCropController == null || _heroCropController.State == null)
                return;

            var soilState = _heroCropController.SoilState;
            var soilStatus = soilState?.Status ?? PlotStatus.Untilled;
            var cropId = soilState?.CurrentCropId;
            var taskId = _heroCropController.State.CurrentTaskId;

            _missionService.Observe(cropId, soilStatus, taskId);
            _currentObjective = _missionService.CurrentObjective;

            if (_missionService.IsComplete && !_showCompletion)
                CompleteScene();
        }

        private void TickPackagePlantRowsMode()
        {
            if (_showCompletion)
            {
                _completionTimer -= Time.deltaTime;
                if (_completionTimer <= 0f)
                    _showCompletion = false;
                return;
            }

            if (!_packagePlotsRegistered || _allPlotControllers.Length < _packagePlantRowsService.DesiredPlotCount)
                EnsurePackagePlantRowsPlots();

            if (_driver != null)
                _driver.SetSelectedSeed(ResolveSelectedSeedIndex(_packagePlantRowsService.TargetSeedId));

            var plantedCount = CountTargetCropPlots();
            _packagePlantRowsService.Observe(plantedCount, Time.deltaTime);
            _currentObjective = _packagePlantRowsService.CurrentObjective;

            if (_packagePlantRowsService.IsComplete)
                CompleteScene();
        }

        private void CompleteScene()
        {
            _currentObjective = string.Empty;
            _showCompletion = true;
            _completionTimer = _completionBannerDuration;
            TutorialFlowController.Instance?.CompleteCurrentSceneAndLoadNext();
        }

        private void OnGUI()
        {
            BuildStyles();

            if (_showCompletion)
            {
                DrawCompletion();
                return;
            }

            DrawObjective();
        }

        private void DrawObjective()
        {
            if (string.IsNullOrEmpty(CurrentObjectiveText))
                return;

            const float width = 420f;
            const float height = 38f;
            var x = (Screen.width - width) * 0.5f;
            const float y = 24f;

            GUI.color = new Color(0.05f, 0.07f, 0.04f, 0.82f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 16f, y + 8f, width - 32f, height - 16f), CurrentObjectiveText, _objectiveStyle);
        }

        private void DrawCompletion()
        {
            const float width = 520f;
            const float height = 70f;
            var x = (Screen.width - width) * 0.5f;
            var y = (Screen.height - height) * 0.5f;

            GUI.color = new Color(0.10f, 0.28f, 0.10f, 0.92f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 20f, y + 14f, width - 40f, height - 28f), "First harvest complete.", _completionStyle);
        }

        private void BuildStyles()
        {
            if (_stylesBuilt)
                return;

            _objectiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _objectiveStyle.normal.textColor = new Color(0.95f, 0.94f, 0.82f);

            _completionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _completionStyle.normal.textColor = Color.white;

            _stylesBuilt = true;
        }

        private bool IsHeroPlot(CropPlotController plotController)
        {
            return plotController != null && plotController == _heroCropController;
        }

        private bool TryConfigurePackagePlantRowsMode()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (!StoryPackageRuntimeCatalog.TryGetMinigameConfig(sceneName, out _, out var minigame))
                return false;

            if (minigame == null || !string.Equals(minigame.AdapterId, "tutorial.plant_rows", System.StringComparison.Ordinal))
                return false;

            var cropType = "carrot";
            if (!minigame.TryGetStringParameter("cropType", out cropType))
                cropType = InferCropType(minigame.ObjectiveText);
            if (!minigame.TryGetIntParameter("rowCount", out _packageRowCount))
                _packageRowCount = 1;

            _packagePlantRowsService.Configure(
                minigame.ObjectiveText,
                cropType,
                minigame.RequiredCount,
                _packageRowCount,
                minigame.TimeLimitSeconds);
            _packagePlotsRegistered = false;
            _usePackagePlantRowsMode = true;
            return true;
        }

        private void EnsurePackagePlantRowsPlots()
        {
            if (!_usePackagePlantRowsMode || _heroCropPlot == null)
                return;

            PackagePlantRowsPlotSpawner.EnsurePlots(
                _heroCropPlot,
                _packagePlantRowsService.DesiredPlotCount,
                _packageRowCount);
            if (_allPlotControllers.Length < _packagePlantRowsService.DesiredPlotCount)
                CacheAllPlotControllers();

            if (_driver == null)
                _driver = FindAnyObjectByType<FarmSimDriver>();
            if (_driver == null || !_driver.IsInitialized)
                return;

            var registeredCount = 0;
            for (int i = 0; i < _allPlotControllers.Length; i++)
            {
                var plot = _allPlotControllers[i];
                if (plot == null)
                    continue;

                _driver.RegisterRuntimePlot(plot.gameObject);
                if (_driver.HasRegisteredPlot(plot.gameObject.name))
                    registeredCount++;
            }

            _packagePlotsRegistered = registeredCount >= _packagePlantRowsService.DesiredPlotCount;
        }

        private int CountTargetCropPlots()
        {
            var seedId = _packagePlantRowsService.TargetSeedId;
            var cropId = seedId.Replace("seed_", "crop_");
            var count = 0;

            for (int i = 0; i < _allPlotControllers.Length; i++)
            {
                var soilState = _allPlotControllers[i]?.SoilState;
                var currentCropId = soilState?.CurrentCropId;
                if (string.Equals(currentCropId, seedId, System.StringComparison.Ordinal) ||
                    string.Equals(currentCropId, cropId, System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int ResolveSelectedSeedIndex(string seedId)
        {
            return seedId switch
            {
                "seed_tomato" => 0,
                "seed_carrot" => 1,
                "seed_lettuce" => 2,
                _ => 1,
            };
        }

        private static string InferCropType(string objectiveText)
        {
            if (string.IsNullOrWhiteSpace(objectiveText))
                return "carrot";

            var normalized = objectiveText.ToLowerInvariant();
            if (normalized.Contains("tomato"))
                return "tomato";
            if (normalized.Contains("lettuce"))
                return "lettuce";
            return "carrot";
        }
    }
}
