using System;

namespace FarmSimVR.Core.Farming
{
    public enum PlotPhase
    {
        Empty,
        Planted,
        Growing,
        Ready
    }

    public class CropPlotState
    {
        public PlotPhase Phase { get; private set; } = PlotPhase.Empty;
        public float CurrentGrowth { get; private set; }
        public CropData CropData { get; private set; }
        public float GrowthPercent => CropData.MaxGrowth > 0f
            ? CurrentGrowth / CropData.MaxGrowth
            : 0f;

        private readonly ICropGrowthCalculator _calculator;
        private int _lastMilestone;

        public event Action<int> OnMilestone; // fires at 25, 50, 75, 100

        public CropPlotState(ICropGrowthCalculator calculator)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public void Plant(CropData cropData)
        {
            if (Phase != PlotPhase.Empty)
                throw new InvalidOperationException($"Cannot plant in phase {Phase}");

            CropData = cropData;
            CurrentGrowth = 0f;
            _lastMilestone = 0;
            Phase = PlotPhase.Planted;
        }

        public void Tick(GrowthConditions conditions, float deltaTime)
        {
            if (Phase == PlotPhase.Empty || Phase == PlotPhase.Ready)
                return;

            if (Phase == PlotPhase.Planted)
                Phase = PlotPhase.Growing;

            var result = _calculator.CalculateGrowth(CropData, conditions, CurrentGrowth, deltaTime);
            CurrentGrowth += result.GrowthAmount;

            CheckMilestones();

            if (result.IsFullyGrown)
                Phase = PlotPhase.Ready;
        }

        public void Harvest()
        {
            if (Phase != PlotPhase.Ready)
                throw new InvalidOperationException($"Cannot harvest in phase {Phase}");

            CurrentGrowth = 0f;
            _lastMilestone = 0;
            Phase = PlotPhase.Empty;
        }

        private void CheckMilestones()
        {
            int percent = (int)(GrowthPercent * 100);
            int[] milestones = { 25, 50, 75, 100 };
            foreach (int m in milestones)
            {
                if (percent >= m && _lastMilestone < m)
                {
                    _lastMilestone = m;
                    OnMilestone?.Invoke(m);
                }
            }
        }
    }
}
