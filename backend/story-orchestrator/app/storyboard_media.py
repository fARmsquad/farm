from __future__ import annotations

import json
import textwrap
import wave
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


class ChainImageGenerator:
    def __init__(self, generators: list[Any]) -> None:
        self._generators = generators

    def generate_image(self, **kwargs: Any) -> Any:
        last_error: Exception | None = None
        for generator in self._generators:
            try:
                return generator.generate_image(**kwargs)
            except Exception as error:  # pragma: no cover - fallback path is integration-led
                last_error = error

        if last_error is not None:
            raise last_error

        raise RuntimeError("No image generator is configured.")


class PlaceholderImageGenerator:
    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> Any:
        width, height = _resolve_canvas_size(aspect_ratio)
        image = Image.new("RGB", (width, height), "#203328")
        draw = ImageDraw.Draw(image)

        for y in range(height):
            color = _lerp_color((35, 53, 46), (214, 173, 96), y / max(height - 1, 1))
            draw.line([(0, y), (width, y)], fill=color)

        draw.ellipse((width - 280, 60, width - 120, 220), fill=(246, 208, 122))
        draw.rectangle((0, int(height * 0.58), width, height), fill=(63, 111, 62))
        draw.polygon(
            [(0, height), (int(width * 0.3), int(height * 0.62)), (int(width * 0.55), height)],
            fill=(92, 140, 78),
        )
        draw.polygon(
            [(int(width * 0.35), height), (int(width * 0.7), int(height * 0.66)), (width, height)],
            fill=(124, 160, 82),
        )

        caption_box = (40, height - 210, width - 40, height - 40)
        draw.rounded_rectangle(caption_box, radius=24, fill=(18, 28, 20))
        font = ImageFont.load_default()
        caption = textwrap.fill(_extract_caption(prompt), width=max(28, width // 28))
        draw.multiline_text(
            (caption_box[0] + 28, caption_box[1] + 28),
            caption,
            fill=(245, 241, 228),
            font=font,
            spacing=10,
        )

        output_path.parent.mkdir(parents=True, exist_ok=True)
        image.save(output_path, format="PNG")

        from .generated_storyboards import GeneratedImageAsset

        return GeneratedImageAsset(output_path=output_path, mime_type="image/png")


class ChainSpeechGenerator:
    def __init__(self, generators: list[Any]) -> None:
        self._generators = generators

    def generate_speech(self, **kwargs: Any) -> Any:
        last_error: Exception | None = None
        for generator in self._generators:
            try:
                return generator.generate_speech(**kwargs)
            except Exception as error:  # pragma: no cover - fallback path is integration-led
                last_error = error

        if last_error is not None:
            raise last_error

        raise RuntimeError("No speech generator is configured.")


class PlaceholderSpeechGenerator:
    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> Any:
        duration_seconds = max(2.5, len(text.split()) / 2.4)
        wav_path = output_path.with_suffix(".wav")
        _remove_stale_audio_variants(output_path)
        wav_path.parent.mkdir(parents=True, exist_ok=True)
        _write_silence_wav(wav_path, duration_seconds)

        alignment = _build_placeholder_alignment(text, duration_seconds)
        alignment_path = wav_path.with_suffix(".alignment.json")
        alignment_path.write_text(json.dumps(alignment, indent=2), encoding="utf-8")

        from .generated_storyboards import GeneratedSpeechAsset

        return GeneratedSpeechAsset(
            output_path=wav_path,
            alignment_path=alignment_path,
            duration_seconds=duration_seconds,
            mime_type="audio/wav",
        )


def _resolve_canvas_size(aspect_ratio: str) -> tuple[int, int]:
    try:
        width_str, height_str = aspect_ratio.split(":", maxsplit=1)
        width_ratio = int(width_str)
        height_ratio = int(height_str)
        base_width = 1280
        resolved_height = max(1, int(base_width * height_ratio / width_ratio))
        return base_width, resolved_height
    except Exception:
        return 1280, 720


def _extract_caption(prompt: str) -> str:
    marker = "Frame direction:"
    if marker in prompt:
        return prompt.split(marker, maxsplit=1)[1].strip()

    return prompt.strip()[:220]


def _lerp_color(start: tuple[int, int, int], end: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return (
        int(start[0] + (end[0] - start[0]) * t),
        int(start[1] + (end[1] - start[1]) * t),
        int(start[2] + (end[2] - start[2]) * t),
    )


def _write_silence_wav(path: Path, duration_seconds: float) -> None:
    sample_rate = 24000
    frame_count = int(sample_rate * duration_seconds)
    with wave.open(str(path), "wb") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(sample_rate)
        wav_file.writeframes(b"\x00\x00" * frame_count)


def _build_placeholder_alignment(text: str, duration_seconds: float) -> dict[str, Any]:
    characters = list(text)
    if not characters:
        return {
            "characters": [],
            "character_start_times_seconds": [],
            "character_end_times_seconds": [],
        }

    character_duration = duration_seconds / len(characters)
    start_times = [round(index * character_duration, 4) for index in range(len(characters))]
    end_times = [round((index + 1) * character_duration, 4) for index in range(len(characters))]
    return {
        "characters": characters,
        "character_start_times_seconds": start_times,
        "character_end_times_seconds": end_times,
    }


def _remove_stale_audio_variants(output_path: Path) -> None:
    stem = output_path.with_suffix("")
    for suffix in (".mp3", ".wav", ".ogg", ".m4a", ".aac"):
        candidate = stem.with_suffix(suffix)
        candidate_meta = Path(f"{candidate}.meta")
        if candidate.exists():
            candidate.unlink()
        if candidate_meta.exists():
            candidate_meta.unlink()
