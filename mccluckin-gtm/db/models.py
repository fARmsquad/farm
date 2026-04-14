from __future__ import annotations

from datetime import UTC, date, datetime
from typing import Any
from uuid import uuid4

from sqlalchemy import Boolean, Date, DateTime, ForeignKey, Integer, JSON, String, Text, UniqueConstraint
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, relationship


def utc_now() -> datetime:
    return datetime.now(UTC)


def generate_uuid() -> str:
    return str(uuid4())


class Base(DeclarativeBase):
    pass


class Lead(Base):
    __tablename__ = "leads"
    __table_args__ = (UniqueConstraint("platform", "platform_id", name="uq_leads_platform_platform_id"),)

    id: Mapped[str] = mapped_column(String(36), primary_key=True, default=generate_uuid)
    platform: Mapped[str] = mapped_column(String(20))
    platform_id: Mapped[str] = mapped_column(String(100))
    subreddit: Mapped[str | None] = mapped_column(String(100), nullable=True)
    author: Mapped[str] = mapped_column(String(100))
    title: Mapped[str | None] = mapped_column(String(300), nullable=True)
    body: Mapped[str] = mapped_column(Text)
    url: Mapped[str] = mapped_column(Text)
    matched_keywords: Mapped[list[str]] = mapped_column(JSON, default=list)
    discovered_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=utc_now)
    status: Mapped[str] = mapped_column(String(20), default="new")
    decision_note: Mapped[str | None] = mapped_column(Text, nullable=True)

    drafts: Mapped[list["Draft"]] = relationship(back_populates="lead")


class ContentCalendarItem(Base):
    __tablename__ = "content_calendar"

    id: Mapped[str] = mapped_column(String(36), primary_key=True, default=generate_uuid)
    content_type: Mapped[str] = mapped_column(String(40))
    topic: Mapped[str] = mapped_column(String(300))
    scheduled_date: Mapped[datetime] = mapped_column(DateTime(timezone=True))
    platform: Mapped[str] = mapped_column(String(20))
    subreddit: Mapped[str | None] = mapped_column(String(100), nullable=True)
    description: Mapped[str | None] = mapped_column(Text, nullable=True)
    draft_text: Mapped[str | None] = mapped_column(Text, nullable=True)
    status: Mapped[str] = mapped_column(String(20), default="new")
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=utc_now)

    drafts: Mapped[list["Draft"]] = relationship(back_populates="content_item")


class Draft(Base):
    __tablename__ = "drafts"

    id: Mapped[str] = mapped_column(String(36), primary_key=True, default=generate_uuid)
    lead_id: Mapped[str | None] = mapped_column(ForeignKey("leads.id"), nullable=True)
    content_calendar_id: Mapped[str | None] = mapped_column(
        ForeignKey("content_calendar.id"),
        nullable=True,
    )
    draft_text: Mapped[str] = mapped_column(Text)
    model_used: Mapped[str] = mapped_column(String(100))
    prompt_hash: Mapped[str] = mapped_column(String(64))
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=utc_now)
    reviewed_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    reviewer_action: Mapped[str | None] = mapped_column(String(20), nullable=True)
    edited_text: Mapped[str | None] = mapped_column(Text, nullable=True)

    lead: Mapped[Lead | None] = relationship(back_populates="drafts")
    content_item: Mapped[ContentCalendarItem | None] = relationship(back_populates="drafts")
    publication: Mapped["Published | None"] = relationship(back_populates="draft", uselist=False)

    @property
    def final_text(self) -> str:
        return (self.edited_text or self.draft_text).strip()


class Published(Base):
    __tablename__ = "published"
    __table_args__ = (UniqueConstraint("draft_id", name="uq_published_draft_id"),)

    id: Mapped[str] = mapped_column(String(36), primary_key=True, default=generate_uuid)
    draft_id: Mapped[str] = mapped_column(ForeignKey("drafts.id"))
    platform: Mapped[str] = mapped_column(String(20))
    platform_response_id: Mapped[str] = mapped_column(String(100))
    published_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=utc_now)
    final_text: Mapped[str] = mapped_column(Text)

    draft: Mapped[Draft] = relationship(back_populates="publication")


class DailyQuota(Base):
    __tablename__ = "daily_quotas"
    __table_args__ = (
        UniqueConstraint("platform", "content_kind", "quota_date", name="uq_daily_quota"),
    )

    id: Mapped[str] = mapped_column(String(36), primary_key=True, default=generate_uuid)
    platform: Mapped[str] = mapped_column(String(20))
    content_kind: Mapped[str] = mapped_column(String(20))
    quota_date: Mapped[date] = mapped_column(Date)
    count: Mapped[int] = mapped_column(Integer, default=0)


class ControlFlag(Base):
    __tablename__ = "control_flags"

    key: Mapped[str] = mapped_column(String(100), primary_key=True)
    value: Mapped[str] = mapped_column(Text, default="")
    updated_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=utc_now, onupdate=utc_now)


class ApiCallLog(Base):
    __tablename__ = "api_call_logs"

    id: Mapped[str] = mapped_column(String(36), primary_key=True, default=generate_uuid)
    provider: Mapped[str] = mapped_column(String(30))
    action: Mapped[str] = mapped_column(String(100))
    payload: Mapped[dict[str, Any]] = mapped_column(JSON, default=dict)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=utc_now)
    succeeded: Mapped[bool] = mapped_column(Boolean, default=True)
