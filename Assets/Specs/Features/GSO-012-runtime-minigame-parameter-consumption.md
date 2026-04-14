# Feature Spec: Runtime Minigame Parameter Consumption — GSO-012

## Summary
The standing `Generative Story Slice` can now fetch and continue generated
story turns, but not every generated minigame parameter materially changes
runtime play.

Current state:
- `tutorial.find_tools` already consumes its generated tool-set, count, zone,
  and hint parameters at runtime.
- `tutorial.plant_rows` reads generated parameters, but it over-expands plot
  count by multiplying `requiredCount * rowCount` instead of shaping a bounded
  layout from the requested target.
- `tutorial.chicken_chase` still behaves like a fixed authored scene even when
  the generated package provides `targetCaptureCount`, `arenaPresetId`,
  `guidanceLevel`, and `timeLimitSeconds`.

This slice closes that gap so the same standing slice the developer already
uses can show clearer variation from generated minigame settings.

## User Story
As a developer, I want generated minigame parameters to alter the real Unity
minigame behavior, so endless generated turns feel like actual gameplay
variants instead of static scenes with different text.

## Product Intent
Generated minigames should behave like bounded runtime adapters:
- the story package picks the adapter and parameters
- Unity consumes those parameters through controlled runtime hooks
- the scene changes only within safe, known limits

This is not procedural scene generation. It is runtime parameter consumption
for existing minigames.

## Acceptance Criteria

### Plant Rows Runtime Consumption
- [ ] `PackagePlantRowsMissionService` rounds the desired plot count up to a
      full row layout instead of multiplying by `rowCount`.
- [ ] A generated request for `targetCount=5` and `rowCount=2` produces `6`
      interactive plots, not `10`.
- [ ] `rowCount` still affects the runtime layout shape through the existing
      plot spawner.

### Chicken Chase Runtime Consumption
- [ ] `TutorialChickenSceneController` reads the active generated minigame
      config when the scene uses adapter `tutorial.chicken_chase`.
- [ ] `ChickenGameManager` can apply generated package config without needing
      a separate scene variant.
- [ ] Generated `timeLimitSeconds` updates the runtime timer.
- [ ] Generated `arenaPresetId` updates bounded chase-space tuning.
- [ ] Generated `targetCaptureCount` can require more than one successful coop
      drop before the scene is considered complete.
- [ ] Generated `guidanceLevel` changes the guidance text shown in the scene.

### Standing Slice Proof
- [ ] The existing `Generative Story Slice` entry point remains the primary
      test surface for this work.
- [ ] The current generated package sample can express at least one visible
      runtime difference in farm layout and one visible runtime difference in
      chicken-chase behavior.

### Regression Coverage
- [ ] Focused EditMode coverage locks the plant-rows desired-plot rule.
- [ ] Focused EditMode coverage locks multi-capture chicken mission progress.
- [ ] Focused EditMode coverage locks package-driven chicken runtime config
      application.

## Non-Negotiable Rules
- Do not add a new sample launcher; keep updating the standing
  `Generative Story Slice`.
- Do not introduce freeform runtime parameter names outside the existing story
  package contract.
- Do not create new Unity scenes just to express parameter variants that the
  existing minigame can absorb safely.
- Do not let package-driven chicken capture loops advance the tutorial flow
  early before the configured capture target is met.

## Out Of Scope
- Spawning multiple simultaneous chicken actors from `chickenCount`
- Brand-new crop types or inventory expansion beyond the currently supported
  runtime seeds
- Procedural environment generation for minigame arenas
- Re-authoring the find-tools scene, which already consumes its generated
  parameters in the current standing slice

## Done Definition
- [ ] Spec exists.
- [ ] Plant-rows package mode uses bounded row-shaped plot expansion.
- [ ] Chicken package mode consumes generated timer, arena, capture-count, and
      guidance settings.
- [ ] The standing generated slice remains the place to test the latest
      runtime variation.
- [ ] Focused EditMode tests cover the new runtime consumption rules.
