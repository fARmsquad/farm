from __future__ import annotations

from datetime import UTC, datetime
from typing import Any, Literal
from uuid import uuid4

from pydantic import BaseModel, Field, model_validator


RuntimeSessionStatus = Literal["active", "failed", "completed"]
RuntimeJobStatus = Literal[
    "queued",
    "planning",
    "generating_images",
    "generating_audio",
    "assembling_contract",
    "validating",
    "ready",
    "failed",
    "cancelled",
]
RuntimeArtifactType = Literal["image", "audio", "alignment"]
RuntimeOutcomeResult = Literal["success", "failure"]


def _default_fit_tags() -> list[str]:
    return ["teaching", "cozy", "farm"]


def _default_world_state() -> list[str]:
    return [
        "farm_plots_unlocked",
        "tool_search_enabled",
        "chicken_pen_intro_available",
        "earliest_tutorial_bridge",
    ]


def _default_character_pool() -> list[str]:
    return ["Old Garrett", "Miss Clara", "Young Pip"]


class RuntimeSessionCreateRequest(BaseModel):
    package_id: str = Field(default="storypkg_intro_chicken_sample", min_length=1)
    package_display_name: str = Field(default="Generative Story Slice", min_length=1)
    narrative_seed: str = Field(
        default="Continue the farm story through cozy teaching beats.",
        min_length=1,
    )
    fit_tags: list[str] = Field(default_factory=_default_fit_tags)
    world_state: list[str] = Field(default_factory=_default_world_state)
    difficulty_band: str | None = Field(default="tutorial")
    character_pool: list[str] = Field(default_factory=_default_character_pool)
    max_turns: int = 3
    seed: str = ""

    @model_validator(mode="after")
    def validate_request(self) -> "RuntimeSessionCreateRequest":
        if not self.character_pool:
            raise ValueError("character_pool must contain at least one character.")
        if self.max_turns < 1:
            raise ValueError("max_turns must be at least 1.")
        return self


class RuntimeContinuityImageRecord(BaseModel):
    asset_id: str
    shot_id: str
    turn_id: str
    turn_index: int
    character_name: str
    stored_path: str


class RuntimeSessionState(BaseModel):
    beat_cursor: int = 0
    recent_generator_ids: list[str] = Field(default_factory=list)
    recent_character_names: list[str] = Field(default_factory=list)
    recent_crop_types: list[str] = Field(default_factory=list)
    fit_tags: list[str] = Field(default_factory=list)
    world_state: list[str] = Field(default_factory=list)
    difficulty_band: str | None = None
    character_pool: list[str] = Field(default_factory=list)
    narrative_seed: str = ""
    seed: str = ""
    max_turns: int = 3
    max_recent_generators: int = 2
    max_continuity_images: int = 6
    continuity_images: list[RuntimeContinuityImageRecord] = Field(default_factory=list)
    last_generator_id: str = ""
    last_character_name: str = ""
    last_minigame_goal: str = ""


class RuntimeSession(BaseModel):
    session_id: str
    status: RuntimeSessionStatus = "active"
    package_id: str
    package_display_name: str
    active_job_id: str = ""
    last_ready_turn_id: str = ""
    state: RuntimeSessionState
    created_at: str
    updated_at: str

    @classmethod
    def create_active(cls, request: RuntimeSessionCreateRequest) -> "RuntimeSession":
        now = datetime.now(UTC).isoformat()
        session_id = str(uuid4())
        state = RuntimeSessionState(
            beat_cursor=0,
            fit_tags=list(dict.fromkeys(request.fit_tags)),
            world_state=list(dict.fromkeys(request.world_state)),
            difficulty_band=request.difficulty_band,
            character_pool=list(request.character_pool),
            narrative_seed=request.narrative_seed,
            seed=request.seed or session_id,
            max_turns=request.max_turns,
        )
        return cls(
            session_id=session_id,
            status="active",
            package_id=request.package_id,
            package_display_name=request.package_display_name,
            active_job_id="",
            last_ready_turn_id="",
            state=state,
            created_at=now,
            updated_at=now,
        )


class RuntimeTurnJob(BaseModel):
    job_id: str
    session_id: str
    turn_index: int
    turn_id: str = ""
    status: RuntimeJobStatus = "queued"
    error_message: str = ""
    created_at: str
    updated_at: str

    @classmethod
    def create_queued(cls, session_id: str, turn_index: int) -> "RuntimeTurnJob":
        now = datetime.now(UTC).isoformat()
        return cls(
            job_id=str(uuid4()),
            session_id=session_id,
            turn_index=turn_index,
            turn_id="",
            status="queued",
            error_message="",
            created_at=now,
            updated_at=now,
        )


class RuntimeJobStepRecord(BaseModel):
    step_name: RuntimeJobStatus
    status: Literal["pending", "running", "completed", "failed"] = "pending"
    error_message: str = ""


class ArtifactDescriptor(BaseModel):
    asset_id: str
    turn_id: str
    artifact_type: RuntimeArtifactType
    beat_id: str = ""
    shot_id: str = ""
    mime_type: str
    provider_name: str = ""
    provider_model: str = ""
    fallback_used: bool = False
    stored_path: str
    metadata: dict[str, Any] = Field(default_factory=dict)


class CutsceneShotContract(BaseModel):
    shot_id: str
    subtitle_text: str
    narration_text: str
    duration_seconds: float
    image_asset_id: str
    audio_asset_id: str
    alignment_asset_id: str = ""

    @model_validator(mode="after")
    def validate_audio_matches_subtitle(self) -> "CutsceneShotContract":
        if self.subtitle_text.strip() != self.narration_text.strip():
            raise ValueError("Cutscene narration_text must exactly match subtitle_text.")
        return self


class CutsceneContract(BaseModel):
    beat_id: str
    display_name: str
    scene_name: str
    next_scene_name: str = ""
    style_preset_id: str
    shots: list[CutsceneShotContract] = Field(default_factory=list, min_length=3, max_length=6)


class MinigameContract(BaseModel):
    beat_id: str
    display_name: str
    scene_name: str
    adapter_id: str
    objective_text: str
    required_count: int
    time_limit_seconds: float
    generator_id: str
    minigame_id: str
    fallback_generator_ids: list[str] = Field(default_factory=list)
    resolved_parameters: dict[str, Any] = Field(default_factory=dict)
    resolved_parameter_entries: list[dict[str, Any]] = Field(default_factory=list)


class RuntimeContinuityContract(BaseModel):
    reference_image_asset_ids: list[str] = Field(default_factory=list)
    world_state: list[str] = Field(default_factory=list)
    present_character_names: list[str] = Field(default_factory=list)
    prior_story_summary: str = ""


class RuntimeDebugContract(BaseModel):
    job_id: str
    generator_id: str
    character_name: str
    package_id: str
    package_display_name: str
    fallback_generator_ids: list[str] = Field(default_factory=list)
    provider_errors: list[str] = Field(default_factory=list)


class PlayableTurnEnvelope(BaseModel):
    contract_version: str = "runtime/v1"
    session_id: str
    turn_id: str
    status: Literal["ready"] = "ready"
    entry_scene_name: str
    cutscene: CutsceneContract
    minigame: MinigameContract
    artifacts: list[ArtifactDescriptor] = Field(default_factory=list)
    continuity: RuntimeContinuityContract
    debug: RuntimeDebugContract


class RuntimeTurnRecord(BaseModel):
    turn_id: str
    session_id: str
    turn_index: int
    status: Literal["ready"] = "ready"
    entry_scene_name: str
    generator_id: str
    character_name: str
    summary: str
    envelope: PlayableTurnEnvelope
    created_at: str
    updated_at: str


class RuntimeTurnSummary(BaseModel):
    turn_id: str
    turn_index: int
    status: Literal["ready"]
    entry_scene_name: str
    generator_id: str
    character_name: str
    summary: str
    created_at: str


class RuntimeSessionDetail(BaseModel):
    session_id: str
    status: RuntimeSessionStatus
    state: RuntimeSessionState
    active_job: RuntimeTurnJob | None = None
    last_ready_turn: RuntimeTurnSummary | None = None
    created_at: str
    updated_at: str


class RuntimeSessionCreateResponse(BaseModel):
    session_id: str
    job_id: str
    status: RuntimeJobStatus


class TurnOutcomeRequest(BaseModel):
    result: RuntimeOutcomeResult
    score: float = 0.0
    completed_objective_count: int = 0
    notes: str = ""


class TurnOutcomeResponse(BaseModel):
    next_job_id: str
    session_state: RuntimeSession


class RuntimeTrackerSessionSummary(BaseModel):
    session_id: str
    status: RuntimeSessionStatus
    package_display_name: str
    current_stage: str
    ready_turn_count: int
    max_turns: int
    updated_at: str


class RuntimeTrackerJobDetail(BaseModel):
    job_id: str
    turn_index: int
    status: RuntimeJobStatus
    error_message: str = ""
    current_step_name: str = ""
    steps: list[RuntimeJobStepRecord] = Field(default_factory=list)
    updated_at: str


class RuntimeTrackerTurnDetail(BaseModel):
    turn_id: str
    turn_index: int
    character_name: str
    generator_id: str
    scene_name: str
    objective_text: str
    summary: str
    image_asset_ids: list[str] = Field(default_factory=list)
    artifact_count: int = 0
    fallback_artifact_count: int = 0
    created_at: str


class RuntimeTrackerSessionDetail(BaseModel):
    session_id: str
    status: RuntimeSessionStatus
    package_display_name: str
    beat_cursor: int
    max_turns: int
    current_stage: str
    ready_turn_count: int
    active_job: RuntimeTrackerJobDetail | None = None
    turns: list[RuntimeTrackerTurnDetail] = Field(default_factory=list)
    updated_at: str
    created_at: str
