# Feature Spec: Crop Growth System — L2-002

## Summary
Planted crops grow through stages over time. Growth rate affected by moisture,
soil quality, weather, temperature. Pure math in Core/. This is the expanded
system spec — the foundational calculator (crop-growth-calculator.md) is
already implemented. This spec covers the full stage-based growth system,
CropData definitions, and the GrowthTicker integration layer.

## User Story
As a farmer, I want my crops to visually grow through stages over time,
with growth speed affected by how well I care for them, so that farming
feels responsive and rewarding.

## Acceptance Criteria
- [x] Default conditions -> base growth rate (implemented)
- [x] Rain weather -> 2x growth (implemented)
- [x] Temperature outside 10-35C -> 0.5x growth (implemented)
- [x] Temperature in preferred range -> 1.0x (implemented)
- [x] Soil quality multiplier: poor=0.5, normal=1.0, rich=1.5 (implemented)
- [x] Moisture at 0 -> growth stops (implemented)
- [x] Growth cannot exceed maxGrowth (implemented)
- [x] Negative deltaTime -> ArgumentException (implemented)
- [ ] CropData defines growth stages with threshold percentages
- [ ] Stage transitions fire at correct thresholds
- [ ] Multiple conditions compound multiplicatively
- [ ] GrowthResult includes readable modifier list (for debug UI)
- [ ] Moisture at 1 -> optimal (1x multiplier)
- [ ] Each crop has preferred temperature range (not just global 10-35)
- [ ] Water sensitivity per crop (how much moisture affects growth)
- [ ] Soil preference per crop (bonus when planted in matching soil)
- [ ] HarvestYield data defined per crop

## VR Interaction Model
Not applicable — Core/ calculation system. Visual stage transitions
(CropVisualController) are Sprint 2 MonoBehaviours.

## Edge Cases
- Zero deltaTime -> zero growth, no stage change
- Already at maxGrowth -> no additional growth, isFullyGrown = true
- Multiple conditions stack multiplicatively
- Crop with 1 stage (e.g., instant harvest herb) -> immediately harvestable
- Stage thresholds at exact boundaries (e.g., 0.5 threshold at exactly 50% growth)
- Preferred temperature range that spans 0 degrees (edge: min > max is invalid)

## Performance Impact
- Pure calculation, no allocations, no Unity dependency
- Called once per crop per growth tick (not per frame)
- Negligible Quest impact

## Dependencies
- L2-001 (soil provides moisture + quality inputs)
- Existing: CropGrowthCalculator (already implemented, needs extension)

## Out of Scope
- Visual stage model swapping (Sprint 2 CropVisualController)
- GrowthTicker MonoBehaviour (Sprint 2)
- Season-based crop availability (L3-003)

---

## Data Model (Extended)

```csharp
// Already implemented (in CropTypes.cs):
// WeatherType, SoilQuality, CropData (basic), GrowthConditions, GrowthResult

// Extended CropData (replaces basic version):
public class CropData
{
    public string CropId { get; }
    public string DisplayName { get; }
    public float BaseGrowthRate { get; }        // units per real-second
    public float MaxGrowth { get; }             // total to reach maturity
    public GrowthStage[] Stages { get; }        // visual stage definitions
    public (float Min, float Max) PreferredTemperatureRange { get; }
    public float WaterSensitivity { get; }      // how much moisture affects growth
    public SoilType SoilPreference { get; }     // bonus when matching
    public HarvestYieldData HarvestYield { get; }
}

public class GrowthStage
{
    public float ThresholdPercent { get; }  // 0-1, when this stage activates
    public string StageName { get; }        // "Seedling", "Sprout", "Mature", etc.
}

public class HarvestYieldData
{
    public string ProducedItemId { get; }
    public int BaseQuantity { get; }
    public float QualityMultiplierFromSoil { get; }  // nutrients -> quality
}

// Extended GrowthResult:
public class GrowthResult
{
    public float GrowthAmount { get; }
    public float NewTotalGrowth { get; }
    public int CurrentStageIndex { get; }
    public bool IsFullyGrown { get; }
    public string[] GrowthModifiers { get; }  // e.g., "rain bonus +2x", "cold penalty -0.5x"
}
```

---

## Technical Plan

### Architecture
- **Core/ classes**: CropData (extended), GrowthStage, HarvestYieldData, GrowthResult (extended)
- **Existing**: CropGrowthCalculator, ICropGrowthCalculator — extend, don't replace
- **Namespace**: FarmSimVR.Core.Farming

### Files
- Assets/_Project/Scripts/Core/Farming/CropTypes.cs (extend existing)
- Assets/_Project/Scripts/Core/Farming/CropGrowthCalculator.cs (extend existing)
- Assets/_Project/Scripts/Core/Farming/GrowthStage.cs (new)
- Assets/_Project/Scripts/Core/Farming/HarvestYieldData.cs (new)

### Data Flow
```
CropData + GrowthConditions + deltaTime
  -> CropGrowthCalculator.CalculateGrowth()
  -> apply moisture multiplier (0 at dry, 1 at saturated, scaled by WaterSensitivity)
  -> apply weather multiplier (rain=2x, storm=1.5x, etc.)
  -> apply temperature multiplier (1.0 in preferred range, 0.5 outside)
  -> apply soil match bonus (1.2x if planted in preferred soil type)
  -> clamp total growth at maxGrowth
  -> determine current stage index from thresholds
  -> return GrowthResult with modifier log
```

### Testing Strategy
- **EditMode**: Extend existing CropGrowthCalculatorTests
- **New tests**: Stage transitions, preferred temperature ranges, water sensitivity, soil preference, modifier logging
- **Test file**: Assets/Tests/EditMode/CropGrowthCalculatorTests.cs (extend existing)

---

## Task Breakdown

### Task 1: GrowthStage Data Type
- **File**: Assets/_Project/Scripts/Core/Farming/GrowthStage.cs
- **Tests**: Construction, threshold validation (0-1 range)
- **Depends on**: nothing

### Task 2: HarvestYieldData
- **File**: Assets/_Project/Scripts/Core/Farming/HarvestYieldData.cs
- **Tests**: Construction, base quantity > 0
- **Depends on**: nothing

### Task 3: Extend CropData
- **File**: Assets/_Project/Scripts/Core/Farming/CropTypes.cs
- **Tests**: Extended constructor, stage array validation, preferred temp range
- **Depends on**: Task 1, Task 2

### Task 4: Stage Index Calculation
- **Add to**: CropGrowthCalculator
- **Tests**: Correct stage at each threshold boundary, single-stage crop
- **Depends on**: Task 3

### Task 5: Preferred Temperature Range
- **Add to**: CropGrowthCalculator
- **Tests**: In-range=1.0, out-of-range=0.5, per-crop ranges
- **Depends on**: Task 3

### Task 6: Water Sensitivity
- **Add to**: CropGrowthCalculator
- **Tests**: High sensitivity crop penalized more by low moisture
- **Depends on**: Task 3

### Task 7: Soil Preference Bonus
- **Add to**: CropGrowthCalculator
- **Tests**: Matching soil=1.2x, non-matching=1.0x
- **Depends on**: Task 3

### Task 8: Growth Modifier Logging
- **Add to**: GrowthResult
- **Tests**: Modifier list contains human-readable descriptions of all active modifiers
- **Depends on**: Tasks 4-7

### Note
Tasks 1-3 are new data types. Tasks 4-8 extend the existing calculator.
The existing 7 acceptance criteria are already passing — these tasks add
the remaining criteria without breaking existing behavior.
