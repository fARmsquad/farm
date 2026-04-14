from __future__ import annotations

from .generated_standing_slice import GeneratedStandingSliceRequest, GeneratedStandingSliceResult, GeneratedStandingSliceService
from .models import GeneratedStandingSliceJobRecord, GeneratedStandingSliceJobStepRecord
from .store import GeneratedStandingSliceJobStore


class GeneratedStandingSliceJobService:
    def __init__(
        self,
        *,
        standing_slice_service: GeneratedStandingSliceService,
        job_store: GeneratedStandingSliceJobStore,
    ) -> None:
        self._standing_slice_service = standing_slice_service
        self._job_store = job_store

    def create_job(self, request: GeneratedStandingSliceRequest) -> GeneratedStandingSliceJobRecord:
        created = self._job_store.create_job(request)
        self._job_store.mark_running(created.job_id)
        result = self._standing_slice_service.create_package(request)
        finished = self._job_store.finish_job(
            created.job_id,
            status="completed" if result.is_valid else "failed",
            result=result,
            steps=_build_steps(result),
        )
        if finished is None:
            raise RuntimeError(f"Standing slice job '{created.job_id}' could not be reloaded after finish.")
        return finished

    def get_job(self, job_id: str) -> GeneratedStandingSliceJobRecord | None:
        return self._job_store.get_job(job_id)


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
