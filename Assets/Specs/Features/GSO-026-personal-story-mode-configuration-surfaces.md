## GSO-026 - Personal Story Mode Configuration Surfaces

### Goal
Create a durable backend configuration layer for personal story mode so the
runtime can choose bounded minigames, apply target story types, and shape
prompts through explicit prompt-structure presets instead of hidden prompt
string glue.

### Problem
- The runtime already has bounded minigame generators, but it does not yet
  expose a narrative configuration surface for each game that the LLM can
  reason over directly.
- Story-type targeting is implicit today. The session can carry tags and a
  narrative seed, but there is no typed preset that says "this run should feel
  like a planting lesson", "this run should feel like a tool-recovery detour",
  or "this run should feel like light animal chaos."
- Prompt structure is hardcoded inside the turn-director and storyboard-planner
  prompts, so there is no reusable way to tune how a generated turn should
  anchor context, escalate conflict, and hand off into the next minigame.

### Required Product Shape
- Add a typed story-mode configuration catalog inside
  `backend/story-orchestrator`.
- The catalog must expose three layers:
  - `story_types`
  - `prompt_structures`
  - `minigame_surfaces`
- Each minigame surface must describe the game in LLM-facing terms while still
  pointing back to the bounded generator definition already used for validation.
- Each story type must define:
  - display metadata
  - narrative seed
  - prompt directives
  - fit tags
  - world-state bias
  - character defaults
  - allowed generator IDs
- Each prompt structure must define:
  - display metadata
  - director-facing prompt directives
  - storyboard-facing prompt directives
  - the intended dramatic shape of the turn

### Runtime Contract
- Add a runtime configuration endpoint that returns the full typed config
  document for story mode.
- Extend runtime session creation so callers can select:
  - `story_type_id`
  - `prompt_structure_id`
- Persist the selected IDs in runtime session state.
- Resolve the selected story type and prompt structure during session creation
  and merge them into:
  - fit tags
  - world state
  - character pool
  - narrative seed
  - allowed generator IDs
- Feed the resolved story-mode config into:
  - `StorySequenceTurnDirector`
  - `RuntimeTurnGenerationService`
  - `OpenAIStoryboardPlanner`

### Current Scope
- Keep the default production path aligned with the bounded planting proof run.
- Provide LLM-facing narrative surfaces for all three existing bounded
  generators:
  - `plant_rows_v1`
  - `find_tools_cluster_v1`
  - `chicken_chase_intro_v1`
- Add at least three story-type presets and at least three prompt-structure
  presets.
- Keep the runtime public contract stable for existing callers beyond the new
  optional session-create fields and the new config endpoint.

### Player-Facing Outcome
- Personal story mode gains real authored knobs behind the scenes: the service
  can target different story flavors, pick bounded games intentionally, and
  generate cutscene language with a more explicit narrative shape.
- Future Unity surfaces can inspect the same config catalog instead of inventing
  separate local lists of story types or minigame prompt rules.

### Constraints
- Do not duplicate minigame parameter bounds or coupling rules outside the
  existing `MinigameGeneratorCatalog`; wrap those definitions with narrative
  metadata instead.
- Keep all config surfaces typed through Pydantic models.
- Keep the runtime session authoritative for selected story type and prompt
  structure.
- Preserve the current bounded-session behavior and the existing FarmMain-first
  default generated run.

### Acceptance Criteria
- [ ] A typed story-mode config catalog exists in the backend with story types,
      prompt structures, and minigame surfaces.
- [ ] A runtime API endpoint returns the full config document for story mode.
- [ ] Runtime session creation accepts `story_type_id` and
      `prompt_structure_id` and persists them in session state.
- [ ] The selected story type constrains allowed generator IDs and can change
      the generated minigame for a turn when the preset allows it.
- [ ] The turn-director prompt includes story-type, prompt-structure, and
      minigame-surface context.
- [ ] The storyboard-planner prompt includes story-type, prompt-structure, and
      minigame-surface context.
- [ ] The default runtime flow still produces the current planting-based proof
      session when no explicit story-type override is supplied.

### Out of Scope
- Unity UI for editing story types or prompt structures
- Freeform user-authored prompt editing
- New minigame adapters beyond the current three bounded generators
- Infinite continuation beyond the existing bounded runtime contract
