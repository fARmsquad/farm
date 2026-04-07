# Feature Spec: Game State Manager — F-005

## Summary
A central game session manager coordinates boot, play, pause, and placeholder
game-over flow for the unified farm scene. It provides a deterministic
initialization sequence, a single source of truth for session state, and a
lightweight event system for cross-system notifications.

## User Story
As a player, I want the game to boot into a reliable playing state and pause
cleanly, so the farm and hunting systems always start in a predictable way and
I can safely suspend the simulation.

## Acceptance Criteria
- [x] `GameManager` singleton tracks the current game session state
- [x] Supported states are `Loading`, `Playing`, `Paused`, and `GameOver`
- [x] Boot flow starts in `Loading` and transitions to `Playing` only after all managed systems initialize
- [x] Pause and resume are available through explicit `PauseGame`, `ResumeGame`, and `TogglePause` APIs
- [x] Pausing stops simulation by applying a paused session state and zeroing `Time.timeScale`
- [x] Initialization order is deterministic and inspector-configurable
- [x] Player spawn/root can be positioned during initialization when spawn references are assigned
- [x] A lightweight event bus publishes state changes and system initialization events
- [x] Core game-state logic remains Unity-free and fully EditMode testable

## VR Interaction Model
The manager does not own direct VR interaction, but it is the authority that
future pause menus, controller shortcuts, and system UIs will call into.
For editor validation, a keyboard pause toggle is allowed through the Input
System without changing the Quest-first runtime architecture.

## Edge Cases
- Attempting to pause while loading throws an invalid transition error
- Attempting to resume while not paused throws an invalid transition error
- Duplicate `GameManager` instances destroy themselves and preserve the first singleton
- Managed-system list entries that do not implement the required contract are ignored with warnings
- Missing player spawn references are a no-op, not a boot failure
- Re-initializing an already initialized managed system is idempotent

## Performance Impact
- Event bus publishes only on state transitions and initialization steps
- No per-frame allocations in the Core state machine
- Pause flow relies on existing Unity timing primitives (`Time.timeScale`) plus simple boolean gates
- Negligible Quest impact

## Dependencies
- F-004 Scene Unification

## Out of Scope
- Save/load persistence
- Pause menu UI
- Scene transitions beyond the current farm scene
- Quest controller input bindings for pause

---

## Technical Plan

## Research Reference
- Unity lifecycle ordering is deterministic only at the `Awake`-before-`Start` phase level; explicit execution order is still required for a central bootstrapper.
- `Time.timeScale = 0` pauses physics and `WaitForSeconds`, but explicit paused state is still recommended for custom-tick systems.
- Unity’s own open-project architecture favors event-driven managers without requiring a third-party event framework.
- See `.ai/memory/research-notes.md` entry: `Research: F-005 Game State Manager`.

### Architecture
- **Core/ classes**:
  - `GameSessionState`
  - `GameStateChangedEvent`
  - `GameSystemInitializedEvent`
  - `GameEventBus`
  - `GameStateMachine`
- **MonoBehaviours/**:
  - `GameManager`
  - `IGameManagedSystem`
  - updates to `SimulationManager`
  - updates to `HuntingManager`
- **Files**:
  - `Assets/_Project/Scripts/Core/GameState/GameSessionState.cs`
  - `Assets/_Project/Scripts/Core/GameState/GameEventBus.cs`
  - `Assets/_Project/Scripts/Core/GameState/GameStateMachine.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/IGameManagedSystem.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/GameManager.cs`
  - updated `Assets/_Project/Scripts/MonoBehaviours/SimulationManager.cs`
  - updated `Assets/_Project/Scripts/MonoBehaviours/Hunting/HuntingManager.cs`

### Data Flow
```text
GameManager.Awake
  -> create GameEventBus
  -> create GameStateMachine (starts in Loading)
  -> cache ordered managed systems

GameManager.Start
  -> optionally position player root at spawn point
  -> initialize managed systems in serialized order
  -> publish GameSystemInitializedEvent per system
  -> transition Loading -> Playing
  -> publish GameStateChangedEvent

PauseGame / ResumeGame / TogglePause
  -> validate transition in GameStateMachine
  -> set Time.timeScale
  -> forward paused flag to managed systems
  -> publish GameStateChangedEvent
```

### Testing Strategy
- **EditMode**:
  - `GameStateMachineTests` cover valid/invalid transitions and published events
  - `GameEventBusTests` cover subscribe/unsubscribe/multi-subscriber behavior
- **PlayMode**:
  - deferred; existing repo baseline is not currently green due an unrelated compilation blocker

---

## Task Breakdown

### Task 1: Core session state types
- **Type**: Core classes
- **Files**: `GameSessionState.cs`, `GameStateMachine.cs`
- **Tests**: initial state, valid transitions, invalid transitions
- **Depends on**: nothing
- **Acceptance**: state machine starts in `Loading` and enforces legal transitions

### Task 2: Lightweight game event bus
- **Type**: Core class
- **File**: `GameEventBus.cs`
- **Tests**: subscribe, unsubscribe, publish order
- **Depends on**: Task 1
- **Acceptance**: systems can publish and subscribe to typed events without Unity dependencies

### Task 3: GameManager orchestration
- **Type**: MonoBehaviour
- **Files**: `IGameManagedSystem.cs`, `GameManager.cs`
- **Tests**: covered indirectly through Core state/event tests and code inspection
- **Depends on**: Task 1, Task 2
- **Acceptance**: singleton bootstraps session state, initializes systems in explicit order, exposes pause APIs

### Task 4: System integration
- **Type**: MonoBehaviour updates
- **Files**: `SimulationManager.cs`, `HuntingManager.cs`
- **Tests**: compile validation, manual boot/pause playtest
- **Depends on**: Task 3
- **Acceptance**: existing managers can be initialized and paused by `GameManager` without double-initialization

### Task 5: Verification & handoff
- **Type**: QA / documentation
- **Files**: playtest guide + final handoff notes
- **Tests**: Unity CLI compile/test attempt, manual playtest instructions
- **Depends on**: Task 4
- **Acceptance**: implementation limitations and validation status are documented clearly
