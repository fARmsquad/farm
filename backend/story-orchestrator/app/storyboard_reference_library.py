from __future__ import annotations

import json
from pathlib import Path
from typing import TYPE_CHECKING

from .storyboard_reference_models import StoryboardReferenceAssetRecord, StoryboardReferenceRole

if TYPE_CHECKING:
    from .generated_storyboard_models import GeneratedStoryboardCutsceneRequest


class StoryboardReferenceLibrary:
    def __init__(self, output_root: Path) -> None:
        self._library_root = output_root / "StoryboardReferenceLibrary"
        self._assets_root = self._library_root / "assets"
        self._manifest_path = self._library_root / "references.json"
        self._assets_root.mkdir(parents=True, exist_ok=True)

    @property
    def asset_root(self) -> Path:
        return self._assets_root.resolve()

    def import_reference(
        self,
        *,
        filename: str,
        content: bytes,
        reference_role: StoryboardReferenceRole,
        label: str,
        character_name: str = "",
        tags: list[str] | None = None,
        mime_type: str = "image/png",
    ) -> StoryboardReferenceAssetRecord:
        cleaned_tags = [tag for tag in (tags or []) if tag]
        suffix = Path(filename).suffix or ".png"
        placeholder = StoryboardReferenceAssetRecord.create(
            reference_role=reference_role,
            label=label or Path(filename).stem,
            character_name=character_name.strip(),
            tags=cleaned_tags,
            source_filename=filename,
            stored_path="",
            mime_type=mime_type,
        )
        stored_path = self._assets_root / f"{placeholder.reference_id}{suffix}"
        stored_path.write_bytes(content)
        relative_stored_path = f"assets/{placeholder.reference_id}{suffix}"
        record = placeholder.model_copy(update={"stored_path": relative_stored_path})
        records = self._read_manifest()
        records.append(record)
        self._write_manifest(records)
        return record

    def resolve_stored_path(self, stored_path: str) -> Path:
        candidate = Path(stored_path)
        if candidate.is_absolute():
            return candidate
        return (self._library_root / candidate).resolve()

    def list_references(
        self,
        *,
        character_name: str = "",
        reference_role: StoryboardReferenceRole | None = None,
    ) -> list[StoryboardReferenceAssetRecord]:
        records = self._read_manifest()
        filtered = []
        for record in records:
            if character_name and record.character_name != character_name:
                continue
            if reference_role and record.reference_role != reference_role:
                continue
            filtered.append(record)
        return filtered

    def get_reference(self, reference_id: str) -> StoryboardReferenceAssetRecord | None:
        for record in self._read_manifest():
            if record.reference_id == reference_id:
                return record
        return None

    def _read_manifest(self) -> list[StoryboardReferenceAssetRecord]:
        if not self._manifest_path.exists():
            return []
        payload = json.loads(self._manifest_path.read_text(encoding="utf-8"))
        return [StoryboardReferenceAssetRecord.model_validate(item) for item in payload]

    def _write_manifest(self, records: list[StoryboardReferenceAssetRecord]) -> None:
        self._library_root.mkdir(parents=True, exist_ok=True)
        self._manifest_path.write_text(
            json.dumps([record.model_dump(mode="json") for record in records], indent=2),
            encoding="utf-8",
        )


class StoryboardReferencePathResolver:
    def __init__(self, output_root: Path, reference_library: StoryboardReferenceLibrary | None = None) -> None:
        self._output_root = output_root
        self._reference_library = reference_library

    def resolve_paths(self, request: "GeneratedStoryboardCutsceneRequest") -> list[str]:
        resolved: list[str] = []
        explicit_paths = self._normalize_paths(request.reference_image_paths)
        character_paths = self._collect_character_reference_paths(request.context.character_name)

        if request.continuity_reference_mode == "character_priority":
            self._append_paths(resolved, character_paths)
            continuity_paths = explicit_paths[:1] if character_paths else explicit_paths
            self._append_paths(resolved, continuity_paths)
            if not explicit_paths and not character_paths:
                self._append_recent_generated_images(resolved, request.package_id)
            return resolved[: request.max_reference_images]

        self._append_paths(resolved, explicit_paths)
        explicit_reference_count = len(resolved)
        if request.continuity_reference_mode != "explicit_only":
            self._append_paths(resolved, character_paths)
            if explicit_reference_count == 0:
                self._append_recent_generated_images(resolved, request.package_id)
        return resolved[: request.max_reference_images]

    def _collect_character_reference_paths(self, character_name: str) -> list[str]:
        if self._reference_library is None or not character_name:
            return []
        records = self._reference_library.list_references(character_name=character_name, reference_role="character")
        return [str(self._reference_library.resolve_stored_path(record.stored_path)) for record in reversed(records)]

    def _append_recent_generated_images(self, resolved: list[str], package_id: str) -> None:
        package_root = self._output_root / "GeneratedStoryboards" / package_id
        if not package_root.exists():
            return
        images = sorted(package_root.glob("*/*.png"), key=lambda path: path.stat().st_mtime, reverse=True)
        self._append_paths(resolved, [str(path.resolve()) for path in images])

    @staticmethod
    def _normalize_paths(candidate_paths: list[str]) -> list[str]:
        resolved: list[str] = []
        for path in candidate_paths:
            if not path:
                continue
            resolved_path = str(Path(path).resolve())
            if not Path(resolved_path).exists():
                continue
            if resolved_path in resolved:
                continue
            resolved.append(resolved_path)
        return resolved

    @staticmethod
    def _append_paths(resolved: list[str], candidate_paths: list[str]) -> None:
        for path in candidate_paths:
            if path in resolved:
                continue
            resolved.append(path)
