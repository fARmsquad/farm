from __future__ import annotations

import argparse
import io
import sys
from pathlib import Path

from PIL import Image

from app.storyboard_reference_library import StoryboardReferenceLibrary


_IMAGE_SUFFIXES = {".png", ".jpg", ".jpeg"}


def _downscale_to_png_bytes(source_path: Path, max_edge: int) -> bytes:
    with Image.open(source_path) as image:
        mode = "RGBA" if image.mode == "RGBA" else "RGB"
        image = image.convert(mode)
        width, height = image.size
        longest = max(width, height)
        if longest > max_edge:
            scale = max_edge / longest
            image = image.resize((int(width * scale), int(height * scale)), Image.Resampling.LANCZOS)
        buffer = io.BytesIO()
        image.save(buffer, format="PNG")
        return buffer.getvalue()


def run(
    *,
    output_root: Path,
    source_dirs: list[Path],
    style_preset_id: str,
    style_label_prefix: str,
    max_edge: int = 768,
) -> int:
    library = StoryboardReferenceLibrary(output_root=output_root)
    imported = 0
    for source_dir in source_dirs:
        if not source_dir.is_dir():
            continue
        for source_path in sorted(source_dir.iterdir()):
            if source_path.suffix.lower() not in _IMAGE_SUFFIXES:
                continue
            content = _downscale_to_png_bytes(source_path, max_edge)
            label = f"{style_label_prefix}_{source_path.stem}"
            tags = ["intro_panels", style_preset_id, source_dir.name]
            library.import_reference(
                filename=f"{source_path.stem}.png",
                content=content,
                reference_role="style",
                label=label,
                tags=tags,
                mime_type="image/png",
            )
            imported += 1
    return imported


def _main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Seed StoryboardReferenceLibrary with style anchors")
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--source-dir", type=Path, action="append", required=True, dest="source_dirs")
    parser.add_argument("--style-preset-id", required=True)
    parser.add_argument("--style-label-prefix", default="intro")
    parser.add_argument("--max-edge", type=int, default=768)
    args = parser.parse_args(argv)
    count = run(
        output_root=args.output_root,
        source_dirs=args.source_dirs,
        style_preset_id=args.style_preset_id,
        style_label_prefix=args.style_label_prefix,
        max_edge=args.max_edge,
    )
    print(f"Imported {count} style references into {args.output_root}/StoryboardReferenceLibrary")
    return 0


if __name__ == "__main__":
    sys.exit(_main())
