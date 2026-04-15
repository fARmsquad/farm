# GSO-023: Runtime Progress Tracker

## Summary
Add a lightweight web tracker for generated runtime sessions so the developer
can watch a live experience move through the backend pipeline while Railway is
generating it.

The tracker should feel inspired by Domino's Pizza Tracker:

- bold red/blue progress presentation
- clear current-stage emphasis
- recent sessions list
- per-turn status breakdown
- visible pipeline stages instead of raw logs

The tracker will ship inside the existing `backend/story-orchestrator` service
and deploy with the current Railway app.

## Problem
The runtime service already persists sessions, jobs, turns, job steps, and
artifacts, but there is no operator-facing page that turns that state into an
easy live view.

Current problems:

- backend generation feels opaque while a session is running
- the current runtime job API is too low-level for quick inspection
- there is no live "where is my experience right now?" surface
- provider work can take long enough that users need reassurance and context

## Goal
Ship a deployed operator page that makes runtime generation legible in real
time without adding a second backend or frontend stack.

## Non-Goals
- no auth system in this slice
- no Unity UI changes in this slice
- no WebSocket/SSE transport in this slice
- no new review/publish workflow
- no arbitrary scene editing from the tracker

## UX

### Entry Points
- `GET /review/runtime-tracker`
- the page should auto-load recent sessions
- the page should allow direct session-id lookup

### Tracker Page
The page must show:

1. Recent sessions
2. Selected session summary
3. Current pipeline stage
4. Full pipeline stage rail
5. Turn cards for completed/current turns
6. Artifact thumbnails for generated images
7. Audio/artifact provenance summary
8. Auto-refresh while the session is active

### Visual Direction
- Domino's-inspired red/blue/white palette
- strong horizontal stage rail
- obvious active stage highlight
- no cluttered dashboard-card mosaic
- generated cutscene images should serve as the image surface for the page

## Backend Contract

### New HTML route
- `GET /review/runtime-tracker`

Returns the tracker HTML page.

### New JSON routes
- `GET /api/runtime/v1/tracker/sessions?limit=12`
- `GET /api/runtime/v1/tracker/sessions/{session_id}`

### Tracker detail payload must include
- session id
- session status
- package display name
- beat cursor
- max turns
- active job summary
- active job steps
- last ready turn summary
- all turn summaries for the session
- selected turn artifact summaries needed by the UI

## Runtime Pipeline Behavior
The tracker is only useful if the worker reports meaningful phase changes.

The runtime worker must update persisted job state through these stages:

1. `planning`
2. `generating_images`
3. `generating_audio`
4. `assembling_contract`
5. `validating`
6. `ready` or `failed`

`runtime_job_steps` must reflect the currently running step and completed steps
while a turn is still in progress.

## Implementation Notes
- keep the tracker inside the existing FastAPI service
- serve plain HTML/CSS/JS from `app/static`
- poll existing/new JSON endpoints from the page
- do not introduce a JS framework
- reuse persisted runtime store data instead of duplicating state

## Acceptance Criteria
- A browser can open `/review/runtime-tracker` on Railway.
- The page shows recent runtime sessions.
- The page can load a specific session by id.
- The page shows the current step as a Domino's-style progress rail.
- While a turn is generating, the tracker exposes intermediate stages before
  `ready`.
- The tracker shows completed turns and their configured farm objectives.
- The tracker shows generated image thumbnails from the selected turn.
- The tracker deploys with the existing Railway story-orchestrator service.

## Test Plan
- Backend route test for `/review/runtime-tracker`
- Backend API test for recent tracker sessions
- Backend API test for tracker session detail payload
- Runtime generation test that proves job steps transition through image/audio
  phases before ready
- Local smoke against the tracker page after implementation

## Playtest / Operator Check
1. Open the deployed tracker page.
2. Create a fresh runtime session.
3. Confirm the session appears in the recent list.
4. Confirm the stage rail advances while generation is in flight.
5. Confirm the finished session shows the three farm turns and crop variation.
