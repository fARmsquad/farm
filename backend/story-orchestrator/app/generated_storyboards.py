import base64
import json
import mimetypes
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Protocol

import httpx
from pydantic import BaseModel, Field

from .config import Settings


class GeneratedStoryboardContext(BaseModel):
    character_name: str = Field(min_length=1)
    crop_name: str = Field(min_length=1)
    minigame_goal: str = Field(min_length=1)


class GeneratedStoryboardCutsceneRequest(BaseModel):
    package_id: str = Field(min_length=1)
    package_display_name: str = Field(min_length=1)
    beat_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    scene_name: str = Field(min_length=1)
    next_scene_name: str | None = None
    story_brief: str = Field(min_length=1)
    style_preset_id: str = Field(default="farm_storybook_v1", min_length=1)
    voice_id: str | None = None
    reference_image_paths: list[str] = Field(default_factory=list)
    aspect_ratio: str = Field(default="16:9", min_length=1)
    image_size: str = Field(default="2K", min_length=1)
    context: GeneratedStoryboardContext


class GeneratedStoryboardPackageResult(BaseModel):
    package_output_path: str
    unity_package: dict[str, Any]


@dataclass(frozen=True)
class GeneratedStoryboardPlanShot:
    shot_id: str
    subtitle_text: str
    narration_text: str
    image_prompt: str
    duration_seconds: float


@dataclass(frozen=True)
class GeneratedStoryboardPlan:
    beat_id: str
    display_name: str
    scene_name: str
    next_scene_name: str
    style_preset_id: str
    shots: list[GeneratedStoryboardPlanShot]


@dataclass(frozen=True)
class GeneratedImageAsset:
    output_path: Path
    mime_type: str


@dataclass(frozen=True)
class GeneratedSpeechAsset:
    output_path: Path
    alignment_path: Path
    duration_seconds: float
    mime_type: str


class StoryboardPlanner(Protocol):
    def plan(self, request: GeneratedStoryboardCutsceneRequest) -> GeneratedStoryboardPlan:
        ...


class ImageGenerator(Protocol):
    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        ...


class SpeechGenerator(Protocol):
    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> GeneratedSpeechAsset:
        ...


class TemplateStoryboardPlanner:
    def plan(self, request: GeneratedStoryboardCutsceneRequest) -> GeneratedStoryboardPlan:
        context = request.context
        shots = [
            GeneratedStoryboardPlanShot(
                shot_id="shot_01",
                subtitle_text=f"Nice work. The {context.crop_name} beds are finally ready for you.",
                narration_text=f"Nice work. The {context.crop_name} beds are finally ready for you.",
                image_prompt=(
                    f"{context.character_name} stands at the farm edge after the chicken chase, "
                    f"revealing freshly prepared {context.crop_name} rows at sunrise."
                ),
                duration_seconds=3.2,
            ),
            GeneratedStoryboardPlanShot(
                shot_id="shot_02",
                subtitle_text=f"{context.minigame_goal}, and keep the rows tidy.",
                narration_text=f"{context.minigame_goal}, and keep the rows tidy.",
                image_prompt=(
                    f"{context.character_name} gestures toward the {context.crop_name} rows and the tools "
                    "needed for the next farm task."
                ),
                duration_seconds=3.6,
            ),
            GeneratedStoryboardPlanShot(
                shot_id="shot_03",
                subtitle_text="Head to the plots. The farm is waiting on your hands.",
                narration_text="Head to the plots. The farm is waiting on your hands.",
                image_prompt=(
                    f"A forward-looking shot from the pen toward the {context.crop_name} plots, "
                    "inviting the player into the next minigame."
                ),
                duration_seconds=3.0,
            ),
        ]

        return GeneratedStoryboardPlan(
            beat_id=request.beat_id,
            display_name=request.display_name,
            scene_name=request.scene_name,
            next_scene_name=request.next_scene_name or "",
            style_preset_id=request.style_preset_id,
            shots=shots,
        )


class GeminiImageGenerator:
    def __init__(self, api_key: str, model: str) -> None:
        self._api_key = api_key
        self._model = model

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

        payload = {
            "contents": [{"parts": parts}],
            "generationConfig": {
                "responseModalities": ["TEXT", "IMAGE"],
                "imageConfig": {
                    "aspectRatio": aspect_ratio,
                    "imageSize": image_size,
                },
            },
        }
        response = httpx.post(
            f"https://generativelanguage.googleapis.com/v1beta/models/{self._model}:generateContent",
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
            image_generator=GeminiImageGenerator(
                api_key=settings.gemini_api_key,
                model=settings.gemini_image_model,
            ),
            speech_generator=ElevenLabsSpeechGenerator(
                api_key=settings.elevenlabs_api_key,
                model_id=settings.elevenlabs_model_id,
            ),
            default_voice_id=settings.elevenlabs_voice_id,
        )

    def create_package(
        self,
        request: GeneratedStoryboardCutsceneRequest,
    ) -> GeneratedStoryboardPackageResult:
        plan = self._planner.plan(request)
        voice_id = request.voice_id or self._default_voice_id
        if not voice_id:
            raise RuntimeError("A voice_id is required to generate narration audio.")

        base_asset_dir = self._output_root / "GeneratedStoryboards" / request.package_id / request.beat_id
        shots = []
        for index, shot in enumerate(plan.shots):
            stem = base_asset_dir / shot.shot_id
            image_asset = self._image_generator.generate_image(
                prompt=self._build_image_prompt(request, shot),
                reference_image_paths=request.reference_image_paths,
                output_path=stem.with_suffix(".png"),
                aspect_ratio=request.aspect_ratio,
                image_size=request.image_size,
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

        unity_package = {
            "PackageId": request.package_id,
            "SchemaVersion": 1,
            "PackageVersion": 1,
            "DisplayName": request.package_display_name,
            "Beats": [
                {
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
            ],
        }

        self._package_output_path.parent.mkdir(parents=True, exist_ok=True)
        self._package_output_path.write_text(
            json.dumps(unity_package, indent=2),
            encoding="utf-8",
        )

        return GeneratedStoryboardPackageResult(
            package_output_path=str(self._package_output_path),
            unity_package=unity_package,
        )

    def _build_image_prompt(
        self,
        request: GeneratedStoryboardCutsceneRequest,
        shot: GeneratedStoryboardPlanShot,
    ) -> str:
        context = request.context
        style_prompt = _build_style_prompt(request.style_preset_id)
        return (
            f"{style_prompt} Story brief: {request.story_brief}. "
            f"Character: {context.character_name}. Crop: {context.crop_name}. "
            f"Gameplay goal: {context.minigame_goal}. "
            f"Frame direction: {shot.image_prompt}. "
            "Keep the framing readable for subtitles and preserve character identity across shots."
        )

    def _to_resource_path(self, asset_path: Path) -> str:
        relative_path = asset_path.relative_to(self._output_root)
        return str(relative_path.with_suffix("")).replace("\\", "/")


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


def _extract_alignment_duration(alignment: dict[str, Any]) -> float:
    end_times = alignment.get("character_end_times_seconds") or []
    if not end_times:
        return 0.0

    return float(end_times[-1])
