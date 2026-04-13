using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Farming
{
    public enum FarmPlotAction
    {
        PrimaryInteract,
        PlantTomato,
        PlantCarrot,
        PlantLettuce,
        PlantSelected,
        Water,
        Harvest,
        Compost,
        ClearDead,
        Till
    }

    public readonly struct FarmPlotActionOption
    {
        public FarmPlotActionOption(FarmPlotAction action, string keyLabel, string label)
        {
            Action = action;
            KeyLabel = keyLabel ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public FarmPlotAction Action { get; }
        public string KeyLabel { get; }
        public string Label { get; }
    }

    public sealed class FarmPlotActionPrompt
    {
        public FarmPlotActionPrompt(string title, string detail, IReadOnlyList<FarmPlotActionOption> actions)
        {
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            Actions = actions ?? Array.Empty<FarmPlotActionOption>();
        }

        public string Title { get; }
        public string Detail { get; }
        public IReadOnlyList<FarmPlotActionOption> Actions { get; }
    }

    public static class FarmPlotActionResolver
    {
        public static FarmPlotActionPrompt Build(
            SoilState soil,
            CropPlotState crop,
            int tomatoSeeds,
            int carrotSeeds,
            int lettuceSeeds,
            int selectedSeedIndex = 0)
        {
            if (soil == null) throw new ArgumentNullException(nameof(soil));
            if (crop == null) throw new ArgumentNullException(nameof(crop));

            if (crop.IsTutorialTaskMode)
                return BuildTutorialPrompt(soil, crop, tomatoSeeds);

            var actions = new List<FarmPlotActionOption>(4);

            switch (soil.Status)
            {
                case PlotStatus.Untilled:
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.Till, "LMB", "Till Soil"));
                    break;

                case PlotStatus.Empty:
                    var seedCounts = new[] { tomatoSeeds, carrotSeeds, lettuceSeeds };
                    var seedNames = new[] { "Tomato", "Carrot", "Lettuce" };
                    var idx = selectedSeedIndex >= 0 && selectedSeedIndex < seedCounts.Length
                        ? selectedSeedIndex : 0;
                    if (seedCounts[idx] > 0)
                        actions.Add(new FarmPlotActionOption(FarmPlotAction.PlantSelected, "LMB", $"Plant {seedNames[idx]} ({seedCounts[idx]})"));
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.Compost, "M", "Compost Soil"));
                    break;

                case PlotStatus.Planted:
                case PlotStatus.Growing:
                    actions.Add(new FarmPlotActionOption(
                        FarmPlotAction.Water,
                        "LMB",
                        crop.Phase == PlotPhase.Wilting ? "Water Urgently" : "Pour Water"));
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.Compost, "M", "Compost Soil"));
                    break;

                case PlotStatus.Harvestable:
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.Harvest, "E", "Harvest"));
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.Water, "LMB", "Pour Water"));
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.Compost, "M", "Compost Soil"));
                    break;

                case PlotStatus.Depleted:
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.Compost, "M", "Restore Soil"));
                    break;

                case PlotStatus.Dead:
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.ClearDead, "X", "Clear Dead Plant"));
                    break;
            }

            return new FarmPlotActionPrompt(
                $"{PrettyPlotName(soil.PlotId)} [{soil.Status}]",
                BuildLegacyDetail(soil, crop),
                actions);
        }

        private static FarmPlotActionPrompt BuildTutorialPrompt(SoilState soil, CropPlotState crop, int tomatoSeeds)
        {
            var actions = new List<FarmPlotActionOption>(1);
            string detail;

            if (soil.Status == PlotStatus.Untilled)
            {
                actions.Add(new FarmPlotActionOption(FarmPlotAction.Till, "LMB", "Till Soil"));
                detail = "Till the ground to prepare the soil for planting.";
            }
            else if (soil.Status == PlotStatus.Empty && crop.Phase == PlotPhase.Empty)
            {
                var label = tomatoSeeds > 0 ? $"Plant Tomato Seed ({tomatoSeeds})" : "Need Tomato Seed";
                if (tomatoSeeds > 0)
                    actions.Add(new FarmPlotActionOption(FarmPlotAction.PrimaryInteract, "E", label));

                detail = "Plant the tomato seed to start the staged task ladder.";
            }
            else
            {
                actions.Add(new FarmPlotActionOption(
                    FarmPlotAction.PrimaryInteract,
                    "E",
                    string.IsNullOrEmpty(crop.CurrentActionLabel) ? "Do Task" : crop.CurrentActionLabel));

                detail = $"{crop.CurrentVisualAssetId}  {crop.CurrentTaskPrompt}";
            }

            return new FarmPlotActionPrompt(
                $"{PrettyPlotName(soil.PlotId)} [Tomato Task]",
                detail,
                actions);
        }

        private static string PrettyPlotName(string plotId)
        {
            if (string.IsNullOrWhiteSpace(plotId))
                return "Plot";

            return plotId.Replace("CropPlot_", "Plot ");
        }

        private static string BuildLegacyDetail(SoilState soil, CropPlotState crop)
        {
            string cropLabel = soil.CurrentCropId == null
                ? soil.Type + " soil"
                : soil.CurrentCropId.Replace("seed_", string.Empty);

            string wiltWarning = crop.Phase == PlotPhase.Wilting ? "  WILTING" :
                                 crop.Phase == PlotPhase.Dead ? "  DEAD" : string.Empty;

            return $"{cropLabel}   Moisture {soil.Moisture:P0}   Nutrients {soil.Nutrients:P0}   Growth {crop.GrowthPercent:P0}{wiltWarning}";
        }
    }
}
