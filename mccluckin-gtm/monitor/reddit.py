from __future__ import annotations

import logging
from datetime import UTC, datetime, timedelta

import asyncpraw
from sqlalchemy import select
from sqlalchemy.orm import sessionmaker

from config import Settings
from db.control import is_paused, log_api_call
from db.models import Lead

from .keywords import KEYWORDS, SUBREDDITS, find_matching_keywords

logger = logging.getLogger(__name__)


def build_reddit_client(settings: Settings) -> asyncpraw.Reddit:
    return asyncpraw.Reddit(
        client_id=settings.reddit_client_id,
        client_secret=settings.reddit_client_secret,
        username=settings.reddit_username,
        password=settings.reddit_password,
        user_agent=settings.reddit_user_agent,
    )


async def collect_reddit_leads(
    session_factory: sessionmaker,
    settings: Settings,
    *,
    reddit: asyncpraw.Reddit | None = None,
    now: datetime | None = None,
    subreddits: list[str] | None = None,
    keywords: list[str] | None = None,
) -> int:
    if reddit is None and not settings.reddit_enabled:
        logger.info("Reddit monitor disabled; missing Reddit app credentials.")
        return 0

    current_time = now or datetime.now(UTC)
    active_keywords = keywords or KEYWORDS
    active_subreddits = subreddits or SUBREDDITS
    client = reddit or build_reddit_client(settings)
    created = 0
    seen_ids: set[str] = set()

    try:
        for subreddit_name in active_subreddits:
            subreddit = await client.subreddit(subreddit_name)
            created += await _collect_search_results(
                session_factory,
                subreddit=subreddit,
                subreddit_name=subreddit_name,
                keywords=active_keywords,
                settings=settings,
                now=current_time,
                seen_ids=seen_ids,
            )
            created += await _collect_new_results(
                session_factory,
                subreddit=subreddit,
                subreddit_name=subreddit_name,
                keywords=active_keywords,
                settings=settings,
                now=current_time,
                seen_ids=seen_ids,
            )
    finally:
        if reddit is None:
            await client.close()
    return created


async def _collect_search_results(
    session_factory: sessionmaker,
    *,
    subreddit,
    subreddit_name: str,
    keywords: list[str],
    settings: Settings,
    now: datetime,
    seen_ids: set[str],
) -> int:
    created = 0
    for keyword in keywords:
        with session_factory() as session:
            if is_paused(session):
                logger.info("Monitor paused; skipping Reddit search cycle.")
                return created
            log_api_call(
                session,
                provider="reddit",
                action="subreddit.search",
                payload={"subreddit": subreddit_name, "keyword": keyword},
            )
        async for submission in subreddit.search(keyword, sort="new", time_filter="day", limit=10):
            created += _insert_submission_if_needed(
                session_factory,
                submission=submission,
                settings=settings,
                now=now,
                seen_ids=seen_ids,
                seed_keywords=[keyword],
            )
    return created


async def _collect_new_results(
    session_factory: sessionmaker,
    *,
    subreddit,
    subreddit_name: str,
    keywords: list[str],
    settings: Settings,
    now: datetime,
    seen_ids: set[str],
) -> int:
    with session_factory() as session:
        if is_paused(session):
            logger.info("Monitor paused; skipping Reddit new listing cycle.")
            return 0
        log_api_call(
            session,
            provider="reddit",
            action="subreddit.new",
            payload={"subreddit": subreddit_name, "limit": 25},
        )
    created = 0
    async for submission in subreddit.new(limit=25):
        created += _insert_submission_if_needed(
            session_factory,
            submission=submission,
            settings=settings,
            now=now,
            seen_ids=seen_ids,
            seed_keywords=keywords,
        )
    return created


def _insert_submission_if_needed(
    session_factory: sessionmaker,
    *,
    submission,
    settings: Settings,
    now: datetime,
    seen_ids: set[str],
    seed_keywords: list[str],
) -> int:
    if _is_old_submission(submission, now):
        return 0
    if _is_own_author(submission, settings.reddit_username):
        return 0

    text = " ".join(filter(None, [submission.title, submission.selftext]))
    matches = find_matching_keywords(text, seed_keywords)
    if not matches:
        return 0

    with session_factory() as session:
        existing = session.scalar(
            select(Lead).where(Lead.platform == "reddit", Lead.platform_id == submission.id)
        )
        if existing is not None:
            existing.matched_keywords = sorted(set(existing.matched_keywords + matches))
            session.commit()
            seen_ids.add(submission.id)
            return 0

        session.add(
            Lead(
                platform="reddit",
                platform_id=submission.id,
                subreddit=getattr(submission, "subreddit_name", None) or str(getattr(submission, "subreddit", "")),
                author=getattr(submission.author, "name", "[deleted]"),
                title=submission.title,
                body=submission.selftext or "",
                url=f"https://www.reddit.com{submission.permalink}",
                matched_keywords=sorted(set(matches)),
                status="new",
            )
        )
        session.commit()
    seen_ids.add(submission.id)
    logger.info("Stored Reddit lead %s", submission.id)
    return 1


def _is_old_submission(submission, now: datetime) -> bool:
    created_at = datetime.fromtimestamp(submission.created_utc, UTC)
    return created_at < now - timedelta(hours=48)


def _is_own_author(submission, username: str) -> bool:
    author_name = getattr(getattr(submission, "author", None), "name", "")
    return bool(username) and author_name.casefold() == username.casefold()
