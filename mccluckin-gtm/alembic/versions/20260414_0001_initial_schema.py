"""Initial GTM schema."""

from __future__ import annotations

from alembic import op
import sqlalchemy as sa


revision = "20260414_0001"
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        "leads",
        sa.Column("id", sa.String(length=36), primary_key=True),
        sa.Column("platform", sa.String(length=20), nullable=False),
        sa.Column("platform_id", sa.String(length=100), nullable=False),
        sa.Column("subreddit", sa.String(length=100), nullable=True),
        sa.Column("author", sa.String(length=100), nullable=False),
        sa.Column("title", sa.String(length=300), nullable=True),
        sa.Column("body", sa.Text(), nullable=False),
        sa.Column("url", sa.Text(), nullable=False),
        sa.Column("matched_keywords", sa.JSON(), nullable=False),
        sa.Column("discovered_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("status", sa.String(length=20), nullable=False),
        sa.UniqueConstraint("platform", "platform_id", name="uq_leads_platform_platform_id"),
    )
    op.create_table(
        "content_calendar",
        sa.Column("id", sa.String(length=36), primary_key=True),
        sa.Column("content_type", sa.String(length=40), nullable=False),
        sa.Column("topic", sa.String(length=300), nullable=False),
        sa.Column("scheduled_date", sa.DateTime(timezone=True), nullable=False),
        sa.Column("platform", sa.String(length=20), nullable=False),
        sa.Column("subreddit", sa.String(length=100), nullable=True),
        sa.Column("description", sa.Text(), nullable=True),
        sa.Column("draft_text", sa.Text(), nullable=True),
        sa.Column("status", sa.String(length=20), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
    )
    op.create_table(
        "drafts",
        sa.Column("id", sa.String(length=36), primary_key=True),
        sa.Column("lead_id", sa.String(length=36), sa.ForeignKey("leads.id"), nullable=True),
        sa.Column(
            "content_calendar_id",
            sa.String(length=36),
            sa.ForeignKey("content_calendar.id"),
            nullable=True,
        ),
        sa.Column("draft_text", sa.Text(), nullable=False),
        sa.Column("model_used", sa.String(length=100), nullable=False),
        sa.Column("prompt_hash", sa.String(length=64), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("reviewed_at", sa.DateTime(timezone=True), nullable=True),
        sa.Column("reviewer_action", sa.String(length=20), nullable=True),
        sa.Column("edited_text", sa.Text(), nullable=True),
    )
    op.create_table(
        "published",
        sa.Column("id", sa.String(length=36), primary_key=True),
        sa.Column("draft_id", sa.String(length=36), sa.ForeignKey("drafts.id"), nullable=False),
        sa.Column("platform", sa.String(length=20), nullable=False),
        sa.Column("platform_response_id", sa.String(length=100), nullable=False),
        sa.Column("published_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("final_text", sa.Text(), nullable=False),
        sa.UniqueConstraint("draft_id", name="uq_published_draft_id"),
    )
    op.create_table(
        "daily_quotas",
        sa.Column("id", sa.String(length=36), primary_key=True),
        sa.Column("platform", sa.String(length=20), nullable=False),
        sa.Column("content_kind", sa.String(length=20), nullable=False),
        sa.Column("quota_date", sa.Date(), nullable=False),
        sa.Column("count", sa.Integer(), nullable=False),
        sa.UniqueConstraint("platform", "content_kind", "quota_date", name="uq_daily_quota"),
    )
    op.create_table(
        "control_flags",
        sa.Column("key", sa.String(length=100), primary_key=True),
        sa.Column("value", sa.Text(), nullable=False),
        sa.Column("updated_at", sa.DateTime(timezone=True), nullable=False),
    )
    op.create_table(
        "api_call_logs",
        sa.Column("id", sa.String(length=36), primary_key=True),
        sa.Column("provider", sa.String(length=30), nullable=False),
        sa.Column("action", sa.String(length=100), nullable=False),
        sa.Column("payload", sa.JSON(), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("succeeded", sa.Boolean(), nullable=False),
    )


def downgrade() -> None:
    op.drop_table("api_call_logs")
    op.drop_table("control_flags")
    op.drop_table("daily_quotas")
    op.drop_table("published")
    op.drop_table("drafts")
    op.drop_table("content_calendar")
    op.drop_table("leads")
