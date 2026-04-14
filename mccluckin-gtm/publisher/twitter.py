from __future__ import annotations

import tweepy

from config import Settings


def build_twitter_client(settings: Settings) -> tweepy.Client:
    if not settings.x_publish_enabled:
        raise ValueError("X publishing credentials are incomplete.")
    return tweepy.Client(
        bearer_token=settings.x_bearer_token,
        consumer_key=settings.x_api_key,
        consumer_secret=settings.x_api_secret,
        access_token=settings.x_access_token,
        access_token_secret=settings.x_access_token_secret,
        wait_on_rate_limit=True,
    )


class TwitterPublisher:
    def __init__(self, settings: Settings, client: tweepy.Client | None = None) -> None:
        self._client = client or build_twitter_client(settings)

    def publish(self, draft) -> str:
        response = self._client.create_tweet(
            text=draft.final_text,
            in_reply_to_tweet_id=draft.lead.platform_id if draft.lead is not None else None,
        )
        return str(response.data["id"])
