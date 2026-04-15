## GSO-025 — Generative Playthrough Menu Scene

### Goal
Give generated playthroughs a dedicated, durable menu scene where the player can
create new runs, inspect live progress, resume an active run, and select from
previous service-backed playthroughs.

### Problem
- Generated runtime controls are currently bolted onto the title-screen slice
  launcher, which makes the feature feel like a debug panel instead of a real
  product surface.
- Previous generated playthroughs are not exposed as a first-class selectable
  library in Unity even though the backend already tracks them.
- The current UI is too fragile around focus changes and resume: the backend is
  authoritative, but the player has no clear scene dedicated to checking status
  and continuing a ready run.

### Required Product Shape
- Create a dedicated `GenerativePlaythroughMenu` scene in Build Settings.
- The menu scene must show:
  - a primary action to create a new playthrough
  - a primary action to play a ready playthrough
  - a refresh action
  - a way back to the title screen
  - a list of previous playthrough sessions from the runtime tracker API
  - a status/progress tracker for the selected session
- The selected session detail must show the latest turn status and enough
  information to tell whether the service is queued, generating, ready,
  completed, or failed.
- If Unity already has a locally resumable generated session, the menu must
  recover that state on startup instead of making the player restart.

### Runtime Contract
- Unity remains a thin client:
  - previous playthrough history comes from `/api/runtime/v1/tracker/sessions`
  - selected-session detail comes from
    `/api/runtime/v1/tracker/sessions/{session_id}`
  - ready-turn preparation still goes through the runtime session/turn
    endpoints plus artifact preload
- The menu must allow replaying a ready historical turn by preparing it through
  the same `GenerativePlaythroughController` used for fresh runs.
- The menu must keep `Application.runInBackground` enabled so desktop/editor
  generation can continue while the window is unfocused.

### Player-Facing Outcome
- From the title screen, the player can open a dedicated generated-playthrough
  menu scene.
- In that scene, the player can create a new playthrough, watch the pipeline
  advance, select older runs, and play a ready run without relying on a debug
  overlay.
- Returning to the game later restores any active generated session instead of
  losing it.

### Constraints
- Keep the backend tracker API authoritative for history; Unity should not
  duplicate full session history locally.
- Keep scene loading allowlisted through `SceneWorkCatalog`.
- Keep the menu on uGUI + Input System conventions already used in the repo.
- Do not remove the existing generated runtime controller; extend it to prepare
  specific ready turns when selected from history.

### Acceptance Criteria
- [ ] `SceneWorkCatalog` registers `GenerativePlaythroughMenu` as a real
      launchable scene and includes it in title-screen build paths.
- [ ] A `GenerativePlaythroughMenu.unity` scene asset exists and boots a
      dedicated controller for the generated-playthrough menu.
- [ ] Unity can fetch runtime tracker session summaries and a selected session
      detail from the backend.
- [ ] The menu can start a fresh generated playthrough and update its visible
      progress while the job is running.
- [ ] The menu can select a previous ready session and prepare/play its latest
      ready turn.
- [ ] The menu recovers active generated-session state after returning to the
      title or restarting play mode.

### Out of Scope
- Replacing the backend tracker web UI
- Freeform save-slot naming or deletion
- Infinite continuation beyond the current bounded generated session contract
