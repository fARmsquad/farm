from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient

from datetime import UTC, datetime

from db.models import ApiCallLog, Lead, Published
from publisher.service import publish_ready_drafts
from review.app import create_app
from tests.conftest import create_draft, create_lead


class FakeTwitterPublisher:
    def __init__(self) -> None:
        self.calls: list[str] = []

    def publish(self, draft) -> str:
        self.calls.append(draft.final_text)
        return "tweet-123"


async def noop_sleep(_: float) -> None:
    return None


@pytest.mark.asyncio
async def test_review_api_can_approve_and_publish(session_factory, settings, now) -> None:
    with session_factory() as session:
        lead = create_lead(
            session,
            platform="twitter",
            platform_id="tweet-77",
            subreddit=None,
            author="vrfan",
            title=None,
            body="Anyone know an upcoming cozy VR farming game?",
            url="https://x.com/vrfan/status/tweet-77",
            status="drafted",
        )
        draft = create_draft(session, lead=lead, draft_text="If you're open to in-dev picks, McCluckin Farm is one to watch.", reviewer_action=None)

    app = create_app(session_factory, settings)
    transport = ASGITransport(app=app)

    async with AsyncClient(transport=transport, base_url="http://testserver") as client:
        queue_response = await client.get("/api/queue")
        assert queue_response.status_code == 200
        assert queue_response.json()["count"] == 1

        approve_response = await client.post(f"/api/queue/{draft.id}/approve")
        assert approve_response.status_code == 200
        assert approve_response.json()["reviewer_action"] == "approved"

        stats_response = await client.get("/api/stats")
        assert stats_response.status_code == 200
        assert stats_response.json()["approved"] == 1

    publisher = FakeTwitterPublisher()
    result = await publish_ready_drafts(
        session_factory,
        settings,
        twitter_publisher=publisher,
        sleep_async=noop_sleep,
        now=now,
    )

    with session_factory() as session:
        published = session.query(Published).all()

    assert result["published"] == 1
    assert published[0].platform_response_id == "tweet-123"
    assert publisher.calls == ["If you're open to in-dev picks, McCluckin Farm is one to watch."]


@pytest.mark.asyncio
async def test_dashboard_history_and_activity_show_skip_reasons_and_errors(session_factory, settings) -> None:
    with session_factory() as session:
        skipped = create_lead(
            session,
            platform="twitter",
            platform_id="tweet-skip",
            subreddit=None,
            author="anotherdev",
            title=None,
            body="My own game launch post",
            url="https://x.com/anotherdev/status/tweet-skip",
            status="skipped",
        )
        skipped.decision_note = "SKIP because this is another developer promoting their own game."
        drafted = create_lead(
            session,
            platform="twitter",
            platform_id="tweet-draft",
            subreddit=None,
            author="questfan",
            title=None,
            body="Any upcoming cozy VR games?",
            url="https://x.com/questfan/status/tweet-draft",
            status="drafted",
        )
        create_draft(
            session,
            lead=drafted,
            draft_text="If you're open to in-dev picks, McCluckin Farm is one to watch.",
        )
        session.add(
            ApiCallLog(
                provider="twitter",
                action="search_recent_tweets.error",
                payload={"query": '"cozy VR" -is:retweet', "error": "402 Payment Required"},
                succeeded=False,
                created_at=datetime(2026, 4, 14, 17, 30, tzinfo=UTC),
            )
        )
        session.commit()

    app = create_app(session_factory, settings)
    transport = ASGITransport(app=app)

    async with AsyncClient(transport=transport, base_url="http://testserver") as client:
        history_response = await client.get("/api/history")
        activity_response = await client.get("/api/activity")

    assert history_response.status_code == 200
    history = history_response.json()
    assert history["items"][0]["platform_id"] == "tweet-draft"
    assert history["items"][0]["draft"]["draft_text"].startswith("If you're open to in-dev picks")
    assert history["items"][1]["decision_note"].startswith("SKIP because")

    assert activity_response.status_code == 200
    activity = activity_response.json()
    assert activity["errors"][0]["provider"] == "twitter"
    assert "402 Payment Required" in activity["errors"][0]["payload"]["error"]

@pytest.mark.asyncio
async def test_review_api_lists_ready_drafts_and_can_publish_them(session_factory, settings, now) -> None:
    with session_factory() as session:
        lead = create_lead(
            session,
            platform="twitter",
            platform_id="tweet-ready",
            subreddit=None,
            author="farmfan",
            title=None,
            body="Stardew has me craving another good farm game.",
            url="https://x.com/farmfan/status/tweet-ready",
            status="approved",
        )
        create_draft(
            session,
            lead=lead,
            draft_text="The dangerous part is chasing the cozy feeling without flattening the chores. The rhythm matters more than the crop count.",
            reviewer_action="approved",
        )

    publisher = FakeTwitterPublisher()
    app = create_app(session_factory, settings, twitter_publisher=publisher)
    transport = ASGITransport(app=app)

    async with AsyncClient(transport=transport, base_url="http://testserver") as client:
        ready_response = await client.get("/api/ready")
        assert ready_response.status_code == 200
        payload = ready_response.json()
        assert payload["count"] == 1

        publish_response = await client.post(f"/api/ready/{payload['items'][0]['id']}/publish")
        assert publish_response.status_code == 200
        assert publish_response.json()["published"] is True

    with session_factory() as session:
        published = session.query(Published).all()

    assert publisher.calls == ["The dangerous part is chasing the cozy feeling without flattening the chores. The rhythm matters more than the crop count."]
    assert len(published) == 1
    assert published[0].platform_response_id == "tweet-123"

