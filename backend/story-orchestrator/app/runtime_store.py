from __future__ import annotations

import json
import sqlite3
from contextlib import contextmanager
from datetime import UTC, datetime
from pathlib import Path
from uuid import uuid4

from .runtime_models import (
    ArtifactDescriptor,
    RuntimeJobStepRecord,
    RuntimeSession,
    RuntimeSessionDetail,
    RuntimeTurnJob,
    RuntimeTurnRecord,
    RuntimeTurnSummary,
    TurnOutcomeRequest,
)
from .runtime_store_support import (
    build_artifact_descriptor,
    build_runtime_session,
    build_runtime_turn,
    build_turn_summary,
    default_job_steps,
    initialize_runtime_schema,
    replace_job_steps,
)


class RuntimeSessionStore:
    def __init__(self, database_path: Path) -> None:
        self._database_path = database_path
        self._database_path.parent.mkdir(parents=True, exist_ok=True)
        self._initialize()

    def create_session(self, session: RuntimeSession) -> RuntimeSession:
        with self._connection() as connection:
            connection.execute(
                """
                INSERT INTO runtime_sessions (
                    session_id,
                    status,
                    package_id,
                    package_display_name,
                    active_job_id,
                    last_ready_turn_id,
                    state_json,
                    created_at,
                    updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    session.session_id,
                    session.status,
                    session.package_id,
                    session.package_display_name,
                    session.active_job_id,
                    session.last_ready_turn_id,
                    json.dumps(session.state.model_dump(mode="json"), sort_keys=True),
                    session.created_at,
                    session.updated_at,
                ),
            )
        return session

    def get_session(self, session_id: str) -> RuntimeSession | None:
        with self._connection() as connection:
            row = connection.execute(
                """
                SELECT
                    session_id,
                    status,
                    package_id,
                    package_display_name,
                    active_job_id,
                    last_ready_turn_id,
                    state_json,
                    created_at,
                    updated_at
                FROM runtime_sessions
                WHERE session_id = ?
                """,
                (session_id,),
            ).fetchone()

        if row is None:
            return None

        return build_runtime_session(row)

    def update_session(self, session: RuntimeSession) -> RuntimeSession:
        with self._connection() as connection:
            connection.execute(
                """
                UPDATE runtime_sessions
                SET status = ?, active_job_id = ?, last_ready_turn_id = ?, state_json = ?, updated_at = ?
                WHERE session_id = ?
                """,
                (
                    session.status,
                    session.active_job_id,
                    session.last_ready_turn_id,
                    json.dumps(session.state.model_dump(mode="json"), sort_keys=True),
                    session.updated_at,
                    session.session_id,
                ),
            )
        return session

    def list_sessions(self, limit: int) -> list[RuntimeSession]:
        with self._connection() as connection:
            rows = connection.execute(
                """
                SELECT
                    session_id,
                    status,
                    package_id,
                    package_display_name,
                    active_job_id,
                    last_ready_turn_id,
                    state_json,
                    created_at,
                    updated_at
                FROM runtime_sessions
                ORDER BY updated_at DESC
                LIMIT ?
                """,
                (limit,),
            ).fetchall()

        return [build_runtime_session(row) for row in rows]

    def create_job(self, job: RuntimeTurnJob) -> RuntimeTurnJob:
        with self._connection() as connection:
            connection.execute(
                """
                INSERT INTO runtime_jobs (
                    job_id,
                    session_id,
                    turn_index,
                    turn_id,
                    status,
                    error_message,
                    created_at,
                    updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    job.job_id,
                    job.session_id,
                    job.turn_index,
                    job.turn_id,
                    job.status,
                    job.error_message,
                    job.created_at,
                    job.updated_at,
                ),
            )
            replace_job_steps(connection, job.job_id, default_job_steps())
        return job

    def get_job(self, job_id: str) -> RuntimeTurnJob | None:
        with self._connection() as connection:
            row = connection.execute(
                """
                SELECT
                    job_id,
                    session_id,
                    turn_index,
                    turn_id,
                    status,
                    error_message,
                    created_at,
                    updated_at
                FROM runtime_jobs
                WHERE job_id = ?
                """,
                (job_id,),
            ).fetchone()

        if row is None:
            return None

        return RuntimeTurnJob.model_validate(dict(row))

    def list_jobs_by_status(self, statuses: tuple[str, ...]) -> list[RuntimeTurnJob]:
        if not statuses:
            return []

        placeholders = ", ".join("?" for _ in statuses)
        with self._connection() as connection:
            rows = connection.execute(
                f"""
                SELECT
                    job_id,
                    session_id,
                    turn_index,
                    turn_id,
                    status,
                    error_message,
                    created_at,
                    updated_at
                FROM runtime_jobs
                WHERE status IN ({placeholders})
                ORDER BY created_at ASC
                """,
                statuses,
            ).fetchall()

        return [RuntimeTurnJob.model_validate(dict(row)) for row in rows]

    def update_job(self, job: RuntimeTurnJob) -> RuntimeTurnJob:
        with self._connection() as connection:
            connection.execute(
                """
                UPDATE runtime_jobs
                SET turn_id = ?, status = ?, error_message = ?, updated_at = ?
                WHERE job_id = ?
                """,
                (
                    job.turn_id,
                    job.status,
                    job.error_message,
                    job.updated_at,
                    job.job_id,
                ),
            )
        return job

    def replace_job_steps(self, job_id: str, steps: list[RuntimeJobStepRecord]) -> None:
        with self._connection() as connection:
            replace_job_steps(connection, job_id, steps)

    def list_job_steps(self, job_id: str) -> list[RuntimeJobStepRecord]:
        with self._connection() as connection:
            rows = connection.execute(
                """
                SELECT
                    step_name,
                    status,
                    error_message
                FROM runtime_job_steps
                WHERE job_id = ?
                ORDER BY step_index ASC
                """,
                (job_id,),
            ).fetchall()

        return [RuntimeJobStepRecord.model_validate(dict(row)) for row in rows]

    def create_turn(self, turn: RuntimeTurnRecord) -> RuntimeTurnRecord:
        with self._connection() as connection:
            connection.execute(
                """
                INSERT INTO runtime_turns (
                    turn_id,
                    session_id,
                    turn_index,
                    status,
                    entry_scene_name,
                    generator_id,
                    character_name,
                    summary,
                    envelope_json,
                    created_at,
                    updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    turn.turn_id,
                    turn.session_id,
                    turn.turn_index,
                    turn.status,
                    turn.entry_scene_name,
                    turn.generator_id,
                    turn.character_name,
                    turn.summary,
                    json.dumps(turn.envelope.model_dump(mode="json"), sort_keys=True),
                    turn.created_at,
                    turn.updated_at,
                ),
            )
        return turn

    def list_turns(self, session_id: str) -> list[RuntimeTurnRecord]:
        with self._connection() as connection:
            rows = connection.execute(
                """
                SELECT
                    turn_id,
                    session_id,
                    turn_index,
                    status,
                    entry_scene_name,
                    generator_id,
                    character_name,
                    summary,
                    envelope_json,
                    created_at,
                    updated_at
                FROM runtime_turns
                WHERE session_id = ?
                ORDER BY turn_index ASC
                """,
                (session_id,),
            ).fetchall()

        return [build_runtime_turn(row) for row in rows]

    def get_turn(self, session_id: str, turn_id: str) -> RuntimeTurnRecord | None:
        with self._connection() as connection:
            row = connection.execute(
                """
                SELECT
                    turn_id,
                    session_id,
                    turn_index,
                    status,
                    entry_scene_name,
                    generator_id,
                    character_name,
                    summary,
                    envelope_json,
                    created_at,
                    updated_at
                FROM runtime_turns
                WHERE session_id = ? AND turn_id = ?
                """,
                (session_id, turn_id),
            ).fetchone()

        if row is None:
            return None

        return build_runtime_turn(row)

    def create_artifacts(self, session_id: str, artifacts: list[ArtifactDescriptor]) -> None:
        with self._connection() as connection:
            for artifact in artifacts:
                connection.execute(
                    """
                    INSERT INTO runtime_artifacts (
                        asset_id,
                        session_id,
                        turn_id,
                        artifact_type,
                        beat_id,
                        shot_id,
                        mime_type,
                        provider_name,
                        provider_model,
                        fallback_used,
                        stored_path,
                        metadata_json,
                        created_at
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """,
                    (
                        artifact.asset_id,
                        session_id,
                        artifact.turn_id,
                        artifact.artifact_type,
                        artifact.beat_id,
                        artifact.shot_id,
                        artifact.mime_type,
                        artifact.provider_name,
                        artifact.provider_model,
                        1 if artifact.fallback_used else 0,
                        artifact.stored_path,
                        json.dumps(artifact.metadata, sort_keys=True),
                        datetime.now(UTC).isoformat(),
                    ),
                )

    def get_artifact(self, asset_id: str) -> ArtifactDescriptor | None:
        with self._connection() as connection:
            row = connection.execute(
                """
                SELECT
                    asset_id,
                    turn_id,
                    artifact_type,
                    beat_id,
                    shot_id,
                    mime_type,
                    provider_name,
                    provider_model,
                    fallback_used,
                    stored_path,
                    metadata_json
                FROM runtime_artifacts
                WHERE asset_id = ?
                """,
                (asset_id,),
            ).fetchone()

        if row is None:
            return None

        return build_artifact_descriptor(row)

    def record_outcome(self, session_id: str, turn_id: str, request: TurnOutcomeRequest) -> None:
        with self._connection() as connection:
            connection.execute(
                """
                INSERT INTO runtime_outcomes (
                    outcome_id,
                    session_id,
                    turn_id,
                    result_json,
                    created_at
                ) VALUES (?, ?, ?, ?, ?)
                """,
                (
                    str(uuid4()),
                    session_id,
                    turn_id,
                    json.dumps(request.model_dump(mode="json"), sort_keys=True),
                    datetime.now(UTC).isoformat(),
                ),
            )

    def get_session_detail(self, session_id: str) -> RuntimeSessionDetail | None:
        session = self.get_session(session_id)
        if session is None:
            return None

        active_job = self.get_job(session.active_job_id) if session.active_job_id else None
        last_ready_turn = self._get_turn_summary(session.last_ready_turn_id) if session.last_ready_turn_id else None
        return RuntimeSessionDetail(
            session_id=session.session_id,
            status=session.status,
            state=session.state,
            active_job=active_job,
            last_ready_turn=last_ready_turn,
            created_at=session.created_at,
            updated_at=session.updated_at,
        )

    def _get_turn_summary(self, turn_id: str) -> RuntimeTurnSummary | None:
        with self._connection() as connection:
            row = connection.execute(
                """
                SELECT
                    turn_id,
                    turn_index,
                    status,
                    entry_scene_name,
                    generator_id,
                    character_name,
                    summary,
                    created_at
                FROM runtime_turns
                WHERE turn_id = ?
                """,
                (turn_id,),
            ).fetchone()

        if row is None:
            return None

        return build_turn_summary(row)

    def _initialize(self) -> None:
        with self._connection() as connection:
            initialize_runtime_schema(connection)

    def _connect(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self._database_path)
        connection.row_factory = sqlite3.Row
        return connection

    @contextmanager
    def _connection(self):
        connection = self._connect()
        try:
            with connection:
                yield connection
        finally:
            connection.close()
