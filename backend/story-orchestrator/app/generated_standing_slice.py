from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from pydantic import BaseModel, Field

from .generated_package_assembly import (
    GeneratedPackageAssemblyCutsceneInput,
    GeneratedPackageAssemblyMinigameInput,
    GeneratedPackageAssemblyRequest,
    GeneratedPackageAssemblyResult,
    GeneratedPackageAssemblyService,
)


class GeneratedStandingSliceAssemblyInput(BaseModel):
    minigame: GeneratedPackageAssemblyMinigameInput
    cutscene: GeneratedPackageAssemblyCutsceneInput

    def to_request(
        self,
        *,
        package_id: str,
        package_display_name: str,
    ) -> GeneratedPackageAssemblyRequest:
        return GeneratedPackageAssemblyRequest(
            package_id=package_id,
            package_display_name=package_display_name,
            minigame=self.minigame,
            cutscene=self.cutscene,
        )


class GeneratedStandingSliceRequest(BaseModel):
    package_id: str = Field(min_length=1)
    package_display_name: str = Field(min_length=1)
    post_chicken_to_farm: GeneratedStandingSliceAssemblyInput
    find_tools_to_pre_farm: GeneratedStandingSliceAssemblyInput


class GeneratedStandingSliceResult(BaseModel):
    is_valid: bool
    package_output_path: str = ""
    unity_package: dict[str, Any] = Field(default_factory=dict)
    post_chicken_to_farm_result: GeneratedPackageAssemblyResult | None = None
    find_tools_to_pre_farm_result: GeneratedPackageAssemblyResult | None = None
    errors: list[str] = Field(default_factory=list)
    restored_original_package: bool = False


@dataclass(frozen=True)
class _PackageSnapshot:
    existed: bool
    content: bytes


class GeneratedStandingSliceService:
    def __init__(
        self,
        *,
        package_assembly_service: GeneratedPackageAssemblyService,
        package_output_path: Path,
    ) -> None:
        self._package_assembly_service = package_assembly_service
        self._package_output_path = package_output_path

    def create_package(
        self,
        request: GeneratedStandingSliceRequest,
    ) -> GeneratedStandingSliceResult:
        snapshot = self._capture_snapshot()
        first_result: GeneratedPackageAssemblyResult | None = None
        second_result: GeneratedPackageAssemblyResult | None = None

        try:
            first_result = self._package_assembly_service.create_package(
                request.post_chicken_to_farm.to_request(
                    package_id=request.package_id,
                    package_display_name=request.package_display_name,
                )
            )
            if not first_result.is_valid:
                return GeneratedStandingSliceResult(
                    is_valid=False,
                    package_output_path=self._current_package_output_path(),
                    unity_package=self._load_current_package(),
                    post_chicken_to_farm_result=first_result,
                    errors=list(first_result.errors),
                )

            second_result = self._package_assembly_service.create_package(
                request.find_tools_to_pre_farm.to_request(
                    package_id=request.package_id,
                    package_display_name=request.package_display_name,
                )
            )
            if not second_result.is_valid:
                restored = self._restore_snapshot(snapshot)
                return GeneratedStandingSliceResult(
                    is_valid=False,
                    package_output_path=self._current_package_output_path(),
                    unity_package=self._load_snapshot_package(snapshot),
                    post_chicken_to_farm_result=first_result,
                    find_tools_to_pre_farm_result=second_result,
                    errors=list(second_result.errors),
                    restored_original_package=restored,
                )

            return GeneratedStandingSliceResult(
                is_valid=True,
                package_output_path=second_result.package_output_path or self._current_package_output_path(),
                unity_package=second_result.unity_package,
                post_chicken_to_farm_result=first_result,
                find_tools_to_pre_farm_result=second_result,
            )
        except Exception as error:
            restored = self._restore_snapshot(snapshot)
            return GeneratedStandingSliceResult(
                is_valid=False,
                package_output_path=self._current_package_output_path(),
                unity_package=self._load_snapshot_package(snapshot),
                post_chicken_to_farm_result=first_result,
                find_tools_to_pre_farm_result=second_result,
                errors=[str(error)],
                restored_original_package=restored,
            )

    def _capture_snapshot(self) -> _PackageSnapshot:
        if not self._package_output_path.exists():
            return _PackageSnapshot(existed=False, content=b"")

        return _PackageSnapshot(existed=True, content=self._package_output_path.read_bytes())

    def _restore_snapshot(self, snapshot: _PackageSnapshot) -> bool:
        if snapshot.existed:
            self._package_output_path.parent.mkdir(parents=True, exist_ok=True)
            self._package_output_path.write_bytes(snapshot.content)
            return True

        if self._package_output_path.exists():
            self._package_output_path.unlink()
            return True

        return False

    def _current_package_output_path(self) -> str:
        if self._package_output_path.exists():
            return str(self._package_output_path)
        return ""

    def _load_current_package(self) -> dict[str, Any]:
        if not self._package_output_path.exists():
            return {}

        try:
            return json.loads(self._package_output_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            return {}

    @staticmethod
    def _load_snapshot_package(snapshot: _PackageSnapshot) -> dict[str, Any]:
        if not snapshot.existed or not snapshot.content:
            return {}

        try:
            return json.loads(snapshot.content.decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError):
            return {}
