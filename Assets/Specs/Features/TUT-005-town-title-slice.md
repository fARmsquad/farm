# Feature Spec: Town Title Slice - TUT-005

## Summary
Add `Assets/_Project/Scenes/Town.unity` to the title-screen playable-slice
launcher so the existing town conversation prototype can be opened directly
without inserting the scene into the mandatory linear tutorial route.

## User Story
As the developer iterating on the town conversation prototype, I want Town to
be launchable from the title screen so I can test that slice directly instead
of replaying the tutorial chain to reach it.

## Acceptance Criteria
- [ ] `SceneWorkCatalog` exposes `Town` as a title-screen launchable scene.
- [ ] `CreateTitleScene.GetOrderedBuildScenePaths()` includes
      `Assets/_Project/Scenes/Town.unity`.
- [ ] The shared title-screen/build-settings scene list contains Town between
      the other standalone slices and `WorldMain`.
- [ ] `TutorialSceneCatalog.SceneOrder` does not include `Town`.
- [ ] `Assets/_Project/Scenes/Town.unity` remains a valid standalone slice
      containing the current town conversation wiring:
      `LLMConversationController`, `DialogueChoiceUI`,
      `TownPlayerController`, `TownInteractionAutoplay`, and at least one
      `NPCController`.
- [ ] The existing `Start Game` button on `TitleScreen` still targets the first
      scene in the linear tutorial sequence.

## Technical Plan

### Scene Catalog
- Add `TownSceneName` and `TownScenePath` to `SceneWorkCatalog`.
- Append Town to `TitleScreenLaunchableScenes`.
- Keep Town out of `TutorialOrderedScenes` and `TutorialSceneCatalog`.

### Title Screen / Build Wiring
- Reuse the existing runtime-generated title launcher in `TitleScreenManager`;
  no one-off button path should be added in `TitleScreen.unity`.
- Ensure `CreateTitleScene` and editor build settings use the shared launchable
  scene list so Town is available in development builds.

### Verification
- Extend EditMode tests for catalog ordering and scene lookup metadata.
- Extend EditMode scene configuration tests for build-path ordering and Town
  scene smoke coverage.

## Dependencies
- `Assets/_Project/Scenes/Town.unity`
- `Assets/_Project/Scripts/Core/Tutorial/SceneWorkCatalog.cs`
- `Assets/_Project/Editor/CreateTitleScene.cs`
- `Assets/_Project/Scripts/MonoBehaviours/TitleScreenManager.cs`

## Out Of Scope
- Implementing streamed NPC text delivery; that work stays in `TOWN-001`.
- Reworking the linear tutorial sequence to route through Town.
- Adding new town gameplay beyond exposing the existing playable slice.
