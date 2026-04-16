from __future__ import annotations

import shutil
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Any, Callable
from uuid import uuid4

from .generated_minigames import GeneratedMinigameBeatService
from .generated_package_assembly import (
    GeneratedPackageAssemblyCutsceneInput,
    GeneratedPackageAssemblyMinigameInput,
    GeneratedPackageAssemblyRequest,
    GeneratedPackageAssemblyResult,
    GeneratedPackageAssemblyService,
)
from .generated_storyboard_models import GeneratedStoryboardContext
from .generated_storyboards import GeneratedStoryboardService
from .minigame_generator_models import MinigameGenerationContext
from .minigame_generators import MinigameGeneratorCatalog
from .runtime_models import (
    ArtifactDescriptor,
    CutsceneContract,
    CutsceneShotContract,
    MinigameContract,
    PlayableTurnEnvelope,
    RuntimeContinuityContract,
    RuntimeContinuityImageRecord,
    RuntimeDebugContract,
    RuntimeSession,
    RuntimeTurnRecord,
)
from .story_mode_config import RuntimeStoryModeConfigCatalog, render_story_hook
from .story_mode_config_models import MinigameStorySurface, PromptStructureDefinition, StoryTypeDefinition
from .runtime_turn_generation_support import (
    MINIGAME_SCENES,
    PROOF_CUTSCENE_SCENE,
    build_story_brief,
    build_turn_summary,
    choose_directed_turn,
    display_name_for,
    find_generated_beat,
    mission_configuration_summary,
    objective_text_for,
    ordered_generator_ids,
    present_character_names,
    prior_story_summary,
    roll_continuity_images,
    roll_recent,
    select_character_name,
    select_continuity_reference_paths,
)
from .story_sequence_rules import build_turn_parameters, updated_world_state
from .story_sequence_turn_director import StorySequenceGeneratorOption, StorySequenceTurnDirector


@dataclass(frozen=True)
class RuntimeGeneratedTurn:
    turn: RuntimeTurnRecord
    session: RuntimeSession


@dataclass(frozen=True)
class _TurnPlan:
    generator_id: str
    character_name: str
    parameters: dict[str, Any]
    story_hook: str
    story_type: StoryTypeDefinition
    prompt_structure: PromptStructureDefinition
    minigame_surface: MinigameStorySurface
    request: GeneratedPackageAssemblyRequest


class RuntimeTurnGenerationService:
    def __init__(
        self,
        *,
        runtime_root: Path,
        storyboard_service: GeneratedStoryboardService,
        minigame_service: GeneratedMinigameBeatService,
        catalog: MinigameGeneratorCatalog | None = None,
        default_voice_id: str = "",
        turn_director: StorySequenceTurnDirector | None = None,
        story_mode_catalog: RuntimeStoryModeConfigCatalog | None = None,
    ) -> None:
        self._runtime_root = runtime_root
        self._storyboard_service = storyboard_service
        self._minigame_service = minigame_service
        self._catalog = catalog or MinigameGeneratorCatalog.default()
        self._story_mode_catalog = story_mode_catalog or RuntimeStoryModeConfigCatalog.default(
            catalog=self._catalog
        )
        self._default_voice_id = default_voice_id or getattr(storyboard_service, "_default_voice_id", "")
        self._turn_director = turn_director

    def generate_turn(
        self,
        *,
        session: RuntimeSession,
        prior_turns: list[RuntimeTurnRecord],
        job_id: str,
        progress_callback: Callable[[str], None] | None = None,
    ) -> RuntimeGeneratedTurn:
        turn_index = session.state.beat_cursor
        turn_id = str(uuid4())
        plan = self._plan_turn(session, prior_turns)
        result = self._create_package(
            session,
            turn_id,
            plan.request,
            progress_callback=progress_callback,
        )
        if not result.is_valid:
            raise RuntimeError(" ".join(result.errors) if result.errors else "Runtime turn package generation failed.")

        artifacts = self._archive_artifacts(
            session_id=session.session_id,
            turn_id=turn_id,
            generated_result=result,
        )
        envelope = self._build_envelope(
            session=session,
            turn_id=turn_id,
            job_id=job_id,
            plan=plan,
            result=result,
            artifacts=artifacts,
            prior_turns=prior_turns,
        )
        summary = build_turn_summary(plan.character_name, plan.generator_id, result)
        now = datetime.now(UTC).isoformat()
        turn = RuntimeTurnRecord(
            turn_id=turn_id,
            session_id=session.session_id,
            turn_index=turn_index,
            status="ready",
            entry_scene_name=envelope.entry_scene_name,
            generator_id=plan.generator_id,
            character_name=plan.character_name,
            summary=summary,
            envelope=envelope,
            created_at=now,
            updated_at=now,
        )
        next_session = self._advance_session_state(session, turn)
        return RuntimeGeneratedTurn(turn=turn, session=next_session)

    def _plan_turn(self, session: RuntimeSession, prior_turns: list[RuntimeTurnRecord]) -> _TurnPlan:
        story_type = self._require_story_type(session)
        prompt_structure = self._require_prompt_structure(session)
        candidate_ids = ordered_generator_ids(self._catalog, session)
        plans = [
            plan
            for generator_id in candidate_ids
            if (
                plan := self._build_plan(
                    session,
                    prior_turns,
                    generator_id,
                    story_type=story_type,
                    prompt_structure=prompt_structure,
                )
            )
        ]
        if not plans:
            raise RuntimeError("No valid runtime generator is available for the current session state.")

        default_plan = self._select_preferred_plan(session, plans)
        candidate_generators = [self._to_generator_option(plan) for plan in plans]
        directive = choose_directed_turn(
            self._turn_director,
            session,
            prior_turns,
            candidate_generators,
            default_plan.generator_id,
            default_plan.character_name,
            story_type_id=story_type.story_type_id,
            story_type_display_name=story_type.display_name,
            story_type_prompt_directives=list(story_type.prompt_directives),
            prompt_structure_id=prompt_structure.prompt_structure_id,
            prompt_structure_display_name=prompt_structure.display_name,
            prompt_structure_directives=list(prompt_structure.director_prompt_directives),
        )
        if directive is None:
            return default_plan

        if directive.generator_id not in candidate_ids:
            return default_plan
        if directive.character_name not in session.state.character_pool:
            return default_plan

        directed_plan = self._build_plan(
            session,
            prior_turns,
            directive.generator_id,
            story_type=story_type,
            prompt_structure=prompt_structure,
            character_name=directive.character_name,
            story_brief=directive.story_brief.strip() or default_plan.request.cutscene.story_brief,
            cutscene_display_name=directive.cutscene_display_name.strip() or default_plan.request.cutscene.display_name,
        )
        return directed_plan or default_plan

    def _build_plan(
        self,
        session: RuntimeSession,
        prior_turns: list[RuntimeTurnRecord],
        generator_id: str,
        *,
        story_type: StoryTypeDefinition,
        prompt_structure: PromptStructureDefinition,
        character_name: str | None = None,
        story_brief: str | None = None,
        cutscene_display_name: str | None = None,
    ) -> _TurnPlan | None:
        turn_index = session.state.beat_cursor
        parameters = build_turn_parameters(session, generator_id)
        context = MinigameGenerationContext(
            fit_tags=list(session.state.fit_tags),
            world_state=list(session.state.world_state),
            difficulty_band=session.state.difficulty_band,
        )
        validation = self._catalog.validate_selection(generator_id, parameters=parameters, context=context)
        if not validation.is_valid:
            return None

        minigame_surface = self._require_minigame_surface(generator_id)
        selected_character_name = character_name or select_character_name(session)
        objective_text = objective_text_for(generator_id, validation.resolved_parameters)
        story_hook = render_story_hook(
            minigame_surface,
            character_name=selected_character_name,
            objective_text=objective_text,
            parameters=validation.resolved_parameters,
        )
        selected_story_brief = story_brief or build_story_brief(
            session,
            story_seed=story_type.narrative_seed,
            character_name=selected_character_name,
            story_hook=story_hook,
            prompt_structure_description=prompt_structure.description,
        )
        selected_display_name = cutscene_display_name or f"Sequence Bridge {turn_index + 1}"
        request = self._build_request(
            session=session,
            prior_turns=prior_turns,
            generator_id=generator_id,
            character_name=selected_character_name,
            parameters=validation.resolved_parameters,
            context=context,
            story_brief=selected_story_brief,
            cutscene_display_name=selected_display_name,
            story_type=story_type,
            prompt_structure=prompt_structure,
            minigame_surface=minigame_surface,
            story_hook=story_hook,
        )
        return _TurnPlan(
            generator_id=generator_id,
            character_name=selected_character_name,
            parameters=validation.resolved_parameters,
            story_hook=story_hook,
            story_type=story_type,
            prompt_structure=prompt_structure,
            minigame_surface=minigame_surface,
            request=request,
        )

    def _build_request(
        self,
        *,
        session: RuntimeSession,
        prior_turns: list[RuntimeTurnRecord],
        generator_id: str,
        character_name: str,
        parameters: dict[str, Any],
        context: MinigameGenerationContext,
        story_brief: str,
        cutscene_display_name: str,
        story_type: StoryTypeDefinition,
        prompt_structure: PromptStructureDefinition,
        minigame_surface: MinigameStorySurface,
        story_hook: str,
    ) -> GeneratedPackageAssemblyRequest:
        turn_slug = f"sequence_turn_{session.state.beat_cursor:03d}"
        minigame_scene = MINIGAME_SCENES[generator_id]
        objective_text = objective_text_for(generator_id, parameters)

        return GeneratedPackageAssemblyRequest(
            package_id=session.package_id,
            package_display_name=session.package_display_name,
            minigame=GeneratedPackageAssemblyMinigameInput(
                beat_id=f"{turn_slug}_minigame",
                display_name=display_name_for(self._catalog, generator_id),
                scene_name=minigame_scene,
                next_scene_name=None,
                generator_id=generator_id,
                parameters=parameters,
                context=context,
            ),
            cutscene=GeneratedPackageAssemblyCutsceneInput(
                beat_id=f"{turn_slug}_cutscene",
                display_name=cutscene_display_name,
                scene_name=PROOF_CUTSCENE_SCENE,
                next_scene_name=minigame_scene,
                story_brief=story_brief,
                style_preset_id="farm_storybook_v1",
                voice_id=self._default_voice_id,
                reference_image_paths=select_continuity_reference_paths(session, character_name),
                continuity_reference_mode="character_priority",
                context=GeneratedStoryboardContext(
                    character_name=character_name,
                    crop_name="",
                    focus_label="",
                    minigame_goal=objective_text,
                    prior_story_summary=prior_story_summary(prior_turns),
                    world_state=list(session.state.world_state),
                    present_character_names=present_character_names(session, character_name),
                    selected_generator_id=generator_id,
                    selected_generator_display_name=display_name_for(self._catalog, generator_id),
                    mission_configuration_summary=mission_configuration_summary(generator_id, parameters),
                    story_type_id=story_type.story_type_id,
                    story_type_display_name=story_type.display_name,
                    story_type_prompt_directives=list(story_type.prompt_directives),
                    prompt_structure_id=prompt_structure.prompt_structure_id,
                    prompt_structure_display_name=prompt_structure.display_name,
                    prompt_structure_directives=list(prompt_structure.storyboard_prompt_directives),
                    minigame_story_hook=story_hook,
                    minigame_prompt_directives=list(minigame_surface.llm_prompt_directives)
                    + [hint.llm_guidance for hint in minigame_surface.parameter_prompt_hints],
                ),
            ),
        )

    def _require_story_type(self, session: RuntimeSession) -> StoryTypeDefinition:
        story_type = self._story_mode_catalog.get_story_type(session.state.story_type_id)
        if story_type is None:
            raise RuntimeError(f"Runtime story type '{session.state.story_type_id}' is not configured.")
        return story_type

    def _require_prompt_structure(self, session: RuntimeSession) -> PromptStructureDefinition:
        prompt_structure = self._story_mode_catalog.get_prompt_structure(session.state.prompt_structure_id)
        if prompt_structure is None:
            raise RuntimeError(
                f"Runtime prompt structure '{session.state.prompt_structure_id}' is not configured."
            )
        return prompt_structure

    def _require_minigame_surface(self, generator_id: str) -> MinigameStorySurface:
        minigame_surface = self._story_mode_catalog.get_minigame_surface(generator_id)
        if minigame_surface is None:
            raise RuntimeError(f"Runtime minigame surface for '{generator_id}' is not configured.")
        return minigame_surface

    def _to_generator_option(self, plan: _TurnPlan) -> StorySequenceGeneratorOption:
        return StorySequenceGeneratorOption(
            generator_id=plan.generator_id,
            display_name=plan.minigame_surface.generator.display_name,
            minigame_id=plan.minigame_surface.generator.minigame_id,
            fit_tags=list(plan.minigame_surface.generator.fit_tags),
            preview_text_template=plan.minigame_surface.generator.preview_text_template,
            story_purposes=list(plan.minigame_surface.story_purposes),
            story_hook_template=plan.story_hook,
            llm_prompt_directives=list(plan.minigame_surface.llm_prompt_directives),
            parameter_prompt_hints=[
                f"{hint.parameter_name}: {hint.llm_guidance}"
                for hint in plan.minigame_surface.parameter_prompt_hints
            ],
        )

    def _create_package(
        self,
        session: RuntimeSession,
        turn_id: str,
        request: GeneratedPackageAssemblyRequest,
        *,
        progress_callback: Callable[[str], None] | None = None,
    ) -> GeneratedPackageAssemblyResult:
        workspace_root = (self._runtime_root / "sessions" / session.session_id / turn_id / "workspace").resolve()
        package_output_path = workspace_root / "package.json"
        runtime_storyboard_service = GeneratedStoryboardService(
            output_root=workspace_root,
            package_output_path=package_output_path,
            planner=getattr(self._storyboard_service, "_planner"),
            image_generator=getattr(self._storyboard_service, "_image_generator"),
            speech_generator=getattr(self._storyboard_service, "_speech_generator"),
            reference_library=getattr(self._storyboard_service, "_reference_library"),
            default_voice_id=getattr(self._storyboard_service, "_default_voice_id"),
        )
        runtime_minigame_service = GeneratedMinigameBeatService(
            package_output_path=package_output_path,
            catalog=getattr(self._minigame_service, "_catalog"),
        )
        package_service = GeneratedPackageAssemblyService(
            minigame_service=runtime_minigame_service,
            storyboard_service=runtime_storyboard_service,
        )
        try:
            return package_service.create_package(
                request,
                progress_callback=progress_callback,
            )
        except Exception as error:
            return GeneratedPackageAssemblyResult(is_valid=False, errors=[str(error)])

    def _archive_artifacts(
        self,
        *,
        session_id: str,
        turn_id: str,
        generated_result: GeneratedPackageAssemblyResult,
    ) -> list[ArtifactDescriptor]:
        cutscene_result = generated_result.cutscene_result
        if cutscene_result is None:
            return []

        archived: list[ArtifactDescriptor] = []
        artifact_root = (self._runtime_root / "artifacts" / session_id / turn_id).resolve()
        artifact_root.mkdir(parents=True, exist_ok=True)

        for generated_asset in cutscene_result.generated_assets:
            source_path = Path(generated_asset.output_path).resolve()
            if not source_path.exists():
                raise RuntimeError(f"Generated asset is missing: {source_path}")

            asset_id = str(uuid4())
            stored_path = artifact_root / f"{asset_id}{source_path.suffix}"
            shutil.copy2(source_path, stored_path)
            archived.append(
                ArtifactDescriptor(
                    asset_id=asset_id,
                    turn_id=turn_id,
                    artifact_type=generated_asset.asset_type,
                    beat_id=generated_asset.beat_id,
                    shot_id=generated_asset.shot_id,
                    mime_type=generated_asset.mime_type,
                    provider_name=generated_asset.provider_name,
                    provider_model=generated_asset.provider_model,
                    fallback_used=generated_asset.fallback_used,
                    stored_path=str(stored_path),
                    metadata=dict(generated_asset.metadata),
                )
            )

            if generated_asset.asset_type != "audio":
                continue

            alignment_path = Path(str(generated_asset.metadata.get("alignment_path", ""))).resolve()
            if not alignment_path.exists():
                raise RuntimeError(f"Generated alignment asset is missing: {alignment_path}")

            alignment_asset_id = str(uuid4())
            alignment_stored_path = artifact_root / f"{alignment_asset_id}{alignment_path.suffix}"
            shutil.copy2(alignment_path, alignment_stored_path)
            archived.append(
                ArtifactDescriptor(
                    asset_id=alignment_asset_id,
                    turn_id=turn_id,
                    artifact_type="alignment",
                    beat_id=generated_asset.beat_id,
                    shot_id=generated_asset.shot_id,
                    mime_type="application/json",
                    stored_path=str(alignment_stored_path),
                    metadata={"source_audio_asset_id": asset_id},
                )
            )

        return archived

    def _build_envelope(
        self,
        *,
        session: RuntimeSession,
        turn_id: str,
        job_id: str,
        plan: _TurnPlan,
        result: GeneratedPackageAssemblyResult,
        artifacts: list[ArtifactDescriptor],
        prior_turns: list[RuntimeTurnRecord],
    ) -> PlayableTurnEnvelope:
        cutscene_beat = find_generated_beat(result.cutscene_result.unity_package, plan.request.cutscene.beat_id)
        minigame_payload = dict(result.minigame_result.materialized_minigame)
        artifact_lookup = {
            (artifact.artifact_type, artifact.shot_id): artifact.asset_id
            for artifact in artifacts
        }
        shots: list[CutsceneShotContract] = []
        for shot in cutscene_beat["Storyboard"]["Shots"]:
            shot_id = shot["ShotId"]
            image_asset_id = artifact_lookup.get(("image", shot_id), "")
            audio_asset_id = artifact_lookup.get(("audio", shot_id), "")
            alignment_asset_id = artifact_lookup.get(("alignment", shot_id), "")
            if not image_asset_id or not audio_asset_id or not alignment_asset_id:
                raise RuntimeError(f"Missing required artifacts for storyboard shot '{shot_id}'.")
            shots.append(
                CutsceneShotContract(
                    shot_id=shot_id,
                    subtitle_text=shot["SubtitleText"],
                    narration_text=shot["NarrationText"],
                    duration_seconds=float(shot["DurationSeconds"]),
                    image_asset_id=image_asset_id,
                    audio_asset_id=audio_asset_id,
                    alignment_asset_id=alignment_asset_id,
                )
            )

        if len(shots) < 3 or len(shots) > 6:
            raise RuntimeError(
                f"Runtime generated cutscene '{cutscene_beat['BeatId']}' must contain 3 to 6 shots; received {len(shots)}."
            )

        continuity_lookup = {
            Path(image.stored_path).resolve(): image.asset_id
            for image in session.state.continuity_images
        }
        reference_asset_ids = []
        for path in plan.request.cutscene.reference_image_paths:
            asset_id = continuity_lookup.get(Path(path).resolve())
            if asset_id and asset_id not in reference_asset_ids:
                reference_asset_ids.append(asset_id)

        return PlayableTurnEnvelope(
            session_id=session.session_id,
            turn_id=turn_id,
            entry_scene_name=cutscene_beat["SceneName"],
            cutscene=CutsceneContract(
                beat_id=cutscene_beat["BeatId"],
                display_name=cutscene_beat["DisplayName"],
                scene_name=cutscene_beat["SceneName"],
                next_scene_name=cutscene_beat.get("NextSceneName", ""),
                style_preset_id=cutscene_beat["Storyboard"]["StylePresetId"],
                shots=shots,
            ),
            minigame=MinigameContract(
                beat_id=plan.request.minigame.beat_id,
                display_name=plan.request.minigame.display_name,
                scene_name=plan.request.minigame.scene_name,
                adapter_id=minigame_payload["AdapterId"],
                objective_text=minigame_payload["ObjectiveText"],
                required_count=int(minigame_payload["RequiredCount"]),
                time_limit_seconds=float(minigame_payload["TimeLimitSeconds"]),
                generator_id=minigame_payload["GeneratorId"],
                minigame_id=minigame_payload["MinigameId"],
                fallback_generator_ids=list(minigame_payload.get("FallbackGeneratorIds", [])),
                resolved_parameters=dict(minigame_payload.get("ResolvedParameters", {})),
                resolved_parameter_entries=list(minigame_payload.get("ResolvedParameterEntries", [])),
            ),
            artifacts=artifacts,
            continuity=RuntimeContinuityContract(
                reference_image_asset_ids=reference_asset_ids,
                world_state=list(session.state.world_state),
                present_character_names=list(plan.request.cutscene.context.present_character_names),
                prior_story_summary=prior_story_summary(prior_turns),
            ),
            debug=RuntimeDebugContract(
                job_id=job_id,
                generator_id=plan.generator_id,
                character_name=plan.character_name,
                story_type_id=plan.story_type.story_type_id,
                prompt_structure_id=plan.prompt_structure.prompt_structure_id,
                package_id=session.package_id,
                package_display_name=session.package_display_name,
                fallback_generator_ids=list(result.fallback_generator_ids),
                provider_errors=list(result.errors),
            ),
        )

    def _advance_session_state(self, session: RuntimeSession, turn: RuntimeTurnRecord) -> RuntimeSession:
        world_state = updated_world_state(session, turn.turn_index)
        crop_type = turn.envelope.minigame.resolved_parameters.get("cropType")
        recent_crops = list(session.state.recent_crop_types)
        if isinstance(crop_type, str):
            recent_crops = roll_recent(recent_crops, crop_type, 2)

        continuity_images = roll_continuity_images(
            session.state.continuity_images,
            [
                RuntimeContinuityImageRecord(
                    asset_id=artifact.asset_id,
                    shot_id=artifact.shot_id,
                    turn_id=turn.turn_id,
                    turn_index=turn.turn_index,
                    character_name=turn.character_name,
                    stored_path=artifact.stored_path,
                )
                for artifact in turn.envelope.artifacts
                if artifact.artifact_type == "image"
            ],
            session.state.max_continuity_images,
        )
        next_state = session.state.model_copy(
            update={
                "beat_cursor": session.state.beat_cursor + 1,
                "recent_generator_ids": roll_recent(
                    session.state.recent_generator_ids,
                    turn.generator_id,
                    session.state.max_recent_generators,
                ),
                "recent_character_names": roll_recent(session.state.recent_character_names, turn.character_name, 2),
                "recent_crop_types": recent_crops,
                "world_state": world_state,
                "continuity_images": continuity_images,
                "last_generator_id": turn.generator_id,
                "last_character_name": turn.character_name,
                "last_minigame_goal": turn.envelope.minigame.objective_text,
            }
        )
        return session.model_copy(
            update={
                "state": next_state,
                "last_ready_turn_id": turn.turn_id,
                "updated_at": datetime.now(UTC).isoformat(),
            }
        )

    def _select_preferred_plan(self, session: RuntimeSession, plans: list[_TurnPlan]) -> _TurnPlan:
        recent = set(session.state.recent_generator_ids[-session.state.max_recent_generators :])
        for plan in plans:
            if plan.generator_id not in recent:
                return plan
        return plans[0]
