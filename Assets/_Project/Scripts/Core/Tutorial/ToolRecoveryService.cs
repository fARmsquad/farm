using System.Collections.Generic;

namespace FarmSimVR.Core.Tutorial
{
    public sealed class ToolRecoveryService
    {
        private static readonly TutorialToolId[] RequiredTools =
        {
            TutorialToolId.WateringCan,
            TutorialToolId.SeedPouch,
            TutorialToolId.HarvestBasket,
        };

        private readonly HashSet<TutorialToolId> _recovered = new();

        public int RecoveredCount => _recovered.Count;
        public int RemainingCount => RequiredTools.Length - _recovered.Count;
        public bool IsComplete => RemainingCount == 0;

        public bool Recover(TutorialToolId tool)
        {
            if (tool == TutorialToolId.None)
                return false;

            return _recovered.Add(tool);
        }

        public bool IsRecovered(TutorialToolId tool)
        {
            return _recovered.Contains(tool);
        }
    }
}
