from __future__ import annotations

from datetime import UTC, datetime
from pathlib import Path

from fastapi import FastAPI, HTTPException
from fastapi.responses import HTMLResponse
from pydantic import BaseModel, Field
from sqlalchemy import func, select
from sqlalchemy.orm import joinedload, sessionmaker

from config import Settings
from db.models import ApiCallLog, Draft, Lead, Published
from publisher.service import publish_draft


class DraftEditRequest(BaseModel):
    text: str = Field(min_length=1)


def create_app(
    session_factory: sessionmaker,
    settings: Settings,
    *,
    reddit_publisher=None,
    twitter_publisher=None,
) -> FastAPI:
    template_path = Path(__file__).resolve().parents[1] / "templates" / "index.html"
    app = FastAPI(title="McCluckin GTM Review", version="0.1.0")
    app.state.session_factory = session_factory
    app.state.settings = settings
    app.state.template_path = template_path
    app.state.reddit_publisher = reddit_publisher
    app.state.twitter_publisher = twitter_publisher

    @app.get("/", response_class=HTMLResponse)
    def index() -> HTMLResponse:
        if not template_path.exists():
            raise HTTPException(status_code=404, detail="Review UI not found.")
        return HTMLResponse(template_path.read_text(encoding="utf-8"))

    @app.get("/api/queue")
    def queue() -> dict[str, object]:
        with session_factory() as session:
            drafts = _pending_drafts(session)
            publish_errors = _latest_publish_errors(session, [draft.id for draft in drafts])
            return {
                "count": len(drafts),
                "items": [
                    _serialize_draft(draft, publish_error=publish_errors.get(draft.id))
                    for draft in drafts
                ],
            }

    @app.get("/api/ready")
    def ready() -> dict[str, object]:
        with session_factory() as session:
            drafts = _ready_drafts(session)
            publish_errors = _latest_publish_errors(session, [draft.id for draft in drafts])
            return {
                "count": len(drafts),
                "items": [
                    _serialize_draft(draft, publish_error=publish_errors.get(draft.id))
                    for draft in drafts
                ],
            }

    @app.get("/api/queue/{draft_id}")
    def queue_item(draft_id: str) -> dict[str, object]:
        with session_factory() as session:
            draft = _load_draft(session, draft_id)
            return _serialize_draft(draft)

    @app.post("/api/queue/{draft_id}/approve")
    async def approve(draft_id: str, publish: bool = False) -> dict[str, object]:
        return await _review_draft(
            session_factory,
            settings,
            draft_id,
            action="approved",
            text=None,
            publish=publish,
            reddit_publisher=reddit_publisher,
            twitter_publisher=twitter_publisher,
        )

    @app.post("/api/queue/{draft_id}/edit")
    async def edit(draft_id: str, request: DraftEditRequest, publish: bool = False) -> dict[str, object]:
        return await _review_draft(
            session_factory,
            settings,
            draft_id,
            action="edited",
            text=request.text,
            publish=publish,
            reddit_publisher=reddit_publisher,
            twitter_publisher=twitter_publisher,
        )

    @app.post("/api/queue/{draft_id}/reject")
    async def reject(draft_id: str) -> dict[str, object]:
        return await _review_draft(
            session_factory,
            settings,
            draft_id,
            action="rejected",
            text=None,
            publish=False,
            reddit_publisher=reddit_publisher,
            twitter_publisher=twitter_publisher,
        )

    @app.post("/api/ready/{draft_id}/publish")
    async def publish_ready_item(draft_id: str) -> dict[str, object]:
        result = await publish_draft(
            session_factory,
            settings,
            draft_id,
            reddit_publisher=reddit_publisher,
            twitter_publisher=twitter_publisher,
            apply_delay=False,
        )
        draft = _reload_draft_if_present(session_factory, draft_id)
        if draft is not None:
            result["draft"] = _serialize_draft(draft)
        return result

    @app.get("/api/stats")
    def stats() -> dict[str, object]:
        with session_factory() as session:
            total = session.scalar(select(func.count(Lead.id))) or 0
            drafted = session.scalar(select(func.count(Lead.id)).where(Lead.status == "drafted")) or 0
            approved = session.scalar(select(func.count(Lead.id)).where(Lead.status == "approved")) or 0
            published = session.scalar(select(func.count(Published.id))) or 0
            skipped = session.scalar(select(func.count(Lead.id)).where(Lead.status == "skipped")) or 0
            publish_errors = session.scalar(
                select(func.count(ApiCallLog.id)).where(
                    ApiCallLog.action == "publish.error",
                    ApiCallLog.succeeded == False,  # noqa: E712
                )
            ) or 0
            ready = session.scalar(
                select(func.count(Draft.id))
                .outerjoin(Published, Published.draft_id == Draft.id)
                .where(Draft.reviewer_action.in_(("approved", "edited")), Published.id == None)  # noqa: E711
            ) or 0
        skip_rate = 0 if total == 0 else round((skipped / total) * 100, 2)
        return {
            "total_leads": total,
            "drafted": drafted,
            "approved": approved,
            "published": published,
            "ready": ready,
            "publish_errors": publish_errors,
            "skip_rate": skip_rate,
        }

    @app.get("/api/history")
    def history(limit: int = 25) -> dict[str, object]:
        with session_factory() as session:
            leads = session.execute(
                select(Lead)
                .options(joinedload(Lead.drafts).joinedload(Draft.publication))
                .order_by(Lead.discovered_at.desc())
                .limit(limit)
            ).unique().scalars().all()
            latest_draft_ids = [
                latest_draft.id
                for latest_draft in (
                    max(lead.drafts, key=lambda item: item.created_at, default=None)
                    for lead in leads
                )
                if latest_draft is not None
            ]
            publish_errors = _latest_publish_errors(session, latest_draft_ids)
            return {
                "count": len(leads),
                "items": [
                    _serialize_history_item(
                        lead,
                        publish_error=publish_errors.get(
                            max(lead.drafts, key=lambda item: item.created_at, default=None).id
                        )
                        if lead.drafts
                        else None,
                    )
                    for lead in leads
                ],
            }

    @app.get("/api/activity")
    def activity(limit: int = 25) -> dict[str, object]:
        with session_factory() as session:
            errors = session.scalars(
                select(ApiCallLog)
                .where(ApiCallLog.succeeded == False)  # noqa: E712
                .order_by(ApiCallLog.created_at.desc())
                .limit(limit)
            ).all()
            recent = session.scalars(
                select(ApiCallLog).order_by(ApiCallLog.created_at.desc()).limit(limit)
            ).all()
            return {
                "errors": [_serialize_activity_item(item) for item in errors],
                "recent": [_serialize_activity_item(item) for item in recent],
            }

    return app


def _pending_drafts(session) -> list[Draft]:
    return session.scalars(
        select(Draft)
        .options(joinedload(Draft.lead), joinedload(Draft.content_item), joinedload(Draft.publication))
        .where(Draft.reviewer_action == None)  # noqa: E711
        .order_by(Draft.created_at)
    ).all()


def _ready_drafts(session) -> list[Draft]:
    return session.scalars(
        select(Draft)
        .options(joinedload(Draft.lead), joinedload(Draft.content_item), joinedload(Draft.publication))
        .outerjoin(Published, Published.draft_id == Draft.id)
        .where(Draft.reviewer_action.in_(("approved", "edited")), Published.id == None)  # noqa: E711
        .order_by(Draft.reviewed_at, Draft.created_at)
    ).all()


def _load_draft(session, draft_id: str) -> Draft:
    draft = session.scalar(
        select(Draft)
        .options(joinedload(Draft.lead), joinedload(Draft.content_item), joinedload(Draft.publication))
        .where(Draft.id == draft_id)
    )
    if draft is None:
        raise HTTPException(status_code=404, detail="Draft not found.")
    return draft


def _reload_draft_if_present(session_factory, draft_id: str) -> Draft | None:
    with session_factory() as session:
        return session.scalar(
            select(Draft)
            .options(joinedload(Draft.lead), joinedload(Draft.content_item), joinedload(Draft.publication))
            .where(Draft.id == draft_id)
        )


async def _review_draft(
    session_factory,
    settings: Settings,
    draft_id: str,
    *,
    action: str,
    text: str | None,
    publish: bool,
    reddit_publisher,
    twitter_publisher,
) -> dict[str, object]:
    with session_factory() as session:
        draft = _load_draft(session, draft_id)
        draft.reviewer_action = action
        draft.reviewed_at = datetime.now(UTC)
        if text is not None:
            draft.edited_text = text.strip()
        if draft.lead is not None:
            draft.lead.status = "approved" if action in {"approved", "edited"} else "skipped"
        if draft.content_item is not None:
            draft.content_item.status = "approved" if action in {"approved", "edited"} else "skipped"
        session.commit()

    publish_result = None
    if publish and action in {"approved", "edited"}:
        publish_result = await publish_draft(
            session_factory,
            settings,
            draft_id,
            reddit_publisher=reddit_publisher,
            twitter_publisher=twitter_publisher,
            apply_delay=False,
        )

    draft = _reload_draft_if_present(session_factory, draft_id)
    if draft is None:
        raise HTTPException(status_code=404, detail="Draft not found.")
    with session_factory() as session:
        publish_error = _latest_publish_errors(session, [draft.id]).get(draft.id)
    payload = _serialize_draft(draft, publish_error=publish_error)
    if publish_result is not None:
        payload["publish_result"] = publish_result
    return payload


def _serialize_draft(draft: Draft, *, publish_error: ApiCallLog | None = None) -> dict[str, object]:
    lead = draft.lead
    item = draft.content_item
    publication = draft.publication
    return {
        "id": draft.id,
        "draft_text": draft.draft_text,
        "final_text": draft.final_text,
        "model_used": draft.model_used,
        "created_at": draft.created_at.isoformat(),
        "reviewer_action": draft.reviewer_action,
        "publication": None
        if publication is None
        else {
            "id": publication.id,
            "platform": publication.platform,
            "platform_response_id": publication.platform_response_id,
            "published_at": publication.published_at.isoformat(),
        },
        "publish_error": _serialize_publish_error(publish_error),
        "lead": None
        if lead is None
        else {
            "id": lead.id,
            "platform": lead.platform,
            "platform_id": lead.platform_id,
            "author": lead.author,
            "subreddit": lead.subreddit,
            "title": lead.title,
            "body": lead.body,
            "url": lead.url,
            "matched_keywords": lead.matched_keywords,
            "status": lead.status,
            "decision_note": lead.decision_note,
        },
        "content_item": None
        if item is None
        else {
            "id": item.id,
            "content_type": item.content_type,
            "topic": item.topic,
            "platform": item.platform,
            "subreddit": item.subreddit,
            "scheduled_date": item.scheduled_date.isoformat(),
        },
    }


def _serialize_history_item(lead: Lead, *, publish_error: ApiCallLog | None = None) -> dict[str, object]:
    latest_draft = max(lead.drafts, key=lambda item: item.created_at, default=None)
    return {
        "id": lead.id,
        "platform": lead.platform,
        "platform_id": lead.platform_id,
        "author": lead.author,
        "subreddit": lead.subreddit,
        "title": lead.title,
        "body": lead.body,
        "url": lead.url,
        "matched_keywords": lead.matched_keywords,
        "status": lead.status,
        "decision_note": lead.decision_note,
        "discovered_at": lead.discovered_at.isoformat(),
        "draft": None
        if latest_draft is None
        else {
            "id": latest_draft.id,
            "draft_text": latest_draft.draft_text,
            "final_text": latest_draft.final_text,
            "reviewer_action": latest_draft.reviewer_action,
            "created_at": latest_draft.created_at.isoformat(),
            "publish_error": _serialize_publish_error(publish_error),
            "publication": None
            if latest_draft.publication is None
            else {
                "platform_response_id": latest_draft.publication.platform_response_id,
                "published_at": latest_draft.publication.published_at.isoformat(),
            },
        },
    }


def _serialize_activity_item(item: ApiCallLog) -> dict[str, object]:
    return {
        "id": item.id,
        "provider": item.provider,
        "action": item.action,
        "payload": item.payload,
        "succeeded": item.succeeded,
        "created_at": item.created_at.isoformat(),
    }


def _latest_publish_errors(session, draft_ids: list[str]) -> dict[str, ApiCallLog]:
    if not draft_ids:
        return {}
    draft_id_set = set(draft_ids)
    entries = session.scalars(
        select(ApiCallLog)
        .where(ApiCallLog.action == "publish.error", ApiCallLog.succeeded == False)  # noqa: E712
        .order_by(ApiCallLog.created_at.desc())
    ).all()
    errors: dict[str, ApiCallLog] = {}
    for entry in entries:
        payload = entry.payload or {}
        draft_id = payload.get("draft_id")
        if draft_id in draft_id_set and draft_id not in errors:
            errors[draft_id] = entry
    return errors


def _serialize_publish_error(item: ApiCallLog | None) -> dict[str, object] | None:
    if item is None:
        return None
    payload = item.payload or {}
    return {
        "error": payload.get("error", "Publish failed."),
        "created_at": item.created_at.isoformat(),
        "provider": item.provider,
        "action": item.action,
    }
