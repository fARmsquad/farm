# Feature Spec: Generated Playthrough Title Diagnostics Surface — GSO-020

## Summary
The generated title-screen slice now separates preparation from play, but the
player still has limited visibility into what the orchestrator actually
produced. This slice adds an always-visible diagnostics block on the title
surface so the developer can inspect what is happening during generation and
what exact package/beat is ready before pressing play.

## User Story
As a developer, I want the generated title-screen slice to show live generation
state and prepared run details, so I can tell whether the backend is still
working, what was generated, and whether the next playthrough is the one I
expect to test.

## Research Reference
- See `.ai/memory/research-notes.md#research-generated-playthrough-title-diagnostics-surface`.
- Apply existing project-memory guidance for visible async title-screen state,
  fail-closed generated slices, and canonical scene resolution.
- Cross-check `.ai/memory/completion-learnings.md` entries about generated
  title-screen flows needing obvious loading/availability state.

## Acceptance Criteria

### Diagnostics Surface
- [x] The standing generated slice shows a multiline diagnostics block on the
      title screen without leaving the menu.
- [x] The diagnostics block starts in an explicit idle state that makes it clear
      no generated run is currently prepared.
- [x] Clicking `Generate Unique Playthrough` updates the diagnostics block to an
      in-flight generating state immediately.
- [x] When a generated run is prepared, the diagnostics block shows the prepared
      session and entry-scene identity.
- [x] When a generated run is prepared, the diagnostics block also shows the
      generated package/beat summary for that prepared entry beat.
- [x] If generation fails, the diagnostics block shows a failed state and the
      last error.
- [x] Launching an authored title-screen scene clears generated diagnostics back
      to idle so stale generated details are not left behind.

### Testing
- [x] An EditMode title-screen test proves the diagnostics block exists and
      starts in idle state.
- [x] An EditMode title-screen test proves generation immediately switches the
      diagnostics block to a generating state.
- [x] An EditMode title-screen test proves a prepared callback populates the
      diagnostics block with generated package/beat details.

## Non-Negotiable Rules
- Do not present authored fallback package details as live generated output.
- Do not weaken the existing blocking loading state while adding diagnostics.
- Do not leave stale generated diagnostics visible after an authored launch
  clears runtime state.

## Out Of Scope
- A separate dashboard scene in Unity
- Thumbnail/image preview rendering on the title screen
- Backend-side polling of standing-slice job records
- Persistent storage of title diagnostics across app restarts

## Done Definition
- [x] Spec exists.
- [x] The title screen shows useful generated-run diagnostics during and after
      preparation.
- [x] Focused EditMode coverage proves idle, generating, and prepared states.
