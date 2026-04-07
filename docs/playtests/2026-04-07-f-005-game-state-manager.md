# F-005 Playtest Guide — Game State Manager

## Current Branch Reality
- `GameManager` exists as a new bootstrapper MonoBehaviour, but no unified scene on this branch currently contains both `SimulationManager` and `HuntingManager`.
- `FarmMain.unity` contains a `SpawnPoint` marker, but it does not currently reference either manager script.
- `HuntingTest.unity` contains `HuntingManager`.
- `Assets/_Recovery/0.unity` contains `SimulationManager`.

## What To Verify In Editor
1. Open the scene you want to validate.
2. Create a new empty GameObject named `GameManager`.
3. Add the `GameManager` component.
4. Populate `Managed Systems` in the desired deterministic order.
5. Optionally assign:
   - `Player Root` to the player rig/root transform
   - `Player Spawn Point` to the existing `SpawnPoint` transform
6. Enter Play Mode.

## Expected Behavior
- On play, the session starts in `Loading` and transitions to `Playing` after the assigned systems initialize.
- Pressing `Escape` toggles pause in editor.
- While paused:
  - `Time.timeScale` should be `0`
  - `SimulationManager` should stop ticking crop growth/input
  - `HuntingManager` should disable its pause-sensitive components
- Pressing `Escape` again resumes play and restores time progression.

## Split-Scene Validation

### HuntingTest
- Assign only `HuntingManager` in `Managed Systems`.
- Expected:
  - `HuntingManager` initializes once
  - `Escape` pauses/resumes the hunting loop cleanly

### Recovery Scene
- Assign only `SimulationManager` in `Managed Systems`.
- Expected:
  - Crop simulation initializes once
  - `Escape` pauses/resumes crop ticking and plot input

## Blockers
- There is not yet a single committed scene on this branch where both gameplay managers coexist, so the full `Loading -> Playing -> Paused` flow for the unified farm session cannot be playtested end-to-end without additional scene assembly work.
- Local automated verification is also blocked by the current editor/CLI state:
  - batchmode cannot open while the project is already open in Unity
  - the Unity MCP bridge is disconnected
  - the existing compile baseline previously reported `GameStateLogger.cs` as a blocking compiler issue before tests ran

## Handoff Checklist
- [x] F-005 spec added
- [x] research brief added
- [x] EditMode tests for Core state machine/event bus added
- [x] Core game-state implementation added
- [x] `SimulationManager` updated for explicit init/pause control
- [x] `HuntingManager` updated for explicit init/pause control
- [x] AI wiring audit run after `.ai/` edits
- [ ] Unified scene wiring completed in editor
- [ ] End-to-end pause/resume playtest completed in a scene containing both managers
- [ ] Clean automated Unity test pass captured on this branch
