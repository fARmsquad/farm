# Feature Spec: Sequence Session Context And Auto Next Turn — GSO-011a

## Summary
The current backend can generate one minigame beat, one linked cutscene, and
the standing intro slice. What it still cannot do is continue on its own.

This slice adds the first autonomous sequence layer:
- create a persistent story-sequence session
- store evolving narrative/gameplay context per session
- select the next minigame generator automatically
- vary bounded parameters to avoid immediate repetition
- assemble the next minigame + cutscene turn through the existing package
  assembly service
- persist turn history so Unity or a local operator can keep asking for the
  next generated step

This is the first foundation for the actual goal: endless unique combinations
of cutscenes and tuned minigames with carried-forward context.

## User Story
As a developer, I want to create one persistent sequence session and keep
advancing it turn by turn, so the generative system can keep producing fresh
cutscene/minigame combinations without manual planning for every beat.

## Acceptance Criteria

### Session Contract
- [x] A typed request model exists for creating a story-sequence session.
- [x] A typed record exists for persisted session state.
- [x] A typed record exists for each persisted generated turn.
- [x] A typed session detail response can return the session plus all turns.

### Persistence
- [x] Sequence sessions persist in SQLite independently from the standing-slice
      job tables.
- [x] Each session persists its evolving context/state as structured JSON.
- [x] Each generated turn persists:
      - selected generator ID
      - selected character
      - request payload
      - generated package result
      - short summary metadata
- [x] A fetched session includes turn history in order.

### Automatic Planning
- [x] A service can create a new sequence session with initial context.
- [x] A service can advance an existing session by generating the next turn.
- [x] The planner selects from the existing V1 generator catalog:
      - `plant_rows_v1`
      - `find_tools_cluster_v1`
      - `chicken_chase_intro_v1`
- [x] The planner avoids immediately repeating the same generator when another
      valid option exists.
- [x] The planner derives bounded parameters from session context and generator
      rules instead of inventing unbounded freeform config.
- [x] The planner carries world-state unlocks and recent-history context
      forward after each successful turn.

### API Surface
- [x] `POST /api/v1/story-sequence-sessions` creates a new session.
- [x] `GET /api/v1/story-sequence-sessions/{session_id}` returns persisted
      session detail.
- [x] `POST /api/v1/story-sequence-sessions/{session_id}/next-turn` advances
      the session and persists the resulting turn.
- [x] Unknown session IDs return `404`.

### Integration Proof
- [x] One backend test proves a session can be created and advanced.
- [x] One backend test proves turn history is persisted and returned in order.
- [x] One backend test proves the second generated turn avoids an immediate
      generator repeat when multiple options are valid.
- [x] One API test proves the create -> next-turn -> fetch sequence works end
      to end.

## Non-Negotiable Rules
- Do not duplicate minigame materialization or storyboard generation logic in
  the session planner; use `GeneratedPackageAssemblyService`.
- Do not write this session state into the already crowded `store.py`; use
  dedicated sequence-session persistence modules.
- Do not require human approval to continue a session.
- Do not let the planner produce parameters that violate the generator catalog
  validation rules.
- Do not claim “endless sequences” at the Unity live-package level yet if the
  implementation still writes into one rolling package path; session history
  must persist even when the live package is only a current proof slice.

## Initial Planning Rules
- Start from the existing V1 generator definitions and their fit tags.
- Use session history plus world-state flags to rotate gameplay focus:
  planting -> tool recovery -> chicken chase -> repeat with varied bounded
  parameters.
- Keep a recent-generator window in session state so the planner can avoid
  back-to-back repeats.
- Carry a small rotating character pool so cutscene narration is not always
  spoken by the same NPC.
- Unlock additional parameter space over time through world-state flags rather
  than jumping directly to all possible variations on turn one.

## Out Of Scope
- Full branching narrative memory across dozens of sessions
- Unity runtime auto-play through an arbitrarily long scene chain
- Background workers or queue-based async execution
- Provider-level retry orchestration beyond the existing generator fallbacks
- Dynamic creation of brand-new minigame generator definitions at runtime

## Done Definition
- [x] Spec exists.
- [x] Sequence sessions and turns persist in SQLite.
- [x] The backend can create and advance a session automatically.
- [x] Turn generation uses the existing package assembly service.
- [x] Focused tests cover creation, persistence, non-repetition, and API flow.
