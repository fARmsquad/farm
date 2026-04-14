from __future__ import annotations

import asyncio
import inspect
import logging
import random
from datetime import UTC, datetime, timedelta

from sqlalchemy import func, select
from sqlalchemy.orm import joinedload, sessionmaker

from db.control import is_paused, log_api_call
from db.models import ContentCalendarItem, DailyQuota, Draft, Lead, Published

logger = logging.getLogger(__name__)


async def publish_ready_drafts(
    session_factory: sessionmaker,
    settings,
    *,
    reddit_publisher=None,
    twitter_publisher=None,
    sleep_async=asyncio.sleep,
    now: datetime | None = None,
) -> dict[str, int]:
    current_time = now or datetime.now(UTC)
    published = 0
    with session_factory() as session:
        if is_paused(session):
            logger.info("Publishing paused by kill switch.")
            return {"published": 0}

    for draft_id in _load_publishable_draft_ids(session_factory, settings):
        result = await publish_draft(
            session_factory,
            settings,
            draft_id,
            reddit_publisher=reddit_publisher,
            twitter_publisher=twitter_publisher,
            sleep_async=sleep_async,
            now=current_time,
            apply_delay=True,
        )
        if result["published"]:
            published += 1

    return {"published": published}


async def publish_draft(
    session_factory: sessionmaker,
    settings,
    draft_id: str,
    *,
    reddit_publisher=None,
    twitter_publisher=None,
    sleep_async=asyncio.sleep,
    now: datetime | None = None,
    apply_delay: bool = False,
) -> dict[str, object]:
    current_time = now or datetime.now(UTC)
    with session_factory() as session:
        if is_paused(session):
            logger.info("Publishing paused by kill switch.")
            return {"published": False, "reason": "paused", "draft_id": draft_id}

    draft = _load_draft(session_factory, draft_id)
    if draft is None:
        return {"published": False, "reason": "not_found", "draft_id": draft_id}
    if draft.publication is not None:
        return {"published": False, "reason": "already_published", "draft_id": draft_id}
    if not _eligible_for_publish(draft, settings):
        return {"published": False, "reason": "not_ready", "draft_id": draft_id}

    try:
        if draft.lead is not None:
            result = await _publish_lead_draft(
                session_factory,
                settings,
                draft,
                reddit_publisher=reddit_publisher,
                twitter_publisher=twitter_publisher,
                now=current_time,
            )
        else:
            result = await _publish_calendar_draft(
                session_factory,
                settings,
                draft,
                twitter_publisher=twitter_publisher,
                now=current_time,
            )
    except Exception as exc:  # pragma: no cover - exercised through callers
        logger.exception("Publish failed for draft %s", draft.id)
        provider = _draft_platform(draft)
        with session_factory() as session:
            log_api_call(
                session,
                provider=provider,
                action="publish.error",
                payload={"draft_id": draft.id, "error": str(exc)},
                succeeded=False,
            )
        return {
            "published": False,
            "reason": "error",
            "error": str(exc),
            "draft_id": draft.id,
            "platform": provider,
        }

    if result["published"] and apply_delay:
        await _sleep_after_publish(settings, draft, sleep_async)
    return result


def _load_publishable_draft_ids(session_factory: sessionmaker, settings) -> list[str]:
    with session_factory() as session:
        drafted_query = (
            select(Draft)
            .options(joinedload(Draft.lead), joinedload(Draft.content_item), joinedload(Draft.publication))
            .where(Draft.publication == None)  # noqa: E711
        )
        drafts = session.scalars(drafted_query.order_by(Draft.created_at)).all()
        return [draft.id for draft in drafts if _eligible_for_publish(draft, settings)]


def _load_draft(session_factory: sessionmaker, draft_id: str) -> Draft | None:
    with session_factory() as session:
        return session.scalar(
            select(Draft)
            .options(joinedload(Draft.lead), joinedload(Draft.content_item), joinedload(Draft.publication))
            .where(Draft.id == draft_id)
        )


def _eligible_for_publish(draft: Draft, settings) -> bool:
    if draft.reviewer_action in {"approved", "edited"}:
        return True
    item = draft.content_item
    return bool(
        settings.auto_publish
        and item is not None
        and item.platform == "twitter"
        and item.content_type == "engagement"
        and item.status == "drafted"
    )


async def _publish_lead_draft(
    session_factory,
    settings,
    draft: Draft,
    *,
    reddit_publisher,
    twitter_publisher,
    now: datetime,
) -> dict[str, object]:
    lead = draft.lead
    if lead is None:
        return {"published": False, "reason": "missing_lead", "draft_id": draft.id}

    if lead.platform == "reddit":
        if not await _can_publish_reddit(session_factory, draft, settings, reddit_publisher, now):
            return {"published": False, "reason": "guardrail", "draft_id": draft.id, "platform": "reddit"}
        response_id = await _invoke_publish(reddit_publisher, draft)
        await _record_publication(
            session_factory,
            draft_id=draft.id,
            platform="reddit",
            response_id=response_id,
            content_kind="reply",
            final_text=draft.final_text,
            now=now,
        )
        with session_factory() as session:
            log_api_call(
                session,
                provider="reddit",
                action="reply.publish.success",
                payload={"draft_id": draft.id, "lead_id": lead.id, "response_id": response_id, "final_text": draft.final_text},
            )
        return {"published": True, "draft_id": draft.id, "platform": "reddit", "response_id": response_id}

    if not _can_publish_twitter(
        session_factory,
        draft,
        settings,
        now,
        content_kind="reply",
        twitter_publisher=twitter_publisher,
    ):
        return {"published": False, "reason": "guardrail", "draft_id": draft.id, "platform": "twitter"}
    response_id = await _invoke_publish(twitter_publisher, draft)
    await _record_publication(
        session_factory,
        draft_id=draft.id,
        platform="twitter",
        response_id=response_id,
        content_kind="reply",
        final_text=draft.final_text,
        now=now,
    )
    with session_factory() as session:
        log_api_call(
            session,
            provider="twitter",
            action="reply.publish.success",
            payload={"draft_id": draft.id, "lead_id": lead.id, "response_id": response_id, "final_text": draft.final_text},
        )
    return {"published": True, "draft_id": draft.id, "platform": "twitter", "response_id": response_id}


async def _publish_calendar_draft(
    session_factory,
    settings,
    draft: Draft,
    *,
    twitter_publisher,
    now: datetime,
) -> dict[str, object]:
    item = draft.content_item
    if item is None:
        return {"published": False, "reason": "missing_content_item", "draft_id": draft.id}
    if not _can_publish_calendar_item(
        session_factory,
        draft,
        settings,
        now,
        twitter_publisher=twitter_publisher,
    ):
        return {"published": False, "reason": "guardrail", "draft_id": draft.id, "platform": item.platform}
    response_id = await _invoke_publish(twitter_publisher, draft)
    await _record_publication(
        session_factory,
        draft_id=draft.id,
        platform=item.platform,
        response_id=response_id,
        content_kind="post",
        final_text=draft.final_text,
        now=now,
    )
    with session_factory() as session:
        log_api_call(
            session,
            provider=item.platform,
            action="post.publish.success",
            payload={"draft_id": draft.id, "content_item_id": item.id, "response_id": response_id, "final_text": draft.final_text},
        )
    return {"published": True, "draft_id": draft.id, "platform": item.platform, "response_id": response_id}


async def _sleep_after_publish(settings, draft: Draft, sleep_async) -> None:
    if draft.lead is not None and draft.lead.platform == "reddit":
        delay_minutes = _delay_minutes(settings.reddit_publish_delay_min_minutes, settings.reddit_publish_delay_max_minutes)
    else:
        delay_minutes = _delay_minutes(settings.twitter_publish_delay_min_minutes, settings.twitter_publish_delay_max_minutes)
    await sleep_async(delay_minutes * 60)


async def _can_publish_reddit(session_factory, draft: Draft, settings, reddit_publisher, now: datetime) -> bool:
    if draft.lead is None:
        return False
    with session_factory() as session:
        if _quota_exhausted(session, platform="reddit", content_kind="reply", limit=settings.reddit_daily_reply_limit, now=now):
            return False
        if _already_replied(session, draft.lead):
            return False
        if _subreddit_on_cooldown(session, draft.lead, settings, now):
            return False
        log_api_call(session, provider="reddit", action="reply", payload={"draft_id": draft.id, "lead_id": draft.lead.id})
    if reddit_publisher is None:
        return False
    account_age = await _maybe_get_account_age_days(reddit_publisher)
    if account_age is None:
        return True
    return account_age >= settings.reddit_account_min_age_days


def _can_publish_twitter(
    session_factory,
    draft: Draft,
    settings,
    now: datetime,
    *,
    content_kind: str,
    twitter_publisher,
) -> bool:
    if draft.lead is None:
        return False
    if twitter_publisher is None or not getattr(settings, "x_publish_enabled", True):
        logger.info("Skipping X publish for draft %s; X publishing is disabled.", draft.id)
        return False
    with session_factory() as session:
        if _quota_exhausted(session, platform="twitter", content_kind=content_kind, limit=settings.twitter_daily_reply_limit, now=now):
            return False
        if _already_replied(session, draft.lead):
            return False
        log_api_call(session, provider="twitter", action="reply", payload={"draft_id": draft.id, "lead_id": draft.lead.id})
    return True


def _can_publish_calendar_item(session_factory, draft: Draft, settings, now: datetime, *, twitter_publisher) -> bool:
    item = draft.content_item
    scheduled_date = _as_utc(item.scheduled_date) if item is not None else None
    if item is None or item.platform != "twitter" or scheduled_date is None or now < scheduled_date:
        return False
    if twitter_publisher is None or not getattr(settings, "x_publish_enabled", True):
        logger.info("Skipping X calendar publish for draft %s; X publishing is disabled.", draft.id)
        return False
    with session_factory() as session:
        if _quota_exhausted(session, platform="twitter", content_kind="post", limit=settings.twitter_daily_post_limit, now=now):
            return False
        log_api_call(session, provider="twitter", action="post", payload={"draft_id": draft.id, "content_item_id": item.id})
    return True


async def _invoke_publish(publisher, draft: Draft) -> str:
    if publisher is None:
        raise RuntimeError("Publisher is required for live publishing.")
    result = publisher.publish(draft)
    if inspect.isawaitable(result):
        return await result
    return result


async def _record_publication(
    session_factory,
    *,
    draft_id: str,
    platform: str,
    response_id: str,
    content_kind: str,
    final_text: str,
    now: datetime,
) -> None:
    with session_factory() as session:
        draft = session.get(Draft, draft_id)
        session.add(
            Published(
                draft_id=draft_id,
                platform=platform,
                platform_response_id=response_id,
                final_text=final_text,
                published_at=now,
            )
        )
        _increment_quota(session, platform=platform, content_kind=content_kind, now=now)
        if draft.lead is not None:
            lead = session.get(Lead, draft.lead.id)
            lead.status = "published"
        if draft.content_item is not None:
            item = session.get(ContentCalendarItem, draft.content_item.id)
            item.status = "published"
        session.commit()


def _quota_exhausted(session, *, platform: str, content_kind: str, limit: int, now: datetime) -> bool:
    row = session.scalar(
        select(DailyQuota).where(
            DailyQuota.platform == platform,
            DailyQuota.content_kind == content_kind,
            DailyQuota.quota_date == now.date(),
        )
    )
    return row is not None and row.count >= limit


def _increment_quota(session, *, platform: str, content_kind: str, now: datetime) -> None:
    row = session.scalar(
        select(DailyQuota).where(
            DailyQuota.platform == platform,
            DailyQuota.content_kind == content_kind,
            DailyQuota.quota_date == now.date(),
        )
    )
    if row is None:
        row = DailyQuota(platform=platform, content_kind=content_kind, quota_date=now.date(), count=0)
        session.add(row)
    row.count += 1


def _already_replied(session, lead: Lead) -> bool:
    count = session.scalar(
        select(func.count(Published.id))
        .join(Draft, Draft.id == Published.draft_id)
        .where(Draft.lead_id == lead.id)
    )
    return bool(count)


def _subreddit_on_cooldown(session, lead: Lead, settings, now: datetime) -> bool:
    if not lead.subreddit:
        return False
    threshold = now - timedelta(hours=settings.reddit_subreddit_cooldown_hours)
    count = session.scalar(
        select(func.count(Published.id))
        .join(Draft, Draft.id == Published.draft_id)
        .join(Lead, Lead.id == Draft.lead_id)
        .where(
            Lead.platform == "reddit",
            Lead.subreddit == lead.subreddit,
            Published.published_at >= threshold,
        )
    )
    return bool(count)


def _delay_minutes(minimum: int, maximum: int) -> int:
    return random.randint(minimum, maximum)


def _as_utc(value: datetime) -> datetime:
    if value.tzinfo is None:
        return value.replace(tzinfo=UTC)
    return value.astimezone(UTC)


def _draft_platform(draft: Draft) -> str:
    if draft.lead is not None:
        return draft.lead.platform
    if draft.content_item is not None:
        return draft.content_item.platform
    return "unknown"


async def _maybe_get_account_age_days(reddit_publisher) -> int | None:
    getter = getattr(reddit_publisher, "get_account_age_days", None)
    if getter is None:
        return None
    result = getter()
    if inspect.isawaitable(result):
        return await result
    return result
