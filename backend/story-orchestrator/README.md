# Story Orchestrator Backend

Local development scaffold for the Generative Story Orchestrator backend.

Planned V1 stack:

- Python
- FastAPI
- SQLite
- local filesystem artifact storage

This folder is intentionally minimal for now. The first active implementation
work remains inside Unity:

- `StoryPackage` contract
- hand-authored package proof
- Unity import and playback binding

When backend implementation starts, this folder will own:

- package submission endpoints
- job tracking
- artifact storage
- provider workers
- local `.env.local` configuration

Current implemented foundation:

- `POST /api/v1/generated-minigame-beats`
- `POST /api/v1/generated-storyboards/cutscene`
- `POST /api/v1/generated-package-assemblies`
- `POST /api/v1/story-sequence-sessions`
- `GET /api/v1/story-sequence-sessions/{session_id}`
- `POST /api/v1/story-sequence-sessions/{session_id}/next-turn`
- `POST /api/v1/generated-standing-slice-regenerations`
- `POST /api/v1/generated-standing-slice-jobs`
- `GET /api/v1/generated-standing-slice-jobs/{job_id}`
- `POST /api/v1/generated-standing-slice-jobs/{job_id}/review`
- `POST /api/v1/generated-standing-slice-jobs/{job_id}/publish`
- `GET /api/v1/generated-standing-slice-jobs/{job_id}/assets/{asset_id}/content`
- `GET /api/v1/minigame-generators`
- `GET /api/v1/minigame-generators/{generator_id}`
- `POST /api/v1/minigame-generators/{generator_id}/validate`

`POST /api/v1/generated-storyboards/cutscene` can now derive `minigame_goal`
and `crop_name` from a linked materialized minigame beat in the same package
via `linked_minigame_beat_id`.

`POST /api/v1/generated-package-assemblies` materializes a generated minigame
beat first, then generates a linked cutscene beat against the same package in
one request so the operator does not have to coordinate two separate writes.

`POST /api/v1/story-sequence-sessions` creates a persistent autonomous
sequence-planning session with its own stored context and turn history.

`POST /api/v1/story-sequence-sessions/{session_id}/next-turn` selects the next
valid minigame generator, varies bounded parameters, generates the next
minigame + cutscene turn through the existing package assembly service, and
persists the updated session state.

`GET /api/v1/story-sequence-sessions/{session_id}` returns the stored session
plus its generated turn history in order.

Story-sequence session state now also persists a bounded continuity-image
ledger from successful generated cutscenes. When the next turn is planned, the
orchestrator seeds explicit same-character continuity references from that
ledger before falling back to a generic package-wide recent-image sweep.

`POST /api/v1/generated-standing-slice-regenerations` refreshes the standing
`Generative Story Slice` path in one call by running the post-chicken and
pre-farm assembly legs in sequence. If the later leg fails, it restores the
original package manifest so the title-screen sample is not left half-updated.

`POST /api/v1/generated-standing-slice-jobs` persists a standing-slice
regeneration run as a queryable SQLite job with per-step outputs for
`post_chicken_to_farm` and `find_tools_to_pre_farm`. Use
`GET /api/v1/generated-standing-slice-jobs/{job_id}` to fetch the stored run
after submission.

`POST /api/v1/generated-standing-slice-jobs/{job_id}/review` persists operator
review state for a stored job, including `approval_status` and freeform
`review_notes`.

`POST /api/v1/generated-standing-slice-jobs/{job_id}/publish` promotes one
approved archived standing-slice job back into the live story package and
shared generated asset paths. The job record then persists `publish_status`
plus `published_at`.

`GET /api/v1/generated-standing-slice-jobs/{job_id}/assets/{asset_id}/content`
serves one persisted generated image/audio file from the configured storyboard
output root for local review use.

Standing-slice job persistence now archives immutable package/media snapshots
under `GeneratedStoryboards/_job_runs/<job_id>/...` before storing the job
record, so older jobs keep serving the exact reviewed artifacts even after
newer regenerations overwrite the shared live package/beat paths.

Local operator surface:

- `GET /review/standing-slice`

This page can submit a standing-slice job, fetch a stored job by `job_id`, and
inspect the persisted per-step status plus per-asset provenance and content. It
can also mark jobs as pending review, approved, or rejected with saved notes,
publish an approved archived run back into the live standing slice, and it
shows the archived package snapshot path for the stored run.

The minigame-generator catalog currently ships with three V1 definitions:

- `plant_rows_v1`
- `find_tools_cluster_v1`
- `chicken_chase_intro_v1`

## Local run

```bash
cd backend/story-orchestrator
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload
```

## Local test

```bash
cd backend/story-orchestrator
source .venv/bin/activate
python -m unittest discover -s tests
```
