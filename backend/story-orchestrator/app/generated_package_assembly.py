from __future__ import annotations

from typing import Any

from pydantic import BaseModel, Field

from .generated_minigames import (
    GeneratedMinigameBeatRequest,
    GeneratedMinigameBeatResult,
    GeneratedMinigameBeatService,
)
from .generated_storyboard_models import GeneratedStoryboardContext
from .generated_storyboards import (
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardService,
)
from .minigame_generator_models import MinigameGenerationContext


class GeneratedPackageAssemblyMinigameInput(BaseModel):
    beat_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    scene_name: str = Field(min_length=1)
    next_scene_name: str | None = None
    generator_id: str = Field(min_length=1)
    parameters: dict[str, Any] = Field(default_factory=dict)
    context: MinigameGenerationContext = Field(default_factory=MinigameGenerationContext)

    def to_request(
        self,
        *,
        package_id: str,
        package_display_name: str,
    ) -> GeneratedMinigameBeatRequest:
        return GeneratedMinigameBeatRequest(
            package_id=package_id,
            package_display_name=package_display_name,
            beat_id=self.beat_id,
            display_name=self.display_name,
            scene_name=self.scene_name,
            next_scene_name=self.next_scene_name,
            generator_id=self.generator_id,
            parameters=self.parameters,
            context=self.context,
        )


class GeneratedPackageAssemblyCutsceneInput(BaseModel):
    beat_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    scene_name: str = Field(min_length=1)
    next_scene_name: str | None = None
    story_brief: str = Field(min_length=1)
    style_preset_id: str = Field(default="farm_storybook_v1", min_length=1)
    voice_id: str | None = None
    reference_image_paths: list[str] = Field(default_factory=list)
    aspect_ratio: str = Field(default="16:9", min_length=1)
    image_size: str = Field(default="2K", min_length=1)
    context: GeneratedStoryboardContext

    def to_request(
        self,
        *,
        package_id: str,
        package_display_name: str,
        linked_minigame_beat_id: str,
    ) -> GeneratedStoryboardCutsceneRequest:
        return GeneratedStoryboardCutsceneRequest(
            package_id=package_id,
            package_display_name=package_display_name,
            beat_id=self.beat_id,
            display_name=self.display_name,
            scene_name=self.scene_name,
            next_scene_name=self.next_scene_name,
            linked_minigame_beat_id=linked_minigame_beat_id,
            story_brief=self.story_brief,
            style_preset_id=self.style_preset_id,
            voice_id=self.voice_id,
            reference_image_paths=self.reference_image_paths,
            aspect_ratio=self.aspect_ratio,
            image_size=self.image_size,
            context=self.context,
        )


class GeneratedPackageAssemblyRequest(BaseModel):
    package_id: str = Field(min_length=1)
    package_display_name: str = Field(min_length=1)
    minigame: GeneratedPackageAssemblyMinigameInput
    cutscene: GeneratedPackageAssemblyCutsceneInput


class GeneratedPackageAssemblyResult(BaseModel):
    is_valid: bool
    package_output_path: str = ""
    unity_package: dict[str, Any] = Field(default_factory=dict)
    minigame_result: GeneratedMinigameBeatResult | None = None
    errors: list[str] = Field(default_factory=list)
    fallback_generator_ids: list[str] = Field(default_factory=list)


class GeneratedPackageAssemblyService:
    def __init__(
        self,
        *,
        minigame_service: GeneratedMinigameBeatService,
        storyboard_service: GeneratedStoryboardService,
    ) -> None:
        self._minigame_service = minigame_service
        self._storyboard_service = storyboard_service

    def create_package(
        self,
        request: GeneratedPackageAssemblyRequest,
    ) -> GeneratedPackageAssemblyResult:
        minigame_result = self._minigame_service.create_package(
            request.minigame.to_request(
                package_id=request.package_id,
                package_display_name=request.package_display_name,
            )
        )
        if not minigame_result.is_valid:
            return GeneratedPackageAssemblyResult(
                is_valid=False,
                minigame_result=minigame_result,
                errors=minigame_result.errors,
                fallback_generator_ids=minigame_result.fallback_generator_ids,
            )

        cutscene_result = self._storyboard_service.create_package(
            request.cutscene.to_request(
                package_id=request.package_id,
                package_display_name=request.package_display_name,
                linked_minigame_beat_id=request.minigame.beat_id,
            )
        )

        return GeneratedPackageAssemblyResult(
            is_valid=True,
            package_output_path=cutscene_result.package_output_path,
            unity_package=cutscene_result.unity_package,
            minigame_result=minigame_result,
            fallback_generator_ids=minigame_result.fallback_generator_ids,
        )
