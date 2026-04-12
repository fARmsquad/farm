# Feature Spec: Horse Training Title Slice — TUT-003

## Summary
Add a greybox horse-training gameplay slice that can be launched directly from
`TitleScreen` without disturbing the existing linear tutorial route. The slice
uses the provided storyboard as its structure: setup menu, guided walk with
treats, jumping test, slalom test with balance pressure, then a clear success
or failure finish.

## User Story
As a developer iterating on onboarding minigames, I want a horse-training slice
I can launch from the title screen so I can tune a self-contained tutorial beat
without reworking the intro-to-farm sequence.

## Acceptance Criteria
- [ ] The title screen exposes a launch target for a horse-training slice using
      the shared scene catalog, not a one-off hardcoded button path.
- [ ] The existing `Start Game` flow still targets the first scene in the
      current linear tutorial chain.
- [ ] The horse slice is included in the title-screen/build-settings launchable
      scene list but is not inserted into `TutorialSceneCatalog.SceneOrder`.
- [ ] A `HorseTrainingGame` scene asset exists in `Assets/_Project/Scenes/`.
- [ ] The slice opens on a readable setup state with a begin prompt and a clear
      "Training Grounds" identity.
- [ ] The gameplay loop follows the storyboarded beats:
      setup -> guide horse with treats -> jumping test -> slalom test ->
      success or failure.
- [ ] Success and failure are driven by a pure C# training service so the
      progression can be covered by EditMode tests.
- [ ] The scene controller is a thin MonoBehaviour wrapper that builds greybox
      visuals, reads player overlap/input, and mirrors service state in UI.

## Storyboard Translation

### 1. Training Grounds Setup
- Title-card style prompt identifies the slice as `Horse Training Grounds`.
- Player can begin from an explicit start prompt instead of spawning directly
  into challenge state.

### 2. Guided Walk With Treats
- The player leads the horse proxy along the first section by collecting or
  walking through treat markers in sequence.
- The horse advances visibly as the sequence completes.

### 3. Jumping Test
- The player reaches a short jump lane with clear rails.
- Missing the jump interaction fails the slice immediately.

### 4. Slalom Test
- The player threads alternating gates in order.
- Slalom mistakes drain a balance meter until the slice fails.

### 5. Success
- Completion shows full progress bars and a celebratory end state.

### 6. Failure
- Failure explains whether the miss came from jumping or slalom.
- Retry messaging is immediate and readable.

## Technical Plan

### Core
Create a pure C# service under `Assets/_Project/Scripts/Core/Tutorial/`:
- `HorseTrainingStep`
- `HorseTrainingFailureReason`
- `HorseTrainingSnapshot`
- `HorseTrainingService`

Responsibilities:
- Track current phase
- Count completed treat markers, jump rails, and slalom gates
- Track slalom balance
- Resolve `Success` vs `Failure`

### MonoBehaviour
Add `TutorialHorseTrainingSceneController` under
`Assets/_Project/Scripts/MonoBehaviours/Tutorial/`.

Responsibilities:
- Build the greybox paddock/course
- Ensure the existing first-person rig is present
- Translate trigger/input events into service calls
- Render objective, meter, success, and failure UI

### Scene & Title Wiring
- Add horse-scene metadata to `SceneWorkCatalog`
- Keep `TutorialSceneCatalog` unchanged for the mandatory linear flow
- Extend the title-screen launchable scene list and build-scene ordering to
  include `HorseTrainingGame`
- Add an editor builder that can regenerate `HorseTrainingGame.unity`

## Testing Strategy
- EditMode: service progression, success path, jump failure, slalom failure,
  and title-screen/catalog wiring
- EditMode scene smoke: controller builds the training-ground root and player
  rig in an empty scene
- Manual/Desktop: launch from title screen, complete once, fail jump once, fail
  slalom once

## Out Of Scope
- Real horse animation, mounted riding, or reins
- XR hand interactions
- Narrative integration into the mandatory tutorial chain
- Final art, audio, or polished horse assets beyond greybox readability
