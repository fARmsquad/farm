import json
import sqlite3
from datetime import UTC, datetime
from pathlib import Path

from .models import (
    GeneratedStandingSliceJobRecord,
    GeneratedStandingSliceJobStepRecord,
    StoryJobCreateRequest,
    StoryJobRecord,
)


class StoryJobStore:
    def __init__(self, database_path: Path) -> None:
        self._database_path = database_path
        self._database_path.parent.mkdir(parents=True, exist_ok=True)
        self._initialize()

    def create_job(self, request: StoryJobCreateRequest) -> StoryJobRecord:
        record = StoryJobRecord.create_pending(request)
        with self._connect() as connection:
            connection.execute(
                """
                INSERT INTO story_jobs (
                    job_id,
                    status,
                    story_brief,
                    package_template,
                    target_scene,
                    metadata_json,
                    created_at,
                    updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    record.job_id,
                    record.status,
                    record.story_brief,
                    record.package_template,
                    record.target_scene,
                    json.dumps(record.metadata, sort_keys=True),
                    record.created_at,
                    record.updated_at,
                ),
            )
        return record

    def get_job(self, job_id: str) -> StoryJobRecord | None:
        with self._connect() as connection:
            row = connection.execute(
                """
                SELECT
                    job_id,
                    status,
                    story_brief,
                    package_template,
                    target_scene,
                    metadata_json,
                    created_at,
                    updated_at
                FROM story_jobs
                WHERE job_id = ?
                """,
                (job_id,),
            ).fetchone()

        if row is None:
            return None

        return StoryJobRecord(
            job_id=row["job_id"],
            status=row["status"],
            story_brief=row["story_brief"],
            package_template=row["package_template"],
            target_scene=row["target_scene"],
            metadata=json.loads(row["metadata_json"] or "{}"),
            created_at=row["created_at"],
            updated_at=row["updated_at"],
        )

    def _initialize(self) -> None:
        with self._connect() as connection:
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS story_jobs (
                    job_id TEXT PRIMARY KEY,
                    status TEXT NOT NULL,
                    story_brief TEXT NOT NULL,
                    package_template TEXT NOT NULL,
                    target_scene TEXT,
                    metadata_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
                """
            )

    def _connect(self) -> sqlite3.Connection:
        return _connect(self._database_path)


class GeneratedStandingSliceJobStore:
    def __init__(self, database_path: Path) -> None:
        self._database_path = database_path
        self._database_path.parent.mkdir(parents=True, exist_ok=True)
        self._initialize()

    def create_job(self, request) -> GeneratedStandingSliceJobRecord:
        record = GeneratedStandingSliceJobRecord.create_pending(request)
        with self._connect() as connection:
            connection.execute(
                """
                INSERT INTO generated_standing_slice_jobs (
                    job_id,
                    status,
                    approval_status,
                    request_json,
                    result_json,
                    created_at,
                    updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    record.job_id,
                    record.status,
                    record.approval_status,
                    json.dumps(record.request.model_dump(mode="json"), sort_keys=True),
                    "null",
                    record.created_at,
                    record.updated_at,
                ),
            )
            self._replace_steps(connection, record.job_id, record.steps)

        return record

    def mark_running(self, job_id: str) -> GeneratedStandingSliceJobRecord | None:
        updated_at = datetime.now(UTC).isoformat()
        with self._connect() as connection:
            connection.execute(
                """
                UPDATE generated_standing_slice_jobs
                SET status = ?, updated_at = ?
                WHERE job_id = ?
                """,
                ("running", updated_at, job_id),
            )

        return self.get_job(job_id)

    def finish_job(
        self,
        job_id: str,
        *,
        status: str,
        result,
        steps: list[GeneratedStandingSliceJobStepRecord],
    ) -> GeneratedStandingSliceJobRecord | None:
        updated_at = datetime.now(UTC).isoformat()
        with self._connect() as connection:
            connection.execute(
                """
                UPDATE generated_standing_slice_jobs
                SET status = ?, result_json = ?, updated_at = ?
                WHERE job_id = ?
                """,
                (
                    status,
                    json.dumps(result.model_dump(mode="json"), sort_keys=True),
                    updated_at,
                    job_id,
                ),
            )
            self._replace_steps(connection, job_id, steps)

        return self.get_job(job_id)

    def get_job(self, job_id: str) -> GeneratedStandingSliceJobRecord | None:
        with self._connect() as connection:
            job_row = connection.execute(
                """
                SELECT
                    job_id,
                    status,
                    approval_status,
                    request_json,
                    result_json,
                    created_at,
                    updated_at
                FROM generated_standing_slice_jobs
                WHERE job_id = ?
                """,
                (job_id,),
            ).fetchone()
            if job_row is None:
                return None

            step_rows = connection.execute(
                """
                SELECT
                    step_id,
                    status,
                    output_json,
                    errors_json
                FROM generated_standing_slice_job_steps
                WHERE job_id = ?
                ORDER BY step_index ASC
                """,
                (job_id,),
            ).fetchall()

        request_payload = json.loads(job_row["request_json"])
        result_payload = json.loads(job_row["result_json"]) if job_row["result_json"] else None
        return GeneratedStandingSliceJobRecord(
            job_id=job_row["job_id"],
            status=job_row["status"],
            approval_status=job_row["approval_status"],
            request=request_payload,
            result=result_payload,
            steps=[
                GeneratedStandingSliceJobStepRecord(
                    step_id=row["step_id"],
                    status=row["status"],
                    output=json.loads(row["output_json"]) if row["output_json"] else None,
                    errors=json.loads(row["errors_json"] or "[]"),
                )
                for row in step_rows
            ],
            created_at=job_row["created_at"],
            updated_at=job_row["updated_at"],
        )

    def _replace_steps(
        self,
        connection: sqlite3.Connection,
        job_id: str,
        steps: list[GeneratedStandingSliceJobStepRecord],
    ) -> None:
        connection.execute(
            "DELETE FROM generated_standing_slice_job_steps WHERE job_id = ?",
            (job_id,),
        )
        for index, step in enumerate(steps):
            connection.execute(
                """
                INSERT INTO generated_standing_slice_job_steps (
                    job_id,
                    step_index,
                    step_id,
                    status,
                    output_json,
                    errors_json
                ) VALUES (?, ?, ?, ?, ?, ?)
                """,
                (
                    job_id,
                    index,
                    step.step_id,
                    step.status,
                    json.dumps(step.output.model_dump(mode="json"), sort_keys=True) if step.output is not None else "",
                    json.dumps(step.errors, sort_keys=True),
                ),
            )

    def _initialize(self) -> None:
        with self._connect() as connection:
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS generated_standing_slice_jobs (
                    job_id TEXT PRIMARY KEY,
                    status TEXT NOT NULL,
                    approval_status TEXT NOT NULL,
                    request_json TEXT NOT NULL,
                    result_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
                """
            )
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS generated_standing_slice_job_steps (
                    job_id TEXT NOT NULL,
                    step_index INTEGER NOT NULL,
                    step_id TEXT NOT NULL,
                    status TEXT NOT NULL,
                    output_json TEXT NOT NULL,
                    errors_json TEXT NOT NULL,
                    PRIMARY KEY (job_id, step_id),
                    FOREIGN KEY (job_id) REFERENCES generated_standing_slice_jobs(job_id) ON DELETE CASCADE
                )
                """
            )

    def _connect(self) -> sqlite3.Connection:
        return _connect(self._database_path)


def _connect(database_path: Path) -> sqlite3.Connection:
    connection = sqlite3.connect(database_path)
    connection.row_factory = sqlite3.Row
    connection.execute("PRAGMA foreign_keys = ON")
    return connection
