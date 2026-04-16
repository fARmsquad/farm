from __future__ import annotations

from pathlib import Path
from typing import Protocol, runtime_checkable

from .generated_storyboard_models import GeneratedImageAsset


@runtime_checkable
class ImageGenerator(Protocol):
    """Structural protocol satisfied by GeminiImageGenerator and OpenAIImageGenerator
    (and future Phase B implementations such as FluxImageGenerator).

    Implementations perform the I/O of generating an image from ``prompt`` (with
    optional ``reference_image_paths`` for style or continuity transfer) and write
    the resulting bytes to ``output_path``. The returned GeneratedImageAsset
    captures provenance metadata (provider name, model, fallback flag, stored path).
    """

    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset: ...
