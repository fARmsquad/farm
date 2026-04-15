import base64
import json
import logging
import mimetypes
from pathlib import Path
from typing import Any

import httpx

from .generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset

_LOGGER = logging.getLogger("uvicorn.error")
REQUEST_TIMEOUT_SECONDS = 30


def _describe_error(error: Exception) -> str:
    message = str(error).strip()
    return message or type(error).__name__


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
                _LOGGER.info(
                    "[GeneratedStoryBackend] gemini image request model=%s refs=%s output=%s",
                    model,
                    len(reference_image_paths),
                    output_path,
                )
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
                    timeout=REQUEST_TIMEOUT_SECONDS,
                )
                response.raise_for_status()

                data = response.json()
                inline_data = self._extract_inline_image(data)
                output_path.parent.mkdir(parents=True, exist_ok=True)
                output_path.write_bytes(base64.b64decode(inline_data["data"]))
                _LOGGER.info(
                    "[GeneratedStoryBackend] gemini image success model=%s output=%s",
                    model,
                    output_path,
                )
                return GeneratedImageAsset(
                    output_path=output_path,
                    mime_type=inline_data.get("mimeType", "image/png"),
                    provider_name="gemini-image",
                    provider_model=model,
                    fallback_used=False,
                    source_metadata={
                        "prompt": prompt,
                        "reference_image_paths": list(reference_image_paths),
                        "aspect_ratio": aspect_ratio,
                        "image_size": image_size,
                    },
                )
            except httpx.HTTPStatusError as error:
                last_error = error
                _LOGGER.warning(
                    "[GeneratedStoryBackend] gemini image http_error model=%s status=%s output=%s",
                    model,
                    error.response.status_code,
                    output_path,
                )
                if error.response.status_code not in (403, 404, 429):
                    raise
            except httpx.TimeoutException as error:
                last_error = error
                _LOGGER.warning(
                    "[GeneratedStoryBackend] gemini image timeout model=%s output=%s detail=%s",
                    model,
                    output_path,
                    _describe_error(error),
                )
            except Exception as error:
                last_error = error
                _LOGGER.warning(
                    "[GeneratedStoryBackend] gemini image failure model=%s output=%s detail=%s",
                    model,
                    output_path,
                    _describe_error(error),
                )
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


class OpenAIImageGenerator:
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
        if not self._api_key:
            raise RuntimeError("OpenAI API key is not configured for image generation.")
        if not self._model:
            raise RuntimeError("OpenAI image model is not configured.")

        resolved_size = _resolve_openai_image_size(aspect_ratio, image_size)
        _LOGGER.info(
            "[GeneratedStoryBackend] openai image request model=%s refs=%s output=%s",
            self._model,
            len(reference_image_paths),
            output_path,
        )
        try:
            response = httpx.post(
                "https://api.openai.com/v1/images/generations",
                headers={
                    "Authorization": f"Bearer {self._api_key}",
                    "Content-Type": "application/json",
                },
                json={
                    "model": self._model,
                    "prompt": prompt,
                    "size": resolved_size,
                },
                timeout=REQUEST_TIMEOUT_SECONDS,
            )
            response.raise_for_status()
            body = response.json()
            image_base64 = _extract_openai_generated_image(body)
            output_path.parent.mkdir(parents=True, exist_ok=True)
            output_path.write_bytes(base64.b64decode(image_base64))
            _LOGGER.info(
                "[GeneratedStoryBackend] openai image success model=%s output=%s",
                self._model,
                output_path,
            )
            return GeneratedImageAsset(
                output_path=output_path,
                mime_type="image/png",
                provider_name="openai-image",
                provider_model=self._model,
                fallback_used=False,
                source_metadata={
                    "prompt": prompt,
                    "reference_image_paths": list(reference_image_paths),
                    "aspect_ratio": aspect_ratio,
                    "image_size": resolved_size,
                },
            )
        except httpx.TimeoutException as error:
            _LOGGER.warning(
                "[GeneratedStoryBackend] openai image timeout model=%s output=%s detail=%s",
                self._model,
                output_path,
                _describe_error(error),
            )
            raise


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

        _LOGGER.info(
            "[GeneratedStoryBackend] elevenlabs narration request voice_id=%s model=%s output=%s",
            voice_id,
            self._model_id,
            output_path,
        )
        response = httpx.post(
            f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}/with-timestamps",
            headers={
                "Content-Type": "application/json",
                "xi-api-key": self._api_key,
            },
            json=payload,
            timeout=REQUEST_TIMEOUT_SECONDS,
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
        _LOGGER.info(
            "[GeneratedStoryBackend] elevenlabs narration success voice_id=%s output=%s duration=%.2fs",
            voice_id,
            output_path,
            duration_seconds,
        )
        return GeneratedSpeechAsset(
            output_path=output_path,
            alignment_path=alignment_path,
            duration_seconds=duration_seconds,
            mime_type="audio/mpeg",
            provider_name="elevenlabs-speech",
            provider_model=self._model_id,
            fallback_used=False,
            source_metadata={
                "text": text,
                "voice_id": voice_id,
                "previous_text": previous_text,
                "next_text": next_text,
            },
        )


class OpenAISpeechGenerator:
    def __init__(self, api_key: str, model_id: str, voice: str) -> None:
        self._api_key = api_key
        self._model_id = model_id
        self._voice = voice

    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> GeneratedSpeechAsset:
        if not self._api_key:
            raise RuntimeError("OpenAI API key is not configured for speech generation.")
        if not self._model_id:
            raise RuntimeError("OpenAI speech model is not configured.")
        if not self._voice:
            raise RuntimeError("OpenAI speech voice is not configured.")

        _LOGGER.info(
            "[GeneratedStoryBackend] openai speech request voice=%s model=%s output=%s",
            self._voice,
            self._model_id,
            output_path,
        )
        response = httpx.post(
            "https://api.openai.com/v1/audio/speech",
            headers={
                "Authorization": f"Bearer {self._api_key}",
                "Content-Type": "application/json",
            },
            json={
                "model": self._model_id,
                "voice": self._voice,
                "input": text,
                "format": "mp3",
            },
            timeout=120,
        )
        response.raise_for_status()
        if not response.content:
            raise RuntimeError("OpenAI did not return audio data.")

        output_path.parent.mkdir(parents=True, exist_ok=True)
        _remove_stale_audio_variants(output_path)
        output_path.write_bytes(response.content)
        alignment = _build_alignment_from_text(text)
        alignment_path = output_path.with_suffix(".alignment.json")
        alignment_path.write_text(json.dumps(alignment, indent=2), encoding="utf-8")
        duration_seconds = _estimate_audio_duration_seconds(text)
        _LOGGER.info(
            "[GeneratedStoryBackend] openai speech success voice=%s output=%s duration=%.2fs",
            self._voice,
            output_path,
            duration_seconds,
        )
        return GeneratedSpeechAsset(
            output_path=output_path,
            alignment_path=alignment_path,
            duration_seconds=duration_seconds,
            mime_type="audio/mpeg",
            provider_name="openai-speech",
            provider_model=self._model_id,
            fallback_used=False,
            source_metadata={
                "text": text,
                "requested_voice_id": voice_id,
                "voice": self._voice,
                "previous_text": previous_text,
                "next_text": next_text,
            },
        )


def _extract_alignment_duration(alignment: dict[str, Any]) -> float:
    end_times = alignment.get("character_end_times_seconds") or []
    if not end_times:
        return 0.0

    return float(end_times[-1])


def _extract_openai_generated_image(body: dict[str, Any]) -> str:
    for item in body.get("data", []):
        image_base64 = item.get("b64_json")
        if isinstance(image_base64, str) and image_base64.strip():
            return image_base64

    raise RuntimeError("OpenAI did not return an image payload.")


def _resolve_openai_image_size(aspect_ratio: str, image_size: str) -> str:
    if image_size in {"1024x1024", "1536x1024", "1024x1536"}:
        return image_size

    normalized_ratio = (aspect_ratio or "").strip()
    if normalized_ratio == "9:16":
        return "1024x1536"
    if normalized_ratio == "16:9":
        return "1536x1024"
    return "1024x1024"


def _build_alignment_from_text(text: str) -> dict[str, Any]:
    characters = list(text)
    if not characters:
        return {
            "characters": [],
            "character_start_times_seconds": [],
            "character_end_times_seconds": [],
        }

    duration_seconds = _estimate_audio_duration_seconds(text)
    character_duration = duration_seconds / len(characters)
    start_times = [round(index * character_duration, 4) for index in range(len(characters))]
    end_times = [round((index + 1) * character_duration, 4) for index in range(len(characters))]
    return {
        "characters": characters,
        "character_start_times_seconds": start_times,
        "character_end_times_seconds": end_times,
    }


def _estimate_audio_duration_seconds(text: str) -> float:
    word_count = max(1, len(text.split()))
    return max(2.5, word_count / 2.8)


def _remove_stale_audio_variants(output_path: Path) -> None:
    stem = output_path.with_suffix("")
    for suffix in (".mp3", ".wav", ".ogg", ".m4a", ".aac"):
        candidate = stem.with_suffix(suffix)
        candidate_meta = Path(f"{candidate}.meta")
        if candidate.exists():
            candidate.unlink()
        if candidate_meta.exists():
            candidate_meta.unlink()
