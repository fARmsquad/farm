# Feature Spec: Sequence Session Continuity Memory — GSO-016

## Summary
The Generative Story Orchestrator already supports uploaded character
references and automatic reuse of recent package images, but the current
continuity path is still too blunt for autonomous sequence sessions. The
sequence planner does not remember which generated storyboard images belonged
to the prior turn or the current character, so later turns can inherit broad
package-wide image history instead of the most relevant continuity assets.

This slice makes continuity session-aware. Each generated story-sequence turn
will persist a bounded set of generated storyboard image artifacts in session
state, and the next turn will use those persisted images as explicit Gemini
reference inputs before any generic package sweep logic runs.

## User Story
As a developer, I want autonomous story-sequence sessions to remember the
right generated art from earlier turns, so each new cutscene keeps character
and visual continuity without drifting into unrelated package imagery.

## Acceptance Criteria

### Session Continuity Memory
- [ ] Story-sequence session state persists a bounded set of generated image
      continuity records from successful cutscene turns.
- [ ] Each continuity record carries enough provenance to identify the source
      turn and image asset.
- [ ] `GET /api/v1/story-sequence-sessions/{session_id}` returns the stored
      continuity memory as part of the session state.

### Continuity Selection
- [ ] When a new turn is planned, the cutscene request is seeded with explicit
      continuity reference image paths from the current session state.
- [ ] Same-character continuity images are preferred over generic recent
      images.
- [ ] If no same-character continuity images exist yet, the planner falls back
      to the most recent session continuity images.
- [ ] When explicit continuity reference paths are already present on a
      storyboard request, the generic package-wide recent-image sweep does not
      append extra unrelated generated art.

### Testing
- [ ] A backend test proves story-sequence advancement persists continuity
      image memory into session state.
- [ ] A backend test proves a later turn reuses prior session continuity image
      paths in its generated cutscene request.
- [ ] A backend test proves explicit continuity paths take precedence over the
      generic package-wide recent-image fallback.

## Non-Negotiable Rules
- Do not scan arbitrary package history first when the current session already
  knows which generated images are relevant.
- Do not persist audio files in continuity memory for Gemini image reuse.
- Do not let continuity memory grow without a bounded cap.
- Do not break existing uploaded character-reference behavior.

## Out Of Scope
- Semantic image similarity ranking
- Cross-session global continuity memory
- Operator controls for editing or pinning continuity images
- Changing the Unity runtime bridge in this slice

## Done Definition
- [ ] Spec exists.
- [ ] Story-sequence sessions persist generated image continuity provenance.
- [ ] Planned turns reuse prioritized session continuity image references.
- [ ] Focused backend tests lock the new persistence and precedence behavior.
