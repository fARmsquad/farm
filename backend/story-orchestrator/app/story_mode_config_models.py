from __future__ import annotations

from pydantic import BaseModel, Field

from .minigame_generator_models import MinigameGeneratorDefinition


class StoryTypeDefinition(BaseModel):
    story_type_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    description: str = Field(min_length=1)
    narrative_seed: str = Field(min_length=1)
    fit_tags: list[str] = Field(default_factory=list)
    world_state_bias: list[str] = Field(default_factory=list)
    character_pool: list[str] = Field(default_factory=list)
    allowed_generator_ids: list[str] = Field(default_factory=list)
    prompt_directives: list[str] = Field(default_factory=list)


class PromptStructureDefinition(BaseModel):
    prompt_structure_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    description: str = Field(min_length=1)
    director_prompt_directives: list[str] = Field(default_factory=list)
    storyboard_prompt_directives: list[str] = Field(default_factory=list)


class MinigameStoryParameterHint(BaseModel):
    parameter_name: str = Field(min_length=1)
    story_role: str = Field(min_length=1)
    llm_guidance: str = Field(min_length=1)


class MinigameStorySurface(BaseModel):
    generator: MinigameGeneratorDefinition
    adapter_id: str = Field(min_length=1)
    scene_name: str = Field(min_length=1)
    story_purposes: list[str] = Field(default_factory=list)
    story_hook_template: str = Field(min_length=1)
    llm_prompt_directives: list[str] = Field(default_factory=list)
    parameter_prompt_hints: list[MinigameStoryParameterHint] = Field(default_factory=list)


class RuntimeStoryModeConfiguration(BaseModel):
    contract_version: str = "runtime-story-mode/v1"
    default_story_type_id: str = Field(min_length=1)
    default_prompt_structure_id: str = Field(min_length=1)
    story_types: list[StoryTypeDefinition] = Field(default_factory=list)
    prompt_structures: list[PromptStructureDefinition] = Field(default_factory=list)
    minigame_surfaces: list[MinigameStorySurface] = Field(default_factory=list)
