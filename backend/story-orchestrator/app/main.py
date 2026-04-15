from pathlib import Path

import httpx
from fastapi import FastAPI, File, Form, HTTPException, Query, UploadFile
from fastapi.responses import FileResponse, HTMLResponse

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
from .generated_standing_slice_artifacts import GeneratedStandingSliceArtifactArchive
from .generated_standing_slice_publish import GeneratedStandingSlicePublisher, StandingSlicePublishApprovalError
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
    GeneratedStandingSliceJobReviewRequest,
    StoryJobCreateRequest,
    StoryJobRecord,
)
from .story_sequence_models import (
    StorySequenceAdvanceResult,
    StorySequenceSessionCreateRequest,
    StorySequenceSessionDetail,
    StorySequenceSessionRecord,
)
from .story_sequence_service import StorySequenceSessionService
from .story_sequence_store import StorySequenceSessionStore
from .story_sequence_turn_director import OpenAIStorySequenceTurnDirector, StorySequenceTurnDirector
from .store import GeneratedStandingSliceJobStore, StoryJobStore
from .storyboard_reference_library import StoryboardReferenceLibrary
from .storyboard_reference_models import StoryboardReferenceAssetRecord, StoryboardReferenceRole


def create_app(
    settings: Settings | None = None,
    generated_storyboard_service: GeneratedStoryboardService | None = None,
    generated_minigame_service: GeneratedMinigameBeatService | None = None,
    generated_package_assembly_service: GeneratedPackageAssemblyService | None = None,
    generated_standing_slice_service: GeneratedStandingSliceService | None = None,
    generated_standing_slice_job_store: GeneratedStandingSliceJobStore | None = None,
    generated_standing_slice_job_service: GeneratedStandingSliceJobService | None = None,
    generated_standing_slice_publisher: GeneratedStandingSlicePublisher | None = None,
    story_sequence_session_store: StorySequenceSessionStore | None = None,
    story_sequence_turn_director: StorySequenceTurnDirector | None = None,
    story_sequence_session_service: StorySequenceSessionService | None = None,
    storyboard_reference_library: StoryboardReferenceLibrary | None = None,
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
    sequence_session_store = story_sequence_session_store or StorySequenceSessionStore(database_path)
    storyboard_output_root = _resolve_storyboard_output_root(
        storyboard_service=storyboard_service,
        settings=resolved_settings,
        base_dir=base_dir,
    )
    standing_slice_publisher = generated_standing_slice_publisher or GeneratedStandingSlicePublisher(
        output_root=storyboard_output_root,
        live_package_path=resolved_settings.resolve_path(base_dir, resolved_settings.generated_storyboard_package_path),
    )
    reference_library = (
        storyboard_reference_library
        or getattr(storyboard_service, "_reference_library", None)
        or StoryboardReferenceLibrary(storyboard_output_root)
    )
    standing_slice_job_service = generated_standing_slice_job_service or GeneratedStandingSliceJobService(
        standing_slice_service=standing_slice_service,
        job_store=standing_slice_job_store,
        artifact_archive=GeneratedStandingSliceArtifactArchive(output_root=storyboard_output_root),
        publisher=standing_slice_publisher,
    )
    sequence_session_service = story_sequence_session_service or StorySequenceSessionService(
        package_assembly_service=package_assembly_service,
        store=sequence_session_store,
        default_voice_id=resolved_settings.elevenlabs_voice_id or "voice-test",
        turn_director=story_sequence_turn_director or OpenAIStorySequenceTurnDirector.from_settings(resolved_settings),
    )
    minigame_generator_catalog = MinigameGeneratorCatalog.default()
    review_page_path = (base_dir / "app" / "static" / "standing_slice_review.html").resolve()

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
    app.state.story_sequence_session_store = sequence_session_store
    app.state.story_sequence_session_service = sequence_session_service
    app.state.elevenlabs_token_provider = create_elevenlabs_tts_websocket_token
    app.state.storyboard_output_root = storyboard_output_root
    app.state.storyboard_reference_library = reference_library
    app.state.review_page_path = review_page_path

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
        "/api/v1/story-sequence-sessions",
        response_model=StorySequenceSessionRecord,
        status_code=201,
    )
    def create_story_sequence_session(
        request: StorySequenceSessionCreateRequest,
    ) -> StorySequenceSessionRecord:
        return sequence_session_service.create_session(request)

    @app.get(
        "/api/v1/story-sequence-sessions/{session_id}",
        response_model=StorySequenceSessionDetail,
    )
    def get_story_sequence_session(session_id: str) -> StorySequenceSessionDetail:
        detail = sequence_session_service.get_session_detail(session_id)
        if detail is None:
            raise HTTPException(status_code=404, detail="Story sequence session not found.")
        return detail

    @app.post(
        "/api/v1/story-sequence-sessions/{session_id}/next-turn",
        response_model=StorySequenceAdvanceResult,
    )
    def advance_story_sequence_session(session_id: str) -> StorySequenceAdvanceResult:
        result = sequence_session_service.advance_session(session_id)
        if result is None:
            raise HTTPException(status_code=404, detail="Story sequence session not found.")
        return result

    @app.post(
        "/api/v1/generated-storyboards/cutscene",
        response_model=GeneratedStoryboardPackageResult,
        status_code=201,
    )
    def create_generated_storyboard_cutscene(
        request: GeneratedStoryboardCutsceneRequest,
    ) -> GeneratedStoryboardPackageResult:
        return storyboard_service.create_package(request)

    @app.post(
        "/api/v1/storyboard-reference-assets",
        response_model=StoryboardReferenceAssetRecord,
        status_code=201,
    )
    async def create_storyboard_reference_asset(
        file: UploadFile = File(...),
        reference_role: StoryboardReferenceRole = Form("character"),
        label: str = Form(""),
        character_name: str = Form(""),
        tags: str = Form(""),
    ) -> StoryboardReferenceAssetRecord:
        content = await file.read()
        if not content:
            raise HTTPException(status_code=400, detail="Storyboard reference upload is empty.")

        return reference_library.import_reference(
            filename=file.filename or "reference.png",
            content=content,
            reference_role=reference_role,
            label=label or Path(file.filename or "reference").stem,
            character_name=character_name,
            tags=_parse_reference_tags(tags),
            mime_type=file.content_type or "image/png",
        )

    @app.get(
        "/api/v1/storyboard-reference-assets",
        response_model=list[StoryboardReferenceAssetRecord],
    )
    def list_storyboard_reference_assets(
        character_name: str = Query(""),
        reference_role: StoryboardReferenceRole | None = Query(None),
    ) -> list[StoryboardReferenceAssetRecord]:
        return reference_library.list_references(
            character_name=character_name,
            reference_role=reference_role,
        )


    @app.get(
        "/api/v1/storyboard-reference-assets/{reference_id}/content",
    )
    def get_storyboard_reference_asset_content(reference_id: str) -> FileResponse:
        record = reference_library.get_reference(reference_id)
        if record is None:
            raise HTTPException(status_code=404, detail="Storyboard reference asset not found.")

        asset_path = Path(record.stored_path).resolve()
        try:
            asset_path.relative_to(reference_library.asset_root)
        except ValueError as exc:
            raise HTTPException(status_code=404, detail="Storyboard reference asset is outside the reference root.") from exc

        if not asset_path.exists():
            raise HTTPException(status_code=404, detail="Storyboard reference asset file not found.")

        return FileResponse(asset_path, media_type=record.mime_type, filename=asset_path.name)

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

    @app.post(
        "/api/v1/generated-standing-slice-jobs/{job_id}/review",
        response_model=GeneratedStandingSliceJobRecord,
    )
    def update_generated_standing_slice_job_review(
        job_id: str,
        request: GeneratedStandingSliceJobReviewRequest,
    ) -> GeneratedStandingSliceJobRecord:
        record = standing_slice_job_service.update_review(job_id, request)
        if record is None:
            raise HTTPException(status_code=404, detail="Standing slice job not found.")
        return record

    @app.post(
        "/api/v1/generated-standing-slice-jobs/{job_id}/publish",
        response_model=GeneratedStandingSliceJobRecord,
    )
    def publish_generated_standing_slice_job(job_id: str) -> GeneratedStandingSliceJobRecord:
        try:
            record = standing_slice_job_service.publish_job(job_id)
        except StandingSlicePublishApprovalError as exc:
            raise HTTPException(status_code=409, detail=str(exc)) from exc

        if record is None:
            raise HTTPException(status_code=404, detail="Standing slice job not found.")
        return record

    @app.get(
        "/api/v1/generated-standing-slice-jobs/{job_id}/assets/{asset_id}/content",
    )
    def get_generated_standing_slice_job_asset_content(job_id: str, asset_id: str) -> FileResponse:
        record = standing_slice_job_service.get_job(job_id)
        if record is None:
            raise HTTPException(status_code=404, detail="Standing slice job not found.")

        asset = next((candidate for candidate in record.assets if candidate.asset_id == asset_id), None)
        if asset is None:
            raise HTTPException(status_code=404, detail="Standing slice asset not found.")

        asset_path = Path(asset.output_path).resolve()
        try:
            asset_path.relative_to(storyboard_output_root)
        except ValueError as exc:
            raise HTTPException(status_code=404, detail="Standing slice asset is outside the storyboard root.") from exc

        if not asset_path.exists():
            raise HTTPException(status_code=404, detail="Standing slice asset file not found.")

        return FileResponse(asset_path, media_type=asset.mime_type, filename=asset_path.name)

    @app.get("/review/standing-slice", response_class=HTMLResponse)
    def get_generated_standing_slice_review_page() -> HTMLResponse:
        if not review_page_path.exists():
            raise HTTPException(status_code=404, detail="Standing slice review page not found.")
        return HTMLResponse(review_page_path.read_text(encoding="utf-8"))

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


def _resolve_storyboard_output_root(
    *,
    storyboard_service: GeneratedStoryboardService,
    settings: Settings,
    base_dir: Path,
) -> Path:
    service_output_root = getattr(storyboard_service, "_output_root", None)
    if isinstance(service_output_root, Path):
        return service_output_root.resolve()

    return settings.resolve_path(base_dir, settings.generated_storyboard_output_root).resolve()


def _parse_reference_tags(raw_tags: str) -> list[str]:
    return [tag.strip() for tag in raw_tags.split(",") if tag.strip()]


app = create_app()
