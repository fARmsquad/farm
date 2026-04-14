import json
import sqlite3
from datetime import UTC, datetime
from pathlib import Path

from .models import (
    GeneratedStandingSliceJobAssetRecord,
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
                    review_notes,
                    publish_status,
                    published_at,
                    request_json,
                    result_json,
                    created_at,
                    updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    record.job_id,
                    record.status,
                    record.approval_status,
                    record.review_notes,
                    record.publish_status,
                    record.published_at,
                    json.dumps(record.request.model_dump(mode="json"), sort_keys=True),
                    "null",
                    record.created_at,
                    record.updated_at,
                ),
            )
            self._replace_steps(connection, record.job_id, record.steps)
            self._replace_assets(connection, record.job_id, record.assets)

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
        assets: list[GeneratedStandingSliceJobAssetRecord],
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
            self._replace_assets(connection, job_id, assets)

        return self.get_job(job_id)

    def update_review(
        self,
        job_id: str,
        *,
        approval_status: str,
        review_notes: str,
    ) -> GeneratedStandingSliceJobRecord | None:
        updated_at = datetime.now(UTC).isoformat()
        with self._connect() as connection:
            cursor = connection.execute(
                """
                UPDATE generated_standing_slice_jobs
                SET approval_status = ?, review_notes = ?, updated_at = ?
                WHERE job_id = ?
                """,
                (approval_status, review_notes, updated_at, job_id),
            )
            if cursor.rowcount == 0:
                return None

        return self.get_job(job_id)

    def mark_published(self, job_id: str) -> GeneratedStandingSliceJobRecord | None:
        published_at = datetime.now(UTC).isoformat()
        with self._connect() as connection:
            cursor = connection.execute(
                """
                UPDATE generated_standing_slice_jobs
                SET publish_status = 'published', published_at = ?, updated_at = ?
                WHERE job_id = ?
                """,
                (published_at, published_at, job_id),
            )
            if cursor.rowcount == 0:
                return None
        return self.get_job(job_id)

    def get_job(self, job_id: str) -> GeneratedStandingSliceJobRecord | None:
        with self._connect() as connection:
            job_row = connection.execute(
                """
                SELECT
                    job_id,
                    status,
                    approval_status,
                    review_notes,
                    publish_status,
                    published_at,
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
            asset_rows = connection.execute(
                """
                SELECT
                    step_id,
                    asset_id,
                    beat_id,
                    shot_id,
                    asset_type,
                    provider_name,
                    provider_model,
                    fallback_used,
                    mime_type,
                    output_path,
                    resource_path,
                    metadata_json
                FROM generated_standing_slice_job_assets
                WHERE job_id = ?
                ORDER BY step_index ASC, asset_index ASC
                """,
                (job_id,),
            ).fetchall()

        request_payload = json.loads(job_row["request_json"])
        result_payload = json.loads(job_row["result_json"]) if job_row["result_json"] else None
        return GeneratedStandingSliceJobRecord(
            job_id=job_row["job_id"],
            status=job_row["status"],
            approval_status=job_row["approval_status"],
            review_notes=job_row["review_notes"] or "",
            publish_status=job_row["publish_status"] or "not_published",
            published_at=job_row["published_at"] or "",
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
            assets=[
                GeneratedStandingSliceJobAssetRecord(
                    step_id=row["step_id"],
                    asset_id=row["asset_id"],
                    beat_id=row["beat_id"],
                    shot_id=row["shot_id"],
                    asset_type=row["asset_type"],
                    provider_name=row["provider_name"],
                    provider_model=row["provider_model"],
                    fallback_used=bool(row["fallback_used"]),
                    mime_type=row["mime_type"],
                    output_path=row["output_path"],
                    resource_path=row["resource_path"],
                    metadata=json.loads(row["metadata_json"] or "{}"),
                )
                for row in asset_rows
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

    def _replace_assets(
        self,
        connection: sqlite3.Connection,
        job_id: str,
        assets: list[GeneratedStandingSliceJobAssetRecord],
    ) -> None:
        connection.execute(
            "DELETE FROM generated_standing_slice_job_assets WHERE job_id = ?",
            (job_id,),
        )
        for index, asset in enumerate(assets):
            connection.execute(
                """
                INSERT INTO generated_standing_slice_job_assets (
                    job_id,
                    step_index,
                    asset_index,
                    step_id,
                    asset_id,
                    beat_id,
                    shot_id,
                    asset_type,
                    provider_name,
                    provider_model,
                    fallback_used,
                    mime_type,
                    output_path,
                    resource_path,
                    metadata_json
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    job_id,
                    0 if asset.step_id == "post_chicken_to_farm" else 1,
                    index,
                    asset.step_id,
                    asset.asset_id,
                    asset.beat_id,
                    asset.shot_id,
                    asset.asset_type,
                    asset.provider_name,
                    asset.provider_model,
                    1 if asset.fallback_used else 0,
                    asset.mime_type,
                    asset.output_path,
                    asset.resource_path,
                    json.dumps(asset.metadata, sort_keys=True),
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
                    review_notes TEXT NOT NULL DEFAULT '',
                    publish_status TEXT NOT NULL DEFAULT 'not_published',
                    published_at TEXT NOT NULL DEFAULT '',
                    request_json TEXT NOT NULL,
                    result_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
                """
            )
            if not _table_has_column(connection, "generated_standing_slice_jobs", "review_notes"):
                connection.execute(
                    """
                    ALTER TABLE generated_standing_slice_jobs
                    ADD COLUMN review_notes TEXT NOT NULL DEFAULT ''
                    """
                )
            if not _table_has_column(connection, "generated_standing_slice_jobs", "publish_status"):
                connection.execute(
                    """
                    ALTER TABLE generated_standing_slice_jobs
                    ADD COLUMN publish_status TEXT NOT NULL DEFAULT 'not_published'
                    """
                )
            if not _table_has_column(connection, "generated_standing_slice_jobs", "published_at"):
                connection.execute(
                    """
                    ALTER TABLE generated_standing_slice_jobs
                    ADD COLUMN published_at TEXT NOT NULL DEFAULT ''
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
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS generated_standing_slice_job_assets (
                    job_id TEXT NOT NULL,
                    step_index INTEGER NOT NULL,
                    asset_index INTEGER NOT NULL,
                    step_id TEXT NOT NULL,
                    asset_id TEXT NOT NULL,
                    beat_id TEXT NOT NULL,
                    shot_id TEXT NOT NULL,
                    asset_type TEXT NOT NULL,
                    provider_name TEXT NOT NULL,
                    provider_model TEXT NOT NULL,
                    fallback_used INTEGER NOT NULL,
                    mime_type TEXT NOT NULL,
                    output_path TEXT NOT NULL,
                    resource_path TEXT NOT NULL,
                    metadata_json TEXT NOT NULL,
                    PRIMARY KEY (job_id, asset_id),
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


def _table_has_column(connection: sqlite3.Connection, table_name: str, column_name: str) -> bool:
    rows = connection.execute(f"PRAGMA table_info({table_name})").fetchall()
    return any(row["name"] == column_name for row in rows)
