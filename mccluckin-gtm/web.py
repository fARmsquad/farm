from __future__ import annotations

from contextlib import asynccontextmanager

from config import Settings, get_settings
from db.session import create_engine_from_settings, create_session_factory, ensure_parent_paths, init_db
from logging_utils import configure_logging
from publisher.reddit import RedditPublisher
from publisher.twitter import TwitterPublisher
from review.app import create_app
from runtime import PipelineRuntime

_AUTO_RUNTIME = object()


def build_web_app(
    *,
    settings: Settings | None = None,
    session_factory=None,
    runtime=_AUTO_RUNTIME,
):
    active_settings = settings or get_settings()
    active_session_factory = session_factory or _bootstrap_session_factory(active_settings)
    reddit, twitter = _build_publishers(active_settings)
    active_runtime = _resolve_runtime(runtime, active_settings, active_session_factory, reddit, twitter)
    lifespan = _runtime_lifespan(active_runtime)
    return create_app(
        active_session_factory,
        active_settings,
        reddit_publisher=reddit,
        twitter_publisher=twitter,
        lifespan=lifespan,
        runtime=active_runtime,
    )


def _bootstrap_session_factory(settings: Settings):
    ensure_parent_paths(settings)
    configure_logging(settings)
    engine = create_engine_from_settings(settings)
    init_db(engine)
    return create_session_factory(engine)


def _build_publishers(settings: Settings):
    reddit = RedditPublisher(settings) if settings.reddit_enabled else None
    twitter = TwitterPublisher(settings) if settings.x_publish_enabled else None
    return reddit, twitter


def _resolve_runtime(runtime, settings: Settings, session_factory, reddit, twitter):
    if runtime is not _AUTO_RUNTIME:
        return runtime
    if not settings.background_jobs_enabled:
        return None
    return PipelineRuntime(
        settings,
        session_factory,
        reddit_publisher=reddit,
        twitter_publisher=twitter,
    )


def _runtime_lifespan(runtime):
    @asynccontextmanager
    async def lifespan(_: object):
        await _maybe_call(runtime, "start")
        try:
            yield
        finally:
            await _maybe_call(runtime, "stop")

    return lifespan


async def _maybe_call(runtime, method_name: str) -> None:
    if runtime is None:
        return
    method = getattr(runtime, method_name, None)
    if method is None:
        return
    await method()


app = build_web_app()
