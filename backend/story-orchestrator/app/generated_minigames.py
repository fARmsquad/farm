from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from pydantic import BaseModel, Field

from .minigame_generator_models import MinigameGenerationContext
from .minigame_generators import MinigameGeneratorCatalog


class GeneratedMinigameBeatRequest(BaseModel):
    package_id: str = Field(min_length=1)
    package_display_name: str = Field(min_length=1)
    beat_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    scene_name: str = Field(min_length=1)
    next_scene_name: str | None = None
    generator_id: str = Field(min_length=1)
    parameters: dict[str, Any] = Field(default_factory=dict)
    context: MinigameGenerationContext = Field(default_factory=MinigameGenerationContext)


class GeneratedMinigameBeatResult(BaseModel):
    is_valid: bool
    resolved_generator_id: str
    package_output_path: str = ""
    unity_package: dict[str, Any] = Field(default_factory=dict)
    materialized_minigame: dict[str, Any] = Field(default_factory=dict)
    errors: list[str] = Field(default_factory=list)
    fallback_generator_ids: list[str] = Field(default_factory=list)


class GeneratedMinigameBeatService:
    def __init__(
        self,
        *,
        package_output_path: Path,
        catalog: MinigameGeneratorCatalog | None = None,
    ) -> None:
        self._package_output_path = package_output_path
        self._catalog = catalog or MinigameGeneratorCatalog.default()

    def create_package(self, request: GeneratedMinigameBeatRequest) -> GeneratedMinigameBeatResult:
        validation = self._catalog.validate_selection(
            request.generator_id,
            parameters=request.parameters,
            context=request.context,
        )
        if not validation.is_valid:
            return GeneratedMinigameBeatResult(
                is_valid=False,
                resolved_generator_id=request.generator_id,
                errors=validation.errors,
                fallback_generator_ids=validation.fallback_generator_ids,
            )

        materialized_minigame = self._materialize(
            request.generator_id,
            validation.resolved_parameters,
            validation.fallback_generator_ids,
        )
        unity_package = self._load_existing_package(request)
        unity_package["PackageId"] = request.package_id
        unity_package["SchemaVersion"] = unity_package.get("SchemaVersion", 1)
        unity_package["PackageVersion"] = unity_package.get("PackageVersion", 1)
        unity_package["DisplayName"] = request.package_display_name
        beats = unity_package.setdefault("Beats", [])

        updated_beat = {
            "BeatId": request.beat_id,
            "DisplayName": request.display_name,
            "Kind": "Minigame",
            "SceneName": request.scene_name,
            "NextSceneName": request.next_scene_name or "",
            "Minigame": materialized_minigame,
        }

        beat_replaced = False
        for index, beat in enumerate(beats):
            if beat.get("BeatId") == request.beat_id or beat.get("SceneName") == request.scene_name:
                beats[index] = updated_beat
                beat_replaced = True
                break

        if not beat_replaced:
            beats.append(updated_beat)

        self._package_output_path.parent.mkdir(parents=True, exist_ok=True)
        self._package_output_path.write_text(json.dumps(unity_package, indent=2), encoding="utf-8")

        return GeneratedMinigameBeatResult(
            is_valid=True,
            resolved_generator_id=request.generator_id,
            package_output_path=str(self._package_output_path),
            unity_package=unity_package,
            materialized_minigame=materialized_minigame,
            fallback_generator_ids=validation.fallback_generator_ids,
        )

    def _materialize(
        self,
        generator_id: str,
        parameters: dict[str, Any],
        fallback_generator_ids: list[str],
    ) -> dict[str, Any]:
        if generator_id == "plant_rows_v1":
            required_count = int(parameters["targetCount"])
            time_limit_seconds = float(parameters["timeLimitSeconds"])
            crop_name = _pluralize_crop(parameters["cropType"], required_count)
            objective_text = f"Plant {required_count} {crop_name} in {_format_minutes(time_limit_seconds)}."
            return self._build_minigame_payload(
                adapter_id="tutorial.plant_rows",
                generator_id=generator_id,
                minigame_id="planting",
                objective_text=objective_text,
                required_count=required_count,
                time_limit_seconds=time_limit_seconds,
                resolved_parameters=parameters,
                fallback_generator_ids=fallback_generator_ids,
            )

        if generator_id == "find_tools_cluster_v1":
            required_count = int(parameters["toolCount"])
            time_limit_seconds = float(parameters["timeLimitSeconds"])
            tool_label = _pluralize_label(f"{parameters['targetToolSet']} tool", required_count)
            objective_text = (
                f"Find {required_count} {tool_label} around the {parameters['searchZone']} "
                f"in {_format_minutes(time_limit_seconds)}."
            )
            return self._build_minigame_payload(
                adapter_id="tutorial.find_tools",
                generator_id=generator_id,
                minigame_id="find_tools",
                objective_text=objective_text,
                required_count=required_count,
                time_limit_seconds=time_limit_seconds,
                resolved_parameters=parameters,
                fallback_generator_ids=fallback_generator_ids,
            )

        if generator_id == "chicken_chase_intro_v1":
            required_count = int(parameters["targetCaptureCount"])
            time_limit_seconds = float(parameters["timeLimitSeconds"])
            objective_text = f"Catch {required_count} chicken in {_format_minutes(time_limit_seconds)}."
            if required_count != 1:
                objective_text = f"Catch {required_count} chickens in {_format_minutes(time_limit_seconds)}."
            return self._build_minigame_payload(
                adapter_id="tutorial.chicken_chase",
                generator_id=generator_id,
                minigame_id="chicken_chase",
                objective_text=objective_text,
                required_count=required_count,
                time_limit_seconds=time_limit_seconds,
                resolved_parameters=parameters,
                fallback_generator_ids=fallback_generator_ids,
            )

        raise RuntimeError(f"Materializer is not defined for generator '{generator_id}'.")

    @staticmethod
    def _build_minigame_payload(
        *,
        adapter_id: str,
        generator_id: str,
        minigame_id: str,
        objective_text: str,
        required_count: int,
        time_limit_seconds: float,
        resolved_parameters: dict[str, Any],
        fallback_generator_ids: list[str],
    ) -> dict[str, Any]:
        return {
            "AdapterId": adapter_id,
            "ObjectiveText": objective_text,
            "RequiredCount": required_count,
            "TimeLimitSeconds": time_limit_seconds,
            "GeneratorId": generator_id,
            "MinigameId": minigame_id,
            "FallbackGeneratorIds": list(fallback_generator_ids),
            "ResolvedParameters": dict(resolved_parameters),
            "ResolvedParameterEntries": _build_parameter_entries(resolved_parameters),
        }

    def _load_existing_package(self, request: GeneratedMinigameBeatRequest) -> dict[str, Any]:
        if not self._package_output_path.exists():
            return {
                "PackageId": request.package_id,
                "SchemaVersion": 1,
                "PackageVersion": 1,
                "DisplayName": request.package_display_name,
                "Beats": [],
            }

        try:
            return json.loads(self._package_output_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as error:
            raise RuntimeError(f"Existing story package is invalid JSON: {error}") from error


def _format_minutes(time_limit_seconds: float) -> str:
    minutes = int(round(time_limit_seconds / 60.0))
    return f"{minutes} minute" if minutes == 1 else f"{minutes} minutes"


def _pluralize_crop(crop_type: str, count: int) -> str:
    if count == 1:
        return crop_type

    irregular = {"tomato": "tomatoes"}
    return irregular.get(crop_type, f"{crop_type}s")


def _pluralize_label(label: str, count: int) -> str:
    if count == 1:
        return label
    return f"{label}s"


def _build_parameter_entries(resolved_parameters: dict[str, Any]) -> list[dict[str, Any]]:
    entries: list[dict[str, Any]] = []
    for name, value in resolved_parameters.items():
        entry: dict[str, Any] = {
            "Name": name,
            "ValueType": "String",
            "StringValue": "",
            "IntValue": 0,
            "FloatValue": 0.0,
            "BoolValue": False,
        }

        if isinstance(value, bool):
            entry["ValueType"] = "Bool"
            entry["BoolValue"] = value
        elif isinstance(value, int):
            entry["ValueType"] = "Int"
            entry["IntValue"] = value
        elif isinstance(value, float):
            entry["ValueType"] = "Float"
            entry["FloatValue"] = value
        else:
            entry["StringValue"] = str(value)

        entries.append(entry)

    return entries
