from __future__ import annotations

from pathlib import Path
from typing import Any

from .generated_package_assembly import GeneratedPackageAssemblyResult
from .minigame_generators import MinigameGeneratorCatalog
from .runtime_models import RuntimeContinuityImageRecord, RuntimeSession, RuntimeTurnRecord
from .story_mode_config import MINIGAME_SCENES
from .story_sequence_turn_director import StorySequenceGeneratorOption, StorySequenceTurnDirector

PROOF_CUTSCENE_SCENE = "PostChickenCutscene"
LEGACY_RUNTIME_DEFAULT_GENERATOR_IDS = ("plant_rows_v1",)


def build_story_brief(
    session: RuntimeSession,
    *,
    story_seed: str,
    character_name: str,
    story_hook: str,
    prompt_structure_description: str,
) -> str:
    if session.state.last_minigame_goal:
        return (
            f"{story_seed} {character_name} links the previous goal "
            f"'{session.state.last_minigame_goal}' into the next beat. "
            f"{story_hook} Shape the turn so it {prompt_structure_description.lower()}."
        )
    return f"{story_seed} {story_hook} Shape the turn so it {prompt_structure_description.lower()}."


def objective_text_for(generator_id: str, parameters: dict[str, Any]) -> str:
    if generator_id == "plant_rows_v1":
        crop_name = _pluralize_runtime_crop(parameters["cropType"], int(parameters["targetCount"]))
        return f"Plant {parameters['targetCount']} {crop_name} across {parameters['rowCount']} rows before time runs out."
    if generator_id == "find_tools_cluster_v1":
        return f"Recover {parameters['toolCount']} {parameters['targetToolSet']} tools around the {parameters['searchZone']}."
    return f"Catch {parameters['targetCaptureCount']} runaway chickens inside the {parameters['arenaPresetId']} pen."


def mission_configuration_summary(generator_id: str, parameters: dict[str, Any]) -> str:
    if generator_id == "plant_rows_v1":
        return f"Plant {parameters['targetCount']} {parameters['cropType']} seeds across {parameters['rowCount']} neat rows in {parameters['timeLimitSeconds']} seconds with {parameters['assistLevel']} guidance."
    if generator_id == "find_tools_cluster_v1":
        return f"Recover {parameters['toolCount']} {parameters['targetToolSet']} tools around the {parameters['searchZone'].replace('_', ' ')} with {parameters['hintStrength']} hints."
    return f"Catch {parameters['targetCaptureCount']} chickens from a flock of {parameters['chickenCount']} in the {parameters['arenaPresetId'].replace('_', ' ')} within {parameters['timeLimitSeconds']} seconds using {parameters['guidanceLevel']} guidance."


def ordered_generator_ids(catalog: MinigameGeneratorCatalog, session: RuntimeSession) -> list[str]:
    allowed = [
        generator_id
        for generator_id in session.state.allowed_generator_ids
        if catalog.get_definition(generator_id) is not None
    ]
    if allowed:
        return allowed

    legacy_default = [
        generator_id
        for generator_id in LEGACY_RUNTIME_DEFAULT_GENERATOR_IDS
        if catalog.get_definition(generator_id) is not None
    ]
    if legacy_default:
        return legacy_default

    definitions = catalog.list_definitions()
    start_index = session.state.beat_cursor % len(definitions)
    ordered = definitions[start_index:] + definitions[:start_index]
    return [definition.generator_id for definition in ordered]


def choose_directed_turn(
    turn_director: StorySequenceTurnDirector | None,
    session: RuntimeSession,
    prior_turns: list[RuntimeTurnRecord],
    candidate_generators: list[StorySequenceGeneratorOption],
    default_generator_id: str,
    default_character_name: str,
    *,
    story_type_id: str = "",
    story_type_display_name: str = "",
    story_type_prompt_directives: list[str] | None = None,
    prompt_structure_id: str = "",
    prompt_structure_display_name: str = "",
    prompt_structure_directives: list[str] | None = None,
):
    if turn_director is None:
        return None
    try:
        return turn_director.choose_turn(
            narrative_seed=session.state.narrative_seed,
            beat_cursor=session.state.beat_cursor,
            fit_tags=list(session.state.fit_tags),
            world_state=list(session.state.world_state),
            difficulty_band=session.state.difficulty_band,
            last_minigame_goal=session.state.last_minigame_goal,
            recent_turn_summaries=[turn.summary for turn in prior_turns[-3:]],
            story_type_id=story_type_id,
            story_type_display_name=story_type_display_name,
            story_type_prompt_directives=list(story_type_prompt_directives or []),
            prompt_structure_id=prompt_structure_id,
            prompt_structure_display_name=prompt_structure_display_name,
            prompt_structure_directives=list(prompt_structure_directives or []),
            candidate_generators=candidate_generators,
            candidate_character_names=list(session.state.character_pool),
            default_generator_id=default_generator_id,
            default_character_name=default_character_name,
        )
    except Exception:
        return None


def select_character_name(session: RuntimeSession) -> str:
    index = session.state.beat_cursor % len(session.state.character_pool)
    return session.state.character_pool[index]


def display_name_for(catalog: MinigameGeneratorCatalog, generator_id: str) -> str:
    definition = catalog.get_definition(generator_id)
    if definition is None:
        raise RuntimeError(f"Generator definition '{generator_id}' was not found.")
    return definition.display_name


def prior_story_summary(prior_turns: list[RuntimeTurnRecord]) -> str:
    if not prior_turns:
        return "No previous turn summary yet. Use the existing farm state and story brief to create the next conflict."
    recent = [turn.summary for turn in prior_turns[-2:] if turn.summary]
    if not recent:
        return "No previous turn summary yet. Use the existing farm state and story brief to create the next conflict."
    return " ".join(recent)


def present_character_names(session: RuntimeSession, selected_character_name: str) -> list[str]:
    ordered = [selected_character_name, *session.state.recent_character_names, *session.state.character_pool]
    deduped: list[str] = []
    for name in ordered:
        if name and name not in deduped:
            deduped.append(name)
    return deduped


def select_continuity_reference_paths(session: RuntimeSession, character_name: str) -> list[str]:
    available = [image for image in session.state.continuity_images if image.stored_path and Path(image.stored_path).exists()]
    same_character = [image for image in available if image.character_name == character_name]
    selected = _select_continuity_anchor_path(same_character) or _select_continuity_anchor_path(available)
    return [selected] if selected else []


def build_turn_summary(
    character_name: str,
    generator_id: str,
    result: GeneratedPackageAssemblyResult,
) -> str:
    objective_text = ""
    if result.minigame_result is not None:
        objective_text = str(result.minigame_result.materialized_minigame.get("ObjectiveText", ""))
    return f"{character_name}: {objective_text}" if objective_text else f"{character_name}: planned {generator_id}."


def roll_recent(values: list[str], new_value: str, limit: int) -> list[str]:
    rolled = list(values)
    rolled.append(new_value)
    return rolled[-limit:]


def roll_continuity_images(
    values: list[RuntimeContinuityImageRecord],
    new_values: list[RuntimeContinuityImageRecord],
    limit: int,
) -> list[RuntimeContinuityImageRecord]:
    rolled = list(values)
    rolled.extend(new_values)
    return rolled[-limit:]


def find_generated_beat(package: dict[str, Any], beat_id: str) -> dict[str, Any]:
    for beat in package.get("Beats", []):
        if beat.get("BeatId") == beat_id:
            return beat
    raise RuntimeError(f"Generated beat '{beat_id}' was not found in the runtime package.")


def _select_continuity_anchor_path(images: list[RuntimeContinuityImageRecord]) -> str:
    if not images:
        return ""
    latest_turn_index = max(image.turn_index for image in images)
    latest_turn_images = [image for image in images if image.turn_index == latest_turn_index]
    shot_priority = {"shot_01": 0, "shot_02": 1, "shot_03": 2}
    latest_turn_images.sort(key=lambda image: (shot_priority.get(image.shot_id, 99), image.shot_id))
    return latest_turn_images[0].stored_path


def _pluralize_runtime_crop(crop_type: str, count: int) -> str:
    if count == 1:
        return crop_type

    irregular = {"tomato": "tomatoes", "corn": "corn"}
    return irregular.get(crop_type, f"{crop_type}s")
