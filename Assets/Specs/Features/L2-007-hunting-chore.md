# Feature Spec: Hunting Chore (Catch Animals) — L2-007

## Summary
Wild animals spawn at the farm perimeter and wander inward. The player chases them on foot, gets within catch range, and presses a key to grab them. Caught animals are deposited at the barn. This is the first "chore" mechanic beyond crop farming — a gentle catch-and-collect loop with no killing.

## User Story
As a farmer, I want to chase and catch wild animals that wander onto my farm so that I can collect them at my barn (and later choose to keep or sell them).

## Acceptance Criteria
- [ ] Wild animals spawn at the farm perimeter edges at a configurable interval
- [ ] Spawned animals wander inward using the existing AnimalWander behavior
- [ ] When the player gets within a detection radius, the animal enters flee mode (runs away from player)
- [ ] When the player is within catch range (closer than flee range), pressing `E` catches the animal
- [ ] Caught animal is removed from the scene and added to a CaughtAnimalTracker list
- [ ] Player can walk to the barn drop-off zone to deposit caught animals
- [ ] Deposited animals appear in a simple barn inventory (data only for now)
- [ ] A maximum number of wild animals can exist at once (configurable cap)
- [ ] All input flows through an IPlayerInput interface (keyboard now, VR later)
- [ ] AnimalWander is extended (not replaced) with flee behavior via a separate component

## VR Interaction Model (Future — Phase 2)
- **Primary input**: XR grab gesture / trigger press when hand is near animal
- **Feedback**: Haptic pulse on catch, animal squirm animation
- **Comfort**: No rapid camera movement — player moves, not camera shake
- **Extension point**: Implement `IPlayerInput` with XR input source; no Core/ changes needed

## Edge Cases
- Animal reaches farm center without being caught — it wanders back out and despawns at perimeter
- Player tries to catch while too far — nothing happens (no error, just no catch)
- Multiple animals in catch range — catch the closest one
- Max wild animals reached — spawner pauses until count drops
- Player already holding a caught animal — can still catch more (they stack in the tracker)

## Performance Impact
- Max 5 wild animals at once (configurable) — minimal draw call impact
- Flee behavior uses simple distance check, not pathfinding — O(1) per frame per animal
- Spawner uses timer, not per-frame checks
- Quest budget compliant: no new shaders, no physics raycasts in hot path

## Dependencies
- **Existing:** AnimalWander.cs (wander behavior), animal .glb models (chicken, cow, pig, sheep, horse), farm layout with barn position
- **New (self-contained):** All new systems are built in this spec, no external dependencies

## Out of Scope
- Net tool (needs tool/inventory system — Phase 2)
- Keep vs. sell choice (needs economy system — Phase 2)
- Animals spawning outside farm fence in expansion zones (Phase 2)
- Animal-specific rewards (eggs, milk — needs production system)
- Animal animations (catch animation, held animation — art dependency)
- Multiplayer catch conflicts

---

## Technical Plan

### Architecture

```
Core/Hunting/
  IPlayerInput.cs              — interface: bool CatchPressed, Vector3 Position
  ICaughtAnimalTracker.cs      — interface: Add, Remove, GetAll, Count
  CaughtAnimalTracker.cs       — in-memory list of caught animal records
  CaughtAnimalRecord.cs        — data: animal type, catch time
  HuntingConfig.cs             — ScriptableObject: spawn interval, max animals,
                                  detection radius, catch radius, flee speed

MonoBehaviours/Hunting/
  WildAnimalSpawner.cs         — spawns animals at perimeter on timer
  AnimalFleeBehavior.cs        — added alongside AnimalWander, overrides
                                  movement when player is in detection range
  CatchZone.cs                 — detects player proximity, listens for input,
                                  triggers catch
  BarnDropOff.cs               — trigger zone at barn, deposits caught animals
  KeyboardPlayerInput.cs       — IPlayerInput implementation (E key + transform)

Interfaces/
  IPlayerInput.cs              — (or in Core/, project convention TBD)
```

### Build Approach

1. **IPlayerInput interface** — define the input contract first
2. **KeyboardPlayerInput** — MonoBehaviour implementing IPlayerInput (E key)
3. **HuntingConfig** — ScriptableObject with all tuning values
4. **AnimalFleeBehavior** — component that sits alongside AnimalWander, takes over movement when player is near
5. **WildAnimalSpawner** — picks random perimeter points, instantiates animal prefabs with AnimalWander + AnimalFleeBehavior + CatchZone
6. **CatchZone** — per-animal MonoBehaviour, checks distance to player each frame, triggers catch on input
7. **CaughtAnimalTracker** — pure C# class tracking caught animals
8. **BarnDropOff** — trigger collider at barn, calls tracker to deposit

### Testing Strategy

**EditMode (Core/ pure logic):**
- CaughtAnimalTracker: add, remove, count, duplicate handling
- CaughtAnimalRecord: data integrity

**PlayMode (MonoBehaviour integration):**
- WildAnimalSpawner: spawns correct count, respects max cap, spawns at perimeter
- AnimalFleeBehavior: animal moves away from player when in detection range
- CatchZone: catch succeeds at close range, fails at far range
- BarnDropOff: depositing clears caught animal from tracker

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | Interface | Create `IPlayerInput` in Core/ or Interfaces/ | — | Interface compiles, has `CatchPressed` (bool) and `Position` (Vector3) |
| 2 | MonoBehaviour | Create `KeyboardPlayerInput` implementing IPlayerInput | 1 | E key maps to CatchPressed, Position returns transform.position |
| 3 | ScriptableObject | Create `HuntingConfig` with spawn interval, max animals, detection radius, catch radius, flee speed multiplier | — | Asset can be created in editor, all fields exposed |
| 4 | Core class | Create `CaughtAnimalRecord` (animal type enum + catch timestamp) | — | Compiles, stores data |
| 5 | Core class | Create `CaughtAnimalTracker` (add, remove, get all, count) | 4 | EditMode tests pass: add increments count, remove decrements, get returns correct list |
| 6 | MonoBehaviour | Create `AnimalFleeBehavior` — when player enters detection radius, override AnimalWander target to flee away from player | 3 | PlayMode: animal reverses direction when player approaches |
| 7 | MonoBehaviour | Create `CatchZone` — per-animal, checks distance to IPlayerInput.Position, on CatchPressed within catch radius → fires OnCaught event | 1, 3 | PlayMode: catch succeeds at 1m, fails at 5m |
| 8 | MonoBehaviour | Create `WildAnimalSpawner` — timer-based, spawns at random perimeter points, respects max cap | 3, 6, 7 | PlayMode: spawns animals, doesn't exceed max, animals have wander + flee + catch components |
| 9 | MonoBehaviour | Create `BarnDropOff` — trigger zone at barn, on enter deposits all caught animals via CaughtAnimalTracker | 5 | PlayMode: entering barn zone clears caught list, increments barn count |
| 10 | Integration | Wire up in scene: spawner GO, barn trigger, player with KeyboardPlayerInput, HuntingConfig asset | 1-9 | Play the scene: animals spawn, wander in, flee when chased, catch on E, deposit at barn |
