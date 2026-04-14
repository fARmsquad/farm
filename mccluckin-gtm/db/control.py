from __future__ import annotations

from typing import Any

from sqlalchemy.orm import Session

from .models import ApiCallLog, ControlFlag

PAUSE_FLAG_KEY = "app.paused"


def get_flag(session: Session, key: str, default: str = "") -> str:
    flag = session.get(ControlFlag, key)
    return default if flag is None else flag.value


def set_flag(session: Session, key: str, value: str) -> None:
    flag = session.get(ControlFlag, key)
    if flag is None:
        session.add(ControlFlag(key=key, value=value))
    else:
        flag.value = value
    session.commit()


def is_paused(session: Session) -> bool:
    return get_flag(session, PAUSE_FLAG_KEY, "false").lower() == "true"


def set_paused(session: Session, paused: bool) -> None:
    set_flag(session, PAUSE_FLAG_KEY, "true" if paused else "false")


def log_api_call(
    session: Session,
    *,
    provider: str,
    action: str,
    payload: dict[str, Any],
    succeeded: bool = True,
) -> None:
    session.add(
        ApiCallLog(
            provider=provider,
            action=action,
            payload=payload,
            succeeded=succeeded,
        )
    )
    session.commit()

