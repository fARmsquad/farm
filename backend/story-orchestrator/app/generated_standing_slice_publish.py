from __future__ import annotations

import shutil
from pathlib import Path

from .models import GeneratedStandingSliceJobRecord


class StandingSlicePublishApprovalError(RuntimeError):
    pass


class GeneratedStandingSlicePublisher:
    def __init__(self, *, output_root: Path, live_package_path: Path) -> None:
        self._output_root = output_root.resolve()
        self._live_package_path = live_package_path.resolve()

    def publish(self, job: GeneratedStandingSliceJobRecord) -> None:
        if job.approval_status != "approved":
            raise StandingSlicePublishApprovalError("Standing slice job must be approved before publish.")
        if job.result is None or not job.result.package_output_path:
            raise RuntimeError("Standing slice job has no archived package snapshot to publish.")

        archived_package_path = self._require_root_path(Path(job.result.package_output_path))
        self._copy(archived_package_path, self._require_root_path(self._live_package_path))

        for asset in job.assets:
            source_output_path = asset.metadata.get("source_output_path")
            if not source_output_path:
                continue

            self._copy(
                self._require_root_path(Path(asset.output_path)),
                self._require_root_path(Path(str(source_output_path))),
            )

            alignment_path = asset.metadata.get("alignment_path")
            source_alignment_path = asset.metadata.get("source_alignment_path")
            if alignment_path and source_alignment_path:
                self._copy(
                    self._require_root_path(Path(str(alignment_path))),
                    self._require_root_path(Path(str(source_alignment_path))),
                )

    def _copy(self, source_path: Path, destination_path: Path) -> None:
        if not source_path.exists():
            raise RuntimeError(f"Publish source file does not exist: {source_path}")
        destination_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source_path, destination_path)

    def _require_root_path(self, path: Path) -> Path:
        resolved = path.resolve()
        try:
            resolved.relative_to(self._output_root)
        except ValueError as exc:
            raise RuntimeError(f"Publish path is outside storyboard root: {resolved}") from exc
        return resolved
