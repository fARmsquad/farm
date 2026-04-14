from __future__ import annotations

import json
import shutil
from pathlib import Path

from .generated_package_assembly import GeneratedPackageAssemblyResult
from .generated_standing_slice import GeneratedStandingSliceResult
from .generated_storyboard_models import GeneratedStoryboardAssetRecord, GeneratedStoryboardPackageResult


class GeneratedStandingSliceArtifactArchive:
    def __init__(self, *, output_root: Path) -> None:
        self._output_root = output_root
        self._archive_root = output_root / "GeneratedStoryboards" / "_job_runs"

    def archive_result(
        self,
        *,
        job_id: str,
        result: GeneratedStandingSliceResult,
    ) -> GeneratedStandingSliceResult:
        archived_package_path = self._write_package_snapshot(
            job_id=job_id,
            source_package_path=result.package_output_path,
            unity_package=result.unity_package,
        )
        return result.model_copy(
            update={
                "package_output_path": archived_package_path,
                "post_chicken_to_farm_result": self._archive_step_result(
                    job_id=job_id,
                    step_id="post_chicken_to_farm",
                    archived_package_path=archived_package_path,
                    result=result.post_chicken_to_farm_result,
                ),
                "find_tools_to_pre_farm_result": self._archive_step_result(
                    job_id=job_id,
                    step_id="find_tools_to_pre_farm",
                    archived_package_path=archived_package_path,
                    result=result.find_tools_to_pre_farm_result,
                ),
            }
        )

    def _archive_step_result(
        self,
        *,
        job_id: str,
        step_id: str,
        archived_package_path: str,
        result: GeneratedPackageAssemblyResult | None,
    ) -> GeneratedPackageAssemblyResult | None:
        if result is None:
            return None

        return result.model_copy(
            update={
                "package_output_path": archived_package_path,
                "minigame_result": self._archive_minigame_result(
                    archived_package_path=archived_package_path,
                    result=result.minigame_result,
                ),
                "cutscene_result": self._archive_cutscene_result(
                    job_id=job_id,
                    step_id=step_id,
                    archived_package_path=archived_package_path,
                    result=result.cutscene_result,
                ),
            }
        )

    @staticmethod
    def _archive_minigame_result(
        *,
        archived_package_path: str,
        result,
    ):
        if result is None:
            return None

        return result.model_copy(update={"package_output_path": archived_package_path})

    def _archive_cutscene_result(
        self,
        *,
        job_id: str,
        step_id: str,
        archived_package_path: str,
        result: GeneratedStoryboardPackageResult | None,
    ) -> GeneratedStoryboardPackageResult | None:
        if result is None:
            return None

        archived_assets = [
            self._archive_asset(
                job_id=job_id,
                step_id=step_id,
                asset=asset,
            )
            for asset in result.generated_assets
        ]
        return result.model_copy(
            update={
                "package_output_path": archived_package_path,
                "generated_assets": archived_assets,
            }
        )

    def _archive_asset(
        self,
        *,
        job_id: str,
        step_id: str,
        asset: GeneratedStoryboardAssetRecord,
    ) -> GeneratedStoryboardAssetRecord:
        metadata = dict(asset.metadata)
        metadata.setdefault("source_output_path", asset.output_path)
        metadata.setdefault("source_resource_path", asset.resource_path)

        source_path = Path(asset.output_path)
        destination = self._archive_root / job_id / "assets" / step_id / asset.beat_id / f"{asset.asset_id}{source_path.suffix}"
        archived_output_path = self._copy_file(source_path, destination)
        if not archived_output_path:
            metadata["archive_error"] = f"Missing source artifact: {source_path}"
            return asset.model_copy(update={"metadata": metadata})

        if "alignment_path" in metadata and metadata["alignment_path"]:
            source_alignment_path = Path(str(metadata["alignment_path"]))
            alignment_destination = destination.with_name(f"{destination.stem}.alignment.json")
            archived_alignment_path = self._copy_file(source_alignment_path, alignment_destination)
            if archived_alignment_path:
                metadata["source_alignment_path"] = str(source_alignment_path)
                metadata["alignment_path"] = archived_alignment_path

        archived_path = Path(archived_output_path)
        return asset.model_copy(
            update={
                "output_path": archived_output_path,
                "resource_path": self._to_resource_path(archived_path),
                "metadata": metadata,
            }
        )

    def _write_package_snapshot(
        self,
        *,
        job_id: str,
        source_package_path: str,
        unity_package: dict,
    ) -> str:
        if not unity_package:
            return ""

        source_name = Path(source_package_path).name if source_package_path else "story_package_snapshot.json"
        destination = self._archive_root / job_id / "package" / source_name
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(json.dumps(unity_package, indent=2), encoding="utf-8")
        return str(destination)

    def _to_resource_path(self, asset_path: Path) -> str:
        relative_path = asset_path.relative_to(self._output_root)
        return str(relative_path.with_suffix("")).replace("\\", "/")

    @staticmethod
    def _copy_file(source_path: Path, destination: Path) -> str:
        if not source_path.exists():
            return ""

        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source_path, destination)
        return str(destination)
