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
    story_purposes: list[str] = Field(default_factory=list)
    story_hook_template: str = Field(default="")
    llm_prompt_directives: list[str] = Field(default_factory=list)
    parameter_prompt_hints: list[str] = Field(default_factory=list)


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
            "The cutscene must continue the existing story context, create a concrete conflict or change in the world, "
            "and make the upcoming mission feel like a natural consequence. "
            "Treat this like the next beat in an ongoing story, not a reset, recap card, or tutorial prompt. "
            "Characters should notice something, want something, lose something, discover something, or interrupt each other. "
            "Stay grounded, warm, readable, and forward-moving. Do not invent generator IDs, scenes, parameters, or characters outside the allowed lists.\n\n"
            "Continuity rules:\n"
            "- Read the prior_story_summary field carefully and explicitly reference earlier characters, locations, or unresolved threads in your cutscene_display_name and story_brief.\n"
            "- Choose continuity over novelty: when in doubt, extend an existing thread rather than starting a new one.\n"
            "- The story_brief must connect to what just happened, not pretend it didn't."
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
                    "The story_brief must acknowledge previous context or world state instead of feeling like a reset.",
                    "The story_brief must introduce a concrete problem, interruption, discovery, or character motive before the player mission begins.",
                    "The story_brief must imply what is special about the chosen generator configuration, as if the mission were a mad-libs style variation of the farm situation.",
                    "The story_brief must feel like conflict and events are happening in the world, not like an instruction to the player.",
                    "The final sentence should hand off into the mission as the natural next action for the player character.",
                    "Prefer variety relative to recent_turn_summaries when possible.",
                    "Keep the tone cozy, playable, and specific to the farm context.",
                ],
            },
            indent=2,
        )
        directive = self._client.generate(
            response_model=StorySequenceTurnDirective,
            schema_name="farm_story_turn_directive",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )
        if _is_valid_directive(directive, candidate_generators):
            return directive
        return self._retry_with_nudge(
            invalid_directive=directive,
            system_prompt=system_prompt,
            user_prompt=user_prompt,
            candidate_generators=candidate_generators,
        )

    def _retry_with_nudge(
        self,
        *,
        invalid_directive: StorySequenceTurnDirective,
        system_prompt: str,
        user_prompt: str,
        candidate_generators: list[StorySequenceGeneratorOption],
    ) -> StorySequenceTurnDirective:
        allowed_ids_csv = ", ".join(option.generator_id for option in candidate_generators)
        nudge = (
            f"You previously returned an invalid generator_id \"{invalid_directive.generator_id}\". "
            f"You MUST choose a generator_id from this list ONLY: {allowed_ids_csv}.\n\n"
        )
        retry_prompt = nudge + user_prompt
        retry_directive = self._client.generate(
            response_model=StorySequenceTurnDirective,
            schema_name="farm_story_turn_directive",
            system_prompt=system_prompt,
            user_prompt=retry_prompt,
        )
        if _is_valid_directive(retry_directive, candidate_generators):
            return retry_directive
        raise ValueError(
            f"generator_id not in allowed_generator_ids after retry: {retry_directive.generator_id!r}"
        )


def _is_valid_directive(
    directive: StorySequenceTurnDirective,
    candidate_generators: list[StorySequenceGeneratorOption],
) -> bool:
    if not candidate_generators:
        return True
    allowed_ids = {option.generator_id for option in candidate_generators}
    return directive.generator_id in allowed_ids
