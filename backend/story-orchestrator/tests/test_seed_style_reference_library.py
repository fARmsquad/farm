from __future__ import annotations

import json
from pathlib import Path

import pytest
from PIL import Image


@pytest.fixture
def fixture_dir(tmp_path: Path) -> Path:
    src = tmp_path / "fixtures"
    src.mkdir()
    Image.new("RGB", (2000, 1200), (255, 0, 0)).save(src / "panel_a.png")
    Image.new("RGB", (1800, 1100), (0, 255, 0)).save(src / "panel_b.jpg")
    return src


def test_seed_writes_style_role_records_with_downscaled_assets(tmp_path: Path, fixture_dir: Path) -> None:
    from scripts.seed_style_reference_library import run

    output_root = tmp_path / "out"
    run(
        output_root=output_root,
        source_dirs=[fixture_dir],
        style_preset_id="watercolor_intro_v1",
        style_label_prefix="intro",
        max_edge=768,
    )

    manifest_path = output_root / "StoryboardReferenceLibrary" / "references.json"
    assert manifest_path.is_file()
    records = json.loads(manifest_path.read_text(encoding="utf-8"))
    assert len(records) == 2
    for record in records:
        assert record["reference_role"] == "style"
        assert "watercolor_intro_v1" in record["tags"]
        assert "intro_panels" in record["tags"]
        assert not Path(record["stored_path"]).is_absolute(), "stored_path must be relative"
        asset_path = output_root / "StoryboardReferenceLibrary" / record["stored_path"]
        with Image.open(asset_path) as image:
            assert max(image.size) <= 768


def test_seed_skips_non_image_files(tmp_path: Path, fixture_dir: Path) -> None:
    from scripts.seed_style_reference_library import run

    (fixture_dir / "notes.txt").write_text("not an image")
    output_root = tmp_path / "out"
    run(
        output_root=output_root,
        source_dirs=[fixture_dir],
        style_preset_id="watercolor_intro_v1",
        style_label_prefix="intro",
        max_edge=768,
    )
    records = json.loads((output_root / "StoryboardReferenceLibrary" / "references.json").read_text())
    assert len(records) == 2


def test_resolve_stored_path_handles_relative_and_absolute(tmp_path: Path) -> None:
    from app.storyboard_reference_library import StoryboardReferenceLibrary

    lib = StoryboardReferenceLibrary(output_root=tmp_path)

    relative_resolved = lib.resolve_stored_path("assets/foo.png")
    assert relative_resolved == (tmp_path / "StoryboardReferenceLibrary" / "assets" / "foo.png").resolve()

    absolute_input = tmp_path / "elsewhere" / "bar.png"
    absolute_resolved = lib.resolve_stored_path(str(absolute_input))
    assert absolute_resolved == absolute_input
