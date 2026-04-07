# Feature Spec: Scene Unification — F-004

## Summary
Merge the greybox farm layout and the hunting prototype into one playable
`FarmMain` scene. The unified scene must support the existing crop lifecycle,
the existing hunting loop, and the shared `GameManager` bootstrap so the player
can plant/harvest crops, catch wild animals, and deposit them at the barn in a
single session.

## User Story
As a player, I want one playable farm scene where farming and hunting coexist,
so I can move through the full vertical-slice loop without swapping scenes.

## Acceptance Criteria
- [ ] `Assets/_Project/Scenes/FarmMain.unity` is the single playable gameplay scene
- [ ] The scene contains the hunting player controller and third-person camera
- [ ] Existing greybox crop plots in `FarmMain` are wired into `SimulationManager`
- [ ] Hunting systems (`WildAnimalSpawner`, `BarnDropOff`, `AnimalPen`, `HuntingManager`) are present in the same scene
- [ ] Barn drop-off sits at the greybox barn area
- [ ] Animal pen sits near the greybox barn area
- [ ] HUD surfaces both farming and hunting state
- [ ] `SimulationManager` and `HuntingManager` are initialized through one `GameManager`
- [ ] Build settings point at `FarmMain` instead of the tutorial `SampleScene`

## VR Interaction Model
This slice remains desktop-first for validation. The player uses keyboard/mouse
movement and click interaction in the editor, but the scene structure must stay
compatible with later XR input replacement by keeping farming and hunting boot
logic centralized instead of scene-specific.

## Dependencies
- F-001 Farm Layout (Greybox)
- F-003 Player Controller
- F-005 Game State Manager
- F-011 Crop Plot State Machine
- F-020 through F-022 Hunting loop runtime

## Out of Scope
- New art assets
- Additive multi-scene loading
- Save/load persistence
- XR rig integration

---

## Technical Plan

### Architecture
- **Editor/**:
  - Add a dedicated scene unifier/builder that opens `FarmMain`, wires all
    gameplay objects, and saves the result.
- **MonoBehaviours/**:
  - Reuse `GameManager`, `SimulationManager`, `HuntingManager`, and the hunting
    player/camera stack.
  - Update crop visuals so soil plots remain visible while crop growth renders
    through child visuals.
  - Extend the HUD so it can present both farming and hunting status.
- **Scene**:
  - `Assets/_Project/Scenes/FarmMain.unity`
- **Project Settings**:
  - `ProjectSettings/EditorBuildSettings.asset`

### Data Flow
```text
FarmMain scene
  -> GameManager positions player at SpawnPoint
  -> GameManager initializes SimulationManager
  -> GameManager initializes HuntingManager
  -> SimulationManager binds CropPlotControllers to greybox plots
  -> HuntingManager binds spawner, barn, pen, and HUD
  -> HUD reads both simulations for on-screen status
```

### Testing Strategy
- **EditMode**:
  - Add an editor-side scene-builder test that creates a temporary greybox scene,
    runs the unifier, and asserts the unified runtime objects exist.
  - Assert each `CropPlot_*` receives a `CropPlotController` plus a dedicated
    `CropVisual` child with `CropVisualUpdater`.
- **Manual / scene verification**:
  - Execute the builder against `FarmMain`
  - Open `FarmMain`
  - Walk the player around the greybox farm, plant/harvest crops, catch animals,
    and deposit them at the barn

## Research Reference
- Unity editor-scene APIs remain `EditorSceneManager.OpenScene(...)` and
  `EditorSceneManager.SaveScene(...)` for deterministic editor tooling.
- Existing repo patterns already cover scene construction (`FarmSceneBuilder`)
  and post-build runtime wiring (`HuntingSceneSetup`), so this story should
  extend those patterns instead of introducing additive scene-loading.

## Task Breakdown

### Task 1: Red test for the unified-scene builder
- **Files**:
  - `Assets/Tests/EditMode/FarmSimVR.Tests.EditMode.asmdef`
  - `Assets/Tests/EditMode/SceneUnificationBuilderTests.cs`
- **Acceptance**:
  - Tests define the required unified-scene object graph

### Task 2: Editor builder implementation
- **Files**:
  - `Assets/_Project/Editor/SceneUnificationBuilder.cs`
- **Acceptance**:
  - Builder can configure a greybox scene and save `FarmMain`

### Task 3: Runtime visual and HUD integration
- **Files**:
  - `Assets/_Project/Scripts/MonoBehaviours/CropVisualUpdater.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Hunting/HuntingHUD.cs`
- **Acceptance**:
  - Crop visuals no longer hide the soil plot itself
  - HUD reports farming and hunting state together

### Task 4: Apply to FarmMain
- **Files**:
  - `Assets/_Project/Scenes/FarmMain.unity`
  - `ProjectSettings/EditorBuildSettings.asset`
- **Acceptance**:
  - `FarmMain` becomes the unified gameplay scene
