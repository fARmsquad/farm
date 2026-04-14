from pathlib import Path

import httpx
from fastapi import FastAPI, HTTPException

from .config import Settings, get_settings
from .generated_storyboards import (
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardPackageResult,
    GeneratedStoryboardService,
)
from .generated_minigames import (
    GeneratedMinigameBeatRequest,
    GeneratedMinigameBeatResult,
    GeneratedMinigameBeatService,
)
from .generated_package_assembly import (
    GeneratedPackageAssemblyRequest,
    GeneratedPackageAssemblyResult,
    GeneratedPackageAssemblyService,
)
from .generated_standing_slice import (
    GeneratedStandingSliceRequest,
    GeneratedStandingSliceResult,
    GeneratedStandingSliceService,
)
from .generated_standing_slice_jobs import GeneratedStandingSliceJobService
from .minigame_generator_models import (
    MinigameGeneratorDefinition,
    MinigameGeneratorValidationRequest,
    MinigameGeneratorValidationResult,
)
from .minigame_generators import MinigameGeneratorCatalog
from .models import (
    ElevenLabsSingleUseTokenResponse,
    GeneratedStandingSliceJobRecord,
    StoryJobCreateRequest,
    StoryJobRecord,
)
from .store import GeneratedStandingSliceJobStore, StoryJobStore


def create_app(
    settings: Settings | None = None,
    generated_storyboard_service: GeneratedStoryboardService | None = None,
    generated_minigame_service: GeneratedMinigameBeatService | None = None,
    generated_package_assembly_service: GeneratedPackageAssemblyService | None = None,
    generated_standing_slice_service: GeneratedStandingSliceService | None = None,
    generated_standing_slice_job_store: GeneratedStandingSliceJobStore | None = None,
    generated_standing_slice_job_service: GeneratedStandingSliceJobService | None = None,
) -> FastAPI:
    resolved_settings = settings or get_settings()
    base_dir = Path(__file__).resolve().parents[1]
    database_path = resolved_settings.resolve_database_path(base_dir)
    store = StoryJobStore(database_path)
    standing_slice_job_store = generated_standing_slice_job_store or GeneratedStandingSliceJobStore(database_path)
    storyboard_service = generated_storyboard_service or GeneratedStoryboardService.from_settings(
        resolved_settings,
        base_dir,
    )
    minigame_service = generated_minigame_service or GeneratedMinigameBeatService(
        package_output_path=resolved_settings.resolve_path(base_dir, resolved_settings.generated_storyboard_package_path),
    )
    package_assembly_service = generated_package_assembly_service or GeneratedPackageAssemblyService(
        minigame_service=minigame_service,
        storyboard_service=storyboard_service,
    )
    standing_slice_service = generated_standing_slice_service or GeneratedStandingSliceService(
        package_assembly_service=package_assembly_service,
        package_output_path=resolved_settings.resolve_path(base_dir, resolved_settings.generated_storyboard_package_path),
    )
    standing_slice_job_service = generated_standing_slice_job_service or GeneratedStandingSliceJobService(
        standing_slice_service=standing_slice_service,
        job_store=standing_slice_job_store,
    )
    minigame_generator_catalog = MinigameGeneratorCatalog.default()

    app = FastAPI(title="Story Orchestrator", version="0.1.0")
    app.state.settings = resolved_settings
    app.state.store = store
    app.state.generated_storyboard_service = storyboard_service
    app.state.generated_minigame_service = minigame_service
    app.state.generated_package_assembly_service = package_assembly_service
    app.state.generated_standing_slice_service = standing_slice_service
    app.state.generated_standing_slice_job_store = standing_slice_job_store
    app.state.generated_standing_slice_job_service = standing_slice_job_service
    app.state.minigame_generator_catalog = minigame_generator_catalog
    app.state.elevenlabs_token_provider = create_elevenlabs_tts_websocket_token

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

    @app.post("/api/v1/generated-minigame-beats", response_model=GeneratedMinigameBeatResult)
    def create_generated_minigame_beat(
        request: GeneratedMinigameBeatRequest,
    ) -> GeneratedMinigameBeatResult:
        return minigame_service.create_package(request)

    @app.post(
        "/api/v1/generated-package-assemblies",
        response_model=GeneratedPackageAssemblyResult,
    )
    def create_generated_package_assembly(
        request: GeneratedPackageAssemblyRequest,
    ) -> GeneratedPackageAssemblyResult:
        return package_assembly_service.create_package(request)

    @app.post(
        "/api/v1/generated-standing-slice-regenerations",
        response_model=GeneratedStandingSliceResult,
    )
    def create_generated_standing_slice_regeneration(
        request: GeneratedStandingSliceRequest,
    ) -> GeneratedStandingSliceResult:
        return standing_slice_service.create_package(request)

    @app.post(
        "/api/v1/generated-standing-slice-jobs",
        response_model=GeneratedStandingSliceJobRecord,
        status_code=201,
    )
    def create_generated_standing_slice_job(
        request: GeneratedStandingSliceRequest,
    ) -> GeneratedStandingSliceJobRecord:
        return standing_slice_job_service.create_job(request)

    @app.get(
        "/api/v1/generated-standing-slice-jobs/{job_id}",
        response_model=GeneratedStandingSliceJobRecord,
    )
    def get_generated_standing_slice_job(job_id: str) -> GeneratedStandingSliceJobRecord:
        record = standing_slice_job_service.get_job(job_id)
        if record is None:
            raise HTTPException(status_code=404, detail="Standing slice job not found.")
        return record

    @app.get("/api/v1/minigame-generators", response_model=list[MinigameGeneratorDefinition])
    def list_minigame_generators() -> list[MinigameGeneratorDefinition]:
        return minigame_generator_catalog.list_definitions()

    @app.get("/api/v1/minigame-generators/{generator_id}", response_model=MinigameGeneratorDefinition)
    def get_minigame_generator(generator_id: str) -> MinigameGeneratorDefinition:
        definition = minigame_generator_catalog.get_definition(generator_id)
        if definition is None:
            raise HTTPException(status_code=404, detail="Minigame generator not found.")
        return definition

    @app.post(
        "/api/v1/minigame-generators/{generator_id}/validate",
        response_model=MinigameGeneratorValidationResult,
    )
    def validate_minigame_generator(
        generator_id: str,
        request: MinigameGeneratorValidationRequest,
    ) -> MinigameGeneratorValidationResult:
        return minigame_generator_catalog.validate_selection(
            generator_id,
            parameters=request.parameters,
            context=request.context,
        )

    @app.post(
        "/api/v1/elevenlabs/tts-websocket-token",
        response_model=ElevenLabsSingleUseTokenResponse,
    )
    def create_tts_websocket_token() -> ElevenLabsSingleUseTokenResponse:
        if not resolved_settings.elevenlabs_api_key:
            raise HTTPException(
                status_code=503,
                detail="ElevenLabs is not configured for the local story orchestrator.",
            )

        try:
            token = app.state.elevenlabs_token_provider(resolved_settings.elevenlabs_api_key)
        except (RuntimeError, httpx.HTTPError) as exc:
            raise HTTPException(
                status_code=502,
                detail="Unable to mint an ElevenLabs voice token.",
            ) from exc

        return ElevenLabsSingleUseTokenResponse(token=token)

    return app


def create_elevenlabs_tts_websocket_token(api_key: str) -> str:
    response = httpx.post(
        "https://api.elevenlabs.io/v1/single-use-token/tts_websocket",
        headers={"xi-api-key": api_key},
        timeout=30,
    )
    response.raise_for_status()
    payload = response.json()
    token = payload.get("token")
    if not token:
        raise RuntimeError("ElevenLabs did not return a tts_websocket token.")

    return token


app = create_app()
