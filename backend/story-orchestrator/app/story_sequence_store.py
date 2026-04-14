from __future__ import annotations

import json
import sqlite3
from contextlib import contextmanager
from pathlib import Path

from .story_sequence_models import (
    StorySequenceSessionDetail,
    StorySequenceSessionRecord,
    StorySequenceSessionState,
    StorySequenceTurnRecord,
)


class StorySequenceSessionStore:
    def __init__(self, database_path: Path) -> None:
        self._database_path = database_path
        self._database_path.parent.mkdir(parents=True, exist_ok=True)
        self._initialize()

    def create_session(self, record: StorySequenceSessionRecord) -> StorySequenceSessionRecord:
        with self._connection() as connection:
            connection.execute(
                """
                INSERT INTO story_sequence_sessions (
                    session_id,
                    status,
                    package_id,
                    package_display_name,
                    state_json,
                    created_at,
                    updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    record.session_id,
                    record.status,
                    record.package_id,
                    record.package_display_name,
                    json.dumps(record.state.model_dump(mode="json"), sort_keys=True),
                    record.created_at,
                    record.updated_at,
                ),
            )
        return record

    def append_turn(
        self,
        session: StorySequenceSessionRecord,
        turn: StorySequenceTurnRecord,
    ) -> StorySequenceSessionDetail | None:
        with self._connection() as connection:
            updated = connection.execute(
                """
                UPDATE story_sequence_sessions
                SET status = ?, package_id = ?, package_display_name = ?, state_json = ?, updated_at = ?
                WHERE session_id = ?
                """,
                (
                    session.status,
                    session.package_id,
                    session.package_display_name,
                    json.dumps(session.state.model_dump(mode="json"), sort_keys=True),
                    session.updated_at,
                    session.session_id,
                ),
            )
            if updated.rowcount == 0:
                return None

            connection.execute(
                """
                INSERT INTO story_sequence_turns (
                    session_id,
                    turn_index,
                    generator_id,
                    character_name,
                    summary,
                    request_json,
                    result_json,
                    created_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    turn.session_id,
                    turn.turn_index,
                    turn.generator_id,
                    turn.character_name,
                    turn.summary,
                    json.dumps(turn.request.model_dump(mode="json"), sort_keys=True),
                    json.dumps(turn.result.model_dump(mode="json"), sort_keys=True),
                    turn.created_at,
                ),
            )
        return self.get_session_detail(session.session_id)

    def get_session(self, session_id: str) -> StorySequenceSessionRecord | None:
        with self._connection() as connection:
            row = connection.execute(
                """
                SELECT
                    session_id,
                    status,
                    package_id,
                    package_display_name,
                    state_json,
                    created_at,
                    updated_at
                FROM story_sequence_sessions
                WHERE session_id = ?
                """,
                (session_id,),
            ).fetchone()

        if row is None:
            return None

        return StorySequenceSessionRecord(
            session_id=row["session_id"],
            status=row["status"],
            package_id=row["package_id"],
            package_display_name=row["package_display_name"],
            state=StorySequenceSessionState.model_validate(json.loads(row["state_json"])),
            created_at=row["created_at"],
            updated_at=row["updated_at"],
        )

    def get_session_detail(self, session_id: str) -> StorySequenceSessionDetail | None:
        session = self.get_session(session_id)
        if session is None:
            return None
        return StorySequenceSessionDetail(session=session, turns=self.list_turns(session_id))

    def list_turns(self, session_id: str) -> list[StorySequenceTurnRecord]:
        with self._connection() as connection:
            rows = connection.execute(
                """
                SELECT
                    session_id,
                    turn_index,
                    generator_id,
                    character_name,
                    summary,
                    request_json,
                    result_json,
                    created_at
                FROM story_sequence_turns
                WHERE session_id = ?
                ORDER BY turn_index ASC
                """,
                (session_id,),
            ).fetchall()

        return [
            StorySequenceTurnRecord.model_validate(
                {
                    "session_id": row["session_id"],
                    "turn_index": row["turn_index"],
                    "generator_id": row["generator_id"],
                    "character_name": row["character_name"],
                    "summary": row["summary"],
                    "request": json.loads(row["request_json"]),
                    "result": json.loads(row["result_json"]),
                    "created_at": row["created_at"],
                }
            )
            for row in rows
        ]

    def _initialize(self) -> None:
        with self._connection() as connection:
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS story_sequence_sessions (
                    session_id TEXT PRIMARY KEY,
                    status TEXT NOT NULL,
                    package_id TEXT NOT NULL,
                    package_display_name TEXT NOT NULL,
                    state_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
                """
            )
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS story_sequence_turns (
                    session_id TEXT NOT NULL,
                    turn_index INTEGER NOT NULL,
                    generator_id TEXT NOT NULL,
                    character_name TEXT NOT NULL,
                    summary TEXT NOT NULL,
                    request_json TEXT NOT NULL,
                    result_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    PRIMARY KEY (session_id, turn_index)
                )
                """
            )

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
