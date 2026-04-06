# Feature Spec: Soil System — L2-001

## Summary
Each crop plot has state and properties. The soil is the foundation that planting,
watering, and harvesting all operate on. State machine per plot tracking status,
moisture, nutrients. Pure Core/ logic (Sprint 1), MonoBehaviour visuals wired in Sprint 2.

## User Story
As a farmer, I want each plot of soil to have unique properties (moisture, nutrients, type)
so that my farming decisions matter and different plots behave differently.

## Acceptance Criteria
- [ ] SoilState initializes with correct defaults per soil type
- [ ] Moisture decays over time at configurable rate
- [ ] Moisture clamps to 0-1
- [ ] Nutrients deplete when crop is harvested
- [ ] Nutrients restore when composted
- [ ] Status transitions: Empty -> Planted (when seed added)
- [ ] Status transitions: Planted -> Growing (automatic, time-based)
- [ ] Status transitions: Growing -> Harvestable (when growth reaches max)
- [ ] Status transitions: Harvestable -> Empty (when harvested)
- [ ] Cannot plant in non-Empty plot
- [ ] Depleted soil has growth penalty

## VR Interaction Model
Not applicable for Sprint 1 — pure Core/ data system. MonoBehaviour wiring
(SoilPlotController, SoilInteractionZone) is Sprint 2.

## Edge Cases
- Moisture at exactly 0 → stops decay, no negative values
- Nutrients at 0 → soil becomes Depleted type
- Composting already-rich soil → clamps at 1.0
- Transitioning from Harvestable → must go through Empty (can't plant directly)
- Multiple harvests without composting → progressive nutrient loss

## Performance Impact
- Pure data manipulation, no Unity dependency
- One SoilState per plot (~6-30 plots max)
- Negligible Quest impact

## Dependencies
- Nothing for Core/ (Sprint 1)
- L1-001 for MonoBehaviour wiring (Sprint 2)

## Out of Scope
- Visual material swapping (Sprint 2 MonoBehaviours)
- Interaction colliders (Sprint 2)
- Soil analysis UI

---

## Technical Plan

### Data Model
```csharp
public enum SoilType { Sandy, Loam, Clay, Rich }

public enum PlotStatus { Empty, Planted, Growing, Harvestable, Depleted }

public class SoilState
{
    public string PlotId { get; }
    public SoilType Type { get; private set; }
    public float Moisture { get; private set; }  // 0-1, decays over time
    public float Nutrients { get; private set; }  // 0-1, depleted by crops
    public string CurrentCropId { get; private set; }  // null if empty
    public PlotStatus Status { get; private set; }

    // Configuration per soil type
    public float MoistureDecayRate { get; }  // per second
    public float NutrientDepletionPerHarvest { get; }
    public float GrowthMultiplier { get; }  // Sandy=0.7, Loam=1.0, Clay=0.8, Rich=1.3
}

public record SoilTypeDefaults(
    float InitialMoisture,
    float InitialNutrients,
    float MoistureDecayRate,
    float NutrientDepletionPerHarvest,
    float GrowthMultiplier
);

public interface ISoilManager
{
    SoilState GetPlot(string plotId);
    IReadOnlyList<SoilState> AllPlots { get; }
    bool Plant(string plotId, string cropId);
    void Water(string plotId, float amount);
    void Harvest(string plotId);
    void Compost(string plotId, float amount);
    void Tick(float deltaTime);  // decay moisture, advance status
}
```

### Architecture
- **Namespace**: FarmSimVR.Core.Farming
- **Core/ classes**: SoilState, SoilManager, SoilTypeDefaults, PlotStatus, SoilType
- **Interfaces/**: ISoilManager
- **MonoBehaviours/**: (none for Sprint 1)
- **Files**:
  - Assets/_Project/Scripts/Core/Farming/SoilType.cs
  - Assets/_Project/Scripts/Core/Farming/PlotStatus.cs
  - Assets/_Project/Scripts/Core/Farming/SoilState.cs
  - Assets/_Project/Scripts/Core/Farming/SoilTypeDefaults.cs
  - Assets/_Project/Scripts/Core/Farming/SoilManager.cs
  - Assets/_Project/Scripts/Core/Farming/ISoilManager.cs

### Data Flow
```
Plant(plotId, cropId) -> validate Empty status -> set CropId, status=Planted
Tick(dt) -> decay moisture, check status transitions
Water(plotId, amount) -> increase moisture (clamp 0-1)
Harvest(plotId) -> validate Harvestable -> deplete nutrients, clear crop, status=Empty
Compost(plotId, amount) -> restore nutrients (clamp 0-1)
```

### Testing Strategy
- **EditMode**: All logic in Core/ — full NUnit coverage
- **PlayMode**: Not needed (no Unity interaction)
- **Test file**: Assets/Tests/EditMode/SoilSystemTests.cs

---

## Task Breakdown

### Task 1: Enums + Data Types
- **Type**: Core classes (enums + data record)
- **Files**: SoilType.cs, PlotStatus.cs, SoilTypeDefaults.cs
- **Tests**: Construction, value validation
- **Depends on**: nothing
- **Acceptance**: SoilType, PlotStatus, SoilTypeDefaults defined with correct members

### Task 2: SoilState
- **Type**: Core class
- **File**: Assets/_Project/Scripts/Core/Farming/SoilState.cs
- **Tests**: Defaults per soil type, property accessors, clamp logic
- **Depends on**: Task 1
- **Acceptance**: Constructor sets correct defaults per SoilType, all properties accessible

### Task 3: ISoilManager Interface
- **Type**: Interface
- **File**: Assets/_Project/Scripts/Core/Farming/ISoilManager.cs
- **Tests**: (tested through implementation)
- **Depends on**: Task 1, Task 2
- **Acceptance**: ISoilManager with all method signatures defined

### Task 4: SoilManager.Plant
- **Type**: Core class (partial)
- **File**: Assets/_Project/Scripts/Core/Farming/SoilManager.cs
- **Tests**: Plants in Empty plot, returns false for non-Empty, sets CropId, transitions to Planted
- **Depends on**: Task 2, Task 3
- **Acceptance**: Plant validates Empty status, sets crop, transitions status

### Task 5: SoilManager.Water
- **Type**: Core class (partial)
- **File**: Assets/_Project/Scripts/Core/Farming/SoilManager.cs
- **Tests**: Increases moisture, clamps at 1.0, no-ops on invalid plotId
- **Depends on**: Task 4
- **Acceptance**: Moisture increases by amount, never exceeds 1.0

### Task 6: SoilManager.Harvest
- **Type**: Core class (partial)
- **File**: Assets/_Project/Scripts/Core/Farming/SoilManager.cs
- **Tests**: Validates Harvestable status, depletes nutrients, clears CropId, resets to Empty
- **Depends on**: Task 4
- **Acceptance**: Harvest only works on Harvestable plots, nutrient depletion applied

### Task 7: SoilManager.Compost
- **Type**: Core class (partial)
- **File**: Assets/_Project/Scripts/Core/Farming/SoilManager.cs
- **Tests**: Restores nutrients, clamps at 1.0, works on any plot status
- **Depends on**: Task 4
- **Acceptance**: Nutrients increase by amount, never exceed 1.0

### Task 8: SoilManager.Tick
- **Type**: Core class (partial)
- **File**: Assets/_Project/Scripts/Core/Farming/SoilManager.cs
- **Tests**: Moisture decays per deltaTime, stops at 0, Planted->Growing time-based transition
- **Depends on**: Task 4, Task 5
- **Acceptance**: Moisture decays correctly, status transitions fire at correct thresholds

### Task 9: Depleted Soil Detection
- **Type**: Core class (partial)
- **File**: Assets/_Project/Scripts/Core/Farming/SoilManager.cs
- **Tests**: Nutrients at 0 triggers Depleted status, GrowthMultiplier penalty applies
- **Depends on**: Task 6, Task 8
- **Acceptance**: Depleted status set when nutrients reach 0, growth penalty reflected in multiplier
