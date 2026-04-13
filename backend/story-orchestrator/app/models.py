from datetime import datetime, UTC
from typing import Literal
from uuid import uuid4

from pydantic import BaseModel, Field


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
