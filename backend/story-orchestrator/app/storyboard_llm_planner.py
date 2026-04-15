from __future__ import annotations

import json
from typing import Iterable

from pydantic import BaseModel, ConfigDict, Field

from .config import Settings
from .generated_storyboard_models import (
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardPlan,
    GeneratedStoryboardPlanShot,
    StoryboardPlanner,
)
from .openai_structured_outputs import OpenAIStructuredOutputClient


class _StoryboardShotOutput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    subtitle_text: str = Field(min_length=1)
    narration_text: str = Field(min_length=1)
    image_prompt: str = Field(min_length=1)
    duration_seconds: float = Field(ge=2.0, le=8.0)


class _StoryboardPlanOutput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    shots: list[_StoryboardShotOutput] = Field(min_length=3, max_length=4)


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
    def __init__(self, *, client: OpenAIStructuredOutputClient) -> None:
        self._client = client

    @classmethod
    def from_settings(cls, settings: Settings) -> "OpenAIStoryboardPlanner":
        return cls(
            client=OpenAIStructuredOutputClient(
                api_key=settings.openai_api_key,
                model=settings.openai_narrative_model,
                timeout_seconds=settings.openai_timeout_seconds,
            )
        )

    def plan(self, request: GeneratedStoryboardCutsceneRequest) -> GeneratedStoryboardPlan:
        system_prompt = (
            "You are writing a concise storyboard cutscene for a cozy farm tutorial game. "
            "Return only structured shot data. Each shot must have readable subtitle text, matching narration text, "
            "a strong image prompt for concept-art generation, and a short duration. "
            "Keep continuity with the named character and gameplay goal, but avoid repeating the same phrasing across shots."
        )
        subject_label = request.context.crop_name or request.context.focus_label or "farm task"
        user_prompt = json.dumps(
            {
                "display_name": request.display_name,
                "story_brief": request.story_brief,
                "style_preset_id": request.style_preset_id,
                "scene_name": request.scene_name,
                "next_scene_name": request.next_scene_name,
                "character_name": request.context.character_name,
                "subject_label": subject_label,
                "minigame_goal": request.context.minigame_goal,
                "continuity_reference_mode": request.continuity_reference_mode,
                "explicit_reference_image_paths": request.reference_image_paths,
                "requirements": [
                    "Write 3 or 4 storyboard shots.",
                    "Make the cutscene lead directly into the stated minigame goal.",
                    "Keep each subtitle concise and readable in Unity.",
                    "Make narration text natural for ElevenLabs voice playback.",
                    "Image prompts must be fresh compositions and must not include UI text or subtitle panels.",
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
