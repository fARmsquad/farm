from __future__ import annotations

from pathlib import Path

import httpx
import tweepy
from dotenv import set_key
from tweepy.errors import HTTPException

from config import Settings


def build_twitter_client(settings: Settings) -> tweepy.Client:
    if settings.x_oauth2_publish_enabled:
        return build_twitter_oauth2_client(settings.x_oauth2_access_token)
    return build_twitter_oauth1_client(settings)


def build_twitter_oauth1_client(settings: Settings) -> tweepy.Client:
    if not settings.x_oauth1_publish_enabled:
        raise ValueError("X OAuth1 publishing credentials are incomplete.")
    return tweepy.Client(
        bearer_token=settings.x_bearer_token,
        consumer_key=settings.x_api_key,
        consumer_secret=settings.x_api_secret,
        access_token=settings.x_access_token,
        access_token_secret=settings.x_access_token_secret,
        wait_on_rate_limit=True,
    )


def build_twitter_oauth2_client(access_token: str) -> tweepy.Client:
    if not access_token.strip():
        raise ValueError("X OAuth2 access token is missing.")
    return tweepy.Client(access_token.strip(), wait_on_rate_limit=True)


def refresh_twitter_oauth2_tokens(
    *,
    settings: Settings,
    refresh_token: str,
    http_client: httpx.Client | None = None,
) -> dict[str, str]:
    if not settings.x_client_id.strip():
        raise ValueError("X OAuth2 token refresh requires X_CLIENT_ID.")
    request_data = {
        "refresh_token": refresh_token,
        "grant_type": "refresh_token",
    }
    auth = None
    if settings.x_client_secret.strip():
        auth = httpx.BasicAuth(settings.x_client_id, settings.x_client_secret)
    else:
        request_data["client_id"] = settings.x_client_id

    client = http_client or httpx.Client(timeout=30.0)
    try:
        response = client.post(
            "https://api.x.com/2/oauth2/token",
            data=request_data,
            headers={"Content-Type": "application/x-www-form-urlencoded"},
            auth=auth,
        )
        response.raise_for_status()
    finally:
        if http_client is None:
            client.close()

    payload = response.json()
    access_token = str(payload.get("access_token", "")).strip()
    if not access_token:
        raise ValueError("X OAuth2 token refresh did not return an access token.")
    return {
        "access_token": access_token,
        "refresh_token": str(payload.get("refresh_token") or refresh_token).strip(),
    }


class EnvTwitterTokenStore:
    def __init__(self, env_path: Path) -> None:
        self._env_path = env_path

    def persist(self, *, access_token: str, refresh_token: str) -> None:
        self._env_path.touch(exist_ok=True)
        set_key(str(self._env_path), "X_OAUTH2_ACCESS_TOKEN", access_token)
        set_key(str(self._env_path), "X_OAUTH2_REFRESH_TOKEN", refresh_token)


class TwitterPublisher:
    def __init__(
        self,
        settings: Settings,
        client: tweepy.Client | None = None,
        *,
        oauth2_client_factory=build_twitter_oauth2_client,
        token_refresher=refresh_twitter_oauth2_tokens,
        token_store=None,
    ) -> None:
        self._settings = settings
        self._oauth2_access_token = getattr(settings, "x_oauth2_access_token", "").strip()
        self._oauth2_refresh_token = getattr(settings, "x_oauth2_refresh_token", "").strip()
        self._oauth2_client_factory = oauth2_client_factory
        self._token_refresher = token_refresher
        env_file_path = getattr(settings, "env_file_path", Path(".env"))
        self._token_store = token_store or EnvTwitterTokenStore(env_file_path)
        self._use_oauth2 = bool(self._oauth2_access_token)
        if client is not None:
            self._client = client
        elif self._use_oauth2:
            self._client = self._oauth2_client_factory(self._oauth2_access_token)
        else:
            self._client = build_twitter_oauth1_client(settings)

    def publish(self, draft) -> str:
        if self._use_oauth2:
            response = self._publish_with_oauth2(draft)
        else:
            response = self._create_tweet(self._client, draft)
        return str(response.data["id"])

    def _publish_with_oauth2(self, draft):
        try:
            return self._create_tweet(self._client, draft, user_auth=False)
        except HTTPException as exc:
            if not _is_unauthorized(exc):
                raise
        self._refresh_oauth2_client()
        return self._create_tweet(self._client, draft, user_auth=False)

    def _refresh_oauth2_client(self) -> None:
        if not self._oauth2_refresh_token:
            raise ValueError("X OAuth2 access token expired and no refresh token is configured.")
        token_data = self._token_refresher(
            settings=self._settings,
            refresh_token=self._oauth2_refresh_token,
        )
        self._oauth2_access_token = token_data["access_token"].strip()
        self._oauth2_refresh_token = token_data["refresh_token"].strip()
        self._client = self._oauth2_client_factory(self._oauth2_access_token)
        self._token_store.persist(
            access_token=self._oauth2_access_token,
            refresh_token=self._oauth2_refresh_token,
        )

    def _create_tweet(self, client: tweepy.Client, draft, *, user_auth: bool | None = None):
        request = {
            "text": draft.final_text,
            "in_reply_to_tweet_id": draft.lead.platform_id if draft.lead is not None else None,
        }
        if user_auth is not None:
            request["user_auth"] = user_auth
        return client.create_tweet(**request)


def _is_unauthorized(exc: HTTPException) -> bool:
    return getattr(exc.response, "status_code", None) == 401
