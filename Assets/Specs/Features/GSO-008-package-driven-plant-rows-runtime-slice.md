# Feature Spec: Package-Driven Plant Rows Runtime Slice — GSO-008

## Summary
This slice makes the current generated minigame beat playable in Unity instead
of leaving it as backend JSON only.

The immediate goal is narrow:

1. let the story package carry runtime-readable generated minigame parameters
2. add a generated `plant_rows_v1` beat for `FarmMain`
3. let `TutorialFarmSceneController` switch into a package-driven planting
   challenge when that beat is present
4. keep the authored tomato task ladder as the fallback when no package
   minigame beat is available

This keeps the standing `Generative Story Slice` moving forward on the same
visible path: generated post-chicken cutscene -> generated planting beat.

## User Story
As a developer, I want the farm scene to consume a generated planting beat from
the story package, so I can verify that generator output changes the playable
minigame and not just the cutscene copy.

## Acceptance Criteria

### Contract
- [ ] `StoryMinigameConfigSnapshot` can carry Unity-readable generated
      parameter data for runtime consumption.
- [ ] The generated parameter shape is serializable by Unity `JsonUtility`.
- [ ] Existing package import still works for older minigame beats that do not
      include generated parameter entries.

### Runtime Catalog
- [ ] `StoryPackageRuntimeCatalog` can resolve a `Kind=Minigame` beat for the
      current scene.
- [ ] The runtime can read the `tutorial.plant_rows` beat for `FarmMain` from
      `StoryPackage_IntroChickenSample`.

### Farm Scene Runtime
- [ ] `TutorialFarmSceneController` detects the package-driven
      `tutorial.plant_rows` beat for `FarmMain`.
- [ ] In package-driven mode, the farm scene objective comes from the minigame
      beat instead of the authored tomato tutorial objective.
- [ ] In package-driven mode, the farm scene tracks planted target crops across
      the available plots and completes when the generated `RequiredCount` is
      reached.
- [ ] In package-driven mode, the farm scene uses the generated crop type to
      choose the seed context.
- [ ] In package-driven mode, the generated `rowCount` parameter can expand the
      playable patch count; the standing sample beat surfaces 6 plantable plots
      in `FarmMain`.
- [ ] If no package minigame beat exists for `FarmMain`, the existing authored
      tomato task ladder still runs unchanged.

### Standing Slice Wiring
- [ ] The sample package includes a generated `plant_rows_intro` beat for
      `FarmMain`.
- [ ] The post-chicken bridge beat hands off to `FarmMain` for the standing
      generated slice.
- [ ] The standing `Generative Story Slice` therefore exercises:
      generated cutscene -> generated plant rows gameplay.

## Non-Negotiable Rules
- Do not make Unity parse the backend's raw dictionary parameter shape at
  runtime; mirror it into a Unity-serializable contract.
- Do not remove or rewrite the authored farm tutorial path; package-driven
  planting is an additive runtime mode.
- Keep the first runtime adapter narrow to `tutorial.plant_rows`. Other
  adapter runtimes can follow after this path is stable.

## Out of Scope
- `find_tools_cluster_v1` runtime consumption
- `chicken_chase_intro_v1` runtime consumption
- Full planner-driven episode assembly
- Operator review UI
- Quest device optimization for the new slice

## Done Definition
- [ ] Spec exists.
- [ ] Unity can import a generated planting beat for `FarmMain`.
- [ ] `FarmMain` changes objective and completion behavior from package data.
- [ ] The standing `Generative Story Slice` flows from the generated
      post-chicken cutscene into generated planting gameplay.
