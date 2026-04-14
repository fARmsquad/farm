from __future__ import annotations

from .generated_standing_slice_artifacts import GeneratedStandingSliceArtifactArchive
from .generated_standing_slice_publish import GeneratedStandingSlicePublisher
from .generated_standing_slice import GeneratedStandingSliceRequest, GeneratedStandingSliceResult, GeneratedStandingSliceService
from .models import (
    GeneratedStandingSliceJobAssetRecord,
    GeneratedStandingSliceJobRecord,
    GeneratedStandingSliceJobReviewRequest,
    GeneratedStandingSliceJobStepRecord,
)
from .store import GeneratedStandingSliceJobStore


class GeneratedStandingSliceJobService:
    def __init__(
        self,
        *,
        standing_slice_service: GeneratedStandingSliceService,
        job_store: GeneratedStandingSliceJobStore,
        artifact_archive: GeneratedStandingSliceArtifactArchive,
        publisher: GeneratedStandingSlicePublisher,
    ) -> None:
        self._standing_slice_service = standing_slice_service
        self._job_store = job_store
        self._artifact_archive = artifact_archive
        self._publisher = publisher

    def create_job(self, request: GeneratedStandingSliceRequest) -> GeneratedStandingSliceJobRecord:
        created = self._job_store.create_job(request)
        self._job_store.mark_running(created.job_id)
        result = self._standing_slice_service.create_package(request)
        archived_result = self._artifact_archive.archive_result(job_id=created.job_id, result=result)
        finished = self._job_store.finish_job(
            created.job_id,
            status="completed" if archived_result.is_valid else "failed",
            result=archived_result,
            steps=_build_steps(archived_result),
            assets=_build_assets(archived_result),
        )
        if finished is None:
            raise RuntimeError(f"Standing slice job '{created.job_id}' could not be reloaded after finish.")
        return finished

    def get_job(self, job_id: str) -> GeneratedStandingSliceJobRecord | None:
        return self._job_store.get_job(job_id)

    def update_review(
        self,
        job_id: str,
        request: GeneratedStandingSliceJobReviewRequest,
    ) -> GeneratedStandingSliceJobRecord | None:
        return self._job_store.update_review(
            job_id,
            approval_status=request.approval_status,
            review_notes=request.review_notes,
        )

    def publish_job(self, job_id: str) -> GeneratedStandingSliceJobRecord | None:
        record = self._job_store.get_job(job_id)
        if record is None:
            return None
        self._publisher.publish(record)
        return self._job_store.mark_published(job_id)


def _build_steps(result: GeneratedStandingSliceResult) -> list[GeneratedStandingSliceJobStepRecord]:
    post_step = _build_step(
        step_id="post_chicken_to_farm",
        output=result.post_chicken_to_farm_result,
        fallback_status="failed",
    )

    second_fallback_status = "pending"
    if result.post_chicken_to_farm_result is not None and not result.post_chicken_to_farm_result.is_valid:
        second_fallback_status = "skipped"

    find_tools_step = _build_step(
        step_id="find_tools_to_pre_farm",
        output=result.find_tools_to_pre_farm_result,
        fallback_status=second_fallback_status,
    )

    return [post_step, find_tools_step]


def _build_step(
    *,
    step_id: str,
    output,
    fallback_status: str,
) -> GeneratedStandingSliceJobStepRecord:
    if output is None:
        return GeneratedStandingSliceJobStepRecord(
            step_id=step_id,
            status=fallback_status,
            output=None,
            errors=[],
        )

    return GeneratedStandingSliceJobStepRecord(
        step_id=step_id,
        status="completed" if output.is_valid else "failed",
        output=output,
        errors=list(output.errors),
    )


def _build_assets(result: GeneratedStandingSliceResult) -> list[GeneratedStandingSliceJobAssetRecord]:
    assets: list[GeneratedStandingSliceJobAssetRecord] = []
    assets.extend(_step_assets("post_chicken_to_farm", result.post_chicken_to_farm_result))
    assets.extend(_step_assets("find_tools_to_pre_farm", result.find_tools_to_pre_farm_result))
    return assets


def _step_assets(
    step_id: str,
    step_result,
) -> list[GeneratedStandingSliceJobAssetRecord]:
    if step_result is None or step_result.cutscene_result is None:
        return []

    return [
        GeneratedStandingSliceJobAssetRecord(
            step_id=step_id,
            asset_id=asset.asset_id,
            beat_id=asset.beat_id,
            shot_id=asset.shot_id,
            asset_type=asset.asset_type,
            provider_name=asset.provider_name,
            provider_model=asset.provider_model,
            fallback_used=asset.fallback_used,
            mime_type=asset.mime_type,
            output_path=asset.output_path,
            resource_path=asset.resource_path,
            metadata=dict(asset.metadata),
        )
        for asset in step_result.cutscene_result.generated_assets
    ]
