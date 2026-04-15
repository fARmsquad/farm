using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours
{
    public class SimulationManager : MonoBehaviour
    {
        [Header("Crop Settings")]
        [SerializeField] private float baseGrowthRate = 10f;
        [SerializeField] private float maxGrowth = 100f;

        private FarmSimulation _simulation;
        public FarmSimulation Simulation => _simulation;

        private void Awake()
        {
            _simulation = new FarmSimulation();
        }

        private void Start()
        {
            // Find all plot controllers and register them
            var plots = FindObjectsByType<CropPlotController>(FindObjectsSortMode.None);
            var calculator = new CropGrowthCalculator();

            foreach (var plotCtrl in plots)
            {
                var state = new CropPlotState(calculator);
                plotCtrl.Initialize(state);
                _simulation.AddPlot(state);
            }

            // Auto-plant tomatoes
            var tomatoData = new CropData(baseGrowthRate, maxGrowth);
            _simulation.PlantAll(tomatoData);

            Debug.Log($"[FarmSim] Planted {plots.Length} plots. Growth rate={baseGrowthRate}, target={maxGrowth}");
        }

        private void Update()
        {
            _simulation?.Tick(Time.deltaTime);
        }
    }
}
