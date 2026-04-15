# Feature Spec: Unique Playthrough Generate Then Play Title Slice — GSO-019

## Summary
The standing generated slice currently collapses generation and play into a
single click. That makes the title screen feel opaque because the player cannot
see when a unique run is still being prepared versus when it is actually ready
to enter.

This slice turns the generated title-screen entry into a deliberate two-step
flow:
1. `Generate Unique Playthrough` prepares a live generated runtime package while
   the title screen stays visible.
2. `Play Unique Playthrough` remains disabled until that exact prepared package
   is ready, then launches the generated entry beat on demand.

## User Story
As a developer, I want to generate a unique playthrough first and only enter it
once it is actually ready, so I can trust that the button I press will play the
freshly generated run I just waited for.

## Research Reference
- See `.ai/memory/research-notes.md#research-unique-playthrough-generate-then-play-title-slice`.
- Adopt the existing project-memory guidance for title-screen launchers,
  canonical scene resolution, and explicit async launch-state handling.

## Acceptance Criteria

### Title-Screen Unique Playthrough Slice
- [x] The title screen exposes a `Generate Unique Playthrough` control on the
      standing generated slice surface.
- [x] The title screen exposes a `Play Unique Playthrough` control on the same
      surface.
- [x] `Play Unique Playthrough` starts disabled when no prepared generated run
      exists.
- [x] Clicking `Generate Unique Playthrough` does not leave the title screen.
- [x] While generation is in flight, the generate/play controls do not allow the
      player to start stale content.
- [x] When generation succeeds, the title screen shows a ready state and enables
      `Play Unique Playthrough`.
- [x] Clicking `Play Unique Playthrough` launches the prepared generated entry
      beat from the runtime package that just finished generation.
- [x] If generation fails, the title screen stays active, `Play Unique
      Playthrough` stays disabled, and the status text explains the failure.

### Runtime Sequence Preparation
- [x] The Unity runtime bridge can prepare a generated story-sequence session
      without immediately loading its entry scene.
- [x] The runtime bridge persists the prepared session ID, runtime package, and
      prepared entry scene until the player either launches it or clears state.
- [x] The runtime bridge still supports the existing "generate and load now"
      path for the current standing generated slice behavior where needed.
- [x] Starting an authored scene from the title screen still clears prepared
      generated-sequence state.

### Testing
- [x] An EditMode runtime-bridge test proves sequence preparation succeeds
      without loading a scene and leaves a prepared entry available for later
      play.
- [x] An EditMode title-screen test proves the unique-playthrough slice creates
      separate generate/play buttons with play disabled by default.
- [x] An EditMode title-screen test proves a successful prepare callback enables
      `Play Unique Playthrough` and reports ready status.
- [x] Existing generated-slice launch tests continue to pass.

## Non-Negotiable Rules
- Do not auto-enter the generated scene as part of the generation step for this
  slice.
- Do not let `Play Unique Playthrough` start an old prepared run after a new
  generation request begins.
- Do not clear prepared generated runtime state on the play path before scene
  load.
- Do not fork a second backend contract when the existing story-sequence session
  bridge can be extended to support preparation.

## Out Of Scope
- Multi-slot saved generated runs
- Named run history or archive browsing on the title screen
- Backend-side caching beyond the current session/runtime package contract
- Operator approval workflows or publishing into shared Resources

## Done Definition
- [x] Spec exists.
- [x] Title screen supports generate-first, play-when-ready unique playthroughs.
- [x] Runtime controller supports prepared generated sessions without immediate
      scene load.
- [x] Focused EditMode coverage proves the prepare-and-play flow.
