# Feature Spec: Story Orchestrator Backend Foundation — GSO-002

## Summary
This spec creates the smallest useful backend for the Generative Story
Orchestrator. It does not generate story content yet. It establishes the local
service boundary where story generation jobs will later be submitted, tracked,
and inspected.

V1 of this backend foundation is intentionally narrow:

- local configuration from `.env.local`
- FastAPI service
- SQLite-backed job table
- submit-job endpoint
- get-job endpoint
- health endpoint

This is enough to stop treating the backend as a placeholder folder and start
building a real orchestration seam.

## User Story
As a developer, I want a local backend service that can accept and store story
generation jobs, so that the future planner, image worker, and voice worker can
attach to a stable service boundary instead of being invented ad hoc.

## Acceptance Criteria

### Configuration
- [ ] Backend loads local configuration from `.env.local`.
- [ ] Gemini and ElevenLabs credentials are read from settings, not hardcoded in
      source.
- [ ] The local SQLite database path is configurable.

### API
- [ ] `GET /health` returns a healthy status payload.
- [ ] `POST /api/v1/story-jobs` creates a job with status `pending`.
- [ ] `GET /api/v1/story-jobs/{job_id}` returns the created job.
- [ ] Unknown job IDs return `404`.

### Persistence
- [ ] Jobs are stored in SQLite.
- [ ] Job IDs are unique.
- [ ] Job payload includes the original brief and selected template metadata.
- [ ] Created and updated timestamps are recorded.

### Tests
- [ ] Backend tests cover health, job creation, and job retrieval.
- [ ] Tests use a temporary SQLite path, not the developer’s real local DB.

## Product Intent
This backend is not trying to be smart yet. It is trying to be a dependable
place where future orchestration logic can live.

The first useful backend proof is:

Can the project submit and retrieve a bounded story job through a stable local
API?

## Non-Negotiable Rules
- Local secrets stay in `.env.local`.
- SQLite is sufficient for V1 local development.
- No provider calls are required for this first backend foundation.
- The API surface should stay small and explicit.
- Tests must exist before the backend is considered usable.

## Out of Scope
- actual planner execution
- image generation
- ElevenLabs generation
- retries and worker orchestration
- operator review UI
- publish and rollback

## Done Definition
- Spec exists.
- FastAPI app boots locally.
- Tests exist.
- SQLite-backed job create/get flow works.
