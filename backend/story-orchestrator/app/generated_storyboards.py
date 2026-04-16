import json
import logging
from pathlib import Path
from typing import Any, Callable

from .config import Settings
from .generated_storyboard_models import (
    GeneratedImageAsset,
    GeneratedSpeechAsset,
    GeneratedStoryboardAssetRecord,
    GeneratedStoryboardContext,
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardPackageResult,
    GeneratedStoryboardPlan,
    GeneratedStoryboardPlanShot,
    ImageGenerator,
    SpeechGenerator,
    StoryboardPlanner,
)
from .storyboard_media import (
    ChainImageGenerator,
    ChainSpeechGenerator,
    LocalReferenceImageGenerator,
    PlaceholderImageGenerator,
    PlaceholderSpeechGenerator,
)
from .storyboard_provider_clients import (
    ElevenLabsSpeechGenerator,
    GeminiImageGenerator,
    OpenAIImageGenerator,
    OpenAISpeechGenerator,
)
from .storyboard_llm_planner import OpenAIStoryboardPlanner, StoryboardPlannerChain
from .storyboard_quality import QualityGatedImageGenerator
from .storyboard_reference_library import StoryboardReferenceLibrary, StoryboardReferencePathResolver

_LOGGER = logging.getLogger("uvicorn.error")


class TemplateStoryboardPlanner:
    def plan(self, request: GeneratedStoryboardCutsceneRequest) -> GeneratedStoryboardPlan:
        context = request.context
        shots = _build_plan_shots(context)

        return GeneratedStoryboardPlan(
            beat_id=request.beat_id,
            display_name=request.display_name,
            scene_name=request.scene_name,
            next_scene_name=request.next_scene_name or "",
            style_preset_id=request.style_preset_id,
            shots=shots,
        )


class GeneratedStoryboardService:
    def __init__(
        self,
        *,
        output_root: Path,
        package_output_path: Path,
        planner: StoryboardPlanner,
        image_generator: ImageGenerator,
        speech_generator: SpeechGenerator,
        reference_library: StoryboardReferenceLibrary | None = None,
        default_voice_id: str = "",
    ) -> None:
        self._output_root = output_root
        self._package_output_path = package_output_path
        self._planner = planner
        self._image_generator = image_generator
        self._speech_generator = speech_generator
        self._reference_library = reference_library
        self._reference_path_resolver = StoryboardReferencePathResolver(output_root, reference_library)
        self._default_voice_id = default_voice_id

    @classmethod
    def from_settings(cls, settings: Settings, base_dir: Path) -> "GeneratedStoryboardService":
        output_root = settings.resolve_path(base_dir, settings.generated_storyboard_output_root)
        package_output_path = settings.resolve_path(base_dir, settings.generated_storyboard_package_path)
        reference_library = StoryboardReferenceLibrary(output_root)
        return cls(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=StoryboardPlannerChain(
                planners=[
                    OpenAIStoryboardPlanner.from_settings(settings),
                    TemplateStoryboardPlanner(),
                ]
            ),
            image_generator=QualityGatedImageGenerator(
                ChainImageGenerator(
                    [
                        GeminiImageGenerator(
                            api_key=settings.gemini_api_key,
                            models=[
                                settings.gemini_image_model,
                                settings.gemini_image_fallback_model,
                            ],
                        ),
                        OpenAIImageGenerator(
                            api_key=settings.openai_api_key,
                            model=settings.openai_image_model,
                        ),
                        LocalReferenceImageGenerator(),
                        PlaceholderImageGenerator(),
                    ]
                ),
                max_attempts=2,
            ),
            speech_generator=ChainSpeechGenerator(
                [
                    ElevenLabsSpeechGenerator(
                        api_key=settings.elevenlabs_api_key,
                        model_id=settings.elevenlabs_model_id,
                    ),
                    OpenAISpeechGenerator(
                        api_key=settings.openai_api_key,
                        model_id=settings.openai_speech_model,
                        voice=settings.openai_speech_voice,
                    ),
                    PlaceholderSpeechGenerator(),
                ]
            ),
            reference_library=reference_library,
            default_voice_id=settings.elevenlabs_voice_id,
        )

    def create_package(
        self,
        request: GeneratedStoryboardCutsceneRequest,
        *,
        progress_callback: Callable[[str], None] | None = None,
    ) -> GeneratedStoryboardPackageResult:
        resolved_request = self._resolve_request_context(request)
        _LOGGER.info(
            "[GeneratedStoryBackend] storyboard planning start package_id=%s beat_id=%s scene=%s",
            resolved_request.package_id,
            resolved_request.beat_id,
            resolved_request.scene_name,
        )
        plan = self._planner.plan(resolved_request)
        voice_id = request.voice_id or self._default_voice_id
        if not voice_id:
            raise RuntimeError("A voice_id is required to generate narration audio.")

        base_asset_dir = self._output_root / "GeneratedStoryboards" / resolved_request.package_id / resolved_request.beat_id
        reference_image_paths = self._reference_path_resolver.resolve_paths(resolved_request)
        _LOGGER.info(
            "[GeneratedStoryBackend] storyboard plan ready beat_id=%s shots=%s refs=%s character=%s",
            plan.beat_id,
            len(plan.shots),
            len(reference_image_paths),
            resolved_request.context.character_name,
        )
        if progress_callback is not None:
            progress_callback("generating_images")
        shots = []
        generated_assets: list[GeneratedStoryboardAssetRecord] = []
        image_assets: list[GeneratedImageAsset] = []
        image_resource_paths: list[str] = []

        for shot in plan.shots:
            stem = base_asset_dir / shot.shot_id
            image_prompt = self._build_image_prompt(resolved_request, shot)
            _LOGGER.info(
                "[GeneratedStoryBackend] storyboard image start beat_id=%s shot_id=%s",
                plan.beat_id,
                shot.shot_id,
            )
            image_asset = self._image_generator.generate_image(
                prompt=image_prompt,
                reference_image_paths=reference_image_paths,
                output_path=stem.with_suffix(".png"),
                aspect_ratio=resolved_request.aspect_ratio,
                image_size=resolved_request.image_size,
            )
            image_assets.append(image_asset)
            image_resource_paths.append(self._to_resource_path(image_asset.output_path))
            _LOGGER.info(
                "[GeneratedStoryBackend] storyboard image complete beat_id=%s shot_id=%s image_provider=%s",
                plan.beat_id,
                shot.shot_id,
                image_asset.provider_name,
            )

        if progress_callback is not None:
            progress_callback("generating_audio")

        speech_assets: list[GeneratedSpeechAsset] = []
        audio_resource_paths: list[str] = []
        for index, shot in enumerate(plan.shots):
            stem = base_asset_dir / shot.shot_id
            line_text = shot.subtitle_text.strip()
            previous_text = plan.shots[index - 1].subtitle_text.strip() if index > 0 else ""
            next_text = plan.shots[index + 1].subtitle_text.strip() if index + 1 < len(plan.shots) else ""
            _LOGGER.info(
                "[GeneratedStoryBackend] storyboard audio start beat_id=%s shot_id=%s",
                plan.beat_id,
                shot.shot_id,
            )
            speech_asset = self._speech_generator.generate_speech(
                text=line_text,
                voice_id=voice_id,
                output_path=stem.with_suffix(".mp3"),
                previous_text=previous_text,
                next_text=next_text,
            )
            speech_assets.append(speech_asset)
            audio_resource_paths.append(self._to_resource_path(speech_asset.output_path))
            _LOGGER.info(
                "[GeneratedStoryBackend] storyboard audio complete beat_id=%s shot_id=%s audio_provider=%s",
                plan.beat_id,
                shot.shot_id,
                speech_asset.provider_name,
            )

        if progress_callback is not None:
            progress_callback("assembling_contract")

        for shot, image_asset, speech_asset, image_resource_path, audio_resource_path in zip(
            plan.shots,
            image_assets,
            speech_assets,
            image_resource_paths,
            audio_resource_paths,
            strict=True,
        ):
            line_text = shot.subtitle_text.strip()
            shots.append(
                {
                    "ShotId": shot.shot_id,
                    "SubtitleText": line_text,
                    "NarrationText": line_text,
                    "ImageResourcePath": image_resource_path,
                    "AudioResourcePath": audio_resource_path,
                    "DurationSeconds": max(shot.duration_seconds, speech_asset.duration_seconds),
                }
            )
            generated_assets.append(
                GeneratedStoryboardAssetRecord(
                    asset_id=f"{plan.beat_id}_{shot.shot_id}_image",
                    beat_id=plan.beat_id,
                    shot_id=shot.shot_id,
                    asset_type="image",
                    provider_name=image_asset.provider_name,
                    provider_model=image_asset.provider_model,
                    fallback_used=image_asset.fallback_used,
                    mime_type=image_asset.mime_type,
                    output_path=str(image_asset.output_path),
                    resource_path=image_resource_path,
                    metadata=dict(image_asset.source_metadata),
                )
            )
            audio_metadata = dict(speech_asset.source_metadata)
            audio_metadata["alignment_path"] = str(speech_asset.alignment_path)
            audio_metadata["duration_seconds"] = speech_asset.duration_seconds
            generated_assets.append(
                GeneratedStoryboardAssetRecord(
                    asset_id=f"{plan.beat_id}_{shot.shot_id}_audio",
                    beat_id=plan.beat_id,
                    shot_id=shot.shot_id,
                    asset_type="audio",
                    provider_name=speech_asset.provider_name,
                    provider_model=speech_asset.provider_model,
                    fallback_used=speech_asset.fallback_used,
                    mime_type=speech_asset.mime_type,
                    output_path=str(speech_asset.output_path),
                    resource_path=audio_resource_path,
                    metadata=audio_metadata,
                )
            )

        unity_package = self._load_existing_package(resolved_request)
        unity_package["PackageId"] = resolved_request.package_id
        unity_package["SchemaVersion"] = unity_package.get("SchemaVersion", 1)
        unity_package["PackageVersion"] = unity_package.get("PackageVersion", 1)
        unity_package["DisplayName"] = resolved_request.package_display_name
        beats = unity_package.setdefault("Beats", [])

        updated_beat = {
            "BeatId": plan.beat_id,
            "DisplayName": plan.display_name,
            "Kind": "Cutscene",
            "SceneName": plan.scene_name,
            "NextSceneName": plan.next_scene_name,
            "SequenceSteps": [
                {
                    "StepType": "ObjectivePopup",
                    "StringParam": shots[0]["SubtitleText"],
                }
            ],
            "Storyboard": {
                "StylePresetId": plan.style_preset_id,
                "Shots": shots,
            },
        }

        beat_replaced = False
        for index, beat in enumerate(beats):
            if beat.get("BeatId") == plan.beat_id or beat.get("SceneName") == plan.scene_name:
                beats[index] = updated_beat
                beat_replaced = True
                break

        if not beat_replaced:
            beats.append(updated_beat)

        self._package_output_path.parent.mkdir(parents=True, exist_ok=True)
        self._package_output_path.write_text(
            json.dumps(unity_package, indent=2),
            encoding="utf-8",
        )
        _LOGGER.info(
            "[GeneratedStoryBackend] storyboard package written package_id=%s output_path=%s",
            resolved_request.package_id,
            self._package_output_path,
        )

        return GeneratedStoryboardPackageResult(
            package_output_path=str(self._package_output_path),
            unity_package=unity_package,
            generated_assets=generated_assets,
        )

    def _resolve_request_context(
        self,
        request: GeneratedStoryboardCutsceneRequest,
    ) -> GeneratedStoryboardCutsceneRequest:
        crop_name = request.context.crop_name
        focus_label = request.context.focus_label
        minigame_goal = request.context.minigame_goal
        if request.linked_minigame_beat_id:
            package = self._load_existing_package(request)
            linked_beat = self._find_linked_minigame_beat(package, request.linked_minigame_beat_id)
            minigame_payload = linked_beat.get("Minigame") or {}
            if not minigame_goal:
                minigame_goal = minigame_payload.get("ObjectiveText") or ""
            if not crop_name:
                crop_name = self._extract_crop_name(minigame_payload)
            if not focus_label:
                focus_label = self._extract_focus_label(minigame_payload)

        if not minigame_goal:
            raise RuntimeError("Storyboard context requires minigame_goal or linked_minigame_beat_id.")
        if not crop_name and not focus_label:
            raise RuntimeError("Storyboard context requires crop_name, focus_label, or a linked minigame beat that can derive one.")

        return request.model_copy(
            update={
                "context": GeneratedStoryboardContext(
                    character_name=request.context.character_name,
                    crop_name=crop_name,
                    focus_label=focus_label,
                    minigame_goal=minigame_goal,
                    prior_story_summary=request.context.prior_story_summary,
                    world_state=list(request.context.world_state),
                    present_character_names=list(request.context.present_character_names),
                    selected_generator_id=request.context.selected_generator_id,
                    selected_generator_display_name=request.context.selected_generator_display_name,
                    mission_configuration_summary=request.context.mission_configuration_summary,
                    story_type_id=request.context.story_type_id,
                    story_type_display_name=request.context.story_type_display_name,
                    story_type_prompt_directives=list(request.context.story_type_prompt_directives),
                    prompt_structure_id=request.context.prompt_structure_id,
                    prompt_structure_display_name=request.context.prompt_structure_display_name,
                    prompt_structure_directives=list(request.context.prompt_structure_directives),
                    minigame_story_hook=request.context.minigame_story_hook,
                    minigame_prompt_directives=list(request.context.minigame_prompt_directives),
                )
            }
        )

    def _build_image_prompt(
        self,
        request: GeneratedStoryboardCutsceneRequest,
        shot: GeneratedStoryboardPlanShot,
    ) -> str:
        context = request.context
        style_prompt = _build_style_prompt(request.style_preset_id)
        subject_label = _resolve_subject_label(context)
        return (
            f"{style_prompt} Story brief: {request.story_brief}. "
            f"Character: {context.character_name}. Focus: {subject_label}. "
            f"Gameplay goal: {context.minigame_goal}. "
            f"Previous context: {context.prior_story_summary or 'none'}. "
            f"World state: {', '.join(context.world_state) if context.world_state else 'none'}. "
            f"Present characters: {', '.join(context.present_character_names) if context.present_character_names else context.character_name}. "
            f"Mission configuration: {context.mission_configuration_summary or 'none'}. "
            f"Story type: {context.story_type_display_name or context.story_type_id or 'none'}. "
            f"Prompt structure: {context.prompt_structure_display_name or context.prompt_structure_id or 'none'}. "
            f"Minigame story hook: {context.minigame_story_hook or 'none'}. "
            f"Story directives: {', '.join(context.story_type_prompt_directives) if context.story_type_prompt_directives else 'none'}. "
            f"Prompt structure directives: {', '.join(context.prompt_structure_directives) if context.prompt_structure_directives else 'none'}. "
            f"Minigame prompt directives: {', '.join(context.minigame_prompt_directives) if context.minigame_prompt_directives else 'none'}. "
            f"Frame direction: {shot.image_prompt}. "
            "Use any reference images only for character identity, outfit, palette, and overall art style. "
            "Create a fresh composition for this beat as a full-bleed image and preserve character identity across shots. "
            "Do not copy camera angle, framing, background layout, staging, or pose from prior images. "
            "The bottom third of the image must show scene content, not an empty dark band, lower-third plate, title card, or graphic panel. "
            "Do not draw subtitle boxes, caption panels, readable text, speech bubbles, UI overlays, labels, watermarks, or prompt text inside the image."
        )

    def _to_resource_path(self, asset_path: Path) -> str:
        relative_path = asset_path.relative_to(self._output_root)
        return str(relative_path.with_suffix("")).replace("\\", "/")

    def _load_existing_package(self, request: GeneratedStoryboardCutsceneRequest) -> dict[str, Any]:
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

    @staticmethod
    def _find_linked_minigame_beat(package: dict[str, Any], beat_id: str) -> dict[str, Any]:
        for beat in package.get("Beats", []):
            if beat.get("BeatId") != beat_id:
                continue
            if beat.get("Kind") != "Minigame" or not isinstance(beat.get("Minigame"), dict):
                raise RuntimeError(f"Linked beat '{beat_id}' is not a valid minigame beat.")
            return beat

        raise RuntimeError(f"Linked minigame beat '{beat_id}' was not found in the story package.")

    @staticmethod
    def _extract_crop_name(minigame_payload: dict[str, Any]) -> str:
        resolved_parameters = _extract_resolved_parameters(minigame_payload)
        if not resolved_parameters:
            return ""

        crop_type = resolved_parameters.get("cropType")
        if not isinstance(crop_type, str) or not crop_type:
            return ""

        return _pluralize_crop(crop_type)

    @staticmethod
    def _extract_focus_label(minigame_payload: dict[str, Any]) -> str:
        resolved_parameters = _extract_resolved_parameters(minigame_payload)
        if not resolved_parameters:
            return ""

        minigame_id = minigame_payload.get("MinigameId")
        if minigame_id == "find_tools":
            tool_set = resolved_parameters.get("targetToolSet")
            if isinstance(tool_set, str) and tool_set:
                return f"{tool_set} tools"

        if minigame_id == "chicken_chase":
            return "chickens"

        return ""


def _build_style_prompt(style_preset_id: str) -> str:
    if style_preset_id == "farm_storybook_v1":
        return (
            "Create a cohesive farm storybook storyboard frame with warm sunrise lighting, "
            "grounded rural props, painterly but readable textures, and a cinematic 3D-game concept-art feel."
        )

    return (
        f"Create a cohesive storyboard frame that follows the style preset '{style_preset_id}' "
        "while keeping the scene readable and grounded."
    )


def _build_plan_shots(context: GeneratedStoryboardContext) -> list[GeneratedStoryboardPlanShot]:
    if context.crop_name:
        crop_label = _singularize_crop(context.crop_name)
        minigame_prompt = _append_goal_clause(context.minigame_goal or "", "and keep the rows tidy.")
        return [
            GeneratedStoryboardPlanShot(
                shot_id="shot_01",
                subtitle_text=f"Nice work. The {crop_label} beds are finally ready for you.",
                narration_text=f"Nice work. The {crop_label} beds are finally ready for you.",
                image_prompt=(
                    f"{context.character_name} stands at the farm edge after the chicken chase, "
                    f"revealing freshly prepared {crop_label} rows at sunrise."
                ),
                duration_seconds=3.2,
            ),
            GeneratedStoryboardPlanShot(
                shot_id="shot_02",
                subtitle_text=minigame_prompt,
                narration_text=minigame_prompt,
                image_prompt=(
                    f"{context.character_name} gestures toward the {crop_label} rows and the tools "
                    "needed for the next farm task."
                ),
                duration_seconds=3.6,
            ),
            GeneratedStoryboardPlanShot(
                shot_id="shot_03",
                subtitle_text="Head to the plots. The farm is waiting on your hands.",
                narration_text="Head to the plots. The farm is waiting on your hands.",
                image_prompt=(
                    f"A forward-looking shot from the pen toward the {crop_label} plots, "
                    "inviting the player into the next minigame."
                ),
                duration_seconds=3.0,
            ),
        ]

    focus_label = context.focus_label or "tools"
    return [
        GeneratedStoryboardPlanShot(
            shot_id="shot_01",
            subtitle_text=f"Nice work. The {focus_label} are back where they belong.",
            narration_text=f"Nice work. The {focus_label} are back where they belong.",
            image_prompt=(
                f"{context.character_name} stands beside the farm path at sunrise with the recovered {focus_label}, "
                "framing the homestead as the next phase begins."
            ),
            duration_seconds=3.2,
        ),
        GeneratedStoryboardPlanShot(
            shot_id="shot_02",
            subtitle_text=f"The {focus_label} are back in your hands. Head to the plots and get ready to plant.",
            narration_text=f"The {focus_label} are back in your hands. Head to the plots and get ready to plant.",
            image_prompt=(
                f"{context.character_name} gestures from the recovered {focus_label} toward the prepared farm plots, "
                "turning the recovered gear into a clear call toward planting."
            ),
            duration_seconds=3.4,
        ),
        GeneratedStoryboardPlanShot(
            shot_id="shot_03",
            subtitle_text="The fields are waiting. Let's start the first real farm loop.",
            narration_text="The fields are waiting. Let's start the first real farm loop.",
            image_prompt=(
                "A forward-looking sunrise view from the tool path toward the ready plots, "
                "inviting the player into the first full farming loop."
            ),
            duration_seconds=3.0,
        ),
    ]


def _pluralize_crop(crop_type: str) -> str:
    irregular = {"tomato": "tomatoes", "corn": "corn"}
    return irregular.get(crop_type, f"{crop_type}s")


def _append_goal_clause(goal: str, suffix: str) -> str:
    normalized_goal = goal.rstrip(" .!?")
    return f"{normalized_goal}, {suffix}"


def _singularize_crop(crop_name: str) -> str:
    irregular = {"tomatoes": "tomato", "corn": "corn"}
    if crop_name in irregular:
        return irregular[crop_name]
    if crop_name.endswith("s") and len(crop_name) > 1:
        return crop_name[:-1]
    return crop_name


def _resolve_subject_label(context: GeneratedStoryboardContext) -> str:
    if context.crop_name:
        return _singularize_crop(context.crop_name)
    if context.focus_label:
        return context.focus_label
    return "farm task"


def _extract_resolved_parameters(minigame_payload: dict[str, Any]) -> dict[str, Any]:
    resolved_parameters = minigame_payload.get("ResolvedParameters")
    if isinstance(resolved_parameters, dict) and resolved_parameters:
        return resolved_parameters

    entries = minigame_payload.get("ResolvedParameterEntries")
    if not isinstance(entries, list):
        return {}

    extracted: dict[str, Any] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue

        name = entry.get("Name")
        value_type = entry.get("ValueType")
        if not isinstance(name, str) or not name:
            continue

        if value_type == "Bool":
            extracted[name] = bool(entry.get("BoolValue"))
        elif value_type == "Int":
            extracted[name] = int(entry.get("IntValue", 0))
        elif value_type == "Float":
            extracted[name] = float(entry.get("FloatValue", 0.0))
        else:
            extracted[name] = str(entry.get("StringValue", ""))

    return extracted
