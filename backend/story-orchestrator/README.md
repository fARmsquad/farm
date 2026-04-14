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
- `POST /api/v1/generated-standing-slice-regenerations`
- `POST /api/v1/generated-standing-slice-jobs`
- `GET /api/v1/generated-standing-slice-jobs/{job_id}`
- `GET /api/v1/minigame-generators`
- `GET /api/v1/minigame-generators/{generator_id}`
- `POST /api/v1/minigame-generators/{generator_id}/validate`

`POST /api/v1/generated-storyboards/cutscene` can now derive `minigame_goal`
and `crop_name` from a linked materialized minigame beat in the same package
via `linked_minigame_beat_id`.

`POST /api/v1/generated-package-assemblies` materializes a generated minigame
beat first, then generates a linked cutscene beat against the same package in
one request so the operator does not have to coordinate two separate writes.

`POST /api/v1/generated-standing-slice-regenerations` refreshes the standing
`Generative Story Slice` path in one call by running the post-chicken and
pre-farm assembly legs in sequence. If the later leg fails, it restores the
original package manifest so the title-screen sample is not left half-updated.

`POST /api/v1/generated-standing-slice-jobs` persists a standing-slice
regeneration run as a queryable SQLite job with per-step outputs for
`post_chicken_to_farm` and `find_tools_to_pre_farm`. Use
`GET /api/v1/generated-standing-slice-jobs/{job_id}` to fetch the stored run
after submission.

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
