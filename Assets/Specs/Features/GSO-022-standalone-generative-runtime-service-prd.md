# Product Requirements Document: Standalone Generative Runtime Service — GSO-022

## Summary
The current generative playthrough work has proven the core idea, but the
operational shape is too Unity-heavy. Too much responsibility sits inside the
game runtime: local backend bootstrap, provider coordination, runtime package
state, scene-specific binding behavior, and focus-sensitive request flows.

The next version should invert that architecture.

We will build a standalone generative service that owns orchestration,
persistence, provider calls, continuity, asset generation, and contract
assembly. Unity will become a thin client that:

1. requests a generated playthrough or next turn,
2. polls or subscribes to job progress,
3. receives a stable content contract,
4. downloads or resolves referenced assets,
5. inserts the returned cutscene/minigame contract into existing runtime
   adapters.

The service may run locally in development and remotely in staging/production,
but Unity should treat it the same way in every environment: an external API,
not an embedded workflow.

## Problem Statement
The current approach mixes three concerns too tightly:

- **Gameplay runtime**: scene loading, cutscene playback, minigame execution,
  subtitle presentation, and player flow.
- **Content orchestration**: session state, turn planning, continuity tracking,
  minigame selection, validation, and sequencing.
- **Provider execution**: OpenAI, Gemini, ElevenLabs, fallback logic, prompt
  templates, quality gates, retries, and artifact persistence.

This creates the wrong failure surface:

- clicking away from Unity can affect generation reliability,
- local process startup becomes part of the gameplay loop,
- scene-specific Unity behavior can accidentally override generated content,
- provider errors appear as gameplay errors,
- debugging requires reading both gameplay code and orchestration code at once,
- the contract between backend output and Unity input is not yet narrow enough.

The target state is a service-first architecture where Unity does not own any
provider logic, secret handling, orchestration state, or long-running content
generation behavior.

## Vision
Create a standalone Generative Runtime Service that can generate endless,
session-aware sequences of cutscenes and bounded minigames, while exposing a
simple contract for Unity consumption:

- **Unity asks for a turn**
- **The service generates and stores everything**
- **Unity receives a clean, versioned payload**
- **Unity only renders and plays what the service already resolved**

The service should be usable by:

- Unity editor dev builds
- local debug tools
- operator review tools
- future non-Unity clients if needed

## Goals

### Product Goals
- Make Unity a thin consumer of generated narrative/gameplay payloads.
- Keep generation reliable even if the Unity app loses focus or closes
  temporarily during generation.
- Support endless, session-aware narrative progression with bounded gameplay.
- Keep visual and narrative continuity across generated turns.
- Make provider orchestration and fallback logic entirely service-owned.
- Expose a stable, versioned API contract that is easy to bind into Unity.
- Support local dev, remote staging, and remote production with the same API
  shape.

### Developer Experience Goals
- No provider keys in Unity scenes, prefabs, or scripts.
- No Unity-owned orchestration state beyond current session/turn identifiers and
  downloaded payloads.
- A single service endpoint for all generated-playthrough actions.
- Observable per-step generation status and artifact provenance.
- Service-side retries and resumability so Unity does not need custom recovery
  logic.

## Non-Goals
- Runtime synthesis of brand-new Unity mechanics with no prebuilt adapter.
- Arbitrary scene graph authoring from the model.
- Fully unbounded minigame invention.
- Lip-sync, facial animation, or full cinematic camera generation in this
  phase.
- Replacing existing minigame scenes immediately; the first version should
  target existing bounded adapters.
- Multiplayer synchronization or shared live co-op story state.

## Core Product Principles

### 1. Service Owns Generation
All planning, prompting, provider calls, retries, quality checks, and artifact
storage happen in the service.

### 2. Unity Owns Playback
Unity loads scenes, binds payloads to adapters, plays cutscenes, shows
subtitles, and runs minigames.

### 3. Bounded Gameplay, Open Narrative
Narrative can vary broadly; gameplay must stay inside a validated adapter
catalog with explicit parameter contracts.

### 4. Fail Closed For Playable Content
If the service cannot produce a valid turn, Unity should get an explicit failed
job or unavailable state, not stale or half-generated content disguised as
fresh output.

### 5. Contracts Over Implicit Behavior
Every payload consumed by Unity must be versioned, typed, and explicit.
Nothing important should depend on scene-name coincidence or hidden fallback
paths.

### 6. Persistence By Default
Every turn, artifact, prompt decision, fallback, and continuity reference
should be recoverable for inspection.

## Users

### Primary User: Developer / Operator
Wants to generate, inspect, play, and iterate on unique playthroughs quickly.

### Secondary User: Player
Eventually experiences a smooth endless narrative loop with generated cutscenes
and customized minigames without knowing anything about providers or jobs.

### Tertiary User: Content Operator
Needs to review generated runs, inspect provenance, approve/publish curated
content, and diagnose provider or quality failures.

## Target User Experience

### Flow A: Generate First Turn
1. Unity requests `Create Playthrough`.
2. Service creates a session and a turn-generation job.
3. Unity shows generation progress by polling or subscribing to status events.
4. Service plans the turn, generates assets, validates outputs, persists the
   contract, and marks the turn `ready`.
5. Unity fetches the ready turn envelope and enables play.

### Flow B: Play Generated Turn
1. Unity receives the playable turn envelope.
2. Unity preloads referenced images/audio.
3. Unity binds the cutscene contract into the cutscene player.
4. Unity binds the minigame contract into the specified minigame adapter.
5. Unity plays the turn.

### Flow C: Continue After Minigame
1. Unity reports minigame completion and outcome.
2. Service updates session world state and narrative context.
3. Unity requests the next turn.
4. Service generates the next turn asynchronously while the current result can
   still be inspected.

### Flow D: Resume Later
1. Unity or another client asks for the current session state.
2. Service returns the most recent ready turn, current job status, and session
   context.
3. Unity resumes from the authoritative service state instead of reconstructing
   local generation state.

## Proposed Architecture

### High-Level Shape
- **Unity Client**
  - Thin runtime SDK
  - Progress UI
  - Asset preloader
  - Cutscene player binder
  - Minigame adapter binder

- **Generative Runtime Service**
  - API layer
  - Job orchestration layer
  - Session and continuity layer
  - Narrative planning layer
  - Asset generation layer
  - Contract assembly layer
  - Review/publish layer

- **Storage**
  - relational DB for sessions/jobs/turns/provenance
  - object storage or filesystem for images/audio/alignment/manifests
  - reference library for characters/style continuity

### Service Subsystems

#### 1. API Layer
Receives requests from Unity and operator tools. Responsible for auth,
idempotency, validation, and returning stable response shapes.

#### 2. Session Engine
Stores:
- playthrough session state
- beat cursor
- world state
- character pool
- recent turn summaries
- continuity ledger
- last ready turn

#### 3. Turn Director
Chooses the next bounded minigame generator and narrative framing.

This may use:
- OpenAI structured outputs for turn direction
- fallback rule engine if model output is invalid/unavailable

#### 4. Storyboard Planner
Writes:
- subtitle lines
- narration lines
- image prompts
- shot durations
- style and continuity guidance

This may use:
- OpenAI structured outputs for storyboard planning
- fallback template planner

#### 5. Minigame Contract Generator
Materializes a bounded minigame contract from:
- `adapter_id`
- validated parameter values
- difficulty band
- objective text
- hints
- reward/failure consequences

#### 6. Asset Generation Pipeline
Generates:
- storyboard images
- narration audio
- subtitle timing/alignment metadata

Owns:
- provider selection
- retries
- quality gates
- fallback chains
- metadata capture

#### 7. Contract Assembler
Combines narrative output, minigame output, and generated assets into a single
turn envelope that Unity can directly consume.

#### 8. Review / Publish Layer
Supports operator workflows:
- inspect generated jobs
- review assets and prompts
- compare fallback usage
- approve/reject
- optionally publish a generated run into a shared curated slice

## Service Responsibilities
- provider orchestration
- prompt template management
- session persistence
- turn history persistence
- continuity/reference resolution
- asset generation and storage
- artifact provenance capture
- job state transitions
- payload versioning
- quality gating
- bounded validation
- approval/publish flows

## Unity Responsibilities
- send API calls
- show generation progress
- download/preload assets
- render cutscenes using returned contract
- execute existing minigame adapters using returned contract
- report outcomes back to the service
- cache ready payloads for short-lived local replay if desired

Unity should **not**:
- hold provider keys
- build prompts
- choose providers
- generate fallback prompts locally
- own session planning rules
- own continuity reference logic
- interpret raw provider output
- infer missing fields from ambiguous state

## Versioned Contract Design

### Principle
Unity should consume one explicit payload shape:

- `PlayableTurnEnvelope`

This envelope should be the only thing Unity needs to play a generated turn.

## Proposed Primary Contract

### `PlayableTurnEnvelope`
- `contract_version`
- `session_id`
- `turn_id`
- `status`
- `generated_at`
- `entry_beat_id`
- `entry_scene_hint`
- `cutscene`
- `minigame`
- `artifacts`
- `continuity`
- `debug`

### `CutsceneContract`
- `beat_id`
- `display_name`
- `subtitle_track`
- `shots[]`
- `music_cue` (optional)
- `next_action_hint`

### `CutsceneShot`
- `shot_id`
- `subtitle_text`
- `narration_text`
- `duration_seconds`
- `image_asset_id`
- `audio_asset_id`
- `camera_hint` (optional, bounded enum)
- `transition_hint` (optional, bounded enum)

### `MinigameContract`
- `beat_id`
- `adapter_id`
- `adapter_version`
- `display_name`
- `objective_text`
- `success_conditions`
- `failure_conditions`
- `parameters`
- `hint_text`
- `difficulty_band`
- `world_state_effects`

### `ArtifactDescriptor`
- `asset_id`
- `asset_type` (`image`, `audio`, `alignment`, `manifest`)
- `uri`
- `mime_type`
- `provider_name`
- `provider_model`
- `fallback_used`
- `checksum`
- `metadata`

### `ContinuityDescriptor`
- `primary_character`
- `reference_asset_ids`
- `style_preset_id`
- `continuity_mode`

## Example API Surface

### Health / Diagnostics
- `GET /v1/health`
- `GET /v1/system/status`

### Session Lifecycle
- `POST /v1/playthrough-sessions`
- `GET /v1/playthrough-sessions/{session_id}`
- `POST /v1/playthrough-sessions/{session_id}/cancel`
- `POST /v1/playthrough-sessions/{session_id}/archive`

### Turn Generation
- `POST /v1/playthrough-sessions/{session_id}/turns`
- `GET /v1/playthrough-sessions/{session_id}/turns/{turn_id}`
- `GET /v1/playthrough-sessions/{session_id}/current-turn`

### Job Progress
- `GET /v1/jobs/{job_id}`
- `GET /v1/jobs/{job_id}/events`

### Outcome Reporting
- `POST /v1/playthrough-sessions/{session_id}/turns/{turn_id}/complete`
- `POST /v1/playthrough-sessions/{session_id}/turns/{turn_id}/fail`

### Artifacts
- `GET /v1/artifacts/{asset_id}`
- `GET /v1/artifacts/{asset_id}/content`

### Reference Assets
- `POST /v1/reference-assets`
- `GET /v1/reference-assets`
- `GET /v1/reference-assets/{reference_id}/content`

### Operator Surface
- `GET /v1/review/jobs`
- `POST /v1/review/jobs/{job_id}/approve`
- `POST /v1/review/jobs/{job_id}/reject`
- `POST /v1/review/jobs/{job_id}/publish`

## Job Model

### Why Jobs
Generation is asynchronous and may outlive the current Unity focus state,
window state, or even current play session. A job model removes that coupling.

### Job States
- `queued`
- `planning`
- `generating_images`
- `generating_audio`
- `assembling_contract`
- `validating`
- `ready`
- `failed`
- `cancelled`

### Job Step Metadata
Each job step should capture:
- status
- started_at
- finished_at
- provider used
- fallback used
- warnings
- errors
- artifact IDs produced

## Minigame Adapter Model

### Key Decision
Each minigame should not be a freeform runtime script generated by the model.
Instead, each minigame is a **registered adapter** with a bounded contract.

### Adapter Registry Example
- `tutorial.plant_rows`
- `tutorial.find_tools`
- `tutorial.chicken_chase`

### Adapter Definition Includes
- adapter ID and version
- supported parameters
- allowed value ranges
- difficulty bands
- world-state requirements
- fallback adapter IDs
- preview text template
- validation rules

### Unity Binding Rule
Unity chooses the adapter implementation by `adapter_id`. The service only
returns validated parameters. Unity never receives arbitrary executable logic
from the service.

## Narrative / Continuity Model

### Service-Owned Continuity
The service should track:
- recent turn summaries
- character speaking history
- active farm/world state
- continuity image ledger
- style preset usage
- recent visual motifs

### Character / Style References
The service should accept uploaded references for:
- character look
- environment mood
- style examples
- costume variants

These references should be stored once and reused by the planner and image
generation layer without Unity needing to manage them directly.

## Fallback Strategy

### Narrative
- Primary: OpenAI structured turn direction and storyboard planner
- Fallback: bounded local rule engine and template storyboard planner

### Images
- Primary: Gemini image generation
- Secondary: another approved image provider
- Tertiary: approved deterministic remix/reference path
- Final: fail closed if quality gates reject all candidates

### Audio
- Primary: ElevenLabs
- Secondary: OpenAI speech
- Final: approved service-side fallback audio path for dev only, not disguised
  as live premium output

### Contract Assembly
If any required field is missing, do not send an ambiguous partial contract to
Unity. Mark the job failed or explicitly degraded.

## Reliability Requirements

### Functional Reliability
- Generation must continue if Unity loses focus.
- Generation must remain resumable if Unity disconnects and reconnects.
- Repeated client requests must be idempotent when requested with the same
  session/job token.
- Ready turns must remain fetchable after generation completes.

### Operational Reliability
- Provider timeouts and retries are service-owned.
- Job progress must be queryable at every point.
- Failed jobs must retain enough detail for diagnosis.
- Artifacts must include provenance.

### UX Reliability
- Unity should never have to guess whether generation is still running.
- Unity should never silently replay stale content when a fresh request failed.
- Unity should be able to resume from a ready turn even after clicking away or
  re-entering play mode.

## Security / Secret Handling

### Requirement
All provider secrets live in the standalone service only.

### Unity
- stores only service base URL and environment routing config
- never stores Gemini, OpenAI, or ElevenLabs keys

### Development Preference
For development, the easiest acceptable setup is:
- service uses `.env.local`
- Unity uses one service URL
- do not commit `.env.local`

## Storage Requirements

### Relational Data
- sessions
- turns
- jobs
- job steps
- minigame contracts
- approval records
- provenance summaries

### Blob / File Data
- images
- audio
- alignment files
- prompt snapshots
- manifest snapshots
- archived published runs

## Observability Requirements
- service logs per request and per job step
- traceable provider request IDs where available
- per-artifact provenance
- explicit fallback usage flags
- operator-readable failure summaries
- health endpoint for Unity and deployment probes

## Unity Integration Contract

### Thin Client SDK Responsibilities
- `CreateSession`
- `StartTurnGeneration`
- `GetJobStatus`
- `GetReadyTurnEnvelope`
- `ReportTurnOutcome`
- `FetchArtifact`

### Suggested Unity Runtime Components
- `GenerativeRuntimeClient`
- `GenerativePlaythroughController`
- `TurnEnvelopeCache`
- `CutsceneContractPlayer`
- `MinigameContractDispatcher`
- `ArtifactPreloader`

### Success Condition
Unity integration should be light enough that replacing the service URL does
not require gameplay code changes.

## Migration Plan

### Phase 0: Contract-First Design
- freeze the `PlayableTurnEnvelope`
- freeze `MinigameContract` and `CutsceneContract`
- define job lifecycle and error model

### Phase 1: Standalone Service Skeleton
- create new standalone service repo/module boundary
- add session store, job store, artifact store
- add health endpoint and auth/dev config

### Phase 2: Unity Thin Client
- replace direct backend orchestration assumptions with API client calls
- move title-screen generation flow to job creation + polling
- keep existing scene adapters, but feed them from the new contract

### Phase 3: First Real Turn End-To-End
- create session
- generate one cutscene + one minigame contract
- fetch artifacts
- play it in Unity

### Phase 4: Continuation Loop
- report minigame outcome
- generate next turn
- persist continuity
- resume later from current session state

### Phase 5: Review / Publish
- operator job dashboard
- artifact provenance review
- approval and curated publish flows

### Phase 6: Hosted Environments
- local dev service
- staging deployment
- production deployment
- environment-specific storage and provider configuration

## Acceptance Criteria

### Architecture
- [ ] Unity no longer owns provider orchestration or provider secret handling.
- [ ] Unity can generate and resume turns through a standalone API only.
- [ ] The service exposes a versioned playable turn contract.

### Reliability
- [ ] Generation continues even if Unity loses focus.
- [ ] Ready turns can be fetched later without replaying generation.
- [ ] Job status is queryable throughout the generation lifecycle.

### Contract Quality
- [ ] A generated turn payload is sufficient for Unity playback without hidden
      local inference.
- [ ] Minigame payloads remain bounded to registered adapters and validated
      parameters.
- [ ] Artifact provenance is stored per generated asset.

### Operational Visibility
- [ ] Operators can inspect jobs, failures, providers, and artifacts.
- [ ] Approved runs can be published into a standing curated slice.

## Risks
- Overdesigning the service contract before enough runtime adapter examples
  exist.
- Allowing the service contract to become too Unity-specific and hard to evolve.
- Underdefining minigame contracts so Unity still needs scene-specific guesswork.
- Treating service-side fallback content as interchangeable with premium content
  without explicit degraded-state signaling.

## Open Questions
- Should turn progress be polling-only first, or do we need SSE/WebSocket event
  streaming immediately?
- Should artifact storage be plain filesystem in V1 or object storage from day
  one?
- Should the first thin-client Unity integration keep using the current runtime
  package override path internally, or move directly to turn-envelope binding?
- Do we want a single unified session/turn endpoint, or separate job endpoints
  plus result endpoints from day one?
- Should curated approved outputs live in the same service storage namespace as
  ephemeral jobs, or in a separate published namespace?

## Recommendation
Proceed with a service-first rebuild.

The key product decision is:

**Unity should become a playback client, not an orchestration host.**

That is the cleanest way to make generation reliable, resumable, inspectable,
and easy to integrate. It also gives us the right long-term boundary for
provider changes, hosted deployment, review tooling, and future client surfaces.
