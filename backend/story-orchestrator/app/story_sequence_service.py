from __future__ import annotations

from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from random import Random
from typing import Any

from .generated_package_assembly import (
    GeneratedPackageAssemblyCutsceneInput,
    GeneratedPackageAssemblyMinigameInput,
    GeneratedPackageAssemblyRequest,
    GeneratedPackageAssemblyResult,
    GeneratedPackageAssemblyService,
)
from .story_sequence_models import (
    StorySequenceAdvanceResult,
    StorySequenceContinuityImageRecord,
    StorySequenceSessionCreateRequest,
    StorySequenceSessionDetail,
    StorySequenceSessionRecord,
    StorySequenceTurnRecord,
)
from .generated_storyboard_models import GeneratedStoryboardContext
from .minigame_generator_models import MinigameGenerationContext
from .minigame_generators import MinigameGeneratorCatalog
from .story_sequence_store import StorySequenceSessionStore

_DEFAULT_VOICE_ID = "voice-test"
_PROOF_CUTSCENE_SCENE = "PostChickenCutscene"
_MINIGAME_SCENES = {
    "plant_rows_v1": "FarmMain",
    "find_tools_cluster_v1": "FindToolsGame",
    "chicken_chase_intro_v1": "ChickenChaseGame",
}


@dataclass(frozen=True)
class _TurnPlan:
    generator_id: str
    character_name: str
    parameters: dict[str, Any]
    request: GeneratedPackageAssemblyRequest


class StorySequenceSessionService:
    def __init__(
        self,
        *,
        package_assembly_service: GeneratedPackageAssemblyService,
        store: StorySequenceSessionStore,
        catalog: MinigameGeneratorCatalog | None = None,
        default_voice_id: str = "",
    ) -> None:
        self._package_assembly_service = package_assembly_service
        self._store = store
        self._catalog = catalog or MinigameGeneratorCatalog.default()
        self._default_voice_id = default_voice_id or _DEFAULT_VOICE_ID

    def create_session(self, request: StorySequenceSessionCreateRequest) -> StorySequenceSessionRecord:
        record = StorySequenceSessionRecord.create_active(request)
        return self._store.create_session(record)

    def get_session_detail(self, session_id: str) -> StorySequenceSessionDetail | None:
        return self._store.get_session_detail(session_id)

    def advance_session(self, session_id: str) -> StorySequenceAdvanceResult | None:
        detail = self._store.get_session_detail(session_id)
        if detail is None:
            return None

        turn_index = len(detail.turns)
        plan = self._plan_turn(detail.session)
        result = self._create_package(plan.request)
        turn = self._build_turn_record(detail.session, turn_index, plan, result)
        session = self._advance_session_state(detail.session, turn)
        persisted = self._store.append_turn(session, turn)
        if persisted is None:
            raise RuntimeError(f"Story sequence session '{session_id}' could not be updated.")
        return StorySequenceAdvanceResult(session=persisted.session, turn=turn)

    def _plan_turn(self, session: StorySequenceSessionRecord) -> _TurnPlan:
        candidate_ids = self._ordered_generator_ids(session)
        plans = [plan for generator_id in candidate_ids if (plan := self._build_plan(session, generator_id))]
        if not plans:
            raise RuntimeError("No valid sequence generator is available for the current session state.")

        recent = set(session.state.recent_generator_ids[-session.state.max_recent_generators :])
        for plan in plans:
            if plan.generator_id not in recent:
                return plan
        return plans[0]

    def _ordered_generator_ids(self, session: StorySequenceSessionRecord) -> list[str]:
        definitions = self._catalog.list_definitions()
        if not definitions:
            return []

        start_index = session.state.beat_cursor % len(definitions)
        ordered = definitions[start_index:] + definitions[:start_index]
        return [definition.generator_id for definition in ordered]

    def _build_plan(self, session: StorySequenceSessionRecord, generator_id: str) -> _TurnPlan | None:
        turn_index = session.state.beat_cursor
        parameters = self._build_parameters(session, generator_id)
        context = self._build_context(session)
        validation = self._catalog.validate_selection(generator_id, parameters=parameters, context=context)
        if not validation.is_valid:
            return None

        character_name = self._select_character_name(session)
        request = self._build_request(
            session=session,
            generator_id=generator_id,
            character_name=character_name,
            parameters=validation.resolved_parameters,
            turn_index=turn_index,
            context=context,
        )
        return _TurnPlan(
            generator_id=generator_id,
            character_name=character_name,
            parameters=validation.resolved_parameters,
            request=request,
        )

    def _build_context(self, session: StorySequenceSessionRecord) -> MinigameGenerationContext:
        return MinigameGenerationContext(
            fit_tags=list(session.state.fit_tags),
            world_state=list(session.state.world_state),
            difficulty_band=session.state.difficulty_band,
        )

    def _build_request(
        self,
        *,
        session: StorySequenceSessionRecord,
        generator_id: str,
        character_name: str,
        parameters: dict[str, Any],
        turn_index: int,
        context: MinigameGenerationContext,
    ) -> GeneratedPackageAssemblyRequest:
        turn_slug = f"sequence_turn_{turn_index:03d}"
        story_brief = self._build_story_brief(session, generator_id, character_name)
        minigame_scene = _MINIGAME_SCENES[generator_id]
        continuity_reference_paths = self._select_continuity_reference_paths(session, character_name)

        return GeneratedPackageAssemblyRequest(
            package_id=session.package_id,
            package_display_name=session.package_display_name,
            minigame=GeneratedPackageAssemblyMinigameInput(
                beat_id=f"{turn_slug}_minigame",
                display_name=self._display_name_for(generator_id),
                scene_name=minigame_scene,
                next_scene_name=None,
                generator_id=generator_id,
                parameters=parameters,
                context=context,
            ),
            cutscene=GeneratedPackageAssemblyCutsceneInput(
                beat_id=f"{turn_slug}_cutscene",
                display_name=f"Sequence Bridge {turn_index + 1}",
                scene_name=_PROOF_CUTSCENE_SCENE,
                next_scene_name=minigame_scene,
                story_brief=story_brief,
                style_preset_id="farm_storybook_v1",
                voice_id=self._default_voice_id,
                reference_image_paths=continuity_reference_paths,
                context=GeneratedStoryboardContext(
                    character_name=character_name,
                    crop_name="",
                    focus_label="",
                    minigame_goal="",
                ),
            ),
        )

    def _build_story_brief(
        self,
        session: StorySequenceSessionRecord,
        generator_id: str,
        character_name: str,
    ) -> str:
        focus = {
            "plant_rows_v1": "guiding the first planting rhythm",
            "find_tools_cluster_v1": "recovering the right tools around the farm",
            "chicken_chase_intro_v1": "bringing playful chaos back under control",
        }[generator_id]
        if session.state.last_minigame_goal:
            return (
                f"{session.state.narrative_seed} {character_name} links the previous goal "
                f"'{session.state.last_minigame_goal}' into {focus}. Keep the tone warm, brief, and forward-moving."
            )
        return (
            f"{session.state.narrative_seed} {character_name} sets up {focus}. "
            "Keep the bridge concise, readable, and consistent with a cozy farm tutorial."
        )

    def _select_character_name(self, session: StorySequenceSessionRecord) -> str:
        pool = session.state.character_pool
        if not pool:
            raise RuntimeError("Story sequence session has no available character pool.")
        index = session.state.beat_cursor % len(pool)
        return pool[index]

    def _build_parameters(self, session: StorySequenceSessionRecord, generator_id: str) -> dict[str, Any]:
        if generator_id == "plant_rows_v1":
            return self._plant_rows_parameters(session)
        if generator_id == "find_tools_cluster_v1":
            return self._find_tools_parameters(session)
        if generator_id == "chicken_chase_intro_v1":
            return self._chicken_chase_parameters(session)
        raise RuntimeError(f"Sequence planner does not know generator '{generator_id}'.")

    def _plant_rows_parameters(self, session: StorySequenceSessionRecord) -> dict[str, Any]:
        rng = self._rng(session, "plant_rows")
        crop_options = ["carrot"]
        if "tomatoes_unlocked" in set(session.state.world_state):
            crop_options.append("tomato")
        if "corn_unlocked" in set(session.state.world_state):
            crop_options.append("corn")

        crop_type = self._pick_preferred(crop_options, session.state.recent_crop_types[-1:])
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

    def _find_tools_parameters(self, session: StorySequenceSessionRecord) -> dict[str, Any]:
        rng = self._rng(session, "find_tools")
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

    def _chicken_chase_parameters(self, session: StorySequenceSessionRecord) -> dict[str, Any]:
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

    def _pick_preferred(self, options: list[str], recent_values: list[str]) -> str:
        recent = set(recent_values)
        for option in options:
            if option not in recent:
                return option
        return options[0]

    def _display_name_for(self, generator_id: str) -> str:
        definition = self._catalog.get_definition(generator_id)
        if definition is None:
            raise RuntimeError(f"Generator definition '{generator_id}' was not found.")
        return definition.display_name

    def _create_package(self, request: GeneratedPackageAssemblyRequest) -> GeneratedPackageAssemblyResult:
        try:
            return self._package_assembly_service.create_package(request)
        except Exception as error:
            return GeneratedPackageAssemblyResult(
                is_valid=False,
                errors=[str(error)],
            )

    def _build_turn_record(
        self,
        session: StorySequenceSessionRecord,
        turn_index: int,
        plan: _TurnPlan,
        result: GeneratedPackageAssemblyResult,
    ) -> StorySequenceTurnRecord:
        return StorySequenceTurnRecord(
            session_id=session.session_id,
            turn_index=turn_index,
            generator_id=plan.generator_id,
            character_name=plan.character_name,
            summary=self._build_summary(plan, result),
            request=plan.request,
            result=result,
            created_at=datetime.now(UTC).isoformat(),
        )

    def _build_summary(self, plan: _TurnPlan, result: GeneratedPackageAssemblyResult) -> str:
        objective_text = ""
        if result.minigame_result is not None:
            objective_text = str(result.minigame_result.materialized_minigame.get("ObjectiveText", ""))
        if objective_text:
            return f"{plan.character_name}: {objective_text}"
        return f"{plan.character_name}: planned {plan.generator_id}."

    def _advance_session_state(
        self,
        session: StorySequenceSessionRecord,
        turn: StorySequenceTurnRecord,
    ) -> StorySequenceSessionRecord:
        world_state = self._updated_world_state(session, turn.turn_index)
        recent_generators = self._roll_recent(
            session.state.recent_generator_ids,
            turn.generator_id,
            session.state.max_recent_generators,
        )
        recent_characters = self._roll_recent(session.state.recent_character_names, turn.character_name, 2)
        recent_crops = list(session.state.recent_crop_types)
        crop_type = turn.request.minigame.parameters.get("cropType")
        if isinstance(crop_type, str):
            recent_crops = self._roll_recent(recent_crops, crop_type, 2)
        continuity_images = self._roll_continuity_images(
            session.state.continuity_images,
            self._extract_continuity_images(turn),
            session.state.max_continuity_images,
        )

        objective_text = self._extract_objective_text(turn.result)
        next_state = session.state.model_copy(
            update={
                "beat_cursor": session.state.beat_cursor + 1,
                "recent_generator_ids": recent_generators,
                "recent_character_names": recent_characters,
                "recent_crop_types": recent_crops,
                "world_state": world_state,
                "continuity_images": continuity_images,
                "last_generator_id": turn.generator_id,
                "last_character_name": turn.character_name,
                "last_minigame_goal": objective_text or session.state.last_minigame_goal,
            }
        )
        return session.model_copy(update={"state": next_state, "updated_at": datetime.now(UTC).isoformat()})

    def _updated_world_state(self, session: StorySequenceSessionRecord, turn_index: int) -> list[str]:
        world_state = list(dict.fromkeys(session.state.world_state))
        if turn_index == 0:
            world_state = [flag for flag in world_state if flag != "earliest_tutorial_bridge"]
            world_state.extend(["tomatoes_unlocked", "watering_tools_unlocked"])
        elif turn_index == 1:
            world_state.extend(["planting_tools_unlocked", "corn_unlocked"])
        return list(dict.fromkeys(world_state))

    def _extract_objective_text(self, result: GeneratedPackageAssemblyResult) -> str:
        if result.minigame_result is None:
            return ""
        objective_text = result.minigame_result.materialized_minigame.get("ObjectiveText")
        return objective_text if isinstance(objective_text, str) else ""

    def _roll_recent(self, values: list[str], new_value: str, limit: int) -> list[str]:
        rolled = list(values)
        rolled.append(new_value)
        return rolled[-limit:]

    def _roll_continuity_images(
        self,
        values: list[StorySequenceContinuityImageRecord],
        new_values: list[StorySequenceContinuityImageRecord],
        limit: int,
    ) -> list[StorySequenceContinuityImageRecord]:
        rolled = list(values)
        rolled.extend(new_values)
        return rolled[-limit:]

    def _select_continuity_reference_paths(
        self,
        session: StorySequenceSessionRecord,
        character_name: str,
    ) -> list[str]:
        available = [
            image
            for image in reversed(session.state.continuity_images)
            if image.output_path and Path(image.output_path).exists()
        ]
        same_character = [image.output_path for image in available if image.character_name == character_name]
        if same_character:
            return self._dedupe_paths(same_character)[:2]
        return self._dedupe_paths([image.output_path for image in available])[:2]

    def _extract_continuity_images(
        self,
        turn: StorySequenceTurnRecord,
    ) -> list[StorySequenceContinuityImageRecord]:
        cutscene_result = turn.result.cutscene_result
        if cutscene_result is None:
            return []

        continuity_images: list[StorySequenceContinuityImageRecord] = []
        for asset in cutscene_result.generated_assets:
            if asset.asset_type != "image" or not asset.output_path:
                continue
            output_path = str(Path(asset.output_path).resolve())
            if not Path(output_path).exists():
                continue
            continuity_images.append(
                StorySequenceContinuityImageRecord(
                    asset_id=asset.asset_id,
                    beat_id=asset.beat_id,
                    shot_id=asset.shot_id,
                    turn_index=turn.turn_index,
                    character_name=turn.character_name,
                    output_path=output_path,
                )
            )
        return continuity_images

    def _dedupe_paths(self, paths: list[str]) -> list[str]:
        resolved: list[str] = []
        for path in paths:
            if path in resolved:
                continue
            resolved.append(path)
        return resolved

    def _rng(self, session: StorySequenceSessionRecord, label: str) -> Random:
        return Random(f"{session.state.seed}:{session.state.beat_cursor}:{label}")
