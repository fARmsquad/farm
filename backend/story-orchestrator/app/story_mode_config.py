from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from .minigame_generators import MinigameGeneratorCatalog
from .story_mode_config_models import (
    MinigameStoryParameterHint,
    MinigameStorySurface,
    PromptStructureDefinition,
    RuntimeStoryModeConfiguration,
    StoryTypeDefinition,
)


DEFAULT_STORY_TYPE_ID = "farm_cozy_starter_v1"
DEFAULT_PROMPT_STRUCTURE_ID = "conflict_escalation_handoff_v1"

MINIGAME_SCENES = {
    "plant_rows_v1": "FarmMain",
    "find_tools_cluster_v1": "FindToolsGame",
    "chicken_chase_intro_v1": "ChickenChaseGame",
}

MINIGAME_ADAPTER_IDS = {
    "plant_rows_v1": "tutorial.plant_rows",
    "find_tools_cluster_v1": "tutorial.find_tools",
    "chicken_chase_intro_v1": "tutorial.chicken_chase",
}


@dataclass(frozen=True)
class ResolvedStoryModeSelection:
    story_type: StoryTypeDefinition
    prompt_structure: PromptStructureDefinition
    fit_tags: list[str]
    world_state: list[str]
    character_pool: list[str]
    narrative_seed: str
    allowed_generator_ids: list[str]


class RuntimeStoryModeConfigCatalog:
    def __init__(
        self,
        *,
        story_types: list[StoryTypeDefinition],
        prompt_structures: list[PromptStructureDefinition],
        minigame_surfaces: list[MinigameStorySurface],
        default_story_type_id: str = DEFAULT_STORY_TYPE_ID,
        default_prompt_structure_id: str = DEFAULT_PROMPT_STRUCTURE_ID,
    ) -> None:
        self._story_types = {story_type.story_type_id: story_type for story_type in story_types}
        self._prompt_structures = {
            prompt_structure.prompt_structure_id: prompt_structure
            for prompt_structure in prompt_structures
        }
        self._minigame_surfaces = {
            surface.generator.generator_id: surface for surface in minigame_surfaces
        }
        self.default_story_type_id = default_story_type_id
        self.default_prompt_structure_id = default_prompt_structure_id

    @classmethod
    def default(
        cls,
        *,
        catalog: MinigameGeneratorCatalog | None = None,
    ) -> "RuntimeStoryModeConfigCatalog":
        minigame_catalog = catalog or MinigameGeneratorCatalog.default()
        return cls(
            story_types=_build_story_types(),
            prompt_structures=_build_prompt_structures(),
            minigame_surfaces=_build_minigame_surfaces(minigame_catalog),
        )

    def configuration(self) -> RuntimeStoryModeConfiguration:
        return RuntimeStoryModeConfiguration(
            default_story_type_id=self.default_story_type_id,
            default_prompt_structure_id=self.default_prompt_structure_id,
            story_types=self.list_story_types(),
            prompt_structures=self.list_prompt_structures(),
            minigame_surfaces=self.list_minigame_surfaces(),
        )

    def list_story_types(self) -> list[StoryTypeDefinition]:
        return list(self._story_types.values())

    def list_prompt_structures(self) -> list[PromptStructureDefinition]:
        return list(self._prompt_structures.values())

    def list_minigame_surfaces(self) -> list[MinigameStorySurface]:
        return list(self._minigame_surfaces.values())

    def get_story_type(self, story_type_id: str) -> StoryTypeDefinition | None:
        return self._story_types.get(story_type_id)

    def get_prompt_structure(self, prompt_structure_id: str) -> PromptStructureDefinition | None:
        return self._prompt_structures.get(prompt_structure_id)

    def get_minigame_surface(self, generator_id: str) -> MinigameStorySurface | None:
        return self._minigame_surfaces.get(generator_id)

    def resolve_selection(
        self,
        *,
        story_type_id: str,
        prompt_structure_id: str,
        fit_tags: list[str],
        world_state: list[str],
        character_pool: list[str],
        narrative_seed: str,
    ) -> ResolvedStoryModeSelection:
        resolved_story_type_id = story_type_id or self.default_story_type_id
        resolved_prompt_structure_id = prompt_structure_id or self.default_prompt_structure_id

        story_type = self.get_story_type(resolved_story_type_id)
        if story_type is None:
            raise ValueError(f"Unknown runtime story type '{resolved_story_type_id}'.")

        prompt_structure = self.get_prompt_structure(resolved_prompt_structure_id)
        if prompt_structure is None:
            raise ValueError(f"Unknown runtime prompt structure '{resolved_prompt_structure_id}'.")

        combined_narrative_seed = story_type.narrative_seed
        if narrative_seed and narrative_seed.strip() and narrative_seed.strip() != story_type.narrative_seed:
            combined_narrative_seed = f"{story_type.narrative_seed} {narrative_seed.strip()}".strip()

        return ResolvedStoryModeSelection(
            story_type=story_type,
            prompt_structure=prompt_structure,
            fit_tags=_dedupe(story_type.fit_tags + list(fit_tags)),
            world_state=_dedupe(story_type.world_state_bias + list(world_state)),
            character_pool=_dedupe(story_type.character_pool + list(character_pool)),
            narrative_seed=combined_narrative_seed,
            allowed_generator_ids=_dedupe(list(story_type.allowed_generator_ids)),
        )


class _SafeFormatDict(dict[str, Any]):
    def __missing__(self, key: str) -> str:
        return f"{{{key}}}"


def render_story_hook(
    surface: MinigameStorySurface,
    *,
    character_name: str,
    objective_text: str,
    parameters: dict[str, Any],
) -> str:
    format_values: dict[str, Any] = {
        "character_name": character_name,
        "objective_text": objective_text,
    }
    format_values.update(parameters)
    return surface.story_hook_template.format_map(_SafeFormatDict(format_values))


def _build_story_types() -> list[StoryTypeDefinition]:
    return [
        StoryTypeDefinition(
            story_type_id="farm_cozy_starter_v1",
            display_name="Cozy Starter Day",
            description="Warm, confidence-building farm chores that gently teach the core rhythm.",
            narrative_seed="Tell a warm first-day-on-the-farm story where practical chores build the player's confidence.",
            fit_tags=["cozy", "teaching", "starter", "farm"],
            world_state_bias=["farm_plots_unlocked", "earliest_tutorial_bridge"],
            character_pool=["Old Garrett", "Miss Clara", "Young Pip"],
            allowed_generator_ids=["plant_rows_v1"],
            prompt_directives=[
                "Keep the tone warm and confidence-building.",
                "Make each mission feel like the next natural chore on the farm.",
                "Favor practical rhythm and farm routine over mystery or danger.",
            ],
        ),
        StoryTypeDefinition(
            story_type_id="tool_hunt_detour_v1",
            display_name="Tool Hunt Detour",
            description="Practical interruptions and missing equipment push the player into recovery beats.",
            narrative_seed="Turn the next beat into a practical interruption where misplaced equipment blocks the farm rhythm until the player helps.",
            fit_tags=["cozy", "search", "detour", "farm"],
            world_state_bias=["tool_search_enabled", "earliest_tutorial_bridge"],
            character_pool=["Miss Clara", "Old Garrett", "Young Pip"],
            allowed_generator_ids=["find_tools_cluster_v1"],
            prompt_directives=[
                "Center the beat on a practical interruption caused by misplaced equipment.",
                "Keep the tension mild and grounded in chores, clutter, and discovery.",
                "Use clues, missing items, or small accidents to move the player.",
            ],
        ),
        StoryTypeDefinition(
            story_type_id="chicken_ruckus_intro_v1",
            display_name="Chicken Ruckus Intro",
            description="Playful animal chaos breaks the calm and creates an immediate hands-on response.",
            narrative_seed="Use playful animal chaos to break the calm and push the player into quick farm action.",
            fit_tags=["animal", "playful", "light-pressure", "farm"],
            world_state_bias=["chicken_pen_intro_available", "earliest_tutorial_bridge"],
            character_pool=["Old Garrett", "Young Pip", "Miss Clara"],
            allowed_generator_ids=["chicken_chase_intro_v1"],
            prompt_directives=[
                "Use animal behavior and character reaction as the engine of the scene.",
                "Keep the stakes light but immediate.",
                "Let characters react, tease, or improvise instead of explaining too much.",
            ],
        ),
        StoryTypeDefinition(
            story_type_id="mixed_tutorial_arc_v1",
            display_name="Mixed Tutorial Arc",
            description="Alternate between chores, searches, and playful chaos across a bounded tutorial sequence.",
            narrative_seed="Mix chores, searches, and light chaos into one cohesive tutorial farm story.",
            fit_tags=["cozy", "teaching", "varied", "farm"],
            world_state_bias=[
                "farm_plots_unlocked",
                "tool_search_enabled",
                "chicken_pen_intro_available",
                "earliest_tutorial_bridge",
            ],
            character_pool=["Old Garrett", "Miss Clara", "Young Pip"],
            allowed_generator_ids=[
                "plant_rows_v1",
                "find_tools_cluster_v1",
                "chicken_chase_intro_v1",
            ],
            prompt_directives=[
                "Let each turn feel different while still belonging to the same farm day.",
                "Use continuity between chores, searches, and playful disruptions.",
                "Keep the emotional tone readable and player-friendly.",
            ],
        ),
    ]


def _build_prompt_structures() -> list[PromptStructureDefinition]:
    return [
        PromptStructureDefinition(
            prompt_structure_id="conflict_escalation_handoff_v1",
            display_name="Conflict Escalation Handoff",
            description="Anchor prior context, escalate a concrete interruption, and hand off directly into action.",
            director_prompt_directives=[
                "Anchor prior context quickly before introducing something new.",
                "Introduce a concrete interruption, problem, or change in the world.",
                "End with the mission as the natural immediate response.",
            ],
            storyboard_prompt_directives=[
                "Shot one should establish the current farm situation and who is present.",
                "Middle shots should escalate the interruption or reveal the problem clearly.",
                "The final shot should push the player into the minigame as the next natural action.",
            ],
        ),
        PromptStructureDefinition(
            prompt_structure_id="character_request_payoff_v1",
            display_name="Character Request Payoff",
            description="Lead with an NPC request, show the obstacle, and turn the mission into the answer.",
            director_prompt_directives=[
                "Center the scene on one named character asking for help or guidance.",
                "Make the obstacle visible before the player moves.",
                "Frame the mission as the payoff for helping that character.",
            ],
            storyboard_prompt_directives=[
                "Open with the character's ask or concern.",
                "Use a middle shot to reveal why the request matters right now.",
                "Land on the mission as the answer to that request.",
            ],
        ),
        PromptStructureDefinition(
            prompt_structure_id="discovery_pressure_release_v1",
            display_name="Discovery Pressure Release",
            description="Start from a clue or reveal, build mild pressure, and release into energetic action.",
            director_prompt_directives=[
                "Begin from an odd clue, reveal, or small surprise.",
                "Let the reveal create mild pressure or urgency.",
                "Turn the mission into the release valve for that pressure.",
            ],
            storyboard_prompt_directives=[
                "Open on the clue, sign, or discovery.",
                "Escalate by making the consequence legible in the world.",
                "Finish on a clear forward motion into the playable task.",
            ],
        ),
    ]


def _build_minigame_surfaces(catalog: MinigameGeneratorCatalog) -> list[MinigameStorySurface]:
    return [
        MinigameStorySurface(
            generator=_require_generator(catalog, "plant_rows_v1"),
            adapter_id=MINIGAME_ADAPTER_IDS["plant_rows_v1"],
            scene_name=MINIGAME_SCENES["plant_rows_v1"],
            story_purposes=["teaching rhythm", "restoring order", "first real farm work"],
            story_hook_template=(
                "{character_name} realizes the {cropType} rows are the steadiest way to get the farm back on pace. "
                "{objective_text}"
            ),
            llm_prompt_directives=[
                "Make the crop choice feel like a real farm need, season cue, or character preference.",
                "Let row count and assist level imply how orderly or chaotic the field feels.",
                "End with the player stepping into the rows as the natural fix.",
            ],
            parameter_prompt_hints=[
                MinigameStoryParameterHint(
                    parameter_name="cropType",
                    story_role="What crop matters in this beat and why it matters now.",
                    llm_guidance="Use the crop choice to imply seasonality, urgency, or a character preference.",
                ),
                MinigameStoryParameterHint(
                    parameter_name="targetCount",
                    story_role="How much work the farm needs from the player.",
                    llm_guidance="Use the count to suggest scale without making the scene feel abstract.",
                ),
                MinigameStoryParameterHint(
                    parameter_name="rowCount",
                    story_role="How broad the visible field task feels.",
                    llm_guidance="Let more rows imply a messier or more ambitious field setup.",
                ),
                MinigameStoryParameterHint(
                    parameter_name="assistLevel",
                    story_role="How guided the player should feel.",
                    llm_guidance="Tie higher assist to patient teaching beats and lower assist to earned confidence.",
                ),
            ],
        ),
        MinigameStorySurface(
            generator=_require_generator(catalog, "find_tools_cluster_v1"),
            adapter_id=MINIGAME_ADAPTER_IDS["find_tools_cluster_v1"],
            scene_name=MINIGAME_SCENES["find_tools_cluster_v1"],
            story_purposes=["tool recovery", "practical interruption", "scene transition"],
            story_hook_template=(
                "{character_name} hears the shed rattle and realizes the {targetToolSet} tools are scattered around the {searchZone}. "
                "{objective_text}"
            ),
            llm_prompt_directives=[
                "Make the missing tools the direct reason the mission must happen now.",
                "Use the search zone as a visible clue trail or staging area.",
                "Let the specific tool set imply what kind of farm work is blocked.",
            ],
            parameter_prompt_hints=[
                MinigameStoryParameterHint(
                    parameter_name="targetToolSet",
                    story_role="What kind of work is blocked by the missing tools.",
                    llm_guidance="Use the tool family to imply the interrupted farm task.",
                ),
                MinigameStoryParameterHint(
                    parameter_name="searchZone",
                    story_role="Where the interruption visibly spread across the farm.",
                    llm_guidance="Turn the search zone into a concrete location clue, not just a label.",
                ),
                MinigameStoryParameterHint(
                    parameter_name="hintStrength",
                    story_role="How obvious the clue trail is.",
                    llm_guidance="Stronger hints should feel like visible signs or a character guiding the player.",
                ),
            ],
        ),
        MinigameStorySurface(
            generator=_require_generator(catalog, "chicken_chase_intro_v1"),
            adapter_id=MINIGAME_ADAPTER_IDS["chicken_chase_intro_v1"],
            scene_name=MINIGAME_SCENES["chicken_chase_intro_v1"],
            story_purposes=["playful chaos", "animal response", "hands-on interruption"],
            story_hook_template=(
                "{character_name} sees the pen erupt again and has to bring the yard back under control before work can continue. "
                "{objective_text}"
            ),
            llm_prompt_directives=[
                "Use visible flock behavior and character reaction to power the scene.",
                "Keep the mood playful, readable, and immediate.",
                "Make the mission feel like the fastest way to restore order.",
            ],
            parameter_prompt_hints=[
                MinigameStoryParameterHint(
                    parameter_name="targetCaptureCount",
                    story_role="How much animal chaos the player must personally solve.",
                    llm_guidance="Use the capture target to imply scale without turning the scene mean-spirited.",
                ),
                MinigameStoryParameterHint(
                    parameter_name="arenaPresetId",
                    story_role="What the playable pen or chase space feels like.",
                    llm_guidance="Use the arena preset to suggest whether the chaos is tight, crowded, or spread out.",
                ),
                MinigameStoryParameterHint(
                    parameter_name="guidanceLevel",
                    story_role="How coached the player feels while reacting to the chaos.",
                    llm_guidance="Higher guidance should feel like strong adult direction; lower guidance should feel more earned.",
                ),
            ],
        ),
    ]


def _require_generator(catalog: MinigameGeneratorCatalog, generator_id: str):
    definition = catalog.get_definition(generator_id)
    if definition is None:
        raise RuntimeError(f"Story-mode surface requires unknown generator '{generator_id}'.")
    return definition


def _dedupe(values: list[str]) -> list[str]:
    deduped: list[str] = []
    for value in values:
        if value and value not in deduped:
            deduped.append(value)
    return deduped
