from __future__ import annotations

from datetime import UTC, datetime
from typing import Literal
from uuid import uuid4

from pydantic import BaseModel, Field, model_validator

from .generated_package_assembly import GeneratedPackageAssemblyRequest, GeneratedPackageAssemblyResult


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


class StorySequenceSessionCreateRequest(BaseModel):
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
    seed: str = ""

    @model_validator(mode="after")
    def validate_request(self) -> "StorySequenceSessionCreateRequest":
        if not self.character_pool:
            raise ValueError("character_pool must contain at least one character.")
        return self


class StorySequenceContinuityImageRecord(BaseModel):
    asset_id: str
    beat_id: str
    shot_id: str
    turn_index: int
    character_name: str
    output_path: str


class StorySequenceSessionState(BaseModel):
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
    max_recent_generators: int = 2
    max_continuity_images: int = 6
    continuity_images: list[StorySequenceContinuityImageRecord] = Field(default_factory=list)
    last_generator_id: str = ""
    last_character_name: str = ""
    last_minigame_goal: str = ""


class StorySequenceSessionRecord(BaseModel):
    session_id: str
    status: Literal["active"] = "active"
    package_id: str
    package_display_name: str
    state: StorySequenceSessionState
    created_at: str
    updated_at: str

    @classmethod
    def create_active(cls, request: StorySequenceSessionCreateRequest) -> "StorySequenceSessionRecord":
        now = datetime.now(UTC).isoformat()
        session_id = str(uuid4())
        state = StorySequenceSessionState(
            beat_cursor=0,
            fit_tags=list(dict.fromkeys(request.fit_tags)),
            world_state=list(dict.fromkeys(request.world_state)),
            difficulty_band=request.difficulty_band,
            character_pool=list(request.character_pool),
            narrative_seed=request.narrative_seed,
            seed=request.seed or session_id,
        )
        return cls(
            session_id=session_id,
            status="active",
            package_id=request.package_id,
            package_display_name=request.package_display_name,
            state=state,
            created_at=now,
            updated_at=now,
        )


class StorySequenceTurnRecord(BaseModel):
    session_id: str
    turn_index: int
    generator_id: str
    character_name: str
    summary: str
    request: GeneratedPackageAssemblyRequest
    result: GeneratedPackageAssemblyResult
    created_at: str


class StorySequenceSessionDetail(BaseModel):
    session: StorySequenceSessionRecord
    turns: list[StorySequenceTurnRecord] = Field(default_factory=list)


class StorySequenceAdvanceResult(BaseModel):
    session: StorySequenceSessionRecord
    turn: StorySequenceTurnRecord
