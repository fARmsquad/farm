# Feature Spec: Watering System — L2-004

## Summary
A pure C# watering system that increases soil moisture on a crop plot and
manages watering can capacity. Sprint 1 delivers Core/ logic only
(WaterService + WateringCanState). MonoBehaviour visuals — water particles
and soil material wetness lerp — are wired in Sprint 2.

In the editor the action is triggered by clicking a plot with the water tool
selected.

## User Story
As a farmer, I want to water my crops so they grow faster and healthier.

## Acceptance Criteria
- [ ] Watering increases soil moisture by pour rate * delta time
- [ ] Moisture clamps at 1.0
- [ ] Watering can capacity decreases per pour
- [ ] Empty can → cannot water, returns failure with reason "can empty"
- [ ] Refill at well → capacity restored to max
- [ ] Watering a plot with a growing crop → growth accelerates (via moisture input to growth calc)

## VR Interaction Model
Not applicable for Sprint 1 — this is a Core/ calculation system. VR pour
gesture and visual feedback are separate features that consume WaterResult.

## Edge Cases
- Water already-saturated soil (moisture = 1.0) → no effect, but can still loses water
- Water empty plot (no crop) → moisture increases (pre-watering before planting)
- Can capacity hits exactly 0 during pour → stops pouring, partial water applied
- Negative pour rate → ArgumentException

## Performance Impact
- Single operation per water action — not called per frame
- Moisture is a float on SoilState — no allocation
- Negligible Quest impact

## Dependencies
- L2-001 — soil provides moisture state (ISoilManager)
- L2-002 — growth calculator uses moisture as input

## Out of Scope
- Water particle VFX (Sprint 2 MonoBehaviour)
- Soil material wetness lerp (Sprint 2)
- VR pour gesture (Layer X1)
- Sprinkler/irrigation upgrades (L4-002)

---

## Technical Plan

### Architecture
- **Namespace**: FarmSimVR.Core.Farming
- **Core/ classes**: WaterService, WateringCanState, WaterResult
- **Interfaces/**: IWaterService
- **MonoBehaviours/**: (none for this feature — Sprint 1)
- **Dependencies injected**: ISoilManager (to apply moisture)

### Data Model
```csharp
public class WateringCanState
{
    public float Capacity { get; private set; }     // current water amount
    public float MaxCapacity { get; }                // e.g., 10.0
    public float PourRate { get; }                   // units per second
    public bool IsEmpty => Capacity <= 0f;

    public float Pour(float deltaTime);  // returns amount poured, decreases capacity
    public void Refill();                // capacity = MaxCapacity
}

public record WaterResult(
    bool Success,
    string FailReason,  // null if success; "can empty"
    float AmountApplied
);

public interface IWaterService
{
    WaterResult Water(string plotId, float deltaTime);
    void Refill();
    WateringCanState CanState { get; }
}
```

### Data Flow
```
Water(plotId, dt) -> WateringCanState.Pour(dt) -> amount
  -> ISoilManager.Water(plotId, amount) -> moisture updated
  -> return WaterResult(success, amountApplied)

Refill() -> WateringCanState.Refill() -> capacity = max
```

### Files
- `Assets/_Project/Scripts/Core/Farming/WateringCanState.cs`
- `Assets/_Project/Scripts/Core/Farming/WaterResult.cs`
- `Assets/_Project/Scripts/Core/Farming/WaterService.cs`
- `Assets/_Project/Scripts/Interfaces/IWaterService.cs`

### Testing Strategy
- **EditMode**: All logic in Core/ — full test coverage with NUnit
- **PlayMode**: Not needed (no Unity interaction in Sprint 1)
- **Mock**: ISoilManager mocked to isolate WaterService
- **Test file**: `Assets/Tests/EditMode/WateringServiceTests.cs`

---

## Task Breakdown

### Task 1: WateringCanState
- **Type**: Core class
- **File**: Assets/_Project/Scripts/Core/Farming/WateringCanState.cs
- **Tests**: Constructor (maxCapacity, pourRate), Pour(), Refill(), IsEmpty
- **Depends on**: nothing
- **Acceptance**: Pour returns amount poured and decrements capacity; Refill restores to max; IsEmpty true when Capacity <= 0

### Task 2: WaterResult Record
- **Type**: Core data type
- **File**: Assets/_Project/Scripts/Core/Farming/WaterResult.cs
- **Tests**: (tested through WaterService)
- **Depends on**: nothing
- **Acceptance**: WaterResult record with Success, FailReason, AmountApplied defined

### Task 3: IWaterService Interface
- **Type**: Interface
- **File**: Assets/_Project/Scripts/Interfaces/IWaterService.cs
- **Tests**: (tested through implementation)
- **Depends on**: Task 2
- **Acceptance**: IWaterService with Water(), Refill(), CanState defined

### Task 4: WaterService — Constructor
- **Type**: Core class
- **File**: Assets/_Project/Scripts/Core/Farming/WaterService.cs
- **Tests**: Construction with ISoilManager + WateringCanState injection
- **Depends on**: Task 1, Task 2, Task 3
- **Acceptance**: WaterService accepts injected dependencies without throwing

### Task 5: Water — Success Path
- **Type**: Core logic
- **File**: Assets/_Project/Scripts/Core/Farming/WaterService.cs
- **Tests**: Can has water, plot exists → pour applied to soil, WaterResult.Success = true
- **Depends on**: Task 4
- **Acceptance**: AmountApplied = pourRate * deltaTime when capacity is sufficient

### Task 6: Water — Empty Can Failure
- **Type**: Core logic
- **File**: Assets/_Project/Scripts/Core/Farming/WaterService.cs
- **Tests**: Can is empty → WaterResult.Success = false, FailReason = "can empty", AmountApplied = 0
- **Depends on**: Task 4
- **Acceptance**: ISoilManager.Water never called when can is empty

### Task 7: Partial Pour — Can Runs Dry Mid-Pour
- **Type**: Core logic / edge case
- **File**: Assets/_Project/Scripts/Core/Farming/WaterService.cs
- **Tests**: Remaining capacity < pourRate * deltaTime → apply only what was available, capacity hits 0
- **Depends on**: Task 5
- **Acceptance**: AmountApplied equals remaining capacity; WaterResult.Success = true with partial amount

### Task 8: Refill
- **Type**: Core logic
- **File**: Assets/_Project/Scripts/Core/Farming/WaterService.cs
- **Tests**: After emptying can, Refill() → Capacity = MaxCapacity; subsequent Water() succeeds
- **Depends on**: Task 4
- **Acceptance**: WateringCanState.Capacity equals MaxCapacity after Refill()

### Task 9: Integration — Soil Moisture Increases After Watering
- **Type**: Integration test
- **File**: Assets/Tests/EditMode/WateringServiceTests.cs
- **Tests**: Full call chain — Water() → ISoilManager receives correct moisture delta
- **Depends on**: Task 5, Task 7
- **Acceptance**: Mock ISoilManager records the exact amount passed; matches AmountApplied in WaterResult
