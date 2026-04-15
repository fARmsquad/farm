import base64
import json
import logging
import mimetypes
from pathlib import Path
from typing import Any

import httpx

from .generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset

_LOGGER = logging.getLogger("uvicorn.error")


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
                    timeout=120,
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
            except Exception as error:
                last_error = error
                _LOGGER.exception(
                    "[GeneratedStoryBackend] gemini image failure model=%s output=%s",
                    model,
                    output_path,
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


def _extract_alignment_duration(alignment: dict[str, Any]) -> float:
    end_times = alignment.get("character_end_times_seconds") or []
    if not end_times:
        return 0.0

    return float(end_times[-1])
