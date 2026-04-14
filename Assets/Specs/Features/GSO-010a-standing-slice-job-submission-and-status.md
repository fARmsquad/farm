# Feature Spec: Standing Slice Job Submission And Status — GSO-010a

## Summary
The backend can now regenerate the standing `Generative Story Slice` in one
request, but that run is still ephemeral. Once the request ends, there is no
durable record of:
- what request was submitted
- which step succeeded or failed
- what package path was produced
- which result should be reviewed later

This slice adds a small durable job layer for the standing slice only. It lets
the operator submit a standing-slice regeneration job, stores the job and its
two assembly-step outputs in SQLite, and exposes a status endpoint that returns
the persisted record after the run completes.

The job still executes synchronously for now. The goal is durable visibility,
not distributed execution.

## User Story
As a developer, I want standing-slice regeneration runs to be persisted as
queryable jobs with per-step outputs, so I can inspect what happened after a
run without depending on transient logs or rerunning the whole slice.

## Acceptance Criteria

### Submission Contract
- [ ] A typed standing-slice job submission endpoint exists.
- [ ] The submission body reuses the existing `GeneratedStandingSliceRequest`.
- [ ] Submitting a job returns a persisted job record with a stable `job_id`.

### Persistence
- [ ] SQLite persists one job row for each standing-slice run.
- [ ] SQLite persists two step rows per job:
      - `post_chicken_to_farm`
      - `find_tools_to_pre_farm`
- [ ] The persisted record includes:
      - request payload
      - job status
      - approval status placeholder
      - top-level result payload
      - per-step status
      - per-step output payload
      - error lists

### Status Behavior
- [ ] A fetch endpoint returns a persisted job by `job_id`.
- [ ] Successful runs return `completed` status and completed steps.
- [ ] Runs with a failed second leg return `failed` status, a completed first
      step, and a failed second step.
- [ ] Runs with a failed first leg do not mark the second step as completed.

### Standing Slice Integration
- [ ] Job execution reuses the existing `GeneratedStandingSliceService`.
- [ ] The durable job layer does not duplicate package assembly logic.
- [ ] The standing title-screen slice remains backed by the same standing
      package path.

## Non-Negotiable Rules
- Do not introduce a queue or distributed worker system in this slice.
- Do not store only a top-level opaque result blob; per-step outputs must be
  queryable.
- Do not replace the existing direct regeneration endpoint; this job endpoint
  sits alongside it.

## Out of Scope
- Retry queues or long-running background execution
- Human approval workflow actions beyond a stored placeholder status
- Provider-call audit trails
- Asset-level lineage storage

## Done Definition
- [ ] Spec exists.
- [ ] Standing-slice jobs and steps persist in SQLite.
- [ ] Submit and fetch endpoints return typed job records.
- [ ] Focused tests cover success, failed later step, and fetch behavior.
