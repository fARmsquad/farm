using FarmSimVR.Core.Farming;

namespace FarmSimVR.Core.Tutorial
{
    public enum FarmTutorialMissionStep
    {
        AwaitPlant,
        PatSoil,
        ClearWeeds,
        TieVine,
        PinchSuckers,
        BrushBlossoms,
        StripLowerLeaves,
        CheckRipeness,
        HarvestTomato,
        Complete,
    }

    public sealed class FarmTutorialMissionService
    {
        private const string TomatoSeedId = "seed_tomato";
        private const string TillObjective = "Till the soil (LMB).";
        private const string PlantObjective = "Plant the tomato seed.";

        private CropTaskId _lastObservedTaskId = CropTaskId.None;

        public FarmTutorialMissionService()
        {
            Reset();
        }

        public FarmTutorialMissionStep CurrentStep { get; private set; }

        public string CurrentObjective { get; private set; } = string.Empty;

        public bool IsComplete => CurrentStep == FarmTutorialMissionStep.Complete;

        public void Reset()
        {
            CurrentStep = FarmTutorialMissionStep.AwaitPlant;
            CurrentObjective = TillObjective;
            _lastObservedTaskId = CropTaskId.None;
        }

        public void Observe(string cropId, PlotStatus soilStatus, CropTaskId currentTaskId)
        {
            if (IsComplete)
            {
                _lastObservedTaskId = currentTaskId;
                return;
            }

            if (string.IsNullOrWhiteSpace(cropId) && (soilStatus == PlotStatus.Empty || soilStatus == PlotStatus.Untilled))
            {
                if (_lastObservedTaskId == CropTaskId.TwistHarvest)
                {
                    CurrentStep = FarmTutorialMissionStep.Complete;
                    CurrentObjective = string.Empty;
                }
                else
                {
                    CurrentStep = FarmTutorialMissionStep.AwaitPlant;
                    CurrentObjective = soilStatus == PlotStatus.Untilled
                        ? TillObjective
                        : PlantObjective;
                }

                _lastObservedTaskId = currentTaskId;
                return;
            }

            if (cropId != TomatoSeedId)
            {
                CurrentStep = FarmTutorialMissionStep.AwaitPlant;
                CurrentObjective = PlantObjective;
                _lastObservedTaskId = currentTaskId;
                return;
            }

            CurrentStep = ToMissionStep(currentTaskId);
            CurrentObjective = ObjectiveFor(CurrentStep);
            _lastObservedTaskId = currentTaskId;
        }

        public bool ConsumeFastForwardRequest()
        {
            return false;
        }

        public bool IsActionAllowed(FarmPlotAction action, string cropId, PlotStatus soilStatus, CropTaskId currentTaskId)
        {
            if (IsComplete)
                return true;

            // Allow tilling untilled plots during tutorial
            if (soilStatus == PlotStatus.Untilled)
                return action == FarmPlotAction.Till;

            if (action != FarmPlotAction.PrimaryInteract)
                return false;

            if (string.IsNullOrWhiteSpace(cropId))
                return soilStatus == PlotStatus.Empty;

            return cropId == TomatoSeedId && currentTaskId != CropTaskId.None;
        }

        public FarmPlotAction? GetPrimaryAction(string cropId, PlotStatus soilStatus, CropTaskId currentTaskId)
        {
            if (IsComplete)
                return null;

            if (soilStatus == PlotStatus.Untilled)
                return FarmPlotAction.Till;

            if (string.IsNullOrWhiteSpace(cropId))
                return soilStatus == PlotStatus.Empty ? FarmPlotAction.PrimaryInteract : null;

            return cropId == TomatoSeedId && currentTaskId != CropTaskId.None
                ? FarmPlotAction.PrimaryInteract
                : null;
        }

        private static FarmTutorialMissionStep ToMissionStep(CropTaskId taskId)
        {
            return taskId switch
            {
                CropTaskId.PatSoil => FarmTutorialMissionStep.PatSoil,
                CropTaskId.ClearWeeds => FarmTutorialMissionStep.ClearWeeds,
                CropTaskId.TieVine => FarmTutorialMissionStep.TieVine,
                CropTaskId.PinchSuckers => FarmTutorialMissionStep.PinchSuckers,
                CropTaskId.BrushBlossoms => FarmTutorialMissionStep.BrushBlossoms,
                CropTaskId.StripLowerLeaves => FarmTutorialMissionStep.StripLowerLeaves,
                CropTaskId.CheckRipeness => FarmTutorialMissionStep.CheckRipeness,
                CropTaskId.TwistHarvest => FarmTutorialMissionStep.HarvestTomato,
                _ => FarmTutorialMissionStep.AwaitPlant,
            };
        }

        private static string ObjectiveFor(FarmTutorialMissionStep step)
        {
            return step switch
            {
                FarmTutorialMissionStep.AwaitPlant => PlantObjective,
                FarmTutorialMissionStep.PatSoil => "Pat the soil closed.",
                FarmTutorialMissionStep.ClearWeeds => "Clear the weeds at the base.",
                FarmTutorialMissionStep.TieVine => "Tie the vine onto the plank.",
                FarmTutorialMissionStep.PinchSuckers => "Pinch the side shoots cleanly.",
                FarmTutorialMissionStep.BrushBlossoms => "Brush pollen across the blossoms.",
                FarmTutorialMissionStep.StripLowerLeaves => "Strip the lower leaves away.",
                FarmTutorialMissionStep.CheckRipeness => "Check the fruit for ripeness.",
                FarmTutorialMissionStep.HarvestTomato => "Twist harvest the ripe tomato.",
                _ => string.Empty,
            };
        }
    }
}
