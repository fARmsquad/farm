from __future__ import annotations

import json
from typing import Protocol

from pydantic import BaseModel, ConfigDict, Field

from .config import Settings
from .openai_structured_outputs import OpenAIStructuredOutputClient


class StorySequenceGeneratorOption(BaseModel):
    model_config = ConfigDict(extra="forbid")

    generator_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    minigame_id: str = Field(min_length=1)
    fit_tags: list[str] = Field(default_factory=list)
    preview_text_template: str = Field(min_length=1)


class StorySequenceTurnDirective(BaseModel):
    model_config = ConfigDict(extra="forbid")

    generator_id: str = Field(min_length=1)
    character_name: str = Field(min_length=1)
    cutscene_display_name: str = Field(min_length=1)
    story_brief: str = Field(min_length=1)


class StorySequenceTurnDirector(Protocol):
    def choose_turn(
        self,
        *,
        narrative_seed: str,
        beat_cursor: int,
        fit_tags: list[str],
        world_state: list[str],
        difficulty_band: str | None,
        last_minigame_goal: str,
        recent_turn_summaries: list[str],
        candidate_generators: list[StorySequenceGeneratorOption],
        candidate_character_names: list[str],
        default_generator_id: str,
        default_character_name: str,
    ) -> StorySequenceTurnDirective | None:
        ...


class OpenAIStorySequenceTurnDirector:
    def __init__(self, *, client: OpenAIStructuredOutputClient) -> None:
        self._client = client

    @classmethod
    def from_settings(cls, settings: Settings) -> "OpenAIStorySequenceTurnDirector":
        return cls(
            client=OpenAIStructuredOutputClient(
                api_key=settings.openai_api_key,
                model=settings.openai_narrative_model,
                timeout_seconds=settings.openai_timeout_seconds,
            )
        )

    def choose_turn(
        self,
        *,
        narrative_seed: str,
        beat_cursor: int,
        fit_tags: list[str],
        world_state: list[str],
        difficulty_band: str | None,
        last_minigame_goal: str,
        recent_turn_summaries: list[str],
        candidate_generators: list[StorySequenceGeneratorOption],
        candidate_character_names: list[str],
        default_generator_id: str,
        default_character_name: str,
    ) -> StorySequenceTurnDirective:
        system_prompt = (
            "You are the narrative turn director for a cozy farm tutorial game. "
            "Choose exactly one allowed minigame generator and one allowed speaking character. "
            "Write a short cutscene title and a concise story brief that naturally leads into the next minigame. "
            "Stay grounded, warm, readable, and forward-moving. Do not invent generator IDs, scenes, or characters outside the allowed lists."
        )
        user_prompt = json.dumps(
            {
                "narrative_seed": narrative_seed,
                "beat_cursor": beat_cursor,
                "fit_tags": fit_tags,
                "world_state": world_state,
                "difficulty_band": difficulty_band,
                "last_minigame_goal": last_minigame_goal,
                "recent_turn_summaries": recent_turn_summaries,
                "candidate_generators": [option.model_dump(mode="json") for option in candidate_generators],
                "candidate_character_names": candidate_character_names,
                "default_generator_id": default_generator_id,
                "default_character_name": default_character_name,
                "requirements": [
                    "Pick one generator_id exactly from candidate_generators.",
                    "Pick one character_name exactly from candidate_character_names.",
                    "The story_brief must set up the next minigame in one to three sentences.",
                    "Prefer variety relative to recent_turn_summaries when possible.",
                    "Keep the tone cozy, playable, and specific to the farm context.",
                ],
            },
            indent=2,
        )
        return self._client.generate(
            response_model=StorySequenceTurnDirective,
            schema_name="farm_story_turn_directive",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )
