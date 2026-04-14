# Feature Spec: Package-Driven Find Tools Runtime Slice — GSO-009

## Summary
This slice turns the generated `find_tools_cluster_v1` beat into a playable
Unity runtime path instead of leaving `FindToolsGame` as a walk-to-square
placeholder.

The immediate goal is narrow:

1. let the story package carry a generated `tutorial.find_tools` beat
2. add a generated `find_tools_intro` beat for `FindToolsGame`
3. let `TutorialFindToolsSceneController` switch into a package-driven
   scavenger challenge when that beat is present
4. keep the old placeholder bridge as the fallback when no package minigame
   beat is available

This keeps the standing `Generative Story Slice` on one evolving path:
generated cutscene -> generated planting beat -> generated tool-recovery beat.

## User Story
As a developer, I want the find-tools scene to consume a generated minigame
beat from the story package, so I can verify that generator output changes a
real scavenger gameplay slice instead of only changing cutscene copy.

## Acceptance Criteria

### Contract
- [ ] Existing `StoryMinigameConfigSnapshot` parameter access is sufficient for
      `targetToolSet`, `toolCount`, `searchZone`, `hintStrength`, and
      `timeLimitSeconds`.
- [ ] The sample package includes a generated `tutorial.find_tools` beat with
      resolved parameter entries for the runtime-critical fields.

### Runtime Catalog
- [ ] `StoryPackageRuntimeCatalog` can resolve the generated find-tools beat for
      `FindToolsGame`.
- [ ] The standing sample package chains `FarmMain` into `FindToolsGame`.

### Find Tools Runtime
- [ ] `TutorialFindToolsSceneController` detects the package-driven
      `tutorial.find_tools` beat for `FindToolsGame`.
- [ ] In package-driven mode, the scene objective comes from the generated beat
      instead of the fallback placeholder objective.
- [ ] The scene spawns exactly the generated number of collectible tools.
- [ ] `searchZone` changes the pickup layout.
- [ ] `targetToolSet` changes which tools are represented.
- [ ] `hintStrength` changes how visible the pickups are.
- [ ] The scene tracks collected tools against the generated `RequiredCount`
      and completes into the next routed scene when done.

### Fallback Safety
- [ ] If no package minigame beat exists for `FindToolsGame`, the current
      placeholder walk-to-square flow still works.

## Non-Negotiable Rules
- Keep the first runtime adapter deterministic and local; do not add live
  generation or network dependencies to scene playback.
- Do not rely on trigger-only pickup collection for this slice; the current
  CharacterController-based tutorial rig should use simple proximity checks.
- Do not regress the standing `Generative Story Slice` farm beat while adding
  the follow-up find-tools beat.

## Out of Scope
- Full authored pickup art replacement
- Inventory persistence from the find-tools beat into later scenes
- `chicken_chase_intro_v1` runtime consumption
- Planner-driven multi-beat episode assembly

## Done Definition
- [ ] Spec exists.
- [ ] `FindToolsGame` can run from package data as a generated scavenger beat.
- [ ] The standing sample slice flows from `FarmMain` into generated find-tools
      gameplay.
- [ ] Fallback placeholder behavior still works when the package beat is absent.
