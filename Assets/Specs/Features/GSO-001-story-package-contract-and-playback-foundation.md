# Feature Spec: Story Package Contract and Playback Foundation — GSO-001

## Summary
This spec establishes the first executable foundation for the Generative Story
Orchestrator initiative. The goal is not to generate content yet. The goal is
to prove that FarmSim VR can consume a structured `StoryPackage`, bind the
current scene to a beat, build a playable cutscene sequence when needed, and
read the next-scene handoff deterministically.

The first proof package is intentionally small:

`Intro -> ChickenGame -> Tutorial_PostChickenCutscene`

This is a hand-authored package, not an AI-produced one. If this package cannot
be imported, validated, and consumed cleanly by Unity, the rest of the
initiative should not proceed.

## User Story
As a developer, I want a versioned story package contract plus a Unity-side
consumer for that contract, so that generated narrative content can eventually
arrive as data instead of bespoke scene glue.

## Acceptance Criteria

### Contract
- [ ] A pure C# `StoryPackage` contract exists in `Core/` with zero
      `UnityEngine` references.
- [ ] The contract is versioned and includes:
      `packageId`, `schemaVersion`, `packageVersion`, `displayName`, and
      ordered `beats`.
- [ ] Each beat includes:
      `beatId`, `kind`, `sceneName`, `nextSceneName`, and either cutscene
      steps or minigame config.
- [ ] A validator rejects malformed packages with readable error messages.
- [ ] Duplicate beat IDs are rejected.
- [ ] A cutscene beat without sequence steps is rejected.
- [ ] A minigame beat without an adapter ID is rejected.

### Unity Consumption
- [ ] A Unity-side importer can parse a hand-authored package from a `TextAsset`.
- [ ] A Unity-side scene binding can resolve the current beat by scene name.
- [ ] A cutscene beat can build a `CinematicSequence` instance at runtime.
- [ ] The built sequence validates against `CinematicSequencer.Validate`.
- [ ] The scene binding exposes the next scene handoff without additional custom
      scene logic.

### Sample Package
- [ ] A hand-authored sample package exists in-project.
- [ ] The sample package models:
      `Intro -> ChickenGame -> Tutorial_PostChickenCutscene`.
- [ ] The intro and post-chicken beats are cutscene beats.
- [ ] The chicken scene is represented as a minigame beat with a bounded
      adapter configuration.

## Product Intent
This feature defines the core boundary between generation and gameplay.

The package contract must be:
- strict enough to validate
- simple enough to author by hand
- flexible enough to later accept planner output
- decoupled from live provider calls

This feature does **not** try to solve:
- AI planning
- image generation
- VO generation
- operator review tooling
- backend orchestration

It proves only one thing:

Can Unity consume a stable story package as data?

## Non-Negotiable Rules
- `Core/` must remain free of `UnityEngine`.
- The first package is hand-authored.
- The first supported beat kinds are only `Cutscene` and `Minigame`.
- The first cutscene path must use existing `CinematicSequence` /
  `CinematicSequencer` infrastructure.
- The first minigame path must describe an existing gameplay beat, not invent a
  new mechanic.
- Validation errors must fail loudly and read clearly.

## Contract Shape

### StoryPackage
- `PackageId`
- `SchemaVersion`
- `PackageVersion`
- `DisplayName`
- `Beats[]`

### StoryBeat
- `BeatId`
- `DisplayName`
- `Kind`
- `SceneName`
- `NextSceneName`
- `SequenceSteps[]`
- `Minigame`

### StorySequenceStep
- `StepType`
- `StringParam`
- `FloatParam`
- `IntParam`
- `Duration`
- `WaitForCompletion`

### StoryMinigameConfig
- `AdapterId`
- `ObjectiveText`
- `RequiredCount`
- `TimeLimitSeconds`

## First Sample Package

### Beat 1: Intro Opening
- **Scene**: `Intro`
- **Kind**: `Cutscene`
- **Intent**: Set the mission context and hand off into the first gameplay beat.
- **Next Scene**: `ChickenGame`

### Beat 2: Chicken Chase
- **Scene**: `ChickenGame`
- **Kind**: `Minigame`
- **Intent**: Represent the first interactive tutorial beat as a bounded adapter.
- **Adapter ID**: `tutorial.chicken_chase`
- **Next Scene**: `Tutorial_PostChickenCutscene`

### Beat 3: Post-Chicken Bridge
- **Scene**: `Tutorial_PostChickenCutscene`
- **Kind**: `Cutscene`
- **Intent**: Confirm success and end the first proof package cleanly.
- **Next Scene**: none

## Technical Plan

### Core
- `StoryBeatKind`
- `StoryPackageSnapshot`
- `StoryBeatSnapshot`
- `StorySequenceStepSnapshot`
- `StoryMinigameConfigSnapshot`
- `StoryPackageContract`
- `StoryPackageNavigator`
- `StoryPackageValidationResult`

### MonoBehaviour Layer
- `StoryPackageImporter`
- `StoryPackageSequenceBuilder`
- `StoryPackageSceneBinding`

### Data
- `Assets/_Project/Data/StoryPackage_IntroChickenSample.json`

### Tests
- EditMode tests for validation, import, scene binding, and cutscene sequence
  building.

## Out of Scope
- Backend job orchestration
- Gemini worker
- ElevenLabs worker
- asset review tooling
- publish/rollback flow
- broad personalization

## Done Definition
- Spec exists.
- EditMode tests exist and pass.
- Unity can import the sample package and bind the `Intro` scene.
- A valid `CinematicSequence` can be built from the intro beat.
- The sample package exposes a deterministic next-scene handoff for all beats.
