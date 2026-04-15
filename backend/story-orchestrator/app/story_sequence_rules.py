from __future__ import annotations

from random import Random
from typing import Any

from .story_sequence_models import StorySequenceSessionRecord


def build_turn_parameters(session: StorySequenceSessionRecord, generator_id: str) -> dict[str, Any]:
    if generator_id == "plant_rows_v1":
        return _plant_rows_parameters(session)
    if generator_id == "find_tools_cluster_v1":
        return _find_tools_parameters(session)
    if generator_id == "chicken_chase_intro_v1":
        return _chicken_chase_parameters(session)
    raise RuntimeError(f"Sequence planner does not know generator '{generator_id}'.")


def updated_world_state(session: StorySequenceSessionRecord, turn_index: int) -> list[str]:
    world_state = list(dict.fromkeys(session.state.world_state))
    if turn_index == 0:
        world_state = [flag for flag in world_state if flag != "earliest_tutorial_bridge"]
        world_state.extend(["tomatoes_unlocked", "watering_tools_unlocked"])
    elif turn_index == 1:
        world_state.extend(["planting_tools_unlocked", "corn_unlocked"])
    return list(dict.fromkeys(world_state))


def _plant_rows_parameters(session: StorySequenceSessionRecord) -> dict[str, Any]:
    rng = _rng(session, "plant_rows")
    crop_options = ["carrot"]
    if "tomatoes_unlocked" in set(session.state.world_state):
        crop_options.append("tomato")
    if "corn_unlocked" in set(session.state.world_state):
        crop_options.append("corn")

    crop_type = _pick_preferred(crop_options, session.state.recent_crop_types)
    target_count = min(8, 5 + session.state.beat_cursor)
    row_count = 3 if target_count >= 6 and rng.choice([False, True]) else 2
    assist_level = "high" if session.state.beat_cursor == 0 else "medium"
    return {
        "cropType": crop_type,
        "targetCount": target_count,
        "timeLimitSeconds": 300 if target_count <= 6 else 360,
        "rowCount": row_count,
        "assistLevel": assist_level,
    }


def _find_tools_parameters(session: StorySequenceSessionRecord) -> dict[str, Any]:
    rng = _rng(session, "find_tools")
    tool_sets = ["starter"]
    if "watering_tools_unlocked" in set(session.state.world_state):
        tool_sets.append("watering")
    if "planting_tools_unlocked" in set(session.state.world_state):
        tool_sets.append("planting")

    zone_options = ["yard", "shed_edge"]
    if "earliest_tutorial_bridge" not in set(session.state.world_state):
        zone_options.append("field_path")

    tool_count = 2 if session.state.beat_cursor < 2 else 3
    hint_strength = "strong" if tool_count == 3 else rng.choice(["strong", "medium"])
    target_tool_set = tool_sets[min(session.state.beat_cursor, len(tool_sets) - 1)]
    search_zone = zone_options[session.state.beat_cursor % len(zone_options)]
    return {
        "targetToolSet": target_tool_set,
        "toolCount": tool_count,
        "searchZone": search_zone,
        "hintStrength": hint_strength,
        "timeLimitSeconds": 240 if target_tool_set == "starter" else 300,
    }


def _chicken_chase_parameters(session: StorySequenceSessionRecord) -> dict[str, Any]:
    capture_count = 1 if session.state.beat_cursor < 3 else 2
    chicken_count = max(2, capture_count + 1)
    arena_preset_id = "tutorial_pen_small" if capture_count == 1 else "tutorial_pen_medium"
    return {
        "targetCaptureCount": capture_count,
        "chickenCount": chicken_count,
        "arenaPresetId": arena_preset_id,
        "timeLimitSeconds": 120 if arena_preset_id == "tutorial_pen_medium" else 90,
        "guidanceLevel": "high" if session.state.beat_cursor == 0 else "medium",
    }


def _pick_preferred(options: list[str], recent_values: list[str]) -> str:
    recent = set(recent_values)
    for option in options:
        if option not in recent:
            return option
    return options[0]


def _rng(session: StorySequenceSessionRecord, label: str) -> Random:
    return Random(f"{session.state.seed}:{session.state.beat_cursor}:{label}")
