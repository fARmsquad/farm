# Feature Spec: Minigame Generator Materialization Foundation — GSO-007b

## Summary
This slice takes the generator definitions from GSO-005 and makes them useful
to the package pipeline.

The backend must be able to accept:
- a chosen `generatorId`
- bounded parameter values
- generation context

and return a materialized minigame beat payload that can be written into a
`StoryPackage`.

This is still not full AI planning. The planner is out of scope here. The
goal is to prove the layer immediately after planning:

1. validate the chosen generator selection
2. materialize adapter-ready minigame config
3. write or replace the minigame beat in package JSON
4. return structured failure instead of throwing when invalid

## User Story
As a developer, I want a validated generator selection to turn into a concrete
minigame beat payload, so that package assembly can use structured gameplay
data instead of hand-written minigame JSON.

## Acceptance Criteria

### Materialization Contract
- [x] A backend request model exists for a generated minigame beat.
- [x] The request includes package identity, beat identity, scene routing,
      `generatorId`, bounded `parameters`, and generation `context`.
- [x] A backend result model exists for materialized minigame output.
- [x] Invalid generator selections return `isValid=false`, errors, and fallback
      generator IDs without crashing the caller.

### Package Writing
- [x] A service can write or replace a `Kind=Minigame` beat in the target story
      package JSON.
- [x] Successful materialization writes:
      `AdapterId`, `ObjectiveText`, `RequiredCount`, `TimeLimitSeconds`,
      `GeneratorId`, `FallbackGeneratorIds`, and resolved parameter values.
- [x] Existing unrelated beats in the package are preserved.
- [x] Invalid materialization does not write a partial package update.

### V1 Generator Coverage
- [x] `plant_rows_v1` can materialize a planting beat.
- [x] `find_tools_cluster_v1` can materialize a find-tools beat.
- [x] `chicken_chase_intro_v1` can materialize a chicken-chase beat.
- [x] Each materialized beat derives objective text and runtime counts from
      resolved parameter values.

## Non-Negotiable Rules
- The service must validate through the generator catalog before materializing.
- The materializer must not invent parameters that are not already resolved by
  the selected generator definition.
- Invalid selections must produce a structured response, not a thrown planner-
  facing exception.
- This slice may emit provisional adapter IDs for generators that are not yet
  wired into Unity runtime, but they must be explicit and deterministic.

## Proposed Request Shape
```json
{
  "package_id": "storypkg_intro_chicken_sample",
  "package_display_name": "Intro Chicken Sample",
  "beat_id": "plant_rows_intro",
  "display_name": "Plant Rows Intro",
  "scene_name": "PlantRowsScene",
  "next_scene_name": "PostPlantRowsCutscene",
  "generator_id": "plant_rows_v1",
  "parameters": {
    "cropType": "carrot",
    "targetCount": 6,
    "timeLimitSeconds": 300
  },
  "context": {
    "fit_tags": ["intro"],
    "world_state": ["farm_plots_unlocked"],
    "difficulty_band": "tutorial"
  }
}
```

## Proposed Materialized Beat Shape
```json
{
  "BeatId": "plant_rows_intro",
  "DisplayName": "Plant Rows Intro",
  "Kind": "Minigame",
  "SceneName": "PlantRowsScene",
  "NextSceneName": "PostPlantRowsCutscene",
  "Minigame": {
    "AdapterId": "tutorial.plant_rows",
    "ObjectiveText": "Plant 6 carrots in 5 minutes.",
    "RequiredCount": 6,
    "TimeLimitSeconds": 300.0,
    "GeneratorId": "plant_rows_v1",
    "FallbackGeneratorIds": ["plant_rows_tutorial_safe_v1"],
    "ResolvedParameters": {
      "cropType": "carrot",
      "targetCount": 6,
      "timeLimitSeconds": 300,
      "rowCount": 2,
      "assistLevel": "high"
    }
  }
}
```

## Initial Adapter Mapping
- `plant_rows_v1` -> `tutorial.plant_rows`
- `find_tools_cluster_v1` -> `tutorial.find_tools`
- `chicken_chase_intro_v1` -> `tutorial.chicken_chase`

## Out of Scope
- Planner prompt integration
- Multi-beat episode planning
- Unity runtime consumption of every new minigame adapter
- Automatic fallback traversal into not-yet-authored safe generator definitions
- Operator UI for swapping generators

## Done Definition
- [x] Spec exists.
- [x] The backend can materialize the three V1 generators into minigame beat data.
- [x] Invalid selections return structured validation output.
- [x] Successful selections update package JSON deterministically.
