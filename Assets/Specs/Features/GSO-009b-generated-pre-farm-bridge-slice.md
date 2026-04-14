# Feature Spec: Generated Pre-Farm Bridge Slice — GSO-009b

## Summary
This slice keeps the standing `Generative Story Slice` moving forward on the
same playable path instead of dropping back into authored tutorial routing.

Right now the sample package stops at:
- generated post-chicken bridge
- generated plant rows beat
- generated find-tools beat

Then the flow enters `Tutorial_PreFarmCutscene`, but because that scene has no
package beat, the runtime falls back to tutorial defaults and loops back into
`FarmMain`.

This slice fixes that by:
1. letting package beats explicitly terminate the standing slice
2. generating a real package-backed `Tutorial_PreFarmCutscene` bridge beat
3. allowing linked storyboard generation for a non-crop minigame follow-up

The standing path becomes:
generated post-chicken cutscene -> generated plant rows -> generated find tools
-> generated pre-farm bridge -> completion banner

## User Story
As a developer, I want the standing generated slice to end on a generated
pre-farm bridge cutscene instead of looping back into FarmMain, so I can test
the evolving story package as one coherent playable chain.

## Acceptance Criteria

### Runtime Flow
- [ ] Package routing can distinguish between:
      - no package beat for this scene
      - a package beat that exists and has an empty `NextSceneName`
- [ ] When a package beat exists and its `NextSceneName` is empty, the tutorial
      flow treats that scene as terminal and shows the completion banner
      instead of falling back to tutorial defaults.

### Backend Storyboard Context
- [ ] `GeneratedStoryboardService` can create a linked cutscene from a
      non-crop minigame beat such as `tutorial.find_tools`.
- [ ] Linked storyboard generation no longer requires a crop-derived context
      field when the linked minigame is not crop-focused.

### Standing Sample Package
- [ ] `StoryPackage_IntroChickenSample` includes a generated `pre_farm_bridge`
      beat for `Tutorial_PreFarmCutscene`.
- [ ] That beat includes storyboard shots and resource paths.
- [ ] That beat ends the generated slice cleanly.

### Integration Proof
- [ ] Runtime catalog resolves the `Tutorial_PreFarmCutscene` storyboard from
      the sample package.
- [ ] Tutorial flow completion on `Tutorial_PreFarmCutscene` shows the
      completion banner for the standing slice.
- [ ] One backend test proves linked storyboard generation works from a
      `find_tools_intro` beat.

## Non-Negotiable Rules
- Do not add a generated pre-farm beat without fixing terminal package routing.
- Do not force non-crop follow-up cutscenes through crop-specific context.
- Keep the standing title-screen slice on one evolving package path; do not add
  another parallel sample slice.

## Out of Scope
- Full episode planner output across every tutorial beat
- New live runtime generation
- Review UI for selecting between multiple generated pre-farm variants
- Town or horse-training integration

## Done Definition
- [ ] Spec exists.
- [ ] The standing generated slice ends on a generated pre-farm bridge.
- [ ] The slice no longer loops back into FarmMain after `FindToolsGame`.
- [ ] Focused backend and EditMode tests cover the new behavior.
