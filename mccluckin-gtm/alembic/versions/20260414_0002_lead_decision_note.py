"""Add lead decision note."""

from __future__ import annotations

from alembic import op
import sqlalchemy as sa


revision = "20260414_0002"
down_revision = "20260414_0001"
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.add_column("leads", sa.Column("decision_note", sa.Text(), nullable=True))


def downgrade() -> None:
    op.drop_column("leads", "decision_note")
