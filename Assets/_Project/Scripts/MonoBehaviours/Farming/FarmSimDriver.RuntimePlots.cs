using FarmSimVR.Core.Farming;
using FarmSimVR.MonoBehaviours;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public sealed partial class FarmSimDriver
    {
        public bool IsInitialized => _soil != null && _sim != null;
        public bool HasRegisteredPlot(string plotName) => TryGetPlotIndex(plotName, out _);

        public bool RegisterRuntimePlot(GameObject plotGo)
        {
            if (!IsInitialized || plotGo == null || string.IsNullOrWhiteSpace(plotGo.name))
                return false;

            if (HasRegisteredPlot(plotGo.name))
                return false;

            _plots.Add(plotGo);
            _soil.AddPlot(plotGo.name, defaultSoilType);

            var state = new CropPlotState(new CropGrowthCalculator());
            _sim.AddPlot(state);

            var controller = plotGo.GetComponent<CropPlotController>()
                ?? plotGo.AddComponent<CropPlotController>();
            controller.Initialize(state, _soil.GetPlot(plotGo.name));
            EnsureCropVisual(plotGo);
            return true;
        }
    }
}
