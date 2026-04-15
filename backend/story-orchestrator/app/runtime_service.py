from __future__ import annotations

from concurrent.futures import ThreadPoolExecutor
from datetime import UTC, datetime
from threading import Lock

from .runtime_models import (
    PlayableTurnEnvelope,
    RuntimeJobStepRecord,
    RuntimeSession,
    RuntimeSessionCreateRequest,
    RuntimeSessionCreateResponse,
    RuntimeSessionDetail,
    RuntimeTrackerJobDetail,
    RuntimeTrackerSessionDetail,
    RuntimeTrackerSessionSummary,
    RuntimeTrackerTurnDetail,
    RuntimeTurnJob,
    RuntimeTurnRecord,
    TurnOutcomeRequest,
    TurnOutcomeResponse,
)
from .runtime_store import RuntimeSessionStore
from .runtime_turn_generation import RuntimeGeneratedTurn, RuntimeTurnGenerationService


NON_TERMINAL_JOB_STATUSES = (
    "queued",
    "planning",
    "generating_images",
    "generating_audio",
    "assembling_contract",
    "validating",
)

RESUMABLE_JOB_STATUSES = tuple(
    status for status in NON_TERMINAL_JOB_STATUSES if status != "queued"
)
PIPELINE_STEPS = (
    "planning",
    "generating_images",
    "generating_audio",
    "assembling_contract",
    "validating",
)


class RuntimeSessionService:
    def __init__(
        self,
        *,
        store: RuntimeSessionStore,
        generation_service: RuntimeTurnGenerationService,
        max_workers: int = 2,
    ) -> None:
        self._store = store
        self._generation_service = generation_service
        self._live_executor = ThreadPoolExecutor(max_workers=max_workers, thread_name_prefix="runtime-turn-live")
        self._recovery_executor = ThreadPoolExecutor(max_workers=1, thread_name_prefix="runtime-turn-recovery")
        self._scheduled_job_ids: set[str] = set()
        self._scheduled_job_ids_lock = Lock()

    def create_session(self, request: RuntimeSessionCreateRequest) -> RuntimeSessionCreateResponse:
        session = self._store.create_session(RuntimeSession.create_active(request))
        job = self._create_next_job(session)
        return RuntimeSessionCreateResponse(
            session_id=session.session_id,
            job_id=job.job_id,
            status=job.status,
        )

    def get_session_detail(self, session_id: str) -> RuntimeSessionDetail | None:
        detail = self._store.get_session_detail(session_id)
        if detail is not None and detail.active_job is not None:
            self._maybe_schedule_job(detail.active_job)
        return detail

    def get_job(self, job_id: str) -> RuntimeTurnJob | None:
        job = self._store.get_job(job_id)
        if job is not None:
            self._maybe_schedule_job(job)
        return job

    def get_turn_envelope(self, session_id: str, turn_id: str) -> PlayableTurnEnvelope | None:
        turn = self._store.get_turn(session_id, turn_id)
        return turn.envelope if turn is not None else None

    def list_tracker_sessions(self, limit: int = 12) -> list[RuntimeTrackerSessionSummary]:
        return [self._build_tracker_summary(session) for session in self._store.list_sessions(limit)]

    def get_tracker_session(self, session_id: str) -> RuntimeTrackerSessionDetail | None:
        session = self._store.get_session(session_id)
        if session is None:
            return None
        return self._build_tracker_detail(session)

    def submit_outcome(
        self,
        session_id: str,
        turn_id: str,
        request: TurnOutcomeRequest,
    ) -> TurnOutcomeResponse | None:
        session = self._store.get_session(session_id)
        if session is None:
            return None

        turn = self._store.get_turn(session_id, turn_id)
        if turn is None:
            return None

        self._store.record_outcome(session_id, turn_id, request)
        updated_session = self._store.get_session(session_id)
        if updated_session is None:
            return None

        if updated_session.state.beat_cursor >= updated_session.state.max_turns:
            completed_session = updated_session.model_copy(
                update={
                    "status": "completed",
                    "active_job_id": "",
                    "updated_at": datetime.now(UTC).isoformat(),
                }
            )
            self._store.update_session(completed_session)
            return TurnOutcomeResponse(next_job_id="", session_state=completed_session)

        next_job = self._create_next_job(updated_session)
        updated_session = self._store.get_session(session_id)
        if updated_session is None:
            return None

        return TurnOutcomeResponse(next_job_id=next_job.job_id, session_state=updated_session)

    def resume_incomplete_jobs(self) -> None:
        for job in self._store.list_jobs_by_status(RESUMABLE_JOB_STATUSES):
            self._schedule_job(job.job_id, prefer_recovery=True)

    def shutdown(self) -> None:
        self._live_executor.shutdown(wait=True, cancel_futures=False)
        self._recovery_executor.shutdown(wait=True, cancel_futures=False)

    def _create_next_job(self, session: RuntimeSession) -> RuntimeTurnJob:
        if session.state.beat_cursor >= session.state.max_turns:
            raise RuntimeError("Runtime session has already reached its turn limit.")
        job = RuntimeTurnJob.create_queued(session.session_id, session.state.beat_cursor)
        self._store.create_job(job)
        session = session.model_copy(
            update={
                "active_job_id": job.job_id,
                "updated_at": datetime.now(UTC).isoformat(),
            }
        )
        self._store.update_session(session)
        self._schedule_job(job.job_id)
        return job

    def _schedule_job(self, job_id: str, *, prefer_recovery: bool = False) -> None:
        with self._scheduled_job_ids_lock:
            if job_id in self._scheduled_job_ids:
                return
            self._scheduled_job_ids.add(job_id)
        executor = self._recovery_executor if prefer_recovery else self._live_executor
        executor.submit(self._run_job_by_id, job_id)

    def _maybe_schedule_job(self, job: RuntimeTurnJob) -> None:
        if job.status not in NON_TERMINAL_JOB_STATUSES:
            return
        self._schedule_job(job.job_id)

    def _run_job_by_id(self, job_id: str) -> None:
        try:
            job = self._store.get_job(job_id)
            if job is None:
                return
            self._run_job(job)
        finally:
            with self._scheduled_job_ids_lock:
                self._scheduled_job_ids.discard(job_id)

    def _run_job(self, job: RuntimeTurnJob) -> RuntimeTurnJob:
        if job.status in {"ready", "failed", "cancelled"}:
            return job

        session = self._store.get_session(job.session_id)
        if session is None:
            return self._fail_job(job, "Runtime session not found.")

        steps = self._build_steps_for_stage("planning")
        self._store.replace_job_steps(job.job_id, steps)
        job = self._update_job(job, status="planning")

        def progress_callback(stage_name: str) -> None:
            nonlocal job
            if stage_name not in PIPELINE_STEPS:
                return
            self._store.replace_job_steps(job.job_id, self._build_steps_for_stage(stage_name))
            job = self._update_job(job, status=stage_name)

        try:
            prior_turns = self._store.list_turns(session.session_id)
            generated = self._generation_service.generate_turn(
                session=session,
                prior_turns=prior_turns,
                job_id=job.job_id,
                progress_callback=progress_callback,
            )
        except Exception as error:
            return self._fail_job(job, str(error))

        progress_callback("validating")
        self._store.create_turn(generated.turn)
        self._store.create_artifacts(session.session_id, generated.turn.envelope.artifacts)
        self._store.update_session(
            generated.session.model_copy(
                update={
                    "active_job_id": job.job_id,
                    "updated_at": datetime.now(UTC).isoformat(),
                }
            )
        )
        self._store.replace_job_steps(job.job_id, self._build_terminal_ready_steps())
        return self._update_job(job, status="ready", turn_id=generated.turn.turn_id, error_message="")

    def _fail_job(self, job: RuntimeTurnJob, error_message: str) -> RuntimeTurnJob:
        failed_stage = job.status if job.status in PIPELINE_STEPS else "planning"
        self._store.replace_job_steps(job.job_id, self._build_failed_steps(failed_stage, error_message))
        return self._update_job(job, status="failed", error_message=error_message)

    def _update_job(
        self,
        job: RuntimeTurnJob,
        *,
        status: str,
        turn_id: str | None = None,
        error_message: str | None = None,
    ) -> RuntimeTurnJob:
        updated = job.model_copy(
            update={
                "status": status,
                "turn_id": job.turn_id if turn_id is None else turn_id,
                "error_message": job.error_message if error_message is None else error_message,
                "updated_at": datetime.now(UTC).isoformat(),
            }
        )
        self._store.update_job(updated)
        return updated

    def _build_steps_for_stage(self, current_stage: str) -> list[RuntimeJobStepRecord]:
        stage_index = PIPELINE_STEPS.index(current_stage)
        steps: list[RuntimeJobStepRecord] = []
        for index, step_name in enumerate(PIPELINE_STEPS):
            status = "pending"
            if index < stage_index:
                status = "completed"
            elif index == stage_index:
                status = "running"
            steps.append(RuntimeJobStepRecord(step_name=step_name, status=status))
        return steps

    def _build_terminal_ready_steps(self) -> list[RuntimeJobStepRecord]:
        return [
            RuntimeJobStepRecord(step_name=step_name, status="completed")
            for step_name in PIPELINE_STEPS
        ]

    def _build_failed_steps(self, failed_stage: str, error_message: str) -> list[RuntimeJobStepRecord]:
        failed_index = PIPELINE_STEPS.index(failed_stage)
        steps: list[RuntimeJobStepRecord] = []
        for index, step_name in enumerate(PIPELINE_STEPS):
            status = "pending"
            message = ""
            if index < failed_index:
                status = "completed"
            elif index == failed_index:
                status = "failed"
                message = error_message
            steps.append(RuntimeJobStepRecord(step_name=step_name, status=status, error_message=message))
        return steps

    def _build_tracker_summary(self, session: RuntimeSession) -> RuntimeTrackerSessionSummary:
        turns = self._store.list_turns(session.session_id)
        return RuntimeTrackerSessionSummary(
            session_id=session.session_id,
            status=session.status,
            package_display_name=session.package_display_name,
            current_stage=self._resolve_current_stage(session),
            ready_turn_count=len(turns),
            max_turns=session.state.max_turns,
            updated_at=session.updated_at,
        )

    def _build_tracker_detail(self, session: RuntimeSession) -> RuntimeTrackerSessionDetail:
        turns = self._store.list_turns(session.session_id)
        active_job = self._store.get_job(session.active_job_id) if session.active_job_id else None
        return RuntimeTrackerSessionDetail(
            session_id=session.session_id,
            status=session.status,
            package_display_name=session.package_display_name,
            beat_cursor=session.state.beat_cursor,
            max_turns=session.state.max_turns,
            current_stage=self._resolve_current_stage(session, active_job),
            ready_turn_count=len(turns),
            active_job=self._build_tracker_job(active_job),
            turns=[self._build_tracker_turn(turn) for turn in turns],
            updated_at=session.updated_at,
            created_at=session.created_at,
        )

    def _build_tracker_job(self, job: RuntimeTurnJob | None) -> RuntimeTrackerJobDetail | None:
        if job is None:
            return None
        steps = self._store.list_job_steps(job.job_id)
        return RuntimeTrackerJobDetail(
            job_id=job.job_id,
            turn_index=job.turn_index,
            status=job.status,
            error_message=job.error_message,
            current_step_name=self._resolve_current_step_name(steps),
            steps=steps,
            updated_at=job.updated_at,
        )

    def _build_tracker_turn(self, turn: RuntimeTurnRecord) -> RuntimeTrackerTurnDetail:
        image_asset_ids = [
            artifact.asset_id
            for artifact in turn.envelope.artifacts
            if artifact.artifact_type == "image"
        ]
        fallback_artifact_count = sum(1 for artifact in turn.envelope.artifacts if artifact.fallback_used)
        return RuntimeTrackerTurnDetail(
            turn_id=turn.turn_id,
            turn_index=turn.turn_index,
            character_name=turn.character_name,
            generator_id=turn.generator_id,
            scene_name=turn.envelope.minigame.scene_name,
            objective_text=turn.envelope.minigame.objective_text,
            summary=turn.summary,
            image_asset_ids=image_asset_ids,
            artifact_count=len(turn.envelope.artifacts),
            fallback_artifact_count=fallback_artifact_count,
            created_at=turn.created_at,
        )

    @staticmethod
    def _resolve_current_step_name(steps: list[RuntimeJobStepRecord]) -> str:
        running = next((step.step_name for step in steps if step.status == "running"), "")
        if running:
            return running
        completed = [step.step_name for step in steps if step.status == "completed"]
        return completed[-1] if completed else ""

    def _resolve_current_stage(
        self,
        session: RuntimeSession,
        active_job: RuntimeTurnJob | None = None,
    ) -> str:
        if active_job is None and session.active_job_id:
            active_job = self._store.get_job(session.active_job_id)
        if active_job is not None:
            return active_job.status
        return session.status
