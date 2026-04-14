from __future__ import annotations

from collections.abc import Generator
from datetime import UTC, datetime

import pytest
from sqlalchemy.orm import Session

from config import Settings
from db.models import ContentCalendarItem, ControlFlag, Draft, Lead
from db.session import create_engine_from_settings, create_session_factory, init_db


@pytest.fixture()
def settings(tmp_path) -> Settings:
    return Settings(
        database_url=f"sqlite:///{tmp_path / 'gtm.db'}",
        log_path=str(tmp_path / "gtm.log"),
        reddit_username="mccluckinfarm",
        x_username="mccluckinfarm",
        anthropic_api_key="test-key",
        review_port=8010,
    )


@pytest.fixture()
def session_factory(settings):
    engine = create_engine_from_settings(settings)
    init_db(engine)
    return create_session_factory(engine)


@pytest.fixture()
def db_session(session_factory) -> Generator[Session, None, None]:
    with session_factory() as session:
        yield session


@pytest.fixture()
def now() -> datetime:
    return datetime(2026, 4, 14, 16, 0, tzinfo=UTC)


def create_lead(
    session: Session,
    *,
    platform: str = "reddit",
    platform_id: str = "lead-1",
    subreddit: str | None = "OculusQuest",
    author: str = "someone",
    title: str | None = "Need cozy VR games",
    body: str = "Looking for a cozy VR game recommendation.",
    url: str = "https://example.com/post/1",
    matched_keywords: list[str] | None = None,
    status: str = "new",
) -> Lead:
    lead = Lead(
        platform=platform,
        platform_id=platform_id,
        subreddit=subreddit,
        author=author,
        title=title,
        body=body,
        url=url,
        matched_keywords=matched_keywords or ["cozy VR"],
        status=status,
    )
    session.add(lead)
    session.commit()
    session.refresh(lead)
    return lead


def create_draft(
    session: Session,
    *,
    lead: Lead | None = None,
    content_item: ContentCalendarItem | None = None,
    draft_text: str = "This looks relevant.",
    reviewer_action: str | None = None,
    edited_text: str | None = None,
) -> Draft:
    draft = Draft(
        lead_id=lead.id if lead else None,
        content_calendar_id=content_item.id if content_item else None,
        draft_text=draft_text,
        model_used="claude-sonnet-4-20250514",
        prompt_hash="hash",
        reviewer_action=reviewer_action,
        edited_text=edited_text,
    )
    session.add(draft)
    session.commit()
    session.refresh(draft)
    return draft


def create_calendar_item(
    session: Session,
    *,
    content_type: str = "engagement",
    platform: str = "twitter",
    topic: str = "cozy VR question",
    description: str | None = None,
    status: str = "new",
    scheduled_date: datetime | None = None,
) -> ContentCalendarItem:
    item = ContentCalendarItem(
        content_type=content_type,
        platform=platform,
        topic=topic,
        description=description,
        status=status,
        scheduled_date=scheduled_date or datetime(2026, 4, 14, 18, 0, tzinfo=UTC),
    )
    session.add(item)
    session.commit()
    session.refresh(item)
    return item


def set_flag(session: Session, key: str, value: str) -> None:
    flag = session.get(ControlFlag, key)
    if flag is None:
        session.add(ControlFlag(key=key, value=value))
    else:
        flag.value = value
    session.commit()

