from datetime import datetime, UTC
from typing import Literal
from uuid import uuid4

from pydantic import BaseModel, Field

from .generated_package_assembly import GeneratedPackageAssemblyResult
from .generated_standing_slice import GeneratedStandingSliceRequest, GeneratedStandingSliceResult


class StoryJobCreateRequest(BaseModel):
    story_brief: str = Field(min_length=1)
    package_template: str = Field(default="intro_chicken_sample")
    target_scene: str | None = None
    metadata: dict[str, str] = Field(default_factory=dict)


class StoryJobRecord(BaseModel):
    job_id: str
    status: Literal["pending", "running", "failed", "completed"]
    story_brief: str
    package_template: str
    target_scene: str | None = None
    metadata: dict[str, str] = Field(default_factory=dict)
    created_at: str
    updated_at: str

    @classmethod
    def create_pending(cls, request: StoryJobCreateRequest) -> "StoryJobRecord":
        now = datetime.now(UTC).isoformat()
        return cls(
            job_id=str(uuid4()),
            status="pending",
            story_brief=request.story_brief,
            package_template=request.package_template,
            target_scene=request.target_scene,
            metadata=request.metadata,
            created_at=now,
            updated_at=now,
        )


class ElevenLabsSingleUseTokenResponse(BaseModel):
    token: str
    token_type: Literal["tts_websocket"] = "tts_websocket"


class GeneratedStandingSliceJobStepRecord(BaseModel):
    step_id: Literal["post_chicken_to_farm", "find_tools_to_pre_farm"]
    status: Literal["pending", "running", "completed", "failed", "skipped"]
    output: GeneratedPackageAssemblyResult | None = None
    errors: list[str] = Field(default_factory=list)


class GeneratedStandingSliceJobRecord(BaseModel):
    job_id: str
    status: Literal["pending", "running", "failed", "completed"]
    approval_status: Literal["not_requested", "pending_review", "approved", "rejected"] = "not_requested"
    request: GeneratedStandingSliceRequest
    result: GeneratedStandingSliceResult | None = None
    steps: list[GeneratedStandingSliceJobStepRecord] = Field(default_factory=list)
    created_at: str
    updated_at: str

    @classmethod
    def create_pending(cls, request: GeneratedStandingSliceRequest) -> "GeneratedStandingSliceJobRecord":
        now = datetime.now(UTC).isoformat()
        return cls(
            job_id=str(uuid4()),
            status="pending",
            approval_status="not_requested",
            request=request,
            result=None,
            steps=[
                GeneratedStandingSliceJobStepRecord(
                    step_id="post_chicken_to_farm",
                    status="pending",
                ),
                GeneratedStandingSliceJobStepRecord(
                    step_id="find_tools_to_pre_farm",
                    status="pending",
                ),
            ],
            created_at=now,
            updated_at=now,
        )
