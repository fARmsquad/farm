from __future__ import annotations

import asyncio
import hashlib
import logging
from anthropic import AsyncAnthropic
from sqlalchemy import select
from sqlalchemy.orm import sessionmaker

from config import Settings
from db.control import log_api_call
from db.models import ContentCalendarItem, Draft, Lead
from monitor.audience import has_target_audience_context, is_generic_devlog, normalize_candidate_text

SYSTEM_PROMPT = """You are helping with community engagement for McCluckin Farm, a cozy VR farming sim coming to Meta Quest.

About the game:
- Cozy VR farming sim with a mischievous twist (chickens cause chaos)
- Built in Unity for Meta Quest
- Features: farming, animal care, village exploration, day/night cycle
- Website: mccluckinfarm.com
- GitHub: github.com/fARmsquad/farm
- Status: In development, coming soon

Rules:
1. ONLY respond if the post is genuinely relevant to what we're building. If it's a stretch, return exactly: SKIP
2. Be a helpful community member first. Share genuine thoughts, answer questions, add value.
3. Mention McCluckin Farm naturally and only when it fits. Never lead with it.
4. Match platform tone: conversational on Reddit, punchy on Twitter/X.
5. On Reddit: write 2-4 sentences. Don't use bullet points. Sound like a real person.
6. On Twitter/X: keep under 280 chars. Be casual, distinct, and specific.
7. Never be salesy, never use marketing speak, never say "check out".
8. If someone is asking for existing or released games, don't pitch ours. Just be helpful. Only mention us if they're open to upcoming or in-development titles.
9. Don't use emojis on Reddit. Max 1-2 on Twitter/X.
10. Never claim features that aren't confirmed. Stick to what's on the website.
11. On Twitter/X, lead with a concrete observation, answer, or nuanced take tied to the exact post. If you cannot say something specific, return SKIP.
12. Avoid generic openers like "So true", "Nice work", or "That's awesome".
13. Use a light farmhand personality on Twitter/X when it fits: warm, wry, a little dusty-boots energy. Never force it.
14. Prefer textured opinions, mechanic comparisons, and thoughtful tradeoffs over compliments. Sound like someone who has spent muddy hours thinking about why farm games work.
15. Never default to phrases like "we're working on", "we're building", "there's something magical", or "hits different". If you mention McCluckin Farm, do it briefly and only after you've said something interesting.
16. Prioritize people talking about Stardew Valley, cozy farm games, farming-game mechanics, feature wishes, or recommendations. Generic developer workflow chatter is usually a skip.
17. Output only the exact reply text. Never explain your reasoning, never say things like "looking at this tweet", and never describe the task.
18. Most replies should work without mentioning McCluckin Farm. Mention it only when the person is open to recommendations, upcoming games, or a mechanic comparison where our game is genuinely relevant."""

logger = logging.getLogger(__name__)




def build_anthropic_client(settings: Settings) -> AsyncAnthropic:
    return AsyncAnthropic(api_key=settings.anthropic_api_key)


def build_user_prompt(lead: Lead) -> str:
    matched_context = ""
    if lead.matched_keywords:
        matched_context = f"\nMatched signals: {', '.join(lead.matched_keywords)}"
    if lead.platform == "reddit":
        platform_context = (
            f"Subreddit: r/{lead.subreddit}\n"
            f"Post title: {lead.title}\n"
            f"Post body: {lead.body}{matched_context}"
        )
    else:
        platform_context = f"Tweet by @{lead.author}: {lead.body}{matched_context}"
    return f"Platform: {lead.platform}\n{platform_context}\n\nDraft a reply."


def build_calendar_prompt(item: ContentCalendarItem) -> str:
    subreddit_context = f"\nTarget subreddit: r/{item.subreddit}" if item.subreddit else ""
    description = f"\nDescription: {item.description}" if item.description else ""
    return (
        f"Platform: {item.platform}\n"
        f"Content type: {item.content_type}{subreddit_context}\n"
        f"Topic: {item.topic}{description}\n\n"
        "Draft the post."
    )


async def process_new_leads(
    session_factory: sessionmaker,
    settings: Settings,
    *,
    client: AsyncAnthropic | None = None,
    delay_seconds: float | None = None,
) -> dict[str, int]:
    anthropic_client = client or build_anthropic_client(settings)
    processed = drafted = skipped = 0
    with session_factory() as session:
        leads = session.scalars(select(Lead).where(Lead.status == "new").order_by(Lead.discovered_at)).all()

    for lead in leads:
        processed += 1
        prefilter_reason = _prefilter_skip_reason(lead)
        if prefilter_reason is not None:
            with session_factory() as session:
                stored_lead = session.get(Lead, lead.id)
                stored_lead.status = "skipped"
                stored_lead.decision_note = prefilter_reason
                session.commit()
            skipped += 1
            continue
        text, prompt_hash, response_text = await _generate_lead_draft(
            session_factory,
            settings,
            anthropic_client,
            lead,
        )
        with session_factory() as session:
            stored_lead = session.get(Lead, lead.id)
            if text is None:
                stored_lead.status = "skipped"
                stored_lead.decision_note = _extract_skip_note(response_text)
                skipped += 1
            else:
                session.add(
                    Draft(
                        lead_id=stored_lead.id,
                        draft_text=text,
                        model_used=settings.anthropic_model,
                        prompt_hash=prompt_hash,
                    )
                )
                stored_lead.status = "drafted"
                stored_lead.decision_note = "Draft generated for review."
                drafted += 1
            session.commit()
        await asyncio.sleep(settings.draft_delay_seconds if delay_seconds is None else delay_seconds)
    return {"processed": processed, "drafted": drafted, "skipped": skipped}


async def draft_calendar_items(
    session_factory: sessionmaker,
    settings: Settings,
    *,
    client: AsyncAnthropic | None = None,
    delay_seconds: float | None = None,
) -> dict[str, int]:
    anthropic_client = client or build_anthropic_client(settings)
    drafted = 0
    with session_factory() as session:
        items = session.scalars(
            select(ContentCalendarItem).where(ContentCalendarItem.status == "new").order_by(ContentCalendarItem.scheduled_date)
        ).all()

    for item in items:
        text, prompt_hash = await _generate_calendar_draft(session_factory, settings, anthropic_client, item)
        if text is None:
            continue
        with session_factory() as session:
            stored = session.get(ContentCalendarItem, item.id)
            stored.draft_text = text
            stored.status = "drafted"
            session.add(
                Draft(
                    content_calendar_id=stored.id,
                    draft_text=text,
                    model_used=settings.anthropic_model,
                    prompt_hash=prompt_hash,
                )
            )
            session.commit()
        drafted += 1
        await asyncio.sleep(settings.draft_delay_seconds if delay_seconds is None else delay_seconds)
    return {"drafted": drafted}


async def _generate_lead_draft(session_factory, settings, client, lead: Lead) -> tuple[str | None, str, str]:
    user_prompt = build_user_prompt(lead)
    prompt_hash = _hash_prompt(SYSTEM_PROMPT, user_prompt)
    with session_factory() as session:
        log_api_call(
            session,
            provider="anthropic",
            action="messages.create",
            payload={"lead_id": lead.id, "platform": lead.platform},
        )
    response = await client.messages.create(
        model=settings.anthropic_model,
        max_tokens=300,
        system=SYSTEM_PROMPT,
        messages=[{"role": "user", "content": user_prompt}],
    )
    text = response.content[0].text.strip()
    is_skip = _is_skip_response(text)
    logger.info("Model evaluated lead %s: %s", lead.id, "skipped" if is_skip else "drafted")
    return (None if is_skip else text, prompt_hash, text)


async def _generate_calendar_draft(session_factory, settings, client, item: ContentCalendarItem) -> tuple[str | None, str]:
    user_prompt = build_calendar_prompt(item)
    prompt_hash = _hash_prompt(SYSTEM_PROMPT, user_prompt)
    with session_factory() as session:
        log_api_call(
            session,
            provider="anthropic",
            action="messages.create",
            payload={"content_calendar_id": item.id, "platform": item.platform},
        )
    response = await client.messages.create(
        model=settings.anthropic_model,
        max_tokens=300,
        system=SYSTEM_PROMPT,
        messages=[{"role": "user", "content": user_prompt}],
    )
    text = response.content[0].text.strip()
    return (None if _is_skip_response(text) else text, prompt_hash)


def _hash_prompt(system_prompt: str, user_prompt: str) -> str:
    return hashlib.sha256(f"{system_prompt}\n--\n{user_prompt}".encode("utf-8")).hexdigest()


def _is_skip_response(text: str) -> bool:
    return text.strip().upper().startswith("SKIP")


def _extract_skip_note(text: str) -> str:
    normalized = text.strip()
    if normalized.upper() == "SKIP":
        return "Skipped by model."
    remainder = normalized[4:].lstrip(" \n:-") if normalized.upper().startswith("SKIP") else normalized
    return remainder or "Skipped by model."


def _prefilter_skip_reason(lead: Lead) -> str | None:
    if lead.platform != "twitter":
        return None
    source_text = normalize_candidate_text(lead.body, lead.matched_keywords)
    if is_generic_devlog(source_text):
        return "Skipped by prefilter: generic developer build log."
    if has_target_audience_context(source_text):
        return None
    return "Skipped by prefilter: no cozy/farm/Stardew player context."
