# Feature Spec: Farm Scene Demo

## Summary
A playable farm scene with a 3x3 grid of crop plots that grow in real-time.
Each plot wraps the existing CropGrowthCalculator. Growth is visualized by
scaling a cube and lerping its color from green (seedling) to red (mature).
Crops auto-plant on play and reach maturity in ~10 seconds.

## Acceptance Criteria
- [ ] 3x3 grid of crop plots visible in scene
- [ ] Crops auto-plant tomatoes on Play mode start
- [ ] Growth visually represented by cube scaling (0.1 → 1.0 Y scale)
- [ ] Color lerps from green to red as growth progresses
- [ ] Crops reach maturity in ~10 seconds (tuned baseGrowthRate)
- [ ] Console logs growth milestones (25%, 50%, 75%, 100%)
- [ ] SimulationManager ticks all plots each frame
- [ ] Ground plane visible beneath plots

## Architecture
- **CropPlotState** (Core/): Pure C# state machine — Empty/Planted/Growing/Ready
- **FarmSimulation** (Core/): Manages collection of plots, ticks growth
- **CropPlotController** (MonoBehaviours/): Thin wrapper, delegates to CropPlotState
- **CropVisualUpdater** (MonoBehaviours/): View — reads state, updates scale/color
- **SimulationManager** (MonoBehaviours/): Calls FarmSimulation.Tick in Update
- **FarmSceneBuilder** (Editor/): Menu item to assemble scene with one click

## Task Breakdown
1. CropPlotState + tests (Core/)
2. FarmSimulation + tests (Core/)
3. MonoBehaviours (CropPlotController, CropVisualUpdater, SimulationManager)
4. FarmSceneBuilder editor script
