from __future__ import annotations

from datetime import timedelta

import pytest

from config import Settings
from db.models import ApiCallLog, ContentCalendarItem, DailyQuota, Draft, Lead, Published
from publisher.service import publish_ready_drafts
from tests.conftest import create_calendar_item, create_draft, create_lead


class FakeRedditPublisher:
    def __init__(self) -> None:
        self.calls: list[str] = []

    async def publish(self, draft: Draft) -> str:
        self.calls.append(draft.final_text)
        return "reddit-comment-1"


class FakeTwitterPublisher:
    def __init__(self) -> None:
        self.calls: list[str] = []

    def publish(self, draft: Draft) -> str:
        self.calls.append(draft.final_text)
        return "tweet-1"


async def noop_sleep(_: float) -> None:
    return None


@pytest.mark.asyncio
async def test_publish_ready_drafts_uses_edited_text_and_records_publication(session_factory, settings, now) -> None:
    reply_enabled_settings = settings.model_copy(update={"outbound_replies_enabled": True})

    with session_factory() as session:
        lead = create_lead(session, status="approved")
        create_draft(
            session,
            lead=lead,
            draft_text="Original text",
            reviewer_action="edited",
            edited_text="Edited text",
        )

    reddit = FakeRedditPublisher()

    result = await publish_ready_drafts(
        session_factory,
        reply_enabled_settings,
        reddit_publisher=reddit,
        sleep_async=noop_sleep,
        now=now,
    )

    with session_factory() as session:
        lead = session.query(Lead).filter_by(platform_id="lead-1").one()
        published = session.query(Published).all()
        quota = session.query(DailyQuota).filter_by(platform="reddit", content_kind="reply").one()

    assert result["published"] == 1
    assert reddit.calls == ["Edited text"]
    assert lead.status == "published"
    assert published[0].final_text == "Edited text"
    assert quota.count == 1


@pytest.mark.asyncio
async def test_publish_ready_drafts_blocks_subreddit_cooldown_and_daily_limits(session_factory, settings, now) -> None:
    reply_enabled_settings = settings.model_copy(
        update={
            "outbound_replies_enabled": True,
            "x_oauth2_access_token": "oauth-token",
        }
    )

    with session_factory() as session:
        first = create_lead(session, platform_id="lead-a", status="published")
        first_draft = create_draft(session, lead=first, reviewer_action="approved")
        session.add(
            Published(
                draft_id=first_draft.id,
                platform="reddit",
                platform_response_id="old-comment",
                final_text=first_draft.final_text,
                published_at=now - timedelta(hours=2),
            )
        )

        blocked = create_lead(session, platform_id="lead-b", status="approved")
        create_draft(session, lead=blocked, reviewer_action="approved", draft_text="Should be blocked")

        twitter_lead = create_lead(
            session,
            platform="twitter",
            platform_id="tweet-reply",
            subreddit=None,
            status="approved",
            url="https://x.com/vrfan/status/tweet-reply",
        )
        create_draft(session, lead=twitter_lead, reviewer_action="approved", draft_text="Twitter reply")
        session.add(DailyQuota(platform="twitter", content_kind="reply", quota_date=now.date(), count=settings.twitter_daily_reply_limit))
        session.commit()

    reddit = FakeRedditPublisher()
    twitter = FakeTwitterPublisher()

    result = await publish_ready_drafts(
        session_factory,
        reply_enabled_settings,
        reddit_publisher=reddit,
        twitter_publisher=twitter,
        sleep_async=noop_sleep,
        now=now,
    )

    with session_factory() as session:
        blocked = session.query(Lead).filter_by(platform_id="lead-b").one()
        twitter_lead = session.query(Lead).filter_by(platform_id="tweet-reply").one()

    assert result["published"] == 0
    assert reddit.calls == []
    assert twitter.calls == []
    assert blocked.status == "approved"
    assert twitter_lead.status == "approved"


@pytest.mark.asyncio
async def test_publish_ready_drafts_auto_publishes_engagement_tweet_when_enabled(session_factory, settings, now) -> None:
    auto_settings = Settings(
        **{
            **settings.model_dump(),
            "auto_publish": True,
        }
    )

    with session_factory() as session:
        item = create_calendar_item(session, status="drafted", scheduled_date=now - timedelta(minutes=5))
        create_draft(session, content_item=item, draft_text="How early do you get attached to a farm layout in VR?")

    twitter = FakeTwitterPublisher()

    result = await publish_ready_drafts(
        session_factory,
        auto_settings,
        twitter_publisher=twitter,
        sleep_async=noop_sleep,
        now=now,
    )

    with session_factory() as session:
        item = session.query(ContentCalendarItem).one()

    assert result["published"] == 1
    assert twitter.calls == ["How early do you get attached to a farm layout in VR?"]
    assert item.status == "published"


@pytest.mark.asyncio
async def test_publish_ready_drafts_skips_x_items_when_x_publishing_is_disabled(session_factory, settings, now) -> None:
    disabled = settings.model_copy(
        update={
            "auto_publish": True,
            "x_api_key": "",
            "x_api_secret": "",
            "x_access_token": "",
            "x_access_token_secret": "",
        }
    )

    with session_factory() as session:
        item = create_calendar_item(session, status="drafted", scheduled_date=now - timedelta(minutes=5))
        create_draft(session, content_item=item, draft_text="A queued engagement tweet")

    result = await publish_ready_drafts(
        session_factory,
        disabled,
        twitter_publisher=None,
        sleep_async=noop_sleep,
        now=now,
    )

    with session_factory() as session:
        item = session.query(ContentCalendarItem).one()

    assert result["published"] == 0
    assert item.status == "drafted"


@pytest.mark.asyncio
async def test_publish_ready_drafts_skips_terminal_x_reply_errors(session_factory, settings, now) -> None:
    reply_enabled_settings = settings.model_copy(
        update={
            "outbound_replies_enabled": True,
            "x_oauth2_access_token": "oauth-token",
        }
    )

    with session_factory() as session:
        lead = create_lead(
            session,
            platform="twitter",
            platform_id="tweet-blocked",
            subreddit=None,
            status="approved",
            url="https://x.com/farmfan/status/tweet-blocked",
        )
        draft = create_draft(session, lead=lead, reviewer_action="approved", draft_text="Blocked reply")
        session.add(
            ApiCallLog(
                provider="twitter",
                action="publish.error",
                payload={
                    "draft_id": draft.id,
                    "error": "403 Forbidden\nReply to this conversation is not allowed because you have not been mentioned or otherwise engaged by the author of the post you are replying to.",
                },
                succeeded=False,
            )
        )
        session.commit()

    twitter = FakeTwitterPublisher()

    result = await publish_ready_drafts(
        session_factory,
        reply_enabled_settings,
        twitter_publisher=twitter,
        sleep_async=noop_sleep,
        now=now,
    )

    assert result["published"] == 0
    assert twitter.calls == []


@pytest.mark.asyncio
async def test_publish_ready_drafts_skips_reply_drafts_when_replies_are_disabled(session_factory, settings, now) -> None:
    replyless_settings = settings.model_copy(
        update={
            "x_oauth2_access_token": "oauth-token",
        }
    )

    with session_factory() as session:
        lead = create_lead(
            session,
            platform="twitter",
            platform_id="reply-lead",
            subreddit=None,
            status="approved",
            url="https://x.com/farmfan/status/reply-lead",
        )
        create_draft(session, lead=lead, reviewer_action="approved", draft_text="A disabled reply.")

        item = create_calendar_item(
            session,
            content_type="engagement",
            status="approved",
            scheduled_date=now - timedelta(minutes=15),
            topic="A farm-game thought with a repo-star CTA",
        )
        create_draft(session, content_item=item, reviewer_action="approved", draft_text="A standalone post.")

    twitter = FakeTwitterPublisher()

    result = await publish_ready_drafts(
        session_factory,
        replyless_settings,
        twitter_publisher=twitter,
        sleep_async=noop_sleep,
        now=now,
    )

    with session_factory() as session:
        reply_lead = session.query(Lead).filter_by(platform_id="reply-lead").one()
        content_item = session.query(ContentCalendarItem).filter_by(topic="A farm-game thought with a repo-star CTA").one()
        published = session.query(Published).all()

    assert result["published"] == 1
    assert twitter.calls == ["A standalone post."]
    assert reply_lead.status == "approved"
    assert content_item.status == "published"
    assert len(published) == 1
