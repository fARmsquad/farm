from __future__ import annotations

from datetime import UTC, datetime, time, timedelta

from sqlalchemy import select
from sqlalchemy.orm import sessionmaker

from db.models import ContentCalendarItem

STANDALONE_PROMPTS = (
    {
        "topic": "Why Stardew's morning routine feels so good",
        "description": "A nuanced take on why the first few farm chores set the emotional pace for the whole day.",
    },
    {
        "topic": "Farm games are better when chickens are a little unruly",
        "description": "A wry observation about how a touch of chaos makes a farm feel alive instead of optimized.",
    },
    {
        "topic": "The best crop systems feel like rhythm, not homework",
        "description": "A thoughtful take on why watering, harvesting, and replanting should feel like ritual instead of busywork.",
    },
    {
        "topic": "Flower and honey loops make farms feel handmade",
        "description": "A grounded observation about why side systems like flowers, bees, and crafting create emotional texture.",
    },
    {
        "topic": "A good village matters as much as a good field",
        "description": "A take on why farm games get richer when neighbors and town routines pull against pure efficiency.",
    },
    {
        "topic": "Animal care works best when it changes your route",
        "description": "A specific point about how animals should reshape the daily loop instead of acting like passive generators.",
    },
    {
        "topic": "Cozy farm games live or die on pacing, not content volume",
        "description": "A thoughtful opinion about why too many tasks can flatten the feeling that made Stardew special.",
    },
)


def seed_content_calendar(session_factory: sessionmaker, *, weeks: int, now: datetime | None = None) -> int:
    current_time = now or datetime.now(UTC)
    created = 0
    with session_factory() as session:
        for week in range(weeks):
            monday = _week_start(current_time) + timedelta(weeks=week)
            for offset in range(7):
                prompt = STANDALONE_PROMPTS[(week * 7 + offset) % len(STANDALONE_PROMPTS)]
                created += _add_if_missing(
                    session,
                    content_type="engagement",
                    platform="twitter",
                    subreddit=None,
                    scheduled_date=datetime.combine(monday.date() + timedelta(days=offset), time(17, 0), tzinfo=UTC),
                    topic=prompt["topic"],
                    description=prompt["description"],
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
