# Feature Spec: Planting System — L2-003

## Summary
A pure C# service that plants a seed into an empty soil plot. Consumes one
seed from inventory, creates a crop instance with a unique ID, and updates
the plot's soil status to Planted. Sprint 1 = Core/ logic only — no visual
seedling spawning and no VR gesture input.

## User Story
As a farmer, I want to plant seeds in my soil plots so that crops begin growing.

## Acceptance Criteria
- [ ] Plant in empty plot -> success, crop created
- [ ] Plant in occupied plot -> failure, "plot occupied"
- [ ] Plant with no seeds -> failure, "no seeds of this type"
- [ ] Planting consumes one seed from inventory
- [ ] Soil status changes to Planted
- [ ] CropInstanceId is unique per planting
- [ ] Planting returns the new crop's initial state

## VR Interaction Model
Sprint 1 — editor / debug only: click plot + select seed type from debug UI.
Sprint 2+ — on-headset gesture planting (Layer X1, out of scope here).

## Edge Cases
- Plant with exactly 1 seed remaining -> success, seed count becomes 0
- Plant crop type that doesn't exist in CropData -> ArgumentException
- Plant in Depleted soil -> allowed but with warning (growth penalty applied by soil)
- Plant out-of-season crop (future: L3-003) -> for now, always allowed

## Performance Impact
- Single operation per plant action
- No allocations beyond the new crop record creation
- Negligible Quest impact

## Dependencies
- L2-001 — soil plot state (ISoilManager provides plot status and Plant method)
- L2-006 — inventory system (IInventorySystem provides HasItem and RemoveItem)

## Out of Scope
- Visual seedling spawning (Sprint 2 MonoBehaviour)
- VR gesture planting (Layer X1)
- Season restrictions (L3-003)

---

## Data Model

```csharp
public record PlantingRequest(
    string PlotId,
    string CropType,   // references CropData
    string SeedItemId  // references inventory item
);

public record PlantingResult(
    bool Success,
    string FailReason,     // null if success; "plot occupied", "no seeds", etc.
    string CropInstanceId  // null if failure
);

public interface IPlantingService
{
    PlantingResult Plant(PlantingRequest request);
}
```

---

## Technical Plan

### Architecture
- **Namespace**: FarmSimVR.Core.Farming
- **Core/ classes**: PlantingService, PlantingRequest, PlantingResult
- **Interfaces/**: IPlantingService
- **MonoBehaviours/**: (none for this feature)
- **Dependencies injected**: ISoilManager, IInventorySystem

### Files
- Assets/_Project/Scripts/Core/Farming/PlantingRequest.cs
- Assets/_Project/Scripts/Core/Farming/PlantingResult.cs
- Assets/_Project/Scripts/Core/Farming/IPlantingService.cs
- Assets/_Project/Scripts/Core/Farming/PlantingService.cs

### Data Flow
```
PlantingRequest -> PlantingService
  -> ISoilManager.GetPlot(plotId)         -> validate Empty
  -> IInventorySystem.HasItem(seedItemId) -> validate has seeds
  -> IInventorySystem.RemoveItem(seedItemId, 1) -> consume seed
  -> ISoilManager.Plant(plotId, cropType) -> update soil status to Planted
  -> return PlantingResult(success, cropInstanceId)
```

### Testing Strategy
- **EditMode**: All logic in Core/ — mock ISoilManager and IInventorySystem
- **PlayMode**: Not needed (no Unity interaction)
- **Test file**: Assets/Tests/EditMode/PlantingServiceTests.cs

---

## Task Breakdown

### Task 1: Data Types
- **Type**: Core classes (records)
- **Files**: PlantingRequest.cs, PlantingResult.cs
- **Tests**: Construction, property access
- **Depends on**: nothing
- **Acceptance**: PlantingRequest and PlantingResult records defined in FarmSimVR.Core.Farming

### Task 2: IPlantingService Interface
- **Type**: Interface
- **File**: Assets/_Project/Scripts/Core/Farming/IPlantingService.cs
- **Tests**: (tested through implementation)
- **Depends on**: Task 1
- **Acceptance**: IPlantingService with Plant(PlantingRequest) -> PlantingResult method

### Task 3: PlantingService Constructor
- **Type**: Core class
- **File**: Assets/_Project/Scripts/Core/Farming/PlantingService.cs
- **Tests**: Constructor accepts ISoilManager + IInventorySystem
- **Depends on**: Task 1, Task 2
- **Acceptance**: PlantingService instantiates cleanly with injected dependencies

### Task 4: Plant Success Path
- **Type**: Core logic
- **File**: PlantingService.cs
- **Tests**: Empty plot + has seeds -> consume seed, plant, return Success=true with CropInstanceId
- **Depends on**: Task 3
- **Acceptance**: Soil status changes to Planted; inventory decremented by 1; result Success=true

### Task 5: Plant Failure — Occupied Plot
- **Type**: Core logic
- **File**: PlantingService.cs
- **Tests**: Non-empty plot -> return Success=false, FailReason="plot occupied", CropInstanceId=null
- **Depends on**: Task 3
- **Acceptance**: No inventory change; soil unchanged; correct failure reason returned

### Task 6: Plant Failure — No Seeds
- **Type**: Core logic
- **File**: PlantingService.cs
- **Tests**: Empty plot + zero seeds -> return Success=false, FailReason="no seeds of this type", CropInstanceId=null
- **Depends on**: Task 3
- **Acceptance**: No inventory change; soil unchanged; correct failure reason returned

### Task 7: CropInstanceId Generation
- **Type**: Core logic
- **File**: PlantingService.cs
- **Tests**: Two sequential Plant calls on different plots -> CropInstanceIds are not equal
- **Depends on**: Task 4
- **Acceptance**: Each successful planting produces a unique GUID-based CropInstanceId

### Task 8: Integration — Soil Status After Plant
- **Type**: Integration test
- **File**: Assets/Tests/EditMode/PlantingServiceTests.cs
- **Tests**: After successful Plant, ISoilManager.GetPlot(plotId).Status == SoilStatus.Planted
- **Depends on**: Task 4, Task 7
- **Acceptance**: Soil status observable as Planted via ISoilManager immediately after PlantingResult confirms success
