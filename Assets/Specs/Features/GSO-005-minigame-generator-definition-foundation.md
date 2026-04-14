# Feature Spec: Minigame Generator Definition Foundation — GSO-005

## Summary
This spec defines the next contract layer for the Generative Story
Orchestrator: `MinigameGeneratorDefinition`.

The point of this slice is to stop the planner from inventing raw gameplay
config ad hoc. Instead, the planner should select a known generator, fill
bounded parameters, and let the minigame adapter validate and materialize the
runtime config.

This spec is intentionally narrow. It does not try to model every minigame in
FarmSim VR. It defines the contract and the first three generator definitions:

- `plant_rows_v1`
- `find_tools_cluster_v1`
- `chicken_chase_intro_v1`

This spec refines the implementation-plan work item currently described as
`GSO-007a`.

## User Story
As a developer, I want the AI planner to customize gameplay through bounded
generator definitions instead of freeform configs, so that story-driven
minigame selection stays safe, testable, and consistent with the real game.

## Product Intent
Each minigame should feel like its own controllable tool.

That means the planner should be able to answer:
- which minigame fits this beat?
- which generator variant should be used?
- which parameter values are allowed here?
- what should happen if this generator is invalid for the current world state?

This layer becomes the control surface between:
- narrative planning
- gameplay selection
- gameplay tuning
- runtime validation

## Acceptance Criteria

### Contract
- [x] A `MinigameGeneratorDefinition` contract is defined at the planning level.
- [x] Each generator definition includes:
      `generatorId`, `minigameId`, `fitTags`, `parameterSchema`, `defaults`,
      `couplingRules`, and `fallbackGeneratorIds`.
- [x] Parameter schemas support at least:
      enum, int, float, bool, and bounded string/id references.
- [x] Generator definitions can declare world-state preconditions.
- [x] Generator definitions can declare difficulty-band suitability.
- [x] A generator definition can fail validation without crashing the planner.

### Initial Generator Set
- [x] `plant_rows_v1` is defined.
- [x] `find_tools_cluster_v1` is defined.
- [x] `chicken_chase_intro_v1` is defined.
- [x] Each of the three generators includes:
      fit tags, bounded params, defaults, coupling rules, and fallback IDs.

### Planner Behavior
- [ ] The planner is expected to choose a generator ID first, not raw config.
- [x] The planner is expected to fill only parameters exposed by the chosen
      generator.
- [x] Invalid parameter combinations are rejected by validation.
- [x] A fallback generator path is defined for every V1 generator.

## Non-Negotiable Rules
- The generator layer is not a literal MCP service per minigame.
- The planner must never invent parameters outside the contract.
- Parameter bounds must be grounded in existing gameplay reality, not narrative
  wishful thinking.
- V1 should prefer low-cardinality enums and conservative numeric ranges.
- Every generator must be able to degrade to a safer fallback generator.

## Contract Shape

### MinigameGeneratorDefinition
- `GeneratorId`
- `MinigameId`
- `DisplayName`
- `FitTags[]`
- `DifficultyBands[]`
- `RequiredWorldState[]`
- `ParameterSchema`
- `Defaults`
- `CouplingRules[]`
- `FallbackGeneratorIds[]`
- `PreviewTextTemplate`

### ParameterSchema
Each parameter entry should define:
- `name`
- `type`
- `required`
- `default`
- `allowedValues` for enums
- `min` / `max` for numeric types
- `description`

## V1 Generator Definitions

### Generator 1: `plant_rows_v1`
- **Minigame ID**: `planting`
- **Intent**: Configure a crop-planting beat for tutorial or calm farming
  sequences.
- **Fit Tags**:
  - `intro`
  - `teaching`
  - `crop-focused`
  - `calm`
- **Bounded Parameters**:
  - `cropType`: enum
    - allowed: `carrot`, `tomato`, `corn`
    - default: `carrot`
  - `targetCount`: int
    - min: `3`
    - max: `12`
    - default: `5`
  - `timeLimitSeconds`: int
    - min: `180`
    - max: `600`
    - default: `300`
  - `rowCount`: int
    - min: `1`
    - max: `4`
    - default: `2`
  - `assistLevel`: enum
    - allowed: `high`, `medium`, `low`
    - default: `high`
- **Coupling Rules**:
  - `intro` beats may not use `assistLevel=low`
  - `rowCount >= 3` requires `targetCount >= 6`
  - `tomato` is invalid before tomatoes are unlocked in world state
- **Fallback Generator IDs**:
  - `plant_rows_tutorial_safe_v1`

### Generator 2: `find_tools_cluster_v1`
- **Minigame ID**: `find_tools`
- **Intent**: Configure a short tool-recovery or scavenger beat that can bridge
  between story scenes.
- **Fit Tags**:
  - `bridge`
  - `search`
  - `tool-recovery`
  - `teaching`
- **Bounded Parameters**:
  - `targetToolSet`: enum
    - allowed: `starter`, `watering`, `planting`
    - default: `starter`
  - `toolCount`: int
    - min: `1`
    - max: `3`
    - default: `2`
  - `searchZone`: enum
    - allowed: `yard`, `shed_edge`, `field_path`
    - default: `yard`
  - `hintStrength`: enum
    - allowed: `strong`, `medium`, `light`
    - default: `strong`
  - `timeLimitSeconds`: int
    - min: `90`
    - max: `420`
    - default: `240`
- **Coupling Rules**:
  - `toolCount=3` requires `hintStrength` of at least `medium`
  - `searchZone=field_path` is invalid for the earliest tutorial bridge
  - `starter` tool sets should not exceed `timeLimitSeconds=300`
- **Fallback Generator IDs**:
  - `find_tools_linear_safe_v1`

### Generator 3: `chicken_chase_intro_v1`
- **Minigame ID**: `chicken_chase`
- **Intent**: Configure the first light-pressure chase beat used in the intro
  tutorial chain.
- **Fit Tags**:
  - `intro`
  - `light-pressure`
  - `animal`
  - `teaching`
- **Bounded Parameters**:
  - `targetCaptureCount`: int
    - min: `1`
    - max: `3`
    - default: `1`
  - `chickenCount`: int
    - min: `1`
    - max: `4`
    - default: `2`
  - `arenaPresetId`: enum
    - allowed: `tutorial_pen_small`, `tutorial_pen_medium`
    - default: `tutorial_pen_small`
  - `timeLimitSeconds`: int
    - min: `60`
    - max: `300`
    - default: `120`
  - `guidanceLevel`: enum
    - allowed: `high`, `medium`, `low`
    - default: `high`
- **Coupling Rules**:
  - `targetCaptureCount` may not exceed `chickenCount`
  - `guidanceLevel=low` is invalid for `intro` beats
  - `arenaPresetId=tutorial_pen_medium` requires `timeLimitSeconds >= 120`
- **Fallback Generator IDs**:
  - `chicken_chase_basic_safe_v1`

## Why These Three First
- `plant_rows_v1` proves crop-driven minigame tuning.
- `find_tools_cluster_v1` proves search/recovery-style bridge gameplay.
- `chicken_chase_intro_v1` proves that even early tutorial minigames can be
  selected and tuned through the same bounded interface.

Together, they cover:
- farming setup
- bridge-task recovery
- light-pressure action

That is enough variety to validate the generator pattern without expanding the
surface too early.

## Out of Scope
- Runtime materialization code
- Planner prompt implementation
- Backend persistence for generator definitions
- Full generator coverage for every minigame in the repo
- Dynamic authoring UI for generators

## Done Definition
- [x] Spec exists.
- [x] The three V1 generators are defined with bounded parameters.
- [x] The implementation plan references `MinigameGeneratorDefinition` as a real
  next build slice.
- [x] The generator layer is concrete enough that the next implementation task can
  build the schema without re-deciding the first generator set.
