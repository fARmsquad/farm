from __future__ import annotations

import asyncpraw

from config import Settings


def build_reddit_client(settings: Settings) -> asyncpraw.Reddit:
    if not settings.reddit_enabled:
        raise ValueError("Reddit credentials are incomplete.")
    return asyncpraw.Reddit(
        client_id=settings.reddit_client_id,
        client_secret=settings.reddit_client_secret,
        username=settings.reddit_username,
        password=settings.reddit_password,
        user_agent=settings.reddit_user_agent,
    )


class RedditPublisher:
    def __init__(self, settings: Settings, reddit: asyncpraw.Reddit | None = None) -> None:
        self._settings = settings
        self._reddit = reddit or build_reddit_client(settings)

    async def publish(self, draft) -> str:
        submission = await self._reddit.submission(id=draft.lead.platform_id)
        comment = await submission.reply(draft.final_text)
        return comment.id

    async def account_age_days(self) -> int | None:
        me = await self._reddit.user.me()
        created_utc = getattr(me, "created_utc", None)
        if created_utc is None:
            return None
        from datetime import UTC, datetime

        created_at = datetime.fromtimestamp(created_utc, UTC)
        return max(0, (datetime.now(UTC) - created_at).days)

    async def close(self) -> None:
        await self._reddit.close()
