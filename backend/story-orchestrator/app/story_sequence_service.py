from __future__ import annotations

import logging
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from .generated_package_assembly import (
    GeneratedPackageAssemblyCutsceneInput,
    GeneratedPackageAssemblyMinigameInput,
    GeneratedPackageAssemblyRequest,
    GeneratedPackageAssemblyResult,
    GeneratedPackageAssemblyService,
)
from .generated_storyboard_models import GeneratedStoryboardContext
from .minigame_generator_models import MinigameGenerationContext
from .minigame_generators import MinigameGeneratorCatalog
from .story_sequence_models import (
    StorySequenceAdvanceResult,
    StorySequenceContinuityImageRecord,
    StorySequenceSessionCreateRequest,
    StorySequenceSessionDetail,
    StorySequenceSessionRecord,
    StorySequenceTurnRecord,
)
from .story_sequence_store import StorySequenceSessionStore
from .story_sequence_rules import build_turn_parameters, updated_world_state
from .story_sequence_turn_director import StorySequenceGeneratorOption, StorySequenceTurnDirector

_DEFAULT_VOICE_ID = "voice-test"
_PROOF_CUTSCENE_SCENE = "PostChickenCutscene"
_LOGGER = logging.getLogger("uvicorn.error")
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
        turn_director: StorySequenceTurnDirector | None = None,
    ) -> None:
        self._package_assembly_service = package_assembly_service
        self._store = store
        self._catalog = catalog or MinigameGeneratorCatalog.default()
        self._default_voice_id = default_voice_id or _DEFAULT_VOICE_ID
        self._turn_director = turn_director

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
        _LOGGER.info(
            "[GeneratedStoryBackend] advance_session start session_id=%s turn_index=%s beat_cursor=%s",
            session_id,
            turn_index,
            detail.session.state.beat_cursor,
        )
        plan = self._plan_turn(detail)
        _LOGGER.info(
            "[GeneratedStoryBackend] planned turn session_id=%s turn_index=%s generator=%s character=%s",
            session_id,
            turn_index,
            plan.generator_id,
            plan.character_name,
        )
        result = self._create_package(plan.request)
        turn = self._build_turn_record(detail.session, turn_index, plan, result)
        session = self._advance_session_state(detail.session, turn)
        persisted = self._store.append_turn(session, turn)
        if persisted is None:
            raise RuntimeError(f"Story sequence session '{session_id}' could not be updated.")
        if result.is_valid:
            _LOGGER.info(
                "[GeneratedStoryBackend] advance_session complete session_id=%s turn_index=%s valid=%s errors=%s",
                session_id,
                turn_index,
                result.is_valid,
                len(result.errors),
            )
        else:
            _LOGGER.warning(
                "[GeneratedStoryBackend] advance_session complete session_id=%s turn_index=%s valid=%s errors=%s details=%s",
                session_id,
                turn_index,
                result.is_valid,
                len(result.errors),
                result.errors,
            )
        return StorySequenceAdvanceResult(session=persisted.session, turn=turn)

    def _plan_turn(self, detail: StorySequenceSessionDetail) -> _TurnPlan:
        session = detail.session
        candidate_ids = self._ordered_generator_ids(session)
        plans = [plan for generator_id in candidate_ids if (plan := self._build_plan(session, generator_id))]
        if not plans:
            raise RuntimeError("No valid sequence generator is available for the current session state.")

        default_plan = self._select_preferred_plan(session, plans)
        directive = self._choose_directed_turn(detail, candidate_ids, default_plan)
        if directive is None:
            return default_plan

        if directive.generator_id not in candidate_ids:
            return default_plan
        if directive.character_name not in session.state.character_pool:
            return default_plan

        directed_character = directive.character_name
        directed_display_name = directive.cutscene_display_name.strip() or default_plan.request.cutscene.display_name
        directed_story_brief = directive.story_brief.strip() or default_plan.request.cutscene.story_brief
        try:
            directed_plan = self._build_plan(
                session,
                directive.generator_id,
                character_name=directed_character,
                story_brief=directed_story_brief,
                cutscene_display_name=directed_display_name,
            )
        except Exception:
            directed_plan = None
        return directed_plan or default_plan

    def _select_preferred_plan(
        self,
        session: StorySequenceSessionRecord,
        plans: list[_TurnPlan],
    ) -> _TurnPlan:
        recent = set(session.state.recent_generator_ids[-session.state.max_recent_generators :])
        for plan in plans:
            if plan.generator_id not in recent:
                return plan
        return plans[0]

    def _choose_directed_turn(
        self,
        detail: StorySequenceSessionDetail,
        candidate_ids: list[str],
        default_plan: _TurnPlan,
    ):
        if self._turn_director is None:
            return None

        session = detail.session
        recent_turn_summaries = [turn.summary for turn in detail.turns[-3:]]
        candidate_generators = self._build_generator_options(candidate_ids)
        try:
            return self._turn_director.choose_turn(
                narrative_seed=session.state.narrative_seed,
                beat_cursor=session.state.beat_cursor,
                fit_tags=list(session.state.fit_tags),
                world_state=list(session.state.world_state),
                difficulty_band=session.state.difficulty_band,
                last_minigame_goal=session.state.last_minigame_goal,
                recent_turn_summaries=recent_turn_summaries,
                candidate_generators=candidate_generators,
                candidate_character_names=list(session.state.character_pool),
                default_generator_id=default_plan.generator_id,
                default_character_name=default_plan.character_name,
            )
        except Exception:
            return None

    def _build_generator_options(self, generator_ids: list[str]) -> list[StorySequenceGeneratorOption]:
        options: list[StorySequenceGeneratorOption] = []
        for generator_id in generator_ids:
            definition = self._catalog.get_definition(generator_id)
            if definition is None:
                continue
            options.append(
                StorySequenceGeneratorOption(
                    generator_id=definition.generator_id,
                    display_name=definition.display_name,
                    minigame_id=definition.minigame_id,
                    fit_tags=list(definition.fit_tags),
                    preview_text_template=definition.preview_text_template,
                )
            )
        return options

    def _ordered_generator_ids(self, session: StorySequenceSessionRecord) -> list[str]:
        definitions = self._catalog.list_definitions()
        if not definitions:
            return []

        start_index = session.state.beat_cursor % len(definitions)
        ordered = definitions[start_index:] + definitions[:start_index]
        return [definition.generator_id for definition in ordered]

    def _build_plan(
        self,
        session: StorySequenceSessionRecord,
        generator_id: str,
        *,
        character_name: str | None = None,
        story_brief: str | None = None,
        cutscene_display_name: str | None = None,
    ) -> _TurnPlan | None:
        turn_index = session.state.beat_cursor
        parameters = build_turn_parameters(session, generator_id)
        context = self._build_context(session)
        validation = self._catalog.validate_selection(generator_id, parameters=parameters, context=context)
        if not validation.is_valid:
            return None

        selected_character_name = character_name or self._select_character_name(session)
        selected_story_brief = story_brief or self._build_story_brief(session, generator_id, selected_character_name)
        selected_display_name = cutscene_display_name or f"Sequence Bridge {turn_index + 1}"
        request = self._build_request(
            session=session,
            generator_id=generator_id,
            character_name=selected_character_name,
            parameters=validation.resolved_parameters,
            turn_index=turn_index,
            context=context,
            story_brief=selected_story_brief,
            cutscene_display_name=selected_display_name,
        )
        return _TurnPlan(
            generator_id=generator_id,
            character_name=selected_character_name,
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
        story_brief: str,
        cutscene_display_name: str,
    ) -> GeneratedPackageAssemblyRequest:
        turn_slug = f"sequence_turn_{turn_index:03d}"
        minigame_scene = _MINIGAME_SCENES[generator_id]
        continuity_reference_paths = self._select_continuity_reference_paths(session, character_name)
        objective_text = self._objective_text_for(generator_id, parameters)
        mission_configuration_summary = self._mission_configuration_summary(generator_id, parameters)
        prior_story_summary = self._prior_story_summary(session)
        present_character_names = self._present_character_names(session, character_name)

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
                display_name=cutscene_display_name,
                scene_name=_PROOF_CUTSCENE_SCENE,
                next_scene_name=minigame_scene,
                story_brief=story_brief,
                style_preset_id="farm_storybook_v1",
                voice_id=self._default_voice_id,
                reference_image_paths=continuity_reference_paths,
                continuity_reference_mode="character_priority",
                context=GeneratedStoryboardContext(
                    character_name=character_name,
                    crop_name="",
                    focus_label="",
                    minigame_goal=objective_text,
                    prior_story_summary=prior_story_summary,
                    world_state=list(session.state.world_state),
                    present_character_names=present_character_names,
                    selected_generator_id=generator_id,
                    selected_generator_display_name=self._display_name_for(generator_id),
                    mission_configuration_summary=mission_configuration_summary,
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
                f"'{session.state.last_minigame_goal}' into {focus}. "
                "A new interruption, discovery, or practical farm problem should force the next mission to happen now."
            )
        return (
            f"{session.state.narrative_seed} {character_name} sets up {focus}. "
            "Introduce a concrete farm conflict or event so the next mission feels earned."
        )

    def _objective_text_for(self, generator_id: str, parameters: dict[str, Any]) -> str:
        if generator_id == "plant_rows_v1":
            crop_name = self._pluralize_crop(parameters["cropType"], int(parameters["targetCount"]))
            return (
                f"Plant {parameters['targetCount']} {crop_name} across "
                f"{parameters['rowCount']} rows before time runs out."
            )
        if generator_id == "find_tools_cluster_v1":
            return (
                f"Recover {parameters['toolCount']} {parameters['targetToolSet']} tools "
                f"around the {parameters['searchZone']}."
            )
        if generator_id == "chicken_chase_intro_v1":
            return (
                f"Catch {parameters['targetCaptureCount']} runaway chickens inside "
                f"the {parameters['arenaPresetId']} pen."
            )
        return self._display_name_for(generator_id)

    @staticmethod
    def _pluralize_crop(crop_type: str, count: int) -> str:
        if count == 1:
            return crop_type

        irregular = {"tomato": "tomatoes", "corn": "corn"}
        return irregular.get(crop_type, f"{crop_type}s")

    def _mission_configuration_summary(self, generator_id: str, parameters: dict[str, Any]) -> str:
        if generator_id == "plant_rows_v1":
            return (
                f"Plant {parameters['targetCount']} {parameters['cropType']} seeds across "
                f"{parameters['rowCount']} neat rows in {parameters['timeLimitSeconds']} seconds "
                f"with {parameters['assistLevel']} guidance."
            )
        if generator_id == "find_tools_cluster_v1":
            return (
                f"Recover {parameters['toolCount']} {parameters['targetToolSet']} tools "
                f"around the {parameters['searchZone'].replace('_', ' ')} with "
                f"{parameters['hintStrength']} hints."
            )
        if generator_id == "chicken_chase_intro_v1":
            return (
                f"Catch {parameters['targetCaptureCount']} chickens from a flock of "
                f"{parameters['chickenCount']} in the {parameters['arenaPresetId'].replace('_', ' ')} "
                f"within {parameters['timeLimitSeconds']} seconds using {parameters['guidanceLevel']} guidance."
            )
        return self._display_name_for(generator_id)

    def _prior_story_summary(self, session: StorySequenceSessionRecord) -> str:
        detail = self._store.get_session_detail(session.session_id)
        if detail is None or not detail.turns:
            return "No previous turn summary yet. Use the existing farm state and story brief to create the next conflict."
        recent = [turn.summary for turn in detail.turns[-2:] if turn.summary]
        if not recent:
            return "No previous turn summary yet. Use the existing farm state and story brief to create the next conflict."
        return " ".join(recent)

    def _present_character_names(
        self,
        session: StorySequenceSessionRecord,
        selected_character_name: str,
    ) -> list[str]:
        ordered = [selected_character_name, *session.state.recent_character_names, *session.state.character_pool]
        deduped: list[str] = []
        for name in ordered:
            if not name or name in deduped:
                continue
            deduped.append(name)
        return deduped

    def _select_character_name(self, session: StorySequenceSessionRecord) -> str:
        pool = session.state.character_pool
        if not pool:
            raise RuntimeError("Story sequence session has no available character pool.")
        index = session.state.beat_cursor % len(pool)
        return pool[index]

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
        world_state = updated_world_state(session, turn.turn_index)
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
            for image in session.state.continuity_images
            if image.output_path and Path(image.output_path).exists()
        ]
        same_character = [image for image in available if image.character_name == character_name]
        selected = self._select_continuity_anchor_path(same_character)
        if selected:
            return [selected]

        selected = self._select_continuity_anchor_path(available)
        return [selected] if selected else []

    def _select_continuity_anchor_path(
        self,
        images: list[StorySequenceContinuityImageRecord],
    ) -> str:
        if not images:
            return ""

        latest_turn_index = max(image.turn_index for image in images)
        latest_turn_images = [image for image in images if image.turn_index == latest_turn_index]
        shot_priority = {"shot_01": 0, "shot_02": 1, "shot_03": 2}
        latest_turn_images.sort(key=lambda image: (shot_priority.get(image.shot_id, 99), image.shot_id))
        return latest_turn_images[0].output_path

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
