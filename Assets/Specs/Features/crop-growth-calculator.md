# Feature Spec: Crop Growth Calculator

## Summary
A pure C# calculator that determines how much a crop grows per time step,
based on crop data and environmental conditions. This is the foundational
farming logic — all growth visualization and VR interaction will build on top.

## User Story
As a farmer, I want my crops to grow at different rates based on weather,
temperature, and soil quality, so that farming decisions feel meaningful.

## Acceptance Criteria
- [x] Takes CropData (baseGrowthRate, maxGrowth) and GrowthConditions (weather, temperature, soilQuality)
- [x] Returns GrowthResult (growthAmount, isFullyGrown)
- [x] Rain weather doubles growth rate
- [x] Temperature outside 10-35C halves growth rate
- [x] Soil quality multiplies growth (0.5 poor, 1.0 normal, 1.5 rich)
- [x] Growth cannot exceed maxGrowth
- [x] Negative deltaTime throws ArgumentException

## VR Interaction Model
Not applicable — this is a Core/ calculation system. VR visualization
will be a separate feature that consumes GrowthResult.

## Edge Cases
- Zero deltaTime → zero growth
- Already at maxGrowth → no additional growth, isFullyGrown = true
- Multiple conditions stack multiplicatively (rain + rich soil)
- Extreme temperature + rain → rain doubles first, then temp halves

## Performance Impact
- Pure calculation, no allocations, no Unity dependency
- Called once per crop per growth tick (not per frame)
- Negligible Quest impact

## Dependencies
- None (first Core/ system)

## Out of Scope
- Visual growth stages (separate feature)
- Crop planting/harvesting mechanics (separate feature)
- Weather system (separate feature — this just consumes weather data)

---

## Technical Plan

### Architecture
- **Core/ classes**: CropGrowthCalculator, CropData, GrowthConditions, GrowthResult, WeatherType, SoilQuality
- **Interfaces/**: ICropGrowthCalculator
- **MonoBehaviours/**: (none for this feature)
- **ScriptableObjects/**: (none for this feature — CropData is a plain C# class for now)

### Data Flow
```
CropData + GrowthConditions + deltaTime → CropGrowthCalculator → GrowthResult
```

### Testing Strategy
- **EditMode**: All logic in Core/ — full test coverage with NUnit
- **PlayMode**: Not needed (no Unity interaction)

---

## Task Breakdown

### Task 1: Data Types
- **Type**: Core classes (enums + data structs)
- **File**: Assets/_Project/Scripts/Core/Farming/CropTypes.cs
- **Tests**: Construction, value validation
- **Depends on**: nothing
- **Acceptance**: WeatherType, SoilQuality, CropData, GrowthConditions, GrowthResult defined

### Task 2: Calculator Interface
- **Type**: Interface
- **File**: Assets/_Project/Scripts/Interfaces/ICropGrowthCalculator.cs
- **Tests**: (tested through implementation)
- **Depends on**: Task 1
- **Acceptance**: ICropGrowthCalculator with CalculateGrowth method

### Task 3: Calculator Implementation
- **Type**: Core class
- **File**: Assets/_Project/Scripts/Core/Farming/CropGrowthCalculator.cs
- **Tests**: All 7 acceptance criteria + edge cases
- **Depends on**: Task 1, Task 2
- **Acceptance**: All tests pass, all acceptance criteria met
