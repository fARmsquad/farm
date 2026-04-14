# Generative Story Orchestrator — Implementation Path

Date: 2026-04-13
Status: Draft for execution planning
Owner: Youssef
Related doc: `docs/plans/2026-04-13-generative-story-orchestrator-prd.md`

## 1. Executive Recommendation

Build this initiative in one strict order:

1. Contract-first
2. Consumer-first
3. Generator-second
4. Ops and scale last

That means:

- define the `StoryPackage` contract before building AI generation
- prove Unity can import and play a hand-authored package before calling Gemini or ElevenLabs
- treat each minigame as a backend adapter contract before asking the planner to compose them
- add provider workers only after the package contract, validation rules, and playback path are stable

This is the safest implementation path for FarmSim VR because the biggest project risk is not model access. It is whether generated content can be expressed as reliable, reviewable, Quest-safe game data that fits the current Unity architecture.

## 2. Why This Order Is Correct

The repo already has the right ingredients for a consumer-driven architecture:

- `SceneWorkCatalog` already models ordered scene chains
- `CinematicSequencer` already consumes registry-based sequence data
- `CropLifecycleProfile` already models minigame definitions and gameplay parameters
- `WorldPenGameCatalog` already uses catalog-driven content patterns

The missing piece is a stable package contract that sits between generation and playback.

If the team starts with AI generation first, the likely failure mode is obvious:

- output shape keeps changing
- Unity bindings churn
- minigame assumptions drift
- QA becomes manual guesswork
- model retries and provider failures leak into game runtime design

If the team starts with package playback and validation first, the failure modes get cheaper:

- a hand-authored package can expose gaps early
- importer and validation bugs are deterministic
- minigame adapter limits become explicit
- generated outputs can be judged against a stable schema

## 3. Delivery Assumptions

This roadmap assumes:

- solo developer execution
- Unity repo remains the game client repo
- backend services will likely live outside the Unity project as a separate service
- V1 is a reviewed backstage pipeline, not a player-facing live AI feature
- one vertical slice matters more than broad feature coverage
- human approval remains required before publish

Practical expectation:

- full-time solo: 12-16 weeks for a credible V1 vertical slice
- mixed with normal game work: 16-24 weeks is more realistic

## 4. Product Build Strategy

The implementation program should be split into six workstreams.

### Workstream A: Contracts and Content Model

Owns:

- `StoryPackage` schema
- `CharacterModule`
- `NarrativeWorldState`
- `MinigameCatalog`
- `ReferenceAssetLibrary`
- `VoiceProfileLibrary`
- validation rules

### Workstream B: Unity Consumer and Playback

Owns:

- package importer
- schema validation mirror in Unity
- cutscene mapping into current systems
- minigame config application
- preview scene and debug playback

### Workstream C: Backend Platform

Owns:

- job submission
- durable step execution
- artifact storage
- asset registry
- audit logging
- secrets and provider abstraction

### Workstream D: Planning and Assembly

Owns:

- story planner
- continuity resolver
- package assembler
- fallback selection logic

### Workstream E: Media Workers

Owns:

- Gemini image worker
- dialogue worker
- ElevenLabs voice worker
- sound effects worker

### Workstream F: Review, QA, and Publish

Owns:

- operator review flow
- moderation gates
- package diffing
- publish status
- rollback and traceability

## 5. Dependency Order

Not all work can start at once.

### Hard dependencies

- Workstream A blocks every other stream.
- Workstream B must prove playback before Workstream E is allowed to shape outputs.
- Workstream C must exist before Workstream D and E can run reliably.
- Workstream D depends on A and C.
- Workstream E depends on A, C, and D.
- Workstream F depends on B, C, D, and E.

### Safe parallelism

After the contract is stable:

- Workstream B and C can proceed in parallel.
- Content modeling inside A can continue while B and C stand up.
- Once B and C are stable, D and the first slice of E can proceed in parallel.

## 6. Critical Proof Points

Before scaling the system, the project must prove five things in order.

### Proof 1: Unity can play a package cleanly

Question:

Can a hand-authored package trigger one intro-like sequence with a cutscene beat, a minigame beat, and a return beat?

If no, stop. Fix consumer architecture before building generators.

### Proof 2: Minigames can be represented as bounded adapters

Question:

Can the existing minigames be described by stable capability tags, validation rules, and parameter bounds?

If no, do not build a planner yet.

### Proof 3: Planner outputs are feasible

Question:

Can the planner produce beat JSON that always maps to real characters, references, and minigame adapters without operator cleanup every time?

If no, tighten schemas and planning constraints.

### Proof 4: Generated media can survive QA

Question:

Can generated images, dialogue, and audio stay on-model, pass moderation, and assemble into a publishable package at an acceptable manual edit rate?

If no, the system is still a drafting aid, not a content pipeline.

### Proof 5: Published packages stay stable on Quest

Question:

Can approved packages play without runtime regressions, asset overruns, or fragile network dependencies?

If no, reduce package complexity before scaling volume.

## 7. Recommended Architecture Path

### 7.1 Backend Shape

Do not put the orchestration engine inside Unity.

Recommended V1 shape:

- external backend service
- relational database for metadata and job state
- object storage for generated assets and package payloads
- explicit step-based job runner
- REST endpoints for submit, status, approve, publish, and fetch

### 7.2 Orchestration Choice

Recommended decision:

Start with a simple explicit job-state machine, not LangGraph, and not full Temporal on day one.

Why:

- the workflow is mostly deterministic and stage-based
- the team is solo
- the product needs durable retries and step visibility more than agent autonomy
- a clear job table and step runner are easier to debug at V1

How to future-proof it:

- define each pipeline step as an isolated worker contract
- persist step input and output payloads
- keep state transitions explicit
- keep the orchestration model compatible with later migration to Temporal if scale demands it

Do not start with LangGraph unless the team later needs branching agent behavior that cannot be expressed as explicit stages. That is not the current problem.

### 7.3 Unity Integration Model

Unity should consume package artifacts like content, not conversations.

Recommended approach:

- package manifest plus referenced assets
- local validation before playback
- import into staging cache
- scene and sequence binding through existing registries and catalogs
- explicit fallback handling when optional assets are missing

Unity should never:

- call Gemini directly for V1 content generation
- own fallback retries across providers
- depend on live network generation during normal episode playback

## 8. Milestone Roadmap

### Milestone 0: Contract and Playback Proof

Objective:

Prove that the game can consume a `StoryPackage` before any generative pipeline exists.

Duration:

1-2 weeks

Deliverables:

- `StoryPackage` schema v0
- beat model v0
- package validation rules
- hand-authored sample package
- Unity importer
- preview/debug playback path

Acceptance gate:

- one hand-authored package plays an intro-like flow in Unity
- flow contains at least one cutscene beat, one minigame beat, and one transition back out
- no provider calls required
- playback survives missing optional assets through defined fallbacks

Why it matters:

This is the single most important milestone. If this fails, the rest of the roadmap is waste.

### Milestone 1: Catalog and Adapter Foundation

Objective:

Turn story concepts and gameplay units into structured modules the planner can reason about.

Duration:

1-2 weeks

Deliverables:

- `CharacterModule` schema
- `NarrativeWorldState` schema
- `MinigameCatalog` schema
- `MinigameGeneratorDefinition` schema
- `ReferenceAssetLibrary` schema
- `VoiceProfileLibrary` schema
- first 2-3 minigame adapters

Acceptance gate:

- every supported minigame has capability tags, config schema, validation rules, tuning bounds, and at least one generator definition
- every supported character has approved references and continuity facts
- planner inputs can be validated before generation begins

### Milestone 2: Backend Foundation

Objective:

Stand up the non-Unity delivery backbone.

Duration:

2-3 weeks

Deliverables:

- backend service skeleton
- database schema for jobs, packages, assets, versions, approvals
- artifact storage layout
- submit/status APIs
- step execution engine
- audit logging
- provider credential handling

Acceptance gate:

- a fake pipeline can submit a job, step through mocked stages, store artifacts, and expose statuses end to end
- operator can inspect job history and outputs without opening Unity

### Milestone 3: Planner and Package Assembly

Objective:

Produce feasible package drafts from structured inputs.

Duration:

2-3 weeks

Deliverables:

- continuity resolver
- planner prompts and structured output contracts
- feasibility validator against minigame catalog
- package assembler
- fallback candidate generation

Acceptance gate:

- planner can generate beat JSON for one vertical slice using only supported inputs
- invalid minigame or asset requests are rejected before media generation
- assembler outputs a package Unity can import

### Milestone 4: Media Generation Vertical Slice

Objective:

Add production media workers behind the stable package contract.

Duration:

3-4 weeks

Deliverables:

- Gemini reference-image worker
- dialogue writer worker
- ElevenLabs voice/dialogue worker
- sound effect worker
- per-step retries
- provenance logging
- first moderation pass

Acceptance gate:

- one generated package can produce image, dialogue, voice, and supporting audio assets
- failed steps can be retried without restarting the full job
- fallback paths work when any single provider step fails

### Milestone 5: Review and Publish Beta

Objective:

Make the pipeline operational for actual episode approval.

Duration:

2-3 weeks

Deliverables:

- operator review screen
- regenerate-single-step action
- approve/reject workflow
- package diff view
- publish controls
- Unity staging ingestion flow

Acceptance gate:

- an operator can review a generated package, regenerate one failed asset, approve it, publish it, and verify it in Unity staging

### Milestone 6: Hardening and Repeatability

Objective:

Turn a one-off success into a repeatable content operation.

Duration:

2-4 weeks

Deliverables:

- stronger moderation rules
- package rollback flow
- cost and latency tracking
- analytics for package success rate
- localization hooks
- additional minigame adapters and characters

Acceptance gate:

- the system can ship multiple packages without custom engineering every time
- package quality and cost are measurable
- failures are diagnosable from logs and artifacts

## 9. Story Map and Dependency Order

The first implementation stories should not be provider integrations. They should be contract and playback stories.

### Epic A: Package Contract and Unity Consumer

#### GSO-001: Define `StoryPackage` schema and validation rules

Goal:

Establish the canonical payload shape used by both backend and Unity.

Acceptance:

- package includes metadata, beats, media refs, dialogue refs, minigame configs, QA state, and publish state
- schema is versioned
- invalid packages fail validation with clear errors

#### GSO-002: Build a hand-authored sample package

Goal:

Create one package that represents the intended vertical slice without AI.

Acceptance:

- sample package covers one simple episode path
- includes cutscene beat, minigame beat, and outro beat
- includes fallback examples for missing optional media

#### GSO-003: Build Unity importer and playback harness

Goal:

Prove package playback against current Unity architecture.

Acceptance:

- importer loads package manifest
- importer binds supported beats to current systems
- unsupported beat types fail loudly, not silently
- debug playback path exists in editor

#### GSO-004: Bind one minigame adapter into package playback

Goal:

Prove gameplay configuration can be driven by package data.

Acceptance:

- selected minigame reads config from package data
- invalid configs are rejected before runtime
- fallback minigame substitution path exists

### Epic B: Content Modeling

#### GSO-005: Define `CharacterModule`

Acceptance:

- identity, continuity, voice profile, approved references, exclusions, and relationship hooks are structured

#### GSO-006: Define `NarrativeWorldState`

Acceptance:

- season, location, unlocked systems, crop state, and prior story dependencies can be represented without prompt prose

#### GSO-007: Define `MinigameCatalog`

Acceptance:

- each supported minigame exposes capability tags, parameter bounds, preview data, fallback substitutions, and one or more generator definitions

#### GSO-007a: Define `MinigameGeneratorDefinition`

Acceptance:

- the planner selects a generator ID plus bounded parameters instead of inventing raw minigame config
- each generator declares defaults, allowed ranges, coupling rules, and fallback generators
- at least one planting generator and one fetch/search-style generator are represented in schema

#### GSO-007b: Build generator materialization into package minigame beats

Acceptance:

- a chosen generator ID plus bounded parameters can be validated and materialized into a `Kind=Minigame` package beat
- invalid selections return structured errors and fallback IDs without crashing the caller
- successful materialization writes deterministic `AdapterId`, `ObjectiveText`, `RequiredCount`, `TimeLimitSeconds`, and resolved generator metadata into package JSON

#### Immediate Next Slice: Build one-call intro package assembly

Recommended next step:

- accept one request that contains:
  - minigame generator selection
  - cutscene scene metadata
  - character/storyboard context
- materialize the target minigame beat first
- generate the bridge cutscene second using the linked minigame beat
- return one package result that contains both beats

The point of this slice is to remove the current two-call orchestration burden
from the operator path and make the intro package flow feel like one backend
operation instead of two manually coordinated writes.

#### GSO-008: Define reference and voice libraries

Acceptance:

- visual and voice references can be attached by ID, not free text

### Epic C: Backend Foundation

#### GSO-009: Stand up job and artifact model

Acceptance:

- jobs, steps, outputs, and approvals are persisted and queryable

#### GSO-010: Build submission and status endpoints

Acceptance:

- operator or tool can submit a brief, poll status, and inspect stage outputs

#### GSO-011: Build audit logging and provenance capture

Acceptance:

- every provider call and artifact is tied to package version, seed, and source inputs

### Epic D: Planning and Assembly

#### GSO-012: Build continuity resolver

Acceptance:

- planner receives bounded continuity data, not raw historical dumps

#### GSO-013: Build planner structured output

Acceptance:

- planner emits valid beat JSON against the approved schema
- invalid output is rejected and retried with tighter constraints

#### GSO-014: Build feasibility validator

Acceptance:

- impossible minigame requests and unsupported characters are rejected before media generation

#### GSO-015: Build package assembler

Acceptance:

- planner output and worker artifacts are assembled into a versioned package Unity can import

### Epic E: Media Workers

#### GSO-016: Build Gemini image worker

Acceptance:

- worker accepts approved references and shot requests
- worker stores prompt, seed, references, and output metadata
- failed image requests can be retried per shot

#### GSO-017: Build dialogue and subtitle worker

Acceptance:

- worker emits subtitle-safe dialogue with speaker mapping and beat linkage

#### GSO-018: Build ElevenLabs voice worker

Acceptance:

- worker can render either multi-speaker scene audio or line-by-line fallback

#### GSO-019: Build SFX worker

Acceptance:

- short scene-supporting effects can be generated and attached to beats

### Epic F: Review and Publish

#### GSO-020: Build operator review flow

Acceptance:

- operator can inspect package-level and beat-level outputs before publish

#### GSO-021: Build regenerate-step flow

Acceptance:

- operator can rerun a single failed or weak step without invalidating the whole package

#### GSO-022: Build publish and rollback flow

Acceptance:

- approved packages can be staged, published, and rolled back by version

## 10. Recommended First Vertical Slice

Do not start with broad personalization.

Start with one tightly bounded episode:

- one approved character
- one approved environment context
- one supported minigame adapter
- one crop or story objective
- one intro-like sequence with 3-5 beats

Recommended slice:

- use one currently supported, easy-to-bind gameplay unit from the existing project
- prefer the simplest existing launchable path rather than inventing a new mechanic
- keep generated visuals as storyboard panels or shot stills, not full motion
- require fallback assets for every beat

The goal of the first slice is not content variety. It is proof that:

- structure survives planning
- assets survive assembly
- package survives Unity playback

## 11. What Must Be Built Before Parallel Work Starts

These are the initiative gates.

### Gate A: Contract lock

Required before any serious implementation:

- package schema
- beat taxonomy
- minigame adapter schema
- asset reference conventions

### Gate B: Playback lock

Required before provider integration:

- Unity importer
- preview harness
- one hand-authored package that actually runs

### Gate C: Catalog lock

Required before planner scaling:

- first supported characters
- first supported minigames
- approved references and voice profiles

Without those three gates, parallel work will create churn instead of progress.

## 12. What Not To Build Yet

Do not spend V1 time on:

- live player-facing chat
- generated 3D animation
- full motion cutscenes
- open-ended story sandboxing
- dynamic provider selection inside Unity
- broad personalized story generation for many crops and characters
- elaborate creator tools before the review workflow works

## 13. Delivery Risks

### Risk: Unity playback complexity is underestimated

Response:

- force hand-authored package playback first
- keep beat types narrow in V1

### Risk: planner produces too much invalid output

Response:

- tighten schemas
- validate against catalogs before media generation
- reduce planner freedom

### Risk: generated art is off-model

Response:

- approved-reference-only generation
- panel-based visuals first
- stronger fallback use of canonical art

### Risk: audio workflow becomes brittle

Response:

- support line-by-line TTS fallback from day one
- make subtitle-only publish an allowed degraded mode for internal testing

### Risk: this becomes an AI platform project instead of a content pipeline

Response:

- keep the first milestone anchored to one playable episode
- reject features that do not help that milestone

## 14. Suggested 90-Day Plan

If execution starts now, the first 90 days should look like this.

### Days 1-14

- lock `StoryPackage` contract
- lock beat taxonomy
- build hand-authored sample package
- build Unity importer and playback harness
- prove one package path plays

### Days 15-30

- define character, world state, and minigame catalog schemas
- define first adapters and fallback substitutions
- define reference and voice libraries
- build validation tooling around supported inputs

### Days 31-50

- stand up backend job model
- stand up artifact storage and audit logging
- add submit/status APIs
- run mocked pipeline through all stages

### Days 51-70

- build continuity resolver
- build planner structured output
- build package assembler
- validate planner feasibility before media generation

### Days 71-90

- add Gemini image worker
- add dialogue and ElevenLabs workers
- add step retries and provenance capture
- review one generated package in a staging flow

That 90-day plan should aim for one generated package in staging, not broad content scale.

## 15. Release Criteria

### Internal Alpha

Ship when:

- one hand-authored package plays cleanly
- one generated package assembles successfully
- operator can inspect outputs per stage

### Internal Beta

Ship when:

- package generation works for one bounded episode template
- single-step regeneration works
- publish to Unity staging works
- package quality is stable enough to evaluate manually

### V1

Ship when:

- at least 2-3 supported characters exist
- at least 2-3 minigame adapters exist
- fallback paths are proven
- moderation and provenance are on
- Quest playback is stable
- content cost and failure rates are measurable

## 16. Final Recommendation

The correct implementation path is:

1. Build the package contract.
2. Prove Unity playback with a hand-authored package.
3. Model characters, world state, references, and minigames as strict catalogs.
4. Stand up a simple durable backend job system.
5. Add the planner and package assembler.
6. Add Gemini and ElevenLabs workers behind that contract.
7. Add review, publish, rollback, and analytics.

The recommended first implementation story is:

`GSO-001/GSO-002/GSO-003` as one initial work packet:

- define `StoryPackage`
- build one hand-authored sample package
- import and play it in Unity

That is the highest-leverage starting point because it proves the core system boundary before the project spends time on AI generation.
