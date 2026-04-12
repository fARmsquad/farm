using System;

namespace FarmSimVR.Core.Farming
{
    public sealed class FarmStageMinigameSession
    {
        private float _direction = 1f;
        private int _sequenceIndex;
        private FarmStageMinigameInput _nextAlternateInput;

        public FarmStageMinigameSession(FarmStageMinigameDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            MarkerPosition = 0f;
            _nextAlternateInput = definition.FirstAlternateInput;
            StatusText = definition.ThemeText;
        }

        public FarmStageMinigameDefinition Definition { get; }
        public float MarkerPosition { get; private set; }
        public float Progress { get; private set; }
        public int SequenceIndex => _sequenceIndex;
        public FarmStageMinigameInput NextAlternateInput => _nextAlternateInput;
        public bool IsComplete { get; private set; }
        public string StatusText { get; private set; }

        public void Tick(float deltaTime)
        {
            if (IsComplete || deltaTime <= 0f)
                return;

            switch (Definition.Type)
            {
                case FarmStageMinigameType.StopZone:
                    AdvanceMarker(deltaTime);
                    break;
                case FarmStageMinigameType.RapidTap:
                case FarmStageMinigameType.Alternate:
                    Progress = Clamp01(Progress - Definition.DecayPerSecond * deltaTime);
                    break;
            }
        }

        public void HandleInput(FarmStageMinigameInput input)
        {
            if (IsComplete || input == FarmStageMinigameInput.None)
                return;

            switch (Definition.Type)
            {
                case FarmStageMinigameType.StopZone:
                    HandleStopZoneInput(input);
                    break;
                case FarmStageMinigameType.RapidTap:
                    HandleRapidTapInput(input);
                    break;
                case FarmStageMinigameType.Sequence:
                    HandleSequenceInput(input);
                    break;
                case FarmStageMinigameType.Alternate:
                    HandleAlternateInput(input);
                    break;
            }
        }

        private void AdvanceMarker(float deltaTime)
        {
            MarkerPosition += Definition.Speed * _direction * deltaTime;
            if (MarkerPosition >= 1f)
            {
                MarkerPosition = 1f;
                _direction = -1f;
            }
            else if (MarkerPosition <= 0f)
            {
                MarkerPosition = 0f;
                _direction = 1f;
            }
        }

        private void HandleStopZoneInput(FarmStageMinigameInput input)
        {
            if (input != FarmStageMinigameInput.Confirm)
                return;

            if (MarkerPosition >= Definition.SuccessMin && MarkerPosition <= Definition.SuccessMax)
            {
                Progress = 1f;
                StatusText = "Perfect timing.";
                IsComplete = true;
                return;
            }

            StatusText = "Not quite. Catch the marker in the green band.";
        }

        private void HandleRapidTapInput(FarmStageMinigameInput input)
        {
            if (input != FarmStageMinigameInput.Confirm)
                return;

            Progress = Clamp01(Progress + (1f / Math.Max(1, Definition.RequiredCount)));
            StatusText = "Keep going.";
            if (Progress >= 0.999f)
            {
                Progress = 1f;
                StatusText = "Done.";
                IsComplete = true;
            }
        }

        private void HandleSequenceInput(FarmStageMinigameInput input)
        {
            if (Definition.InputSequence.Count == 0)
                return;

            if (Definition.InputSequence[_sequenceIndex] == input)
            {
                _sequenceIndex++;
                Progress = Clamp01(_sequenceIndex / (float)Definition.InputSequence.Count);
                StatusText = _sequenceIndex >= Definition.InputSequence.Count
                    ? "Clean loop."
                    : "Keep the pattern going.";
                if (_sequenceIndex >= Definition.InputSequence.Count)
                    IsComplete = true;
                return;
            }

            _sequenceIndex = 0;
            Progress = 0f;
            StatusText = "Reset. Start the pattern again.";
        }

        private void HandleAlternateInput(FarmStageMinigameInput input)
        {
            if (input != _nextAlternateInput)
            {
                StatusText = $"Alternate {FormatInput(_nextAlternateInput)} and {FormatInput(Toggle(_nextAlternateInput))}.";
                return;
            }

            Progress = Clamp01(Progress + (1f / Math.Max(1, Definition.RequiredCount)));
            _nextAlternateInput = Toggle(_nextAlternateInput);
            StatusText = Progress >= 0.999f ? "Loose enough." : $"Now hit {FormatInput(_nextAlternateInput)}.";
            if (Progress >= 0.999f)
            {
                Progress = 1f;
                IsComplete = true;
            }
        }

        private static FarmStageMinigameInput Toggle(FarmStageMinigameInput input)
        {
            return input == FarmStageMinigameInput.Left
                ? FarmStageMinigameInput.Right
                : FarmStageMinigameInput.Left;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        private static string FormatInput(FarmStageMinigameInput input)
        {
            return input switch
            {
                FarmStageMinigameInput.Left => "Left",
                FarmStageMinigameInput.Right => "Right",
                FarmStageMinigameInput.Up => "Up",
                FarmStageMinigameInput.Down => "Down",
                _ => "Confirm",
            };
        }
    }
}
