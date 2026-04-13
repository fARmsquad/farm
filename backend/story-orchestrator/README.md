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
