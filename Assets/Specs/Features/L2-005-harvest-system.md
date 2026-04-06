# Feature Spec: Harvest System — L2-005

## Summary
Remove a mature crop from the plot and receive items in return. Yield is calculated
based on crop type, soil quality, and growth conditions maintained over the growth period.
In-editor interaction: click a harvestable plot. Sprint 1 = Core/ logic only
(HarvestService + HarvestCalculator). VFX and VR gesture support are deferred to later sprints.

## User Story
As a farmer, I want to harvest my mature crops so that I receive items I can sell or use.

## Acceptance Criteria
- [ ] Harvest mature crop -> success, yield items returned
- [ ] Harvest immature crop -> failure, "crop not ready"
- [ ] Harvest empty plot -> failure, "nothing to harvest"
- [ ] Yield calculation uses crop data base quantity
- [ ] Quality bonus from soil nutrients (rich = 1.5x)
- [ ] Quality bonus from perfect watering history
- [ ] Plot status -> Empty after harvest
- [ ] Soil nutrients decrease after harvest
- [ ] Harvested items added to inventory
- [ ] XP awarded on harvest (for progression)

## VR Interaction Model
Sprint 1: Editor-only — click a harvestable plot to trigger harvest.
Sprint 2: VFX burst particles on successful harvest.
Layer X1: VR grab-and-pull gesture (out of scope for this spec).

## Edge Cases
- Harvest with full inventory -> items overflow, harvest still succeeds (items not lost, returned in result)
- Harvest on depleted soil -> reduced yield (0.5x)
- Multiple harvests without composting -> progressive nutrient loss visible
- XP amount scales with crop rarity/difficulty

## Performance Impact
- Single operation per harvest event — not per frame
- Yield calculation is pure math, no allocations
- Negligible Quest impact

## Dependencies
- L2-002 (growth system — need mature crops)
- L2-006 (inventory — store harvested items)

## Out of Scope
- Harvest VFX burst particles (Sprint 2)
- VR grab-and-pull gesture (Layer X1)
- Selling harvested items (L4-001 Economy)
- Crop quality grades (future enhancement)

---

## Technical Plan

### Data Model
```csharp
public record HarvestRequest(
    string PlotId
);

public record HarvestYield(
    string ItemId,       // what item is produced
    int Quantity,        // how many
    float QualityScore,  // 0-1 (affects sell price later)
    int XpAwarded
);

public record HarvestResult(
    bool Success,
    string FailReason,    // null if success; "crop not ready", "nothing to harvest"
    HarvestYield Yield,   // null if failure
    int InventoryOverflow // items that didn't fit
);

public interface IHarvestService
{
    HarvestResult Harvest(HarvestRequest request);
}

public interface IHarvestCalculator
{
    HarvestYield CalculateYield(
        string cropType,
        float soilNutrients,    // 0-1
        float averageMoisture,  // 0-1 (average over growth period)
        float growthQuality     // derived from how well conditions were maintained
    );
}
```

### Architecture
- **Namespace**: FarmSimVR.Core.Farming
- **Core/ classes**: HarvestService, HarvestCalculator, HarvestRequest, HarvestResult, HarvestYield
- **Interfaces/**: IHarvestService, IHarvestCalculator
- **Dependencies injected**: ISoilManager, IInventorySystem, IHarvestCalculator
- **Files**:
  - Assets/_Project/Scripts/Core/Farming/HarvestRequest.cs
  - Assets/_Project/Scripts/Core/Farming/HarvestResult.cs
  - Assets/_Project/Scripts/Core/Farming/HarvestYield.cs
  - Assets/_Project/Scripts/Core/Farming/IHarvestService.cs
  - Assets/_Project/Scripts/Core/Farming/IHarvestCalculator.cs
  - Assets/_Project/Scripts/Core/Farming/HarvestService.cs
  - Assets/_Project/Scripts/Core/Farming/HarvestCalculator.cs

### Data Flow
```
Harvest(plotId)
  -> ISoilManager.GetPlot(plotId) -> validate Harvestable
  -> IHarvestCalculator.CalculateYield(crop, soil, moisture, quality) -> HarvestYield
  -> IInventorySystem.AddItem(yield.ItemId, yield.Quantity) -> handle overflow
  -> ISoilManager.Harvest(plotId) -> deplete nutrients, reset to Empty
  -> return HarvestResult(success, yield, overflow)
```

### Testing Strategy
- **EditMode**: All logic in Core/ — mock ISoilManager, IInventorySystem
- **PlayMode**: Not needed (no Unity interaction)
- **Test file**: Assets/Tests/EditMode/HarvestServiceTests.cs

---

## Task Breakdown

### Task 1: Data Types
- **Type**: Core records
- **Files**: HarvestRequest.cs, HarvestYield.cs, HarvestResult.cs
- **Tests**: Construction, field validation
- **Depends on**: nothing
- **Acceptance**: HarvestRequest, HarvestYield, HarvestResult records defined

### Task 2: Interfaces
- **Type**: Interfaces
- **Files**: IHarvestService.cs, IHarvestCalculator.cs
- **Tests**: (tested through implementation)
- **Depends on**: Task 1
- **Acceptance**: IHarvestService.Harvest and IHarvestCalculator.CalculateYield defined

### Task 3: HarvestCalculator
- **Type**: Core class
- **File**: Assets/_Project/Scripts/Core/Farming/HarvestCalculator.cs
- **Tests**: Base yield from crop data, nutrient multiplier (rich=1.5x, depleted=0.5x), moisture multiplier, XP scaling with crop rarity
- **Depends on**: Task 1, Task 2
- **Acceptance**: All yield multiplier tests pass

### Task 4: HarvestService — constructor
- **Type**: Core class (scaffold)
- **File**: Assets/_Project/Scripts/Core/Farming/HarvestService.cs
- **Tests**: Instantiates with injected ISoilManager, IInventorySystem, IHarvestCalculator
- **Depends on**: Task 2
- **Acceptance**: Constructor wires dependencies without throwing

### Task 5: Harvest success path
- **Type**: Core class (behaviour)
- **File**: Assets/_Project/Scripts/Core/Farming/HarvestService.cs
- **Tests**: Mature crop -> HarvestResult.Success=true, yield populated, inventory AddItem called, plot reset to Empty
- **Depends on**: Task 3, Task 4
- **Acceptance**: Full success path acceptance criteria met

### Task 6: Harvest failure — immature crop
- **Type**: Core class (behaviour)
- **File**: Assets/_Project/Scripts/Core/Farming/HarvestService.cs
- **Tests**: Immature crop -> Success=false, FailReason="crop not ready", Yield=null
- **Depends on**: Task 4
- **Acceptance**: Immature-crop acceptance criterion met

### Task 7: Harvest failure — empty plot
- **Type**: Core class (behaviour)
- **File**: Assets/_Project/Scripts/Core/Farming/HarvestService.cs
- **Tests**: Empty plot -> Success=false, FailReason="nothing to harvest", Yield=null
- **Depends on**: Task 4
- **Acceptance**: Empty-plot acceptance criterion met

### Task 8: Inventory overflow handling
- **Type**: Core class (behaviour)
- **File**: Assets/_Project/Scripts/Core/Farming/HarvestService.cs
- **Tests**: Full inventory -> Success=true, InventoryOverflow > 0, items not lost (returned in result)
- **Depends on**: Task 5
- **Acceptance**: Overflow edge case met

### Task 9: Soil nutrient depletion
- **Type**: Core class (behaviour)
- **File**: Assets/_Project/Scripts/Core/Farming/HarvestService.cs
- **Tests**: After harvest ISoilManager.Harvest called, nutrients reduced; multiple harvests show progressive loss
- **Depends on**: Task 5
- **Acceptance**: Nutrient depletion acceptance criterion met

### Task 10: XP award
- **Type**: Core class (behaviour)
- **Files**: HarvestCalculator.cs, HarvestService.cs
- **Tests**: XpAwarded > 0 in HarvestYield; scales with crop rarity/difficulty data
- **Depends on**: Task 3, Task 5
- **Acceptance**: XP acceptance criterion met
