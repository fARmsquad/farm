using System.Collections.Generic;

namespace FarmSimVR.Core.Farming
{
    public class FarmSimulation
    {
        private readonly List<CropPlotState> _plots = new();
        public IReadOnlyList<CropPlotState> Plots => _plots;

        public GrowthConditions Conditions { get; set; } =
            new GrowthConditions(WeatherType.Rain, 25f, SoilQuality.Rich);

        public void AddPlot(CropPlotState plot)
        {
            _plots.Add(plot);
        }

        public void Tick(float deltaTime)
        {
            foreach (var plot in _plots)
                plot.Tick(Conditions, deltaTime);
        }

        public void PlantAll(CropData cropData)
        {
            foreach (var plot in _plots)
            {
                if (plot.Phase == PlotPhase.Empty)
                    plot.Plant(cropData);
            }
        }
    }
}
