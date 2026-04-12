using System;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Visual lifecycle phases for a crop plot.
    /// Legacy auto-growth still uses the coarse phase ladder, while the Scene 7
    /// tutorial can layer a finer task-driven stage index on top of it.
    /// </summary>
    public enum PlotPhase
    {
        Empty,
        Planted,
        Sprout,
        YoungPlant,
        Budding,
        Fruiting,
        Ready,
        Wilting,
        Dead
    }

    public class CropPlotState
    {
        public const float WiltThreshold = 0.20f;
        public const float DeathTimeoutSeconds = 8f;

        public PlotPhase Phase { get; private set; } = PlotPhase.Empty;
        public float CurrentGrowth { get; private set; }
        public CropData CropData { get; private set; }
        public float Moisture { get; private set; } = 1f;

        public bool RequireWateringPerPhase { get; set; }
        public bool IsTutorialTaskMode => _tutorialLifecycleProfile != null;
        public int CurrentStageIndex { get; private set; } = -1;
        public CropTaskId CurrentTaskId => ActiveTutorialStep?.RequiredTaskId ?? CropTaskId.None;
        public string CurrentTaskPrompt => ActiveTutorialStep?.PromptText ?? string.Empty;
        public string CurrentActionLabel => ActiveTutorialStep?.ActionLabel ?? string.Empty;
        public string CurrentVisualAssetId => ActiveTutorialStep?.VisualAssetId ?? string.Empty;
        public FarmStageMinigameDefinition CurrentMinigame => ActiveTutorialStep?.Minigame ?? FarmStageMinigameDefinition.None;
        public bool CurrentTaskCompletesHarvest => ActiveTutorialStep?.CompletesHarvest ?? false;

        public float GrowthPercent
        {
            get
            {
                if (IsTutorialTaskMode && _tutorialLifecycleProfile.Steps.Count > 0 && CurrentStageIndex >= 0)
                    return Mathf01Clamp((CurrentStageIndex + 1f) / _tutorialLifecycleProfile.Steps.Count);

                return CropData.MaxGrowth > 0f
                    ? Mathf01Clamp(CurrentGrowth / CropData.MaxGrowth)
                    : 0f;
            }
        }

        public bool NeedsWaterToAdvance =>
            !IsTutorialTaskMode &&
            (Phase == PlotPhase.Planted
                ? _waterEventCount <= _growthGateWaterEventCount
                : RequireWateringPerPhase &&
                  Phase != PlotPhase.Empty &&
                  Phase != PlotPhase.Ready &&
                  Phase != PlotPhase.Dead &&
                  Phase != PlotPhase.Wilting &&
                  _waterEventCount <= _growthGateWaterEventCount);

        private readonly ICropGrowthCalculator _calculator;
        private CropLifecycleProfile _tutorialLifecycleProfile;
        private int _lastMilestone;
        private float _drySeconds;
        private PlotPhase _phaseBeforeWilt = PlotPhase.Empty;
        private int _waterEventCount;
        private int _growthGateWaterEventCount;

        public event Action<int> OnMilestone;
        public event Action OnWilting;
        public event Action OnRevived;
        public event Action OnDead;

        public CropPlotState(ICropGrowthCalculator calculator)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public void ConfigureTutorialLifecycle(CropLifecycleProfile profile)
        {
            _tutorialLifecycleProfile = profile ?? throw new ArgumentNullException(nameof(profile));

            if (Phase == PlotPhase.Empty)
            {
                CurrentStageIndex = -1;
                CurrentGrowth = 0f;
            }
        }

        public void SetMoisture(float moisture)
        {
            Moisture = Mathf01Clamp(moisture);
        }

        public void NotifyWatered()
        {
            if (!IsTutorialTaskMode)
                _waterEventCount++;
        }

        public void Plant(CropData cropData)
        {
            if (Phase != PlotPhase.Empty)
                throw new InvalidOperationException($"Cannot plant in phase {Phase}");

            CropData = cropData;
            CurrentGrowth = 0f;
            CurrentStageIndex = -1;
            _lastMilestone = 0;
            _drySeconds = 0f;
            _growthGateWaterEventCount = _waterEventCount;

            if (IsTutorialTaskMode)
            {
                CurrentStageIndex = 0;
                ApplyTutorialStage();
                return;
            }

            Phase = PlotPhase.Planted;
        }

        public bool CanCompleteTask(CropTaskId taskId)
        {
            return IsTutorialTaskMode &&
                   taskId != CropTaskId.None &&
                   ActiveTutorialStep != null &&
                   ActiveTutorialStep.RequiredTaskId == taskId;
        }

        public bool TryCompleteTask(CropTaskId taskId)
        {
            if (!CanCompleteTask(taskId))
                return false;

            if (CurrentTaskCompletesHarvest)
                return true;

            if (CurrentStageIndex >= _tutorialLifecycleProfile.Steps.Count - 1)
                return false;

            CurrentStageIndex++;
            ApplyTutorialStage();
            FireMilestone(CurrentStageIndex);
            return true;
        }

        public void Tick(GrowthConditions conditions, float deltaTime)
        {
            if (Phase == PlotPhase.Empty || Phase == PlotPhase.Ready || Phase == PlotPhase.Dead)
                return;

            if (IsTutorialTaskMode)
                return;

            if (Phase == PlotPhase.Wilting)
            {
                if (Moisture >= WiltThreshold)
                {
                    Phase = _phaseBeforeWilt;
                    _drySeconds = 0f;
                    OnRevived?.Invoke();
                }
                else
                {
                    _drySeconds += deltaTime;
                    if (_drySeconds >= DeathTimeoutSeconds)
                    {
                        Phase = PlotPhase.Dead;
                        OnDead?.Invoke();
                    }
                }
                return;
            }

            if (Phase == PlotPhase.Planted && NeedsWaterToAdvance)
                return;

            if (Phase == PlotPhase.Planted && Moisture < WiltThreshold)
                return;

            if (Moisture < WiltThreshold && Phase != PlotPhase.Planted)
            {
                _phaseBeforeWilt = Phase;
                _drySeconds = 0f;
                Phase = PlotPhase.Wilting;
                OnWilting?.Invoke();
                return;
            }

            if (NeedsWaterToAdvance)
                return;

            var moistureBonus = Moisture > 0.6f ? 1.25f : 1.0f;
            var result = _calculator.CalculateGrowth(CropData, conditions, CurrentGrowth, deltaTime * moistureBonus);
            CurrentGrowth = result.IsFullyGrown ? CropData.MaxGrowth : CurrentGrowth + result.GrowthAmount;

            AdvanceLegacyPhase();
        }

        public void Harvest()
        {
            if (Phase != PlotPhase.Ready)
                throw new InvalidOperationException($"Cannot harvest in phase {Phase}");

            CurrentGrowth = 0f;
            CurrentStageIndex = -1;
            _lastMilestone = 0;
            _drySeconds = 0f;
            Phase = PlotPhase.Empty;
        }

        public void ClearDead()
        {
            if (Phase != PlotPhase.Dead)
                throw new InvalidOperationException($"Cannot clear in phase {Phase}");

            CurrentGrowth = 0f;
            CurrentStageIndex = -1;
            _lastMilestone = 0;
            _drySeconds = 0f;
            Phase = PlotPhase.Empty;
        }

        private CropLifecycleStep ActiveTutorialStep =>
            !IsTutorialTaskMode ||
            CurrentStageIndex < 0 ||
            CurrentStageIndex >= _tutorialLifecycleProfile.Steps.Count
                ? null
                : _tutorialLifecycleProfile.Steps[CurrentStageIndex];

        private void ApplyTutorialStage()
        {
            var step = ActiveTutorialStep;
            if (step == null)
            {
                Phase = PlotPhase.Empty;
                CurrentGrowth = 0f;
                return;
            }

            Phase = step.Phase;
            CurrentGrowth = CropData.MaxGrowth > 0f
                ? CropData.MaxGrowth * GrowthPercent
                : CurrentGrowth;
        }

        private void AdvanceLegacyPhase()
        {
            switch (Phase)
            {
                case PlotPhase.Planted when CurrentGrowth > 0f:
                    Phase = PlotPhase.Sprout;
                    ResetPhaseWaterGate();
                    FireMilestone(1);
                    break;
                case PlotPhase.Sprout when CurrentGrowth >= 0.2f:
                    Phase = PlotPhase.YoungPlant;
                    ResetPhaseWaterGate();
                    FireMilestone(2);
                    break;
                case PlotPhase.YoungPlant when CurrentGrowth >= 0.4f:
                    Phase = PlotPhase.Budding;
                    ResetPhaseWaterGate();
                    FireMilestone(3);
                    break;
                case PlotPhase.Budding when CurrentGrowth >= 0.6f:
                    Phase = PlotPhase.Fruiting;
                    ResetPhaseWaterGate();
                    FireMilestone(4);
                    break;
                case PlotPhase.Fruiting when CurrentGrowth >= 0.8f:
                    Phase = PlotPhase.Ready;
                    FireMilestone(5);
                    break;
            }
        }

        private void FireMilestone(int gate)
        {
            if (_lastMilestone < gate)
            {
                _lastMilestone = gate;
                OnMilestone?.Invoke(gate);
            }
        }

        private void ResetPhaseWaterGate()
        {
            if (RequireWateringPerPhase && Phase != PlotPhase.Ready)
                _growthGateWaterEventCount = _waterEventCount;
        }

        private static float Mathf01Clamp(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
