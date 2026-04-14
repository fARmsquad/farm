from __future__ import annotations

import asyncio
import json
from datetime import datetime

import typer
import uvicorn
from sqlalchemy import func, select

from config import get_settings
from db.control import set_paused
from db.models import ContentCalendarItem, Draft, Lead, Published
from db.session import create_engine_from_settings, create_session_factory, ensure_parent_paths, init_db
from drafter.generate import draft_calendar_items, process_new_leads
from logging_utils import configure_logging
from monitor.reddit import collect_reddit_leads
from monitor.twitter import collect_twitter_leads
from publisher.reddit import RedditPublisher
from publisher.service import publish_ready_drafts
from publisher.twitter import TwitterPublisher
from review.app import create_app
from scheduler.content_calendar import seed_content_calendar

app = typer.Typer(help="McCluckin GTM automation")
monitor_app = typer.Typer(help="Monitoring commands")
calendar_app = typer.Typer(help="Content calendar commands")
app.add_typer(monitor_app, name="monitor")
app.add_typer(calendar_app, name="calendar")


def _bootstrap():
    settings = get_settings()
    ensure_parent_paths(settings)
    configure_logging(settings)
    engine = create_engine_from_settings(settings)
    init_db(engine)
    return settings, create_session_factory(engine)


@monitor_app.command("reddit")
def monitor_reddit(once: bool = typer.Option(False, "--once/--loop")) -> None:
    settings, session_factory = _bootstrap()
    if not settings.reddit_enabled:
        typer.echo("Reddit monitor disabled: missing Reddit app credentials.")
        return
    if once:
        asyncio.run(collect_reddit_leads(session_factory, settings))
        return
    while True:
        asyncio.run(collect_reddit_leads(session_factory, settings))
        asyncio.run(asyncio.sleep(1800))


@monitor_app.command("twitter")
def monitor_twitter(once: bool = typer.Option(False, "--once/--loop")) -> None:
    settings, session_factory = _bootstrap()
    if not settings.x_monitor_enabled:
        typer.echo("X monitor disabled: missing bearer token.")
        return
    if once:
        collect_twitter_leads(session_factory, settings)
        return
    while True:
        collect_twitter_leads(session_factory, settings)
        asyncio.run(asyncio.sleep(3600))


@monitor_app.command("run")
def monitor_run(once: bool = typer.Option(False, "--once/--loop")) -> None:
    settings, session_factory = _bootstrap()
    if once:
        if settings.reddit_enabled:
            asyncio.run(collect_reddit_leads(session_factory, settings))
        else:
            typer.echo("Skipping Reddit monitor: missing Reddit app credentials.")
        if settings.x_monitor_enabled:
            collect_twitter_leads(session_factory, settings)
        else:
            typer.echo("Skipping X monitor: missing bearer token.")
        return
    while True:
        if settings.reddit_enabled:
            asyncio.run(collect_reddit_leads(session_factory, settings))
        if settings.x_monitor_enabled:
            collect_twitter_leads(session_factory, settings)
        asyncio.run(asyncio.sleep(1800))


@app.command()
def draft() -> None:
    settings, session_factory = _bootstrap()
    lead_result = asyncio.run(process_new_leads(session_factory, settings))
    calendar_result = asyncio.run(draft_calendar_items(session_factory, settings))
    typer.echo(json.dumps({"leads": lead_result, "calendar": calendar_result}, indent=2))


@app.command()
def review(port: int | None = None) -> None:
    settings, session_factory = _bootstrap()
    app_instance = create_app(session_factory, settings)
    uvicorn.run(app_instance, host="127.0.0.1", port=port or settings.review_port)


@app.command()
def publish() -> None:
    settings, session_factory = _bootstrap()
    reddit = RedditPublisher(settings) if settings.reddit_enabled else None
    twitter = TwitterPublisher(settings) if settings.x_publish_enabled else None
    result = asyncio.run(
        publish_ready_drafts(
            session_factory,
            settings,
            reddit_publisher=reddit,
            twitter_publisher=twitter,
        )
    )
    typer.echo(json.dumps(result, indent=2))


@calendar_app.command("seed")
def calendar_seed(weeks: int = typer.Option(4, min=1)) -> None:
    _, session_factory = _bootstrap()
    created = seed_content_calendar(session_factory, weeks=weeks)
    typer.echo(f"Seeded {created} calendar items.")


@calendar_app.command("draft")
def calendar_draft() -> None:
    settings, session_factory = _bootstrap()
    typer.echo(json.dumps(asyncio.run(draft_calendar_items(session_factory, settings)), indent=2))


@app.command()
def stats() -> None:
    _, session_factory = _bootstrap()
    with session_factory() as session:
        payload = {
            "total_leads": session.scalar(select(func.count(Lead.id))) or 0,
            "drafts": session.scalar(select(func.count(Draft.id))) or 0,
            "published": session.scalar(select(func.count(Published.id))) or 0,
            "calendar_items": session.scalar(select(func.count(ContentCalendarItem.id))) or 0,
        }
    typer.echo(json.dumps(payload, indent=2))


@app.command("run-all")
def run_all() -> None:
    settings, session_factory = _bootstrap()
    if settings.reddit_enabled:
        asyncio.run(collect_reddit_leads(session_factory, settings))
    else:
        typer.echo("Skipping Reddit monitor: missing Reddit app credentials.")
    if settings.x_monitor_enabled:
        collect_twitter_leads(session_factory, settings)
    else:
        typer.echo("Skipping X monitor: missing bearer token.")
    asyncio.run(process_new_leads(session_factory, settings))
    asyncio.run(draft_calendar_items(session_factory, settings))
    result = asyncio.run(
        publish_ready_drafts(
            session_factory,
            settings,
            reddit_publisher=RedditPublisher(settings) if settings.reddit_enabled else None,
            twitter_publisher=TwitterPublisher(settings) if settings.x_publish_enabled else None,
        )
    )
    typer.echo(json.dumps(result, indent=2))


@app.command()
def pause() -> None:
    _, session_factory = _bootstrap()
    with session_factory() as session:
        set_paused(session, True)
    typer.echo("Pipeline paused.")


@app.command()
def resume() -> None:
    _, session_factory = _bootstrap()
    with session_factory() as session:
        set_paused(session, False)
    typer.echo("Pipeline resumed.")


if __name__ == "__main__":
    app()
