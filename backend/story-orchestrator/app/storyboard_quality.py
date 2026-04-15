from __future__ import annotations

import logging
from dataclasses import dataclass, replace
from pathlib import Path

from PIL import Image

from .generated_storyboard_models import GeneratedImageAsset, ImageGenerator

_LOGGER = logging.getLogger("uvicorn.error")


@dataclass(frozen=True)
class StoryboardImageQualityResult:
    accepted: bool
    reason: str = ""


class StoryboardImageQualityGate:
    def evaluate(self, asset: GeneratedImageAsset) -> StoryboardImageQualityResult:
        if asset.fallback_used or asset.provider_name == "placeholder-image":
            return StoryboardImageQualityResult(False, "provider_fallback")

        if not asset.output_path.exists():
            return StoryboardImageQualityResult(False, "missing_output")

        if _has_large_dark_caption_panel(asset.output_path):
            return StoryboardImageQualityResult(False, "caption_panel_detected")

        return StoryboardImageQualityResult(True)


class QualityGatedImageGenerator:
    def __init__(
        self,
        generator: ImageGenerator,
        *,
        quality_gate: StoryboardImageQualityGate | None = None,
        max_attempts: int = 2,
    ) -> None:
        self._generator = generator
        self._quality_gate = quality_gate or StoryboardImageQualityGate()
        self._max_attempts = max(1, max_attempts)

    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        last_reason = "quality_rejected"
        for attempt in range(1, self._max_attempts + 1):
            asset = self._generator.generate_image(
                prompt=prompt,
                reference_image_paths=reference_image_paths,
                output_path=output_path,
                aspect_ratio=aspect_ratio,
                image_size=image_size,
            )
            result = self._quality_gate.evaluate(asset)
            metadata = dict(asset.source_metadata)
            metadata["quality_gate"] = {
                "accepted": result.accepted,
                "reason": result.reason,
                "attempt": attempt,
                "max_attempts": self._max_attempts,
            }
            asset = replace(asset, source_metadata=metadata)
            if result.accepted:
                return asset

            last_reason = result.reason or last_reason
            _LOGGER.warning(
                "[GeneratedStoryBackend] storyboard image rejected attempt=%s/%s reason=%s provider=%s output=%s",
                attempt,
                self._max_attempts,
                last_reason,
                asset.provider_name,
                asset.output_path,
            )
            _delete_generated_file(asset.output_path)

        raise RuntimeError(f"Generated image rejected by quality gate: {last_reason}")


def _delete_generated_file(path: Path) -> None:
    meta_path = Path(f"{path}.meta")
    if path.exists():
        path.unlink()
    if meta_path.exists():
        meta_path.unlink()


def _has_large_dark_caption_panel(path: Path) -> bool:
    with Image.open(path) as image:
        rgb_image = image.convert("RGB")

    width, height = rgb_image.size
    if width < 200 or height < 120:
        return False

    x0 = int(width * 0.08)
    x1 = int(width * 0.92)
    panel_y0 = int(height * 0.69)
    panel_y1 = int(height * 0.96)
    upper_y0 = int(height * 0.2)
    upper_y1 = int(height * 0.48)

    panel_luminance = _crop_mean_luminance(rgb_image, x0, panel_y0, x1, panel_y1)
    upper_luminance = _crop_mean_luminance(rgb_image, x0, upper_y0, x1, upper_y1)
    dark_ratio = _crop_dark_ratio(rgb_image, x0, panel_y0, x1, panel_y1)

    return dark_ratio >= 0.32 and panel_luminance <= 52 and (upper_luminance - panel_luminance) >= 18


def _crop_mean_luminance(image: Image.Image, x0: int, y0: int, x1: int, y1: int) -> float:
    crop = image.crop((x0, y0, x1, y1))
    pixels = list(crop.getdata())
    if not pixels:
        return 255.0

    return sum(_luminance(pixel) for pixel in pixels) / len(pixels)


def _crop_dark_ratio(image: Image.Image, x0: int, y0: int, x1: int, y1: int) -> float:
    crop = image.crop((x0, y0, x1, y1))
    pixels = list(crop.getdata())
    if not pixels:
        return 0.0

    dark_pixels = sum(1 for pixel in pixels if _luminance(pixel) <= 38)
    return dark_pixels / len(pixels)


def _luminance(pixel: tuple[int, int, int]) -> float:
    red, green, blue = pixel
    return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue)
