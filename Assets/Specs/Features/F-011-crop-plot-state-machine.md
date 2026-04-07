# Feature Spec: F-011 Crop Plot State Machine

## Summary
Finish the farming plot lifecycle so plots begin empty, can be manually planted from a lightweight inventory source, grow into a harvestable state, wither if ignored for too long, and return harvested produce back to inventory-facing data.

## User Story
As a player, I want each crop plot to move cleanly through empty, planted, growing, ready, and withered states so planting and harvesting feel intentional instead of auto-running on scene start.

## Acceptance Criteria
- [x] Farm plots start `Empty` on scene start.
- [x] Scene start no longer auto-plants tomatoes.
- [x] Player interaction can plant a tomato seed into an empty plot if inventory has seeds.
- [x] Planting consumes one seed from inventory-facing state.
- [x] Growth still transitions `Planted -> Growing -> Ready`.
- [x] A ready crop transitions to `Withered` after a configurable number of ready ticks.
- [x] Harvesting a `Ready` crop returns yield data that is added to inventory-facing state.
- [x] Harvesting a `Ready` crop resets the plot to `Empty`.
- [x] A `Withered` plot can be cleared and replanted.
- [x] Empty plots are visually empty in scene instead of rendering as tiny planted crops.

## VR Interaction Model
This slice remains desktop-driven for now. A scene-level input bridge handles click interactions in the editor, and future XR stories can replace that bridge with hand/controller input while preserving the same Core plot lifecycle.

## Performance Impact
- One central simulation manager continues to tick plots.
- No Unity references added to `Core/`.
- No per-tick allocations in the plot lifecycle or lightweight inventory path.
- Visual updates stay simple enough for the current Quest plot count.

## Dependencies
- F-010 Crop Growth Calculator

## Out of Scope
- Full generic inventory system with slots/capacity/UI
- Multiple crop selection UX
- Soil moisture/tilling rules
- Harvest VFX/audio polish

---

## Technical Plan

### Architecture
- **Core/**:
  - Extend `CropPlotState` with a `Withered` phase, ready-tick tracking, harvest yield return, and clear-withering path.
  - Extend `CropData` with the small amount of lifecycle data this story needs: seed item id, harvest item id, harvest quantity, ready-to-wither tick budget.
  - Add `CropInventory` as a minimal pure-C# count store for this story's seed-consume / crop-award flow.
- **MonoBehaviours/**:
  - Update `SimulationManager` to initialize plots without auto-planting, seed the lightweight inventory, and route click-based plant/harvest/clear interactions.
  - Update `CropVisualUpdater` so empty plots hide their crop visual and withered plots show a distinct dead state.
  - Keep `CropPlotController` as a thin wrapper around `CropPlotState`.

### Data Flow
```text
Mouse click on plot
  -> SimulationManager routes interaction
  -> CropInventory.TryRemoveItem(seed)
  -> CropPlotState.Plant(cropData)

SimulationManager.Tick
  -> FarmSimulation.Tick
  -> CropPlotState.Tick
  -> Ready plot counts ready ticks
  -> PlotPhase.Withered when timeout reached

Harvest click
  -> CropPlotState.Harvest()
  -> CropInventory.AddItem(harvest item)
  -> Plot resets to Empty
```

### Testing Strategy
- **EditMode**:
  - Extend `CropPlotStateTests` for withering, harvest yield, and replantability.
  - Add tests for the lightweight inventory class because it lives in `Core/`.
- **PlayMode / manual**:
  - Open `Assets/_Project/Scenes/FarmMain.unity`
  - Confirm plots start empty
  - Click empty plot to plant, wait for ready, harvest, wait past timeout for withering on another plot

## Research Reference
- Unity Learn: state pattern keeps lifecycle logic modular and explicit.
- Unity How-To + Manual: keep crop configuration separate from runtime state.
- Unity mobile scripting guidance: centralize/update only what you need and profile before adding complexity.
- Inventory packages exist, but the ones surfaced are broader than this story and not worth adopting yet.

---

## Task Breakdown

### Task 1: Core lifecycle extension
- **Files**:
  - `Assets/_Project/Scripts/Core/Farming/CropTypes.cs`
  - `Assets/_Project/Scripts/Core/Farming/CropPlotState.cs`
- **Acceptance**:
  - `PlotPhase` includes `Withered`
  - `CropData` contains the lifecycle metadata this story needs
  - `CropPlotState` can wither, harvest yield, clear withered crops, and replant

### Task 2: Lightweight inventory bridge
- **Files**:
  - `Assets/_Project/Scripts/Core/Farming/CropInventory.cs`
  - `Assets/Tests/EditMode/CropInventoryTests.cs`
- **Acceptance**:
  - Scene-level code can consume seeds and add harvested crops without Unity dependencies
  - All public inventory methods are covered by EditMode tests

### Task 3: Scene interaction update
- **Files**:
  - `Assets/_Project/Scripts/MonoBehaviours/SimulationManager.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/CropVisualUpdater.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/CropPlotController.cs`
- **Acceptance**:
  - No auto-plant on start
  - Manual planting / harvesting / clearing works in-scene
  - Empty and withered plots render correctly
