from __future__ import annotations

import json
import sqlite3

from .runtime_models import (
    ArtifactDescriptor,
    RuntimeJobStepRecord,
    RuntimeSession,
    RuntimeTurnRecord,
    RuntimeTurnSummary,
)


def initialize_runtime_schema(connection: sqlite3.Connection) -> None:
    connection.execute(
        """
        CREATE TABLE IF NOT EXISTS runtime_sessions (
            session_id TEXT PRIMARY KEY,
            status TEXT NOT NULL,
            package_id TEXT NOT NULL,
            package_display_name TEXT NOT NULL,
            active_job_id TEXT NOT NULL,
            last_ready_turn_id TEXT NOT NULL,
            state_json TEXT NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        )
        """
    )
    connection.execute(
        """
        CREATE TABLE IF NOT EXISTS runtime_turns (
            turn_id TEXT PRIMARY KEY,
            session_id TEXT NOT NULL,
            turn_index INTEGER NOT NULL,
            status TEXT NOT NULL,
            entry_scene_name TEXT NOT NULL,
            generator_id TEXT NOT NULL,
            character_name TEXT NOT NULL,
            summary TEXT NOT NULL,
            envelope_json TEXT NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        )
        """
    )
    connection.execute(
        """
        CREATE TABLE IF NOT EXISTS runtime_jobs (
            job_id TEXT PRIMARY KEY,
            session_id TEXT NOT NULL,
            turn_index INTEGER NOT NULL,
            turn_id TEXT NOT NULL,
            status TEXT NOT NULL,
            error_message TEXT NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        )
        """
    )
    connection.execute(
        """
        CREATE TABLE IF NOT EXISTS runtime_job_steps (
            job_id TEXT NOT NULL,
            step_index INTEGER NOT NULL,
            step_name TEXT NOT NULL,
            status TEXT NOT NULL,
            error_message TEXT NOT NULL,
            PRIMARY KEY (job_id, step_name)
        )
        """
    )
    connection.execute(
        """
        CREATE TABLE IF NOT EXISTS runtime_artifacts (
            asset_id TEXT PRIMARY KEY,
            session_id TEXT NOT NULL,
            turn_id TEXT NOT NULL,
            artifact_type TEXT NOT NULL,
            beat_id TEXT NOT NULL,
            shot_id TEXT NOT NULL,
            mime_type TEXT NOT NULL,
            provider_name TEXT NOT NULL,
            provider_model TEXT NOT NULL,
            fallback_used INTEGER NOT NULL,
            stored_path TEXT NOT NULL,
            metadata_json TEXT NOT NULL,
            created_at TEXT NOT NULL
        )
        """
    )
    connection.execute(
        """
        CREATE TABLE IF NOT EXISTS runtime_outcomes (
            outcome_id TEXT PRIMARY KEY,
            session_id TEXT NOT NULL,
            turn_id TEXT NOT NULL,
            result_json TEXT NOT NULL,
            created_at TEXT NOT NULL
        )
        """
    )


def replace_job_steps(
    connection: sqlite3.Connection,
    job_id: str,
    steps: list[RuntimeJobStepRecord],
) -> None:
    connection.execute("DELETE FROM runtime_job_steps WHERE job_id = ?", (job_id,))
    for index, step in enumerate(steps):
        connection.execute(
            """
            INSERT INTO runtime_job_steps (
                job_id,
                step_index,
                step_name,
                status,
                error_message
            ) VALUES (?, ?, ?, ?, ?)
            """,
            (
                job_id,
                index,
                step.step_name,
                step.status,
                step.error_message,
            ),
        )


def build_runtime_session(row: sqlite3.Row) -> RuntimeSession:
    return RuntimeSession.model_validate(
        {
            "session_id": row["session_id"],
            "status": row["status"],
            "package_id": row["package_id"],
            "package_display_name": row["package_display_name"],
            "active_job_id": row["active_job_id"] or "",
            "last_ready_turn_id": row["last_ready_turn_id"] or "",
            "state": json.loads(row["state_json"]),
            "created_at": row["created_at"],
            "updated_at": row["updated_at"],
        }
    )


def build_runtime_turn(row: sqlite3.Row) -> RuntimeTurnRecord:
    return RuntimeTurnRecord.model_validate(
        {
            "turn_id": row["turn_id"],
            "session_id": row["session_id"],
            "turn_index": row["turn_index"],
            "status": row["status"],
            "entry_scene_name": row["entry_scene_name"],
            "generator_id": row["generator_id"],
            "character_name": row["character_name"],
            "summary": row["summary"],
            "envelope": json.loads(row["envelope_json"]),
            "created_at": row["created_at"],
            "updated_at": row["updated_at"],
        }
    )


def build_artifact_descriptor(row: sqlite3.Row) -> ArtifactDescriptor:
    return ArtifactDescriptor.model_validate(
        {
            "asset_id": row["asset_id"],
            "turn_id": row["turn_id"],
            "artifact_type": row["artifact_type"],
            "beat_id": row["beat_id"],
            "shot_id": row["shot_id"],
            "mime_type": row["mime_type"],
            "provider_name": row["provider_name"],
            "provider_model": row["provider_model"],
            "fallback_used": bool(row["fallback_used"]),
            "stored_path": row["stored_path"],
            "metadata": json.loads(row["metadata_json"] or "{}"),
        }
    )


def build_turn_summary(row: sqlite3.Row) -> RuntimeTurnSummary:
    return RuntimeTurnSummary.model_validate(dict(row))


def default_job_steps() -> list[RuntimeJobStepRecord]:
    return [
        RuntimeJobStepRecord(step_name="planning"),
        RuntimeJobStepRecord(step_name="generating_images"),
        RuntimeJobStepRecord(step_name="generating_audio"),
        RuntimeJobStepRecord(step_name="assembling_contract"),
        RuntimeJobStepRecord(step_name="validating"),
    ]
