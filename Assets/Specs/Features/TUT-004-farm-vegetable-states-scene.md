# Feature Spec: Farm Vegetable States Scene — TUT-004

## Summary
Add a dedicated title-screen slice for reviewing vegetable growth-state prefabs
outside of `FarmMain`. The slice exists to support direct art selection for the
Scene 7 crop visuals before more lifecycle tuning continues.

## User Story
As the developer tuning the farm tutorial, I want a standalone vegetable-state
review scene I can launch from the title screen so I can compare crop-stage
options in one place before more sequencing work.

## Acceptance Criteria
- [ ] `SceneWorkCatalog` exposes `FarmVegetableStates` as a title-screen
      launchable scene without inserting it into the linear tutorial order.
- [ ] The shared title-screen/build-settings scene list includes
      `Assets/_Project/Scenes/FarmVegetableStates.unity`.
- [ ] A `FarmVegetableStates` scene asset exists under `Assets/_Project/Scenes/`.
- [ ] The scene contains a `VegetableStates_Showcase` root with tomato,
      carrot, corn, and wheat state candidates.
- [ ] The scene includes a thin runtime controller that builds the inspection
      floor/spawn point, ensures a first-person rig, and provides on-screen
      review instructions.
- [ ] The slice can return to `TitleScreen` with a direct input shortcut.

## Technical Plan

### Scene Catalog
- Add `FarmVegetableStatesSceneName` and `FarmVegetableStatesScenePath` to
  `SceneWorkCatalog`.
- Append the slice to `TitleScreenLaunchableScenes`.

### Runtime Wrapper
- Add `FarmVegetableStatesSceneController` under
  `Assets/_Project/Scripts/MonoBehaviours/Tutorial/`.
- Reuse `FarmFirstPersonRigUtility` for movement/camera setup.

### Scene Asset
- Create `Assets/_Project/Scenes/FarmVegetableStates.unity`.
- Place the current tomato, carrot, corn, and wheat stage candidates under a
  single `VegetableStates_Showcase` root for direct review.

## Testing Strategy
- EditMode: catalog/build-order coverage for the new launchable scene.
- EditMode: scene smoke check that the scene asset contains the controller and
  showcase root.
- EditMode: controller smoke check that `Start()` builds the ground, spawn
  point, and first-person rig in an empty scene.

## Out Of Scope
- Choosing the winning stage asset automatically
- Additional crop lifecycle tuning
- Reworking Scene 7 mission flow
