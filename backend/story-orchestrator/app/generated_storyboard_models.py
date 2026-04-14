from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any, Protocol

from pydantic import BaseModel, Field


class GeneratedStoryboardContext(BaseModel):
    character_name: str = Field(min_length=1)
    crop_name: str | None = None
    focus_label: str | None = None
    minigame_goal: str | None = None


class GeneratedStoryboardCutsceneRequest(BaseModel):
    package_id: str = Field(min_length=1)
    package_display_name: str = Field(min_length=1)
    beat_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    scene_name: str = Field(min_length=1)
    next_scene_name: str | None = None
    linked_minigame_beat_id: str | None = None
    story_brief: str = Field(min_length=1)
    style_preset_id: str = Field(default="farm_storybook_v1", min_length=1)
    voice_id: str | None = None
    reference_image_paths: list[str] = Field(default_factory=list)
    aspect_ratio: str = Field(default="16:9", min_length=1)
    image_size: str = Field(default="2K", min_length=1)
    context: GeneratedStoryboardContext


class GeneratedStoryboardPackageResult(BaseModel):
    package_output_path: str
    unity_package: dict[str, Any]


@dataclass(frozen=True)
class GeneratedStoryboardPlanShot:
    shot_id: str
    subtitle_text: str
    narration_text: str
    image_prompt: str
    duration_seconds: float


@dataclass(frozen=True)
class GeneratedStoryboardPlan:
    beat_id: str
    display_name: str
    scene_name: str
    next_scene_name: str
    style_preset_id: str
    shots: list[GeneratedStoryboardPlanShot]


@dataclass(frozen=True)
class GeneratedImageAsset:
    output_path: Path
    mime_type: str


@dataclass(frozen=True)
class GeneratedSpeechAsset:
    output_path: Path
    alignment_path: Path
    duration_seconds: float
    mime_type: str


class StoryboardPlanner(Protocol):
    def plan(self, request: GeneratedStoryboardCutsceneRequest) -> GeneratedStoryboardPlan:
        ...


class ImageGenerator(Protocol):
    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        ...


class SpeechGenerator(Protocol):
    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> GeneratedSpeechAsset:
        ...
