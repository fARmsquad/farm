from __future__ import annotations

import json
from typing import Iterable

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

from .config import Settings
from .generated_storyboard_models import (
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardPlan,
    GeneratedStoryboardPlanShot,
    StoryboardPlanner,
)
from .openai_structured_outputs import OpenAIStructuredOutputClient
from .style_preset_catalog import StylePresetCatalog


class _StoryboardShotOutput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    subtitle_text: str = Field(min_length=1)
    narration_text: str = Field(min_length=1)
    image_prompt: str = Field(min_length=1)
    duration_seconds: float = Field(ge=2.0, le=4.0)

    @field_validator("subtitle_text")
    @classmethod
    def _validate_subtitle_length(cls, value: str) -> str:
        if _word_count(value) > 12:
            raise ValueError("subtitle_text must be 12 words or fewer.")
        return value

    @field_validator("narration_text")
    @classmethod
    def _validate_narration_length(cls, value: str) -> str:
        if _word_count(value) > 14:
            raise ValueError("narration_text must be 14 words or fewer.")
        return value

    @model_validator(mode="after")
    def _validate_matching_lines(self) -> "_StoryboardShotOutput":
        if self.subtitle_text.strip() != self.narration_text.strip():
            raise ValueError("narration_text must exactly match subtitle_text.")
        return self


class _StoryboardPlanOutput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    shots: list[_StoryboardShotOutput] = Field(min_length=3, max_length=6)


class StoryboardPlannerChain:
    def __init__(self, *, planners: Iterable[StoryboardPlanner]) -> None:
        self._planners = list(planners)

    def plan(self, request: GeneratedStoryboardCutsceneRequest) -> GeneratedStoryboardPlan:
        errors: list[str] = []
        for planner in self._planners:
            try:
                return planner.plan(request)
            except Exception as error:
                errors.append(str(error))
        joined = "; ".join(errors) if errors else "No planner was configured."
        raise RuntimeError(f"All storyboard planners failed: {joined}")


class OpenAIStoryboardPlanner:
    def __init__(
        self,
        *,
        client: OpenAIStructuredOutputClient,
        style_preset_catalog: StylePresetCatalog | None = None,
    ) -> None:
        self._client = client
        self._style_preset_catalog = style_preset_catalog

    @classmethod
    def from_settings(cls, settings: Settings) -> "OpenAIStoryboardPlanner":
        return cls(
            client=OpenAIStructuredOutputClient(
                api_key=settings.openai_api_key,
                model=settings.openai_narrative_model,
                timeout_seconds=settings.openai_timeout_seconds,
            ),
            style_preset_catalog=StylePresetCatalog.default(),
        )

    def plan(self, request: GeneratedStoryboardCutsceneRequest) -> GeneratedStoryboardPlan:
        system_prompt = (
            "You are writing a concise storyboard cutscene for a cozy farm tutorial game. "
            "Return only structured shot data. Each shot must have readable subtitle text, matching narration text, "
            "a strong image prompt for concept-art generation, and a short duration. "
            "Every cutscene must feel like an actual story beat with characters, conflict, and visible events, not a static instruction card. "
            "The first shot should anchor previous context, the middle shots should escalate or complicate the situation, "
            "and the final shot should land naturally on the next playable mission. "
            "Each shot must be a distinct visual moment with a different action, framing, or staging. "
            "Keep continuity with the named character and gameplay goal, but avoid repeating the same phrasing or composition across shots. "
            "Respect the provided story type, prompt structure, and minigame story hook when shaping the beat. "
            "Write like a storyboard artist: show what changes from shot to shot, make the conflict legible, and end on a handoff into action instead of explanation."
        )
        preset = (
            self._style_preset_catalog.get(request.style_preset_id)
            if self._style_preset_catalog
            else None
        )
        if preset is not None:
            system_prompt = f"{system_prompt}\n\nStyle direction: {preset.style_descriptor_text}"
        subject_label = request.context.crop_name or request.context.focus_label or "farm task"
        user_prompt_payload: dict = {
            "display_name": request.display_name,
            "story_brief": request.story_brief,
            "style_preset_id": request.style_preset_id,
        }
        if preset is not None:
            user_prompt_payload["style_descriptor_text"] = preset.style_descriptor_text
        user_prompt = json.dumps(
            {
                **user_prompt_payload,
                "scene_name": request.scene_name,
                "next_scene_name": request.next_scene_name,
                "character_name": request.context.character_name,
                "present_character_names": request.context.present_character_names,
                "subject_label": subject_label,
                "minigame_goal": request.context.minigame_goal,
                "mission_configuration_summary": request.context.mission_configuration_summary,
                "prior_story_summary": request.context.prior_story_summary,
                "world_state": request.context.world_state,
                "selected_generator_id": request.context.selected_generator_id,
                "selected_generator_display_name": request.context.selected_generator_display_name,
                "continuity_reference_mode": request.continuity_reference_mode,
                "explicit_reference_image_paths": request.reference_image_paths,
                "story_type_id": request.context.story_type_id,
                "story_type_display_name": request.context.story_type_display_name,
                "story_type_prompt_directives": list(request.context.story_type_prompt_directives),
                "prompt_structure_id": request.context.prompt_structure_id,
                "prompt_structure_display_name": request.context.prompt_structure_display_name,
                "prompt_structure_directives": list(request.context.prompt_structure_directives),
                "minigame_story_hook": request.context.minigame_story_hook,
                "minigame_prompt_directives": list(request.context.minigame_prompt_directives),
                "requirements": [
                    "Write 3 to 6 storyboard shots.",
                    "Every shot must last between 2 and 4 seconds.",
                    "Shot 1 should establish where we are in the ongoing story and who is present.",
                    "A middle shot must reveal or escalate a concrete problem, discovery, interruption, or emotional turn.",
                    "The final shot must show the moment that pushes the player into the next mission.",
                    "Make the cutscene lead directly into the stated minigame goal.",
                    "Keep each subtitle concise and readable in Unity, ideally 4 to 10 words and never more than 12 words.",
                    "Narration text must exactly match the subtitle text for every shot.",
                    "Keep the shared subtitle and narration line natural for voice playback, ideally 6 to 12 words and never more than 14 words.",
                    "Image prompts must be fresh compositions and must not include UI text or subtitle panels.",
                    "Do not repeat the same camera angle, pose, blocking, or environment layout across shots.",
                    "Show at least one concrete conflict, interruption, reveal, or character decision before the mission starts.",
                    "The final shot should hand the player into the mission as a natural consequence of the conflict, not by showing a tutorial card.",
                    "Use the previous context, present characters, and mission configuration summary to make this beat specific instead of generic.",
                    "Respect the story type prompt directives, prompt structure directives, and minigame prompt directives without copying them verbatim.",
                ],
            },
            indent=2,
        )
        output = self._client.generate(
            response_model=_StoryboardPlanOutput,
            schema_name="farm_storyboard_plan",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )
        shots = [
            GeneratedStoryboardPlanShot(
                shot_id=f"shot_{index + 1:02d}",
                subtitle_text=shot.subtitle_text,
                narration_text=shot.narration_text,
                image_prompt=shot.image_prompt,
                duration_seconds=shot.duration_seconds,
            )
            for index, shot in enumerate(output.shots)
        ]
        return GeneratedStoryboardPlan(
            beat_id=request.beat_id,
            display_name=request.display_name,
            scene_name=request.scene_name,
            next_scene_name=request.next_scene_name or "",
            style_preset_id=request.style_preset_id,
            shots=shots,
        )


def _word_count(value: str) -> int:
    return len([word for word in value.strip().split() if word])
