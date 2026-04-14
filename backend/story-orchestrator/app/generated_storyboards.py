import base64
import json
import mimetypes
from pathlib import Path
from typing import Any

import httpx

from .config import Settings
from .generated_storyboard_models import (
    GeneratedImageAsset,
    GeneratedSpeechAsset,
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
    PlaceholderImageGenerator,
    PlaceholderSpeechGenerator,
)


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


class GeminiImageGenerator:
    def __init__(self, api_key: str, models: list[str]) -> None:
        self._api_key = api_key
        self._models = [model for model in models if model]

    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        parts = [{"text": prompt}]
        for path in reference_image_paths:
            if not path:
                continue

            reference_path = Path(path)
            mime_type = mimetypes.guess_type(reference_path.name)[0] or "image/png"
            parts.append(
                {
                    "inlineData": {
                        "mimeType": mime_type,
                        "data": base64.b64encode(reference_path.read_bytes()).decode("ascii"),
                    }
                }
            )

        last_error: Exception | None = None
        for model in self._models:
            try:
                payload = self._build_payload(
                    model=model,
                    parts=parts,
                    aspect_ratio=aspect_ratio,
                    image_size=image_size,
                )
                response = httpx.post(
                    f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
                    headers={
                        "Content-Type": "application/json",
                        "x-goog-api-key": self._api_key,
                    },
                    json=payload,
                    timeout=120,
                )
                response.raise_for_status()

                data = response.json()
                inline_data = self._extract_inline_image(data)
                output_path.parent.mkdir(parents=True, exist_ok=True)
                output_path.write_bytes(base64.b64decode(inline_data["data"]))
                return GeneratedImageAsset(output_path=output_path, mime_type=inline_data.get("mimeType", "image/png"))
            except httpx.HTTPStatusError as error:
                last_error = error
                if error.response.status_code not in (403, 404, 429):
                    raise
            except Exception as error:
                last_error = error
                raise

        if last_error is not None:
            raise last_error

        raise RuntimeError("No Gemini image model is configured.")

    @staticmethod
    def _build_payload(
        *,
        model: str,
        parts: list[dict[str, Any]],
        aspect_ratio: str,
        image_size: str,
    ) -> dict[str, Any]:
        image_config: dict[str, Any] = {"aspectRatio": aspect_ratio}
        if image_size and "gemini-2.5" not in model:
            image_config["imageSize"] = image_size

        return {
            "contents": [{"parts": parts}],
            "generationConfig": {
                "responseModalities": ["TEXT", "IMAGE"],
                "imageConfig": image_config,
            },
        }

    @staticmethod
    def _extract_inline_image(response_json: dict[str, Any]) -> dict[str, Any]:
        for candidate in response_json.get("candidates", []):
            content = candidate.get("content", {})
            for part in content.get("parts", []):
                inline_data = part.get("inlineData")
                if inline_data and inline_data.get("data"):
                    return inline_data

        raise RuntimeError("Gemini did not return an inline image payload.")


class ElevenLabsSpeechGenerator:
    def __init__(self, api_key: str, model_id: str) -> None:
        self._api_key = api_key
        self._model_id = model_id

    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> GeneratedSpeechAsset:
        payload: dict[str, Any] = {
            "text": text,
            "model_id": self._model_id,
        }
        if previous_text:
            payload["previous_text"] = previous_text
        if next_text:
            payload["next_text"] = next_text

        response = httpx.post(
            f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}/with-timestamps",
            headers={
                "Content-Type": "application/json",
                "xi-api-key": self._api_key,
            },
            json=payload,
            timeout=120,
        )
        response.raise_for_status()

        data = response.json()
        audio_base64 = data.get("audio_base64")
        if not audio_base64:
            raise RuntimeError("ElevenLabs did not return audio data.")

        alignment = data.get("normalized_alignment") or data.get("alignment") or {}
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_bytes(base64.b64decode(audio_base64))

        alignment_path = output_path.with_suffix(".alignment.json")
        alignment_path.write_text(json.dumps(alignment, indent=2), encoding="utf-8")

        duration_seconds = _extract_alignment_duration(alignment)
        return GeneratedSpeechAsset(
            output_path=output_path,
            alignment_path=alignment_path,
            duration_seconds=duration_seconds,
            mime_type="audio/mpeg",
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
        default_voice_id: str = "",
    ) -> None:
        self._output_root = output_root
        self._package_output_path = package_output_path
        self._planner = planner
        self._image_generator = image_generator
        self._speech_generator = speech_generator
        self._default_voice_id = default_voice_id

    @classmethod
    def from_settings(cls, settings: Settings, base_dir: Path) -> "GeneratedStoryboardService":
        output_root = settings.resolve_path(base_dir, settings.generated_storyboard_output_root)
        package_output_path = settings.resolve_path(base_dir, settings.generated_storyboard_package_path)
        return cls(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=ChainImageGenerator(
                [
                    GeminiImageGenerator(
                        api_key=settings.gemini_api_key,
                        models=[
                            settings.gemini_image_model,
                            settings.gemini_image_fallback_model,
                        ],
                    ),
                    PlaceholderImageGenerator(),
                ]
            ),
            speech_generator=ChainSpeechGenerator(
                [
                    ElevenLabsSpeechGenerator(
                        api_key=settings.elevenlabs_api_key,
                        model_id=settings.elevenlabs_model_id,
                    ),
                    PlaceholderSpeechGenerator(),
                ]
            ),
            default_voice_id=settings.elevenlabs_voice_id,
        )

    def create_package(
        self,
        request: GeneratedStoryboardCutsceneRequest,
    ) -> GeneratedStoryboardPackageResult:
        resolved_request = self._resolve_request_context(request)
        plan = self._planner.plan(resolved_request)
        voice_id = request.voice_id or self._default_voice_id
        if not voice_id:
            raise RuntimeError("A voice_id is required to generate narration audio.")

        base_asset_dir = self._output_root / "GeneratedStoryboards" / resolved_request.package_id / resolved_request.beat_id
        shots = []
        for index, shot in enumerate(plan.shots):
            stem = base_asset_dir / shot.shot_id
            image_asset = self._image_generator.generate_image(
                prompt=self._build_image_prompt(resolved_request, shot),
                reference_image_paths=resolved_request.reference_image_paths,
                output_path=stem.with_suffix(".png"),
                aspect_ratio=resolved_request.aspect_ratio,
                image_size=resolved_request.image_size,
            )
            speech_asset = self._speech_generator.generate_speech(
                text=shot.narration_text,
                voice_id=voice_id,
                output_path=stem.with_suffix(".mp3"),
                previous_text=plan.shots[index - 1].narration_text if index > 0 else "",
                next_text=plan.shots[index + 1].narration_text if index + 1 < len(plan.shots) else "",
            )
            shots.append(
                {
                    "ShotId": shot.shot_id,
                    "SubtitleText": shot.subtitle_text,
                    "NarrationText": shot.narration_text,
                    "ImageResourcePath": self._to_resource_path(image_asset.output_path),
                    "AudioResourcePath": self._to_resource_path(speech_asset.output_path),
                    "DurationSeconds": max(shot.duration_seconds, speech_asset.duration_seconds),
                }
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

        return GeneratedStoryboardPackageResult(
            package_output_path=str(self._package_output_path),
            unity_package=unity_package,
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
            f"Frame direction: {shot.image_prompt}. "
            "Keep the framing readable for subtitles and preserve character identity across shots."
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


def _extract_alignment_duration(alignment: dict[str, Any]) -> float:
    end_times = alignment.get("character_end_times_seconds") or []
    if not end_times:
        return 0.0

    return float(end_times[-1])


def _pluralize_crop(crop_type: str) -> str:
    irregular = {"tomato": "tomatoes"}
    return irregular.get(crop_type, f"{crop_type}s")


def _append_goal_clause(goal: str, suffix: str) -> str:
    normalized_goal = goal.rstrip(" .!?")
    return f"{normalized_goal}, {suffix}"


def _singularize_crop(crop_name: str) -> str:
    irregular = {"tomatoes": "tomato"}
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
