from __future__ import annotations

from pathlib import Path

from sqlalchemy import create_engine, inspect, text
from sqlalchemy.engine import Engine
from sqlalchemy.orm import Session, sessionmaker

from config import Settings

from .models import Base


def create_engine_from_settings(settings: Settings) -> Engine:
    connect_args: dict[str, object] = {}
    if settings.database_url.startswith("sqlite"):
        connect_args["check_same_thread"] = False
    return create_engine(settings.database_url, future=True, connect_args=connect_args)


def create_session_factory(engine: Engine) -> sessionmaker[Session]:
    return sessionmaker(bind=engine, autoflush=False, expire_on_commit=False, future=True)


def init_db(engine: Engine) -> None:
    Base.metadata.create_all(engine)
    ensure_schema_updates(engine)


def ensure_parent_paths(settings: Settings) -> None:
    database_parent = settings.database_path.parent
    log_parent = settings.log_file_path.parent
    Path(database_parent).mkdir(parents=True, exist_ok=True)
    Path(log_parent).mkdir(parents=True, exist_ok=True)


def ensure_schema_updates(engine: Engine) -> None:
    inspector = inspect(engine)
    if "leads" not in inspector.get_table_names():
        return

    lead_columns = {column["name"] for column in inspector.get_columns("leads")}
    statements: list[str] = []
    if "decision_note" not in lead_columns:
        statements.append("ALTER TABLE leads ADD COLUMN decision_note TEXT")

    if not statements:
        return

    with engine.begin() as connection:
        for statement in statements:
            connection.execute(text(statement))
