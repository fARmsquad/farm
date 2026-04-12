using System;

namespace FarmSimVR.Core.Tutorial
{
    public sealed class HorseTrainingService
    {
        private readonly int _requiredTreatMarkers;
        private readonly int _requiredJumpRails;
        private readonly int _requiredSlalomGates;
        private readonly float _maxBalance;
        private readonly float _slalomMissPenalty;

        private HorseTrainingStep _step;
        private HorseTrainingFailureReason _failureReason;
        private int _treatMarkersCleared;
        private int _jumpRailsCleared;
        private int _slalomGatesCleared;
        private float _balance;

        public HorseTrainingService(
            int requiredTreatMarkers = 3,
            int requiredJumpRails = 2,
            int requiredSlalomGates = 4,
            float maxBalance = 1f,
            float slalomMissPenalty = 0.34f)
        {
            _requiredTreatMarkers = Math.Max(1, requiredTreatMarkers);
            _requiredJumpRails = Math.Max(1, requiredJumpRails);
            _requiredSlalomGates = Math.Max(1, requiredSlalomGates);
            _maxBalance = Math.Max(0.1f, maxBalance);
            _slalomMissPenalty = Math.Max(0.01f, slalomMissPenalty);

            _step = HorseTrainingStep.Setup;
            _balance = _maxBalance;
        }

        public HorseTrainingSnapshot Snapshot => new(
            _step,
            _failureReason,
            _treatMarkersCleared,
            _requiredTreatMarkers,
            _jumpRailsCleared,
            _requiredJumpRails,
            _slalomGatesCleared,
            _requiredSlalomGates,
            _balance,
            _maxBalance);

        public HorseTrainingSnapshot Begin()
        {
            if (_step != HorseTrainingStep.Setup)
                return Snapshot;

            _step = HorseTrainingStep.GuidedWalk;
            return Snapshot;
        }

        public HorseTrainingSnapshot RecordTreatMarkerReached()
        {
            if (_step != HorseTrainingStep.GuidedWalk)
                return Snapshot;

            _treatMarkersCleared = Math.Min(_requiredTreatMarkers, _treatMarkersCleared + 1);
            if (_treatMarkersCleared >= _requiredTreatMarkers)
                _step = HorseTrainingStep.Jumping;

            return Snapshot;
        }

        public HorseTrainingSnapshot RecordJumpRailCleared()
        {
            if (_step != HorseTrainingStep.Jumping)
                return Snapshot;

            _jumpRailsCleared = Math.Min(_requiredJumpRails, _jumpRailsCleared + 1);
            if (_jumpRailsCleared >= _requiredJumpRails)
            {
                _step = HorseTrainingStep.Slalom;
                _balance = _maxBalance;
            }

            return Snapshot;
        }

        public HorseTrainingSnapshot RecordJumpMissed()
        {
            if (_step != HorseTrainingStep.Jumping)
                return Snapshot;

            _step = HorseTrainingStep.Failure;
            _failureReason = HorseTrainingFailureReason.FailedJump;
            return Snapshot;
        }

        public HorseTrainingSnapshot RecordSlalomGateCleared()
        {
            if (_step != HorseTrainingStep.Slalom)
                return Snapshot;

            _slalomGatesCleared = Math.Min(_requiredSlalomGates, _slalomGatesCleared + 1);
            if (_slalomGatesCleared >= _requiredSlalomGates)
                _step = HorseTrainingStep.Success;

            return Snapshot;
        }

        public HorseTrainingSnapshot RecordSlalomMiss()
        {
            if (_step != HorseTrainingStep.Slalom)
                return Snapshot;

            _balance = Math.Max(0f, _balance - _slalomMissPenalty);
            if (_balance <= 0f)
            {
                _step = HorseTrainingStep.Failure;
                _failureReason = HorseTrainingFailureReason.FailedSlalom;
            }

            return Snapshot;
        }
    }
}
