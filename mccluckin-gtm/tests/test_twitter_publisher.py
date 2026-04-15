from __future__ import annotations

from types import SimpleNamespace

import pytest
from tweepy.errors import Unauthorized

from publisher.twitter import TwitterPublisher
from tests.conftest import create_draft, create_lead


class CapturingClient:
    def __init__(self, *, response_id: str = "tweet-1") -> None:
        self.calls: list[dict[str, object]] = []
        self.response_id = response_id

    def create_tweet(self, **kwargs):
        self.calls.append(kwargs)
        return SimpleNamespace(data={"id": self.response_id})


class UnauthorizedClient:
    def __init__(self) -> None:
        self.calls: list[dict[str, object]] = []

    def create_tweet(self, **kwargs):
        self.calls.append(kwargs)
        raise Unauthorized(FakeResponse(401, "Unauthorized"))


class FakeResponse:
    def __init__(self, status_code: int, text: str) -> None:
        self.status_code = status_code
        self.text = text
        self.reason = text
        self.headers: dict[str, str] = {}

    def json(self) -> dict[str, object]:
        return {"title": self.text}


class RecordingTokenStore:
    def __init__(self) -> None:
        self.persisted: list[tuple[str, str]] = []

    def persist(self, *, access_token: str, refresh_token: str) -> None:
        self.persisted.append((access_token, refresh_token))


def make_settings(**overrides):
    values = {
        "x_publish_enabled": True,
        "x_oauth2_access_token": "",
        "x_oauth2_refresh_token": "",
        "x_client_id": "",
        "x_client_secret": "",
        "x_api_key": "",
        "x_api_secret": "",
        "x_access_token": "",
        "x_access_token_secret": "",
    }
    values.update(overrides)
    return SimpleNamespace(**values)


def test_twitter_publisher_uses_oauth2_user_context_when_access_token_present(db_session) -> None:
    lead = create_lead(
        db_session,
        platform="twitter",
        platform_id="tweet-42",
        subreddit=None,
        status="approved",
    )
    draft = create_draft(db_session, lead=lead, reviewer_action="approved")
    client = CapturingClient(response_id="tweet-42-reply")

    publisher = TwitterPublisher(
        make_settings(x_oauth2_access_token="oauth2-access-token"),
        client=client,
    )

    response_id = publisher.publish(draft)

    assert response_id == "tweet-42-reply"
    assert client.calls == [
        {
            "text": draft.final_text,
            "in_reply_to_tweet_id": "tweet-42",
            "user_auth": False,
        }
    ]


def test_twitter_publisher_refreshes_oauth2_tokens_after_unauthorized(db_session) -> None:
    lead = create_lead(
        db_session,
        platform="twitter",
        platform_id="tweet-77",
        subreddit=None,
        status="approved",
    )
    draft = create_draft(db_session, lead=lead, reviewer_action="approved")
    expired_client = UnauthorizedClient()
    refreshed_client = CapturingClient(response_id="tweet-77-reply")
    token_store = RecordingTokenStore()
    refreshed_tokens: list[str] = []

    def fake_client_factory(access_token: str):
        if access_token == "expired-access":
            return expired_client
        if access_token == "fresh-access":
            return refreshed_client
        raise AssertionError(f"unexpected access token: {access_token}")

    def fake_refresher(*, settings, refresh_token: str):
        refreshed_tokens.append(refresh_token)
        return {
            "access_token": "fresh-access",
            "refresh_token": "fresh-refresh",
        }

    publisher = TwitterPublisher(
        make_settings(
            x_oauth2_access_token="expired-access",
            x_oauth2_refresh_token="stashed-refresh",
            x_client_id="client-id",
        ),
        oauth2_client_factory=fake_client_factory,
        token_refresher=fake_refresher,
        token_store=token_store,
    )

    response_id = publisher.publish(draft)

    assert response_id == "tweet-77-reply"
    assert refreshed_tokens == ["stashed-refresh"]
    assert expired_client.calls == [
        {
            "text": draft.final_text,
            "in_reply_to_tweet_id": "tweet-77",
            "user_auth": False,
        }
    ]
    assert refreshed_client.calls == [
        {
            "text": draft.final_text,
            "in_reply_to_tweet_id": "tweet-77",
            "user_auth": False,
        }
    ]
    assert token_store.persisted == [("fresh-access", "fresh-refresh")]
