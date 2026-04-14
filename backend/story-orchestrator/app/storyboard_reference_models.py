from __future__ import annotations

from datetime import UTC, datetime
from typing import Literal
from uuid import uuid4

from pydantic import BaseModel, Field

StoryboardReferenceRole = Literal["character", "style", "environment", "prop"]


class StoryboardReferenceAssetRecord(BaseModel):
    reference_id: str
    reference_role: StoryboardReferenceRole
    label: str
    character_name: str = ""
    tags: list[str] = Field(default_factory=list)
    source_filename: str
    stored_path: str
    mime_type: str
    created_at: str

    @classmethod
    def create(
        cls,
        *,
        reference_role: StoryboardReferenceRole,
        label: str,
        character_name: str,
        tags: list[str],
        source_filename: str,
        stored_path: str,
        mime_type: str,
    ) -> "StoryboardReferenceAssetRecord":
        return cls(
            reference_id=str(uuid4()),
            reference_role=reference_role,
            label=label,
            character_name=character_name,
            tags=tags,
            source_filename=source_filename,
            stored_path=stored_path,
            mime_type=mime_type,
            created_at=datetime.now(UTC).isoformat(),
        )
