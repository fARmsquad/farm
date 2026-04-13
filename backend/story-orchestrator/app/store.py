import json
import sqlite3
from pathlib import Path

from .models import StoryJobCreateRequest, StoryJobRecord


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
        connection = sqlite3.connect(self._database_path)
        connection.row_factory = sqlite3.Row
        return connection
