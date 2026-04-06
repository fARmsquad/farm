# Feature Spec: Inventory System — L2-006

## Summary
A pure C# inventory system that stores and manages items: seeds, harvested crops,
and tools. Pure data system — visual display is separate. No Unity dependencies
(Core/ only). First system built in Sprint 1 because it has zero dependencies.

## User Story
As a farmer, I want to store seeds, harvested crops, and tools in my inventory
so that I can manage my farming resources.

## Acceptance Criteria
- [ ] Add item to empty inventory → success
- [ ] Add stackable items → quantity increases
- [ ] Add beyond max stack → overflow to next slot
- [ ] Add to full inventory → failure, items not lost (returned as overflow)
- [ ] Remove items → quantity decreases
- [ ] Remove more than available → failure
- [ ] HasItem check → true/false correctly
- [ ] GetCount across multiple slots of same type
- [ ] Different item types don't stack together
- [ ] Inventory serializable for save system

## VR Interaction Model
Not applicable — pure Core/ data system. VR inventory display will be a separate
MonoBehaviour feature.

## Edge Cases
- Adding 0 quantity → no-op, success
- Removing from empty slot → failure
- Inventory with all slots full of different items → overflow returns all items
- Stack size of 1 (tools) → each tool occupies one slot
- Item not in database → ArgumentException

## Performance Impact
- Pure data manipulation, zero allocations per operation (pre-allocated slots)
- No Unity dependency
- Negligible Quest impact

## Dependencies
- None — pure data system, first thing built

## Out of Scope
- Visual inventory display (MonoBehaviour)
- VR hand interaction with inventory
- Crafting/combining items
- Equipment/wearing system

---

## Technical Plan

### Architecture
- **Core/ classes**: InventorySystem, InventorySlot, ItemDatabase, ItemData, AddResult, RemoveResult
- **Interfaces/**: IInventorySystem, IItemDatabase
- **MonoBehaviours/**: (none — pure data system)
- **Namespace**: FarmSimVR.Core.Inventory

### Data Model
```csharp
// Enums
public enum ItemCategory { Seed, Crop, Tool, Material, Cosmetic }

// Data definitions
public record ItemData(
    string ItemId,
    string DisplayName,
    ItemCategory Category,
    int MaxStack,
    int SellValue,
    string Description
);

// Inventory slot
public class InventorySlot
{
    public string ItemId { get; private set; }
    public int Quantity { get; private set; }
    public int MaxStack { get; private set; }
    public bool IsEmpty => string.IsNullOrEmpty(ItemId);
    public bool IsFull => Quantity >= MaxStack;
    public int RemainingCapacity => MaxStack - Quantity;
}

// Result types
public record AddResult(bool Success, int Overflow);
public record RemoveResult(bool Success, string FailReason);

// Main system
public interface IInventorySystem
{
    AddResult AddItem(string itemId, int quantity);
    RemoveResult RemoveItem(string itemId, int quantity);
    bool HasItem(string itemId, int quantity = 1);
    int GetCount(string itemId);
    IReadOnlyList<InventorySlot> Slots { get; }
}

// Item database
public interface IItemDatabase
{
    ItemData GetItem(string itemId);
    bool Exists(string itemId);
}
```

### Data Flow
```
AddItem(itemId, qty) -> IItemDatabase.GetItem(itemId) -> find/create slot -> update quantity -> AddResult
RemoveItem(itemId, qty) -> find slots with item -> decrease quantity -> RemoveResult
```

### Testing Strategy
- **EditMode**: All logic in Core/ — full NUnit coverage
- **PlayMode**: Not needed (no Unity interaction)
- **Test file**: Assets/Tests/EditMode/InventorySystemTests.cs

### Files
- Assets/_Project/Scripts/Core/Inventory/ItemData.cs
- Assets/_Project/Scripts/Core/Inventory/ItemCategory.cs
- Assets/_Project/Scripts/Core/Inventory/InventorySlot.cs
- Assets/_Project/Scripts/Core/Inventory/InventorySystem.cs
- Assets/_Project/Scripts/Core/Inventory/AddResult.cs
- Assets/_Project/Scripts/Core/Inventory/RemoveResult.cs
- Assets/_Project/Scripts/Core/Inventory/IInventorySystem.cs
- Assets/_Project/Scripts/Core/Inventory/IItemDatabase.cs
- Assets/_Project/Scripts/Core/Inventory/ItemDatabase.cs

---

## Task Breakdown

### Task 1: Item Data Types
- **Type**: Core classes (enums + records)
- **Files**: ItemCategory.cs, ItemData.cs, AddResult.cs, RemoveResult.cs
- **Tests**: Construction, value validation
- **Depends on**: nothing
- **Acceptance**: ItemCategory enum, ItemData record, AddResult, RemoveResult defined

### Task 2: Interfaces
- **Type**: Interfaces
- **Files**: IInventorySystem.cs, IItemDatabase.cs
- **Tests**: (tested through implementation)
- **Depends on**: Task 1
- **Acceptance**: IInventorySystem and IItemDatabase defined with correct signatures

### Task 3: ItemDatabase
- **Type**: Core class
- **File**: ItemDatabase.cs
- **Tests**: Lookup by id, Exists check, unknown item throws ArgumentException
- **Depends on**: Task 1, Task 2
- **Acceptance**: GetItem returns correct ItemData; unknown id throws ArgumentException

### Task 4: InventorySlot
- **Type**: Core class
- **File**: InventorySlot.cs
- **Tests**: IsEmpty, IsFull, RemainingCapacity, internal Add/Remove methods
- **Depends on**: Task 1
- **Acceptance**: All slot properties and mutations correct per acceptance criteria

### Task 5: InventorySystem.AddItem
- **Type**: Core class (partial)
- **File**: InventorySystem.cs
- **Tests**: Empty inventory, stacking, overflow to next slot, full inventory
- **Depends on**: Task 2, Task 3, Task 4
- **Acceptance**: AddItem returns correct AddResult; overflow is non-zero when slots are full

### Task 6: InventorySystem.RemoveItem
- **Type**: Core class (partial)
- **File**: InventorySystem.cs
- **Tests**: Decrease quantity, remove across multiple slots, fail if insufficient
- **Depends on**: Task 5
- **Acceptance**: RemoveItem returns correct RemoveResult; quantity never goes negative

### Task 7: InventorySystem.HasItem + GetCount
- **Type**: Core class (partial)
- **File**: InventorySystem.cs
- **Tests**: HasItem true/false, GetCount across multiple slots of same type
- **Depends on**: Task 5
- **Acceptance**: HasItem and GetCount query across all slots correctly

### Task 8: Serialization
- **Type**: Core class enhancement
- **File**: InventorySystem.cs
- **Tests**: Serialize inventory state, deserialize and verify slot contents match
- **Depends on**: Task 5, Task 6, Task 7
- **Acceptance**: Inventory state can be serialized and deserialized for save system
