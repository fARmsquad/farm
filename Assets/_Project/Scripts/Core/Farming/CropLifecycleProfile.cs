using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.Farming
{
    public enum CropTaskId
    {
        None,
        PatSoil,
        ClearWeeds,
        TieVine,
        PinchSuckers,
        BrushBlossoms,
        StripLowerLeaves,
        CheckRipeness,
        TwistHarvest,
    }

    public enum FarmStageMinigameType
    {
        None,
        StopZone,
        RapidTap,
        Sequence,
        Alternate,
    }

    public enum FarmStageMinigameInput
    {
        None,
        Confirm,
        Left,
        Right,
        Up,
        Down,
    }

    public sealed class FarmStageMinigameDefinition
    {
        public static FarmStageMinigameDefinition None { get; } =
            new FarmStageMinigameDefinition(
                FarmStageMinigameType.None,
                string.Empty,
                string.Empty,
                0f,
                0f,
                0f,
                0,
                0f,
                Array.Empty<FarmStageMinigameInput>(),
                FarmStageMinigameInput.None);

        public FarmStageMinigameType Type { get; }
        public string Title { get; }
        public string ThemeText { get; }
        public float Speed { get; }
        public float SuccessMin { get; }
        public float SuccessMax { get; }
        public int RequiredCount { get; }
        public float DecayPerSecond { get; }
        public IReadOnlyList<FarmStageMinigameInput> InputSequence { get; }
        public FarmStageMinigameInput FirstAlternateInput { get; }

        private FarmStageMinigameDefinition(
            FarmStageMinigameType type,
            string title,
            string themeText,
            float speed,
            float successMin,
            float successMax,
            int requiredCount,
            float decayPerSecond,
            IReadOnlyList<FarmStageMinigameInput> sequence,
            FarmStageMinigameInput firstAlternateInput)
        {
            Type = type;
            Title = title ?? string.Empty;
            ThemeText = themeText ?? string.Empty;
            Speed = speed;
            SuccessMin = successMin;
            SuccessMax = successMax;
            RequiredCount = requiredCount;
            DecayPerSecond = decayPerSecond;
            InputSequence = sequence ?? Array.Empty<FarmStageMinigameInput>();
            FirstAlternateInput = firstAlternateInput;
        }

        public static FarmStageMinigameDefinition StopZone(
            string title,
            string themeText,
            float speed = 0.75f,
            float successMin = 0.42f,
            float successMax = 0.58f)
        {
            return new FarmStageMinigameDefinition(
                FarmStageMinigameType.StopZone,
                title,
                themeText,
                speed,
                successMin,
                successMax,
                0,
                0f,
                Array.Empty<FarmStageMinigameInput>(),
                FarmStageMinigameInput.None);
        }

        public static FarmStageMinigameDefinition RapidTap(
            string title,
            string themeText,
            int requiredCount,
            float decayPerSecond = 0.12f)
        {
            return new FarmStageMinigameDefinition(
                FarmStageMinigameType.RapidTap,
                title,
                themeText,
                0f,
                0f,
                0f,
                requiredCount,
                decayPerSecond,
                Array.Empty<FarmStageMinigameInput>(),
                FarmStageMinigameInput.None);
        }

        public static FarmStageMinigameDefinition Sequence(
            string title,
            string themeText,
            params FarmStageMinigameInput[] sequence)
        {
            return new FarmStageMinigameDefinition(
                FarmStageMinigameType.Sequence,
                title,
                themeText,
                0f,
                0f,
                0f,
                sequence?.Length ?? 0,
                0f,
                sequence ?? Array.Empty<FarmStageMinigameInput>(),
                FarmStageMinigameInput.None);
        }

        public static FarmStageMinigameDefinition Alternate(
            string title,
            string themeText,
            int requiredCount,
            FarmStageMinigameInput firstInput = FarmStageMinigameInput.Left,
            float decayPerSecond = 0.05f)
        {
            return new FarmStageMinigameDefinition(
                FarmStageMinigameType.Alternate,
                title,
                themeText,
                0f,
                0f,
                0f,
                requiredCount,
                decayPerSecond,
                Array.Empty<FarmStageMinigameInput>(),
                firstInput);
        }
    }

    public sealed class CropLifecycleStep
    {
        public CropLifecycleStep(
            PlotPhase phase,
            string visualAssetId,
            string actionLabel,
            string promptText,
            CropTaskId requiredTaskId,
            FarmStageMinigameDefinition minigame,
            bool completesHarvest = false)
        {
            Phase = phase;
            VisualAssetId = visualAssetId ?? string.Empty;
            ActionLabel = actionLabel ?? string.Empty;
            PromptText = promptText ?? string.Empty;
            RequiredTaskId = requiredTaskId;
            Minigame = minigame ?? FarmStageMinigameDefinition.None;
            CompletesHarvest = completesHarvest;
        }

        public PlotPhase Phase { get; }
        public string VisualAssetId { get; }
        public string ActionLabel { get; }
        public string PromptText { get; }
        public CropTaskId RequiredTaskId { get; }
        public FarmStageMinigameDefinition Minigame { get; }
        public bool CompletesHarvest { get; }
    }

    public sealed class CropLifecycleProfile
    {
        public CropLifecycleProfile(string cropId, IReadOnlyList<CropLifecycleStep> steps)
        {
            CropId = cropId ?? string.Empty;
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
            if (Steps.Count == 0)
                throw new ArgumentException("A lifecycle profile needs at least one step.", nameof(steps));
        }

        public string CropId { get; }
        public IReadOnlyList<CropLifecycleStep> Steps { get; }
    }

    public static class CropLifecycleProfiles
    {
        public static CropLifecycleProfile TomatoTutorial { get; } = BuildTomatoTutorial();

        private static CropLifecycleProfile BuildTomatoTutorial()
        {
            return new CropLifecycleProfile(
                "seed_tomato",
                new[]
                {
                    new CropLifecycleStep(
                        PlotPhase.Planted,
                        "TomatoSeed_01",
                        "Pat Soil",
                        "Pat the soil closed so the seed bed is packed and even.",
                        CropTaskId.PatSoil,
                        FarmStageMinigameDefinition.StopZone(
                            "Pat Soil Closed",
                            "Press Space while the tamp lands inside the green zone.")),
                    new CropLifecycleStep(
                        PlotPhase.Sprout,
                        "Tomato_01a",
                        "Clear Weeds",
                        "Pull the weeds crowding the new sprout.",
                        CropTaskId.ClearWeeds,
                        FarmStageMinigameDefinition.RapidTap(
                            "Clear Weeds",
                            "Tap Space to yank the weeds before they grow back.",
                            requiredCount: 5)),
                    new CropLifecycleStep(
                        PlotPhase.YoungPlant,
                        "Tomato_02a",
                        "Tie Vine",
                        "Loop the tie in the right order to guide the plant onto the plank.",
                        CropTaskId.TieVine,
                        FarmStageMinigameDefinition.Sequence(
                            "Tie Vine To Plank",
                            "Press the arrow sequence to wrap the tie cleanly.",
                            FarmStageMinigameInput.Left,
                            FarmStageMinigameInput.Up,
                            FarmStageMinigameInput.Right)),
                    new CropLifecycleStep(
                        PlotPhase.Budding,
                        "Tomato_03a",
                        "Pinch Suckers",
                        "Pinch the side shoots in order so the main vine keeps climbing.",
                        CropTaskId.PinchSuckers,
                        FarmStageMinigameDefinition.Sequence(
                            "Pinch Suckers",
                            "Hit the arrow sequence to remove the side shoots cleanly.",
                            FarmStageMinigameInput.Up,
                            FarmStageMinigameInput.Down,
                            FarmStageMinigameInput.Up,
                            FarmStageMinigameInput.Right)),
                    new CropLifecycleStep(
                        PlotPhase.Budding,
                        "Tomato_04a",
                        "Brush Blossoms",
                        "Brush pollen across the blossoms with steady alternating strokes.",
                        CropTaskId.BrushBlossoms,
                        FarmStageMinigameDefinition.Alternate(
                            "Brush Blossoms",
                            "Alternate Left and Right to move pollen through the flowers.",
                            requiredCount: 8)),
                    new CropLifecycleStep(
                        PlotPhase.Fruiting,
                        "Tomato_05",
                        "Strip Lower Leaves",
                        "Strip the old lower leaves so the fruit cluster can breathe.",
                        CropTaskId.StripLowerLeaves,
                        FarmStageMinigameDefinition.RapidTap(
                            "Strip Lower Leaves",
                            "Tap Space to strip the old leaves before the progress slips back.",
                            requiredCount: 6)),
                    new CropLifecycleStep(
                        PlotPhase.Fruiting,
                        "Tomato_06",
                        "Check Ripeness",
                        "Check the fruit blush and catch the sweet spot before harvest.",
                        CropTaskId.CheckRipeness,
                        FarmStageMinigameDefinition.StopZone(
                            "Check Ripeness",
                            "Press Space when the ripeness marker sits in the ripe band.")),
                    new CropLifecycleStep(
                        PlotPhase.Ready,
                        "Tomato_07",
                        "Twist Harvest",
                        "Twist the ripe fruit free without tearing the vine.",
                        CropTaskId.TwistHarvest,
                        FarmStageMinigameDefinition.Alternate(
                            "Twist Harvest",
                            "Alternate Left and Right to twist the tomato loose.",
                            requiredCount: 10),
                        completesHarvest: true),
                });
        }
    }
}
