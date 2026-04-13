from pathlib import Path

from fastapi import FastAPI, HTTPException

from .config import Settings, get_settings
from .generated_storyboards import (
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardPackageResult,
    GeneratedStoryboardService,
)
from .models import StoryJobCreateRequest, StoryJobRecord
from .store import StoryJobStore


def create_app(
    settings: Settings | None = None,
    generated_storyboard_service: GeneratedStoryboardService | None = None,
) -> FastAPI:
    resolved_settings = settings or get_settings()
    base_dir = Path(__file__).resolve().parents[1]
    database_path = resolved_settings.resolve_database_path(base_dir)
    store = StoryJobStore(database_path)
    storyboard_service = generated_storyboard_service or GeneratedStoryboardService.from_settings(
        resolved_settings,
        base_dir,
    )

    app = FastAPI(title="Story Orchestrator", version="0.1.0")
    app.state.settings = resolved_settings
    app.state.store = store
    app.state.generated_storyboard_service = storyboard_service

    @app.get("/health")
    def health() -> dict[str, str]:
        return {"status": "ok"}

    @app.post("/api/v1/story-jobs", response_model=StoryJobRecord, status_code=201)
    def create_story_job(request: StoryJobCreateRequest) -> StoryJobRecord:
        return store.create_job(request)

    @app.get("/api/v1/story-jobs/{job_id}", response_model=StoryJobRecord)
    def get_story_job(job_id: str) -> StoryJobRecord:
        record = store.get_job(job_id)
        if record is None:
            raise HTTPException(status_code=404, detail="Story job not found.")
        return record

    @app.post(
        "/api/v1/generated-storyboards/cutscene",
        response_model=GeneratedStoryboardPackageResult,
        status_code=201,
    )
    def create_generated_storyboard_cutscene(
        request: GeneratedStoryboardCutsceneRequest,
    ) -> GeneratedStoryboardPackageResult:
        return storyboard_service.create_package(request)

    return app


app = create_app()
