from __future__ import annotations

import logging
from datetime import UTC, datetime
from hashlib import sha1

import tweepy
from tweepy.errors import HTTPException
from sqlalchemy import select
from sqlalchemy.orm import sessionmaker

from config import Settings
from db.control import get_flag, is_paused, log_api_call, set_flag
from db.models import Lead

from .audience import should_store_twitter_lead
from .keywords import QUERIES, TWITTER_QUERY_MAX_RESULTS

logger = logging.getLogger(__name__)


def build_twitter_client(settings: Settings) -> tweepy.Client:
    return tweepy.Client(
        bearer_token=settings.x_bearer_token,
        consumer_key=settings.x_api_key,
        consumer_secret=settings.x_api_secret,
        access_token=settings.x_access_token,
        access_token_secret=settings.x_access_token_secret,
        wait_on_rate_limit=True,
    )


def collect_twitter_leads(
    session_factory: sessionmaker,
    settings: Settings,
    *,
    client: tweepy.Client | None = None,
    now: datetime | None = None,
    queries: list[str] | None = None,
) -> int:
    if client is None and not settings.x_monitor_enabled:
        logger.info("X monitor disabled; missing bearer token.")
        return 0

    _ = now or datetime.now(UTC)
    twitter_client = client or build_twitter_client(settings)
    created = 0
    for query in queries or QUERIES:
        created += _collect_query(session_factory, settings, twitter_client, query)
    return created


def _collect_query(
    session_factory: sessionmaker,
    settings: Settings,
    client: tweepy.Client,
    query: str,
) -> int:
    with session_factory() as session:
        if is_paused(session):
            logger.info("Monitor paused; skipping X search cycle.")
            return 0
        since_id = get_flag(session, _query_state_key(query), "")
        log_api_call(
            session,
            provider="twitter",
            action="search_recent_tweets",
            payload={"query": query, "since_id": since_id},
        )

    try:
        response = client.search_recent_tweets(
            query=query,
            since_id=since_id or None,
            max_results=TWITTER_QUERY_MAX_RESULTS,
            expansions=["author_id"],
            tweet_fields=["created_at", "author_id"],
            user_fields=["username"],
        )
    except HTTPException as exc:
        logger.warning("X recent search failed for query %r: %s", query, exc)
        with session_factory() as session:
            log_api_call(
                session,
                provider="twitter",
                action="search_recent_tweets.error",
                payload={"query": query, "error": str(exc)},
                succeeded=False,
            )
        return 0
    users = _build_user_lookup(response)

    created = 0
    for tweet in response.data or []:
        username = users.get(str(tweet.author_id), "")
        if _is_own_tweet(username, settings.x_username):
            continue
        if not should_store_twitter_lead(tweet.text, [query]):
            continue
        created += _insert_tweet_if_needed(
            session_factory,
            tweet=tweet,
            username=username or "unknown",
            query=query,
        )

    newest_id = getattr(response, "meta", {}).get("newest_id")
    if newest_id:
        with session_factory() as session:
            set_flag(session, _query_state_key(query), str(newest_id))
    return created


def _insert_tweet_if_needed(session_factory: sessionmaker, *, tweet, username: str, query: str) -> int:
    with session_factory() as session:
        existing = session.scalar(
            select(Lead).where(Lead.platform == "twitter", Lead.platform_id == str(tweet.id))
        )
        if existing is not None:
            return 0

        session.add(
            Lead(
                platform="twitter",
                platform_id=str(tweet.id),
                subreddit=None,
                author=username,
                title=None,
                body=tweet.text,
                url=f"https://x.com/{username}/status/{tweet.id}",
                matched_keywords=[query],
                status="new",
            )
        )
        session.commit()
    logger.info("Stored X lead %s", tweet.id)
    return 1


def _query_state_key(query: str) -> str:
    return f"twitter_since::{sha1(query.encode('utf-8')).hexdigest()}"


def _build_user_lookup(response) -> dict[str, str]:
    includes = getattr(response, "includes", {}) or {}
    users = includes.get("users") or []
    lookup: dict[str, str] = {}
    for user in users:
        user_id = _get_user_field(user, "id")
        username = _get_user_field(user, "username")
        if user_id and username:
            lookup[str(user_id)] = str(username)
    return lookup


def _get_user_field(user, field: str) -> str | None:
    if isinstance(user, dict):
        value = user.get(field)
    else:
        value = getattr(user, field, None)
    if value in (None, ""):
        return None
    return str(value)


def _is_own_tweet(author: str, username: str) -> bool:
    return bool(username) and author.casefold() == username.casefold()
