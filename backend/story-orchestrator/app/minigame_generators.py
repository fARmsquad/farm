from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Callable

from .minigame_generator_models import (
    MinigameCouplingRule,
    MinigameGenerationContext,
    MinigameGeneratorDefinition,
    MinigameGeneratorValidationResult,
    MinigameParameterDefinition,
    MinigameParameterType,
    MinigameStringMode,
)

RuleCheck = Callable[[MinigameGenerationContext, dict[str, Any]], str | None]


@dataclass(frozen=True)
class _CatalogEntry:
    definition: MinigameGeneratorDefinition
    rule_checks: tuple[RuleCheck, ...]


class MinigameGeneratorCatalog:
    def __init__(self, entries: list[_CatalogEntry]) -> None:
        self._entries = {entry.definition.generator_id: entry for entry in entries}

    @classmethod
    def default(cls) -> "MinigameGeneratorCatalog":
        return cls(_build_default_entries())

    def list_definitions(self) -> list[MinigameGeneratorDefinition]:
        return [entry.definition for entry in self._entries.values()]

    def get_definition(self, generator_id: str) -> MinigameGeneratorDefinition | None:
        entry = self._entries.get(generator_id)
        return entry.definition if entry is not None else None

    def validate_selection(
        self,
        generator_id: str,
        *,
        parameters: dict[str, Any] | None = None,
        context: MinigameGenerationContext | None = None,
    ) -> MinigameGeneratorValidationResult:
        entry = self._entries.get(generator_id)
        if entry is None:
            return MinigameGeneratorValidationResult(
                is_valid=False,
                generator_id=generator_id,
                errors=[f"Unknown minigame generator '{generator_id}'."],
            )

        context = context or MinigameGenerationContext()
        provided_parameters = parameters or {}
        merged_parameters = dict(entry.definition.defaults)
        errors: list[str] = []

        for name, value in provided_parameters.items():
            if name not in entry.definition.parameter_schema:
                errors.append(f"Parameter '{name}' is not defined for generator '{generator_id}'.")
                continue
            merged_parameters[name] = value

        for world_state_flag in entry.definition.required_world_state:
            if world_state_flag not in set(context.world_state):
                errors.append(f"Generator '{generator_id}' requires world state '{world_state_flag}'.")

        if context.difficulty_band and context.difficulty_band not in entry.definition.difficulty_bands:
            errors.append(
                f"Generator '{generator_id}' does not support difficulty band '{context.difficulty_band}'."
            )

        normalized_parameters: dict[str, Any] = {}
        for name, parameter in entry.definition.parameter_schema.items():
            value = merged_parameters.get(name)
            normalized_value, parameter_errors = parameter.validate_and_normalize(value)
            if parameter_errors:
                for parameter_error in parameter_errors:
                    errors.append(f"Parameter '{name}' {parameter_error}")
                continue
            normalized_parameters[name] = normalized_value

        if not errors:
            effective_context = MinigameGenerationContext(
                fit_tags=sorted(set(entry.definition.fit_tags) | set(context.fit_tags)),
                world_state=context.world_state,
                difficulty_band=context.difficulty_band,
            )
            for rule_check in entry.rule_checks:
                rule_error = rule_check(effective_context, normalized_parameters)
                if rule_error:
                    errors.append(rule_error)

        return MinigameGeneratorValidationResult(
            is_valid=len(errors) == 0,
            generator_id=generator_id,
            errors=errors,
            resolved_parameters=normalized_parameters,
            fallback_generator_ids=entry.definition.fallback_generator_ids,
        )


def _build_default_entries() -> list[_CatalogEntry]:
    return [
        _CatalogEntry(
            definition=MinigameGeneratorDefinition(
                generator_id="plant_rows_v1",
                minigame_id="planting",
                display_name="Plant Rows V1",
                fit_tags=["intro", "teaching", "crop-focused", "calm"],
                difficulty_bands=["tutorial", "easy"],
                required_world_state=["farm_plots_unlocked"],
                parameter_schema={
                    "cropType": _enum_parameter("cropType", "Crop to plant.", ["carrot", "tomato", "corn"], "carrot"),
                    "targetCount": _int_parameter("targetCount", "Plants required for success.", 3, 12, 5),
                    "timeLimitSeconds": _int_parameter("timeLimitSeconds", "Time cap for the beat.", 180, 600, 300),
                    "rowCount": _int_parameter("rowCount", "Number of active planting rows.", 1, 4, 2),
                    "assistLevel": _enum_parameter(
                        "assistLevel",
                        "How much guidance the player receives.",
                        ["high", "medium", "low"],
                        "high",
                    ),
                },
                defaults={
                    "cropType": "carrot",
                    "targetCount": 5,
                    "timeLimitSeconds": 300,
                    "rowCount": 2,
                    "assistLevel": "high",
                },
                coupling_rules=[
                    _rule("plant_rows_intro_assist", "Intro beats may not use assistLevel=low."),
                    _rule("plant_rows_dense_rows", "rowCount >= 3 requires targetCount >= 6."),
                    _rule("plant_rows_tomato_unlock", "cropType=tomato requires tomatoes_unlocked."),
                ],
                fallback_generator_ids=["plant_rows_tutorial_safe_v1"],
                preview_text_template="Plant {targetCount} {cropType} across {rowCount} rows in {timeLimitSeconds} seconds.",
            ),
            rule_checks=(_plant_rows_intro_assist_rule, _plant_rows_row_count_rule, _plant_rows_tomato_unlock_rule),
        ),
        _CatalogEntry(
            definition=MinigameGeneratorDefinition(
                generator_id="find_tools_cluster_v1",
                minigame_id="find_tools",
                display_name="Find Tools Cluster V1",
                fit_tags=["bridge", "search", "tool-recovery", "teaching"],
                difficulty_bands=["tutorial", "easy"],
                required_world_state=["tool_search_enabled"],
                parameter_schema={
                    "targetToolSet": _enum_parameter(
                        "targetToolSet",
                        "Which family of tools should be recovered.",
                        ["starter", "watering", "planting"],
                        "starter",
                    ),
                    "toolCount": _int_parameter("toolCount", "How many tools must be found.", 1, 3, 2),
                    "searchZone": _enum_parameter(
                        "searchZone",
                        "Which search area should be used.",
                        ["yard", "shed_edge", "field_path"],
                        "yard",
                    ),
                    "hintStrength": _enum_parameter(
                        "hintStrength",
                        "How directly the player is guided.",
                        ["strong", "medium", "light"],
                        "strong",
                    ),
                    "timeLimitSeconds": _int_parameter("timeLimitSeconds", "Time cap for the scavenger beat.", 90, 420, 240),
                },
                defaults={
                    "targetToolSet": "starter",
                    "toolCount": 2,
                    "searchZone": "yard",
                    "hintStrength": "strong",
                    "timeLimitSeconds": 240,
                },
                coupling_rules=[
                    _rule("find_tools_three_items", "toolCount=3 requires hintStrength to stay at medium or strong."),
                    _rule("find_tools_intro_zone", "searchZone=field_path is invalid for the earliest tutorial bridge."),
                    _rule("find_tools_starter_timer", "starter tool sets should not exceed timeLimitSeconds=300."),
                ],
                fallback_generator_ids=["find_tools_linear_safe_v1"],
                preview_text_template="Recover {toolCount} {targetToolSet} tools in the {searchZone} within {timeLimitSeconds} seconds.",
            ),
            rule_checks=(
                _find_tools_cluster_hint_rule,
                _find_tools_cluster_zone_rule,
                _find_tools_cluster_starter_timer_rule,
            ),
        ),
        _CatalogEntry(
            definition=MinigameGeneratorDefinition(
                generator_id="chicken_chase_intro_v1",
                minigame_id="chicken_chase",
                display_name="Chicken Chase Intro V1",
                fit_tags=["intro", "light-pressure", "animal", "teaching"],
                difficulty_bands=["tutorial", "easy"],
                required_world_state=["chicken_pen_intro_available"],
                parameter_schema={
                    "targetCaptureCount": _int_parameter("targetCaptureCount", "How many chickens the player must catch.", 1, 3, 1),
                    "chickenCount": _int_parameter("chickenCount", "How many chickens spawn in the arena.", 1, 4, 2),
                    "arenaPresetId": _string_parameter(
                        "arenaPresetId",
                        "Arena preset reference.",
                        ["tutorial_pen_small", "tutorial_pen_medium"],
                        "tutorial_pen_small",
                        string_mode=MinigameStringMode.ID_REFERENCE,
                    ),
                    "timeLimitSeconds": _int_parameter("timeLimitSeconds", "Time cap for the chase beat.", 60, 300, 120),
                    "guidanceLevel": _enum_parameter(
                        "guidanceLevel",
                        "How much chase guidance the player receives.",
                        ["high", "medium", "low"],
                        "high",
                    ),
                },
                defaults={
                    "targetCaptureCount": 1,
                    "chickenCount": 2,
                    "arenaPresetId": "tutorial_pen_small",
                    "timeLimitSeconds": 120,
                    "guidanceLevel": "high",
                },
                coupling_rules=[
                    _rule("chicken_chase_capture_cap", "targetCaptureCount may not exceed chickenCount."),
                    _rule("chicken_chase_intro_guidance", "guidanceLevel=low is invalid for intro beats."),
                    _rule("chicken_chase_medium_arena", "tutorial_pen_medium requires timeLimitSeconds >= 120."),
                ],
                fallback_generator_ids=["chicken_chase_basic_safe_v1"],
                preview_text_template="Catch {targetCaptureCount} of {chickenCount} chickens in {arenaPresetId} within {timeLimitSeconds} seconds.",
            ),
            rule_checks=(_chicken_chase_capture_rule, _chicken_chase_intro_guidance_rule, _chicken_chase_arena_time_rule),
        ),
    ]


def _enum_parameter(name: str, description: str, allowed_values: list[str], default: str) -> MinigameParameterDefinition:
    return MinigameParameterDefinition(
        name=name,
        type=MinigameParameterType.ENUM,
        description=description,
        allowed_values=allowed_values,
        default=default,
    )


def _int_parameter(name: str, description: str, minimum: int, maximum: int, default: int) -> MinigameParameterDefinition:
    return MinigameParameterDefinition(
        name=name,
        type=MinigameParameterType.INT,
        description=description,
        minimum=minimum,
        maximum=maximum,
        default=default,
    )


def _string_parameter(
    name: str,
    description: str,
    allowed_values: list[str],
    default: str,
    *,
    string_mode: MinigameStringMode,
) -> MinigameParameterDefinition:
    return MinigameParameterDefinition(
        name=name,
        type=MinigameParameterType.STRING,
        description=description,
        allowed_values=allowed_values,
        default=default,
        string_mode=string_mode,
        min_length=1,
    )


def _rule(rule_id: str, description: str) -> MinigameCouplingRule:
    return MinigameCouplingRule(rule_id=rule_id, description=description)


def _plant_rows_intro_assist_rule(context: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if "intro" in set(context.fit_tags) and parameters["assistLevel"] == "low":
        return "assistLevel=low is invalid for intro beats."
    return None


def _plant_rows_row_count_rule(_: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if parameters["rowCount"] >= 3 and parameters["targetCount"] < 6:
        return "rowCount >= 3 requires targetCount >= 6."
    return None


def _plant_rows_tomato_unlock_rule(context: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if parameters["cropType"] == "tomato" and "tomatoes_unlocked" not in set(context.world_state):
        return "cropType=tomato requires world state 'tomatoes_unlocked'."
    return None


def _find_tools_cluster_hint_rule(_: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if parameters["toolCount"] == 3 and parameters["hintStrength"] == "light":
        return "toolCount=3 requires hintStrength to stay at medium or strong."
    return None


def _find_tools_cluster_zone_rule(context: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if parameters["searchZone"] == "field_path" and "earliest_tutorial_bridge" in set(context.world_state):
        return "searchZone=field_path is invalid for the earliest tutorial bridge."
    return None


def _find_tools_cluster_starter_timer_rule(_: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if parameters["targetToolSet"] == "starter" and parameters["timeLimitSeconds"] > 300:
        return "starter tool sets should not exceed timeLimitSeconds=300."
    return None


def _chicken_chase_capture_rule(_: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if parameters["targetCaptureCount"] > parameters["chickenCount"]:
        return "targetCaptureCount may not exceed chickenCount."
    return None


def _chicken_chase_intro_guidance_rule(context: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if "intro" in set(context.fit_tags) and parameters["guidanceLevel"] == "low":
        return "guidanceLevel=low is invalid for intro beats."
    return None


def _chicken_chase_arena_time_rule(_: MinigameGenerationContext, parameters: dict[str, Any]) -> str | None:
    if parameters["arenaPresetId"] == "tutorial_pen_medium" and parameters["timeLimitSeconds"] < 120:
        return "arenaPresetId=tutorial_pen_medium requires timeLimitSeconds >= 120."
    return None
