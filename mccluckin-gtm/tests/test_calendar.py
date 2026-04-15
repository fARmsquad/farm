from __future__ import annotations

from db.models import ContentCalendarItem
from scheduler.content_calendar import seed_content_calendar


def test_seed_content_calendar_creates_daily_standalone_x_posts(session_factory, now) -> None:
    created = seed_content_calendar(session_factory, weeks=2, now=now)

    with session_factory() as session:
        items = session.query(ContentCalendarItem).order_by(ContentCalendarItem.scheduled_date).all()

    assert created == 14
    assert len(items) == 14
    assert {item.platform for item in items} == {"twitter"}
    assert {item.content_type for item in items} == {"engagement"}
    assert all(item.subreddit is None for item in items)
    assert all(item.status == "new" for item in items)
    assert any("stardew" in item.topic.lower() for item in items)
    assert all(item.description for item in items)
