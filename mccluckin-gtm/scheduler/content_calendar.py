from __future__ import annotations

from datetime import UTC, datetime, time, timedelta

from sqlalchemy import select
from sqlalchemy.orm import sessionmaker

from db.models import ContentCalendarItem

CONTENT_TYPES = {
    "devlog": {
        "platforms": ["reddit"],
        "subreddits": ["indiegames", "gamedev", "VRGaming"],
        "frequency": "weekly",
        "prompt_template": "Write a casual devlog post about: {topic}. Include what we worked on, what's next, and one interesting challenge. Keep it authentic and under 300 words.",
    },
    "screenshot": {
        "platforms": ["twitter"],
        "frequency": "3x_weekly",
        "prompt_template": "Write a tweet to accompany this screenshot/concept art. Describe: {description}. Keep under 280 chars, be excited but genuine.",
    },
    "engagement": {
        "platforms": ["twitter"],
        "frequency": "daily",
        "prompt_template": "Write an engagement tweet about cozy gaming or VR development. Topic: {topic}. Could be a question, hot take, or observation. Under 280 chars.",
    },
}


def seed_content_calendar(session_factory: sessionmaker, *, weeks: int, now: datetime | None = None) -> int:
    current_time = now or datetime.now(UTC)
    created = 0
    with session_factory() as session:
        for week in range(weeks):
            monday = _week_start(current_time) + timedelta(weeks=week)
            created += _add_if_missing(
                session,
                content_type="devlog",
                platform="reddit",
                subreddit=CONTENT_TYPES["devlog"]["subreddits"][week % 3],
                scheduled_date=datetime.combine(monday.date() + timedelta(days=1), time(10, 0), tzinfo=UTC),
                topic=f"week {week + 1} development progress",
                description="What the team built, what changed, and the toughest implementation challenge.",
            )
            for offset in (0, 2, 4):
                created += _add_if_missing(
                    session,
                    content_type="screenshot",
                    platform="twitter",
                    subreddit=None,
                    scheduled_date=datetime.combine(monday.date() + timedelta(days=offset), time(17, 0), tzinfo=UTC),
                    topic=f"farm snapshot #{week + 1}-{offset}",
                    description="a cozy farm sim moment, a chaotic chicken beat, or a quiet Quest sunset",
                )
            for offset in range(7):
                created += _add_if_missing(
                    session,
                    content_type="engagement",
                    platform="twitter",
                    subreddit=None,
                    scheduled_date=datetime.combine(monday.date() + timedelta(days=offset), time(15, 0), tzinfo=UTC),
                    topic=f"cozy vr conversation prompt #{week + 1}-{offset + 1}",
                    description=None,
                )
        session.commit()
    return created


def _add_if_missing(session, *, content_type: str, platform: str, subreddit: str | None, scheduled_date: datetime, topic: str, description: str | None) -> int:
    existing = session.scalar(
        select(ContentCalendarItem).where(
            ContentCalendarItem.content_type == content_type,
            ContentCalendarItem.platform == platform,
            ContentCalendarItem.scheduled_date == scheduled_date,
        )
    )
    if existing is not None:
        return 0
    session.add(
        ContentCalendarItem(
            content_type=content_type,
            platform=platform,
            subreddit=subreddit,
            topic=topic,
            description=description,
            scheduled_date=scheduled_date,
            status="new",
        )
    )
    return 1


def _week_start(now: datetime) -> datetime:
    midnight = datetime.combine(now.date(), time.min, tzinfo=UTC)
    return midnight - timedelta(days=midnight.weekday())

