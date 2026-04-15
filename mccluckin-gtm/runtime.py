from __future__ import annotations

import asyncio
import logging
from dataclasses import dataclass
from datetime import UTC, datetime

from config import Settings
from drafter.generate import draft_calendar_items, process_new_leads
from monitor.reddit import collect_reddit_leads
from monitor.twitter import collect_twitter_leads
from publisher.service import publish_ready_drafts
from scheduler.content_calendar import seed_content_calendar

logger = logging.getLogger(__name__)


def _utc_now_iso() -> str:
    return datetime.now(UTC).isoformat()


@dataclass
class LoopState:
    interval_seconds: int
    enabled: bool
    running: bool = False
    last_started_at: str | None = None
    last_finished_at: str | None = None
    last_result: object | None = None
    last_error: str | None = None

    def mark_started(self) -> None:
        self.running = True
        self.last_started_at = _utc_now_iso()

    def mark_finished(self, result: object) -> None:
        self.running = False
        self.last_finished_at = _utc_now_iso()
        self.last_result = result
        self.last_error = None

    def mark_failed(self, error: str) -> None:
        self.running = False
        self.last_finished_at = _utc_now_iso()
        self.last_error = error

    def snapshot(self) -> dict[str, object]:
        return {
            "enabled": self.enabled,
            "interval_seconds": self.interval_seconds,
            "running": self.running,
            "last_started_at": self.last_started_at,
            "last_finished_at": self.last_finished_at,
            "last_result": self.last_result,
            "last_error": self.last_error,
        }


class PipelineRuntime:
    def __init__(
        self,
        settings: Settings,
        session_factory,
        *,
        reddit_publisher=None,
        twitter_publisher=None,
    ) -> None:
        self._settings = settings
        self._session_factory = session_factory
        self._reddit_publisher = reddit_publisher
        self._twitter_publisher = twitter_publisher
        self._stop_event = asyncio.Event()
        self._tasks: dict[str, asyncio.Task[None]] = {}
        self._states = {
            "monitor": LoopState(
                interval_seconds=settings.monitor_interval_seconds,
                enabled=settings.reply_monitor_enabled and (settings.reddit_enabled or settings.x_monitor_enabled),
            ),
            "draft": LoopState(
                interval_seconds=settings.draft_interval_seconds,
                enabled=True,
            ),
            "publish": LoopState(
                interval_seconds=settings.publish_interval_seconds,
                enabled=settings.reddit_enabled or settings.x_publish_enabled,
            ),
        }

    async def start(self) -> None:
        if not self._settings.background_jobs_enabled or self._tasks:
            return
        self._stop_event.clear()
        for name, state in self._states.items():
            if state.enabled:
                self._tasks[name] = asyncio.create_task(self._run_loop(name), name=f"gtm-{name}")

    async def stop(self) -> None:
        if not self._tasks:
            return
        self._stop_event.set()
        for task in self._tasks.values():
            task.cancel()
        await asyncio.gather(*self._tasks.values(), return_exceptions=True)
        self._tasks.clear()

    def snapshot(self) -> dict[str, object]:
        return {
            "enabled": self._settings.background_jobs_enabled,
            "tasks": {name: state.snapshot() for name, state in self._states.items()},
        }

    async def _run_loop(self, name: str) -> None:
        state = self._states[name]
        while not self._stop_event.is_set():
            await self._run_once(name, state)
            if await self._stop_requested(state.interval_seconds):
                return

    async def _run_once(self, name: str, state: LoopState) -> None:
        state.mark_started()
        try:
            result = await self._runner(name)()
        except asyncio.CancelledError:
            state.running = False
            raise
        except Exception as exc:  # pragma: no cover - exercised in live runtime
            logger.exception("GTM %s cycle failed", name)
            state.mark_failed(str(exc))
            return
        state.mark_finished(result)

    async def _stop_requested(self, timeout: int) -> bool:
        try:
            await asyncio.wait_for(self._stop_event.wait(), timeout=timeout)
        except asyncio.TimeoutError:
            return False
        return True

    def _runner(self, name: str):
        if name == "monitor":
            return self._run_monitor_cycle
        if name == "draft":
            return self._run_draft_cycle
        return self._run_publish_cycle

    async def _run_monitor_cycle(self) -> dict[str, int]:
        result = {"reddit": 0, "twitter": 0}
        if self._settings.reddit_enabled:
            result["reddit"] = await collect_reddit_leads(self._session_factory, self._settings)
        if self._settings.x_monitor_enabled:
            result["twitter"] = await asyncio.to_thread(
                collect_twitter_leads,
                self._session_factory,
                self._settings,
            )
        return result

    async def _run_draft_cycle(self) -> dict[str, object]:
        seeded = 0
        if self._settings.standalone_calendar_enabled:
            seeded = await asyncio.to_thread(
                seed_content_calendar,
                self._session_factory,
                weeks=self._settings.default_seed_weeks,
            )
        lead_result = {"processed": 0, "drafted": 0, "skipped": 0}
        if self._settings.outbound_replies_enabled:
            lead_result = await process_new_leads(self._session_factory, self._settings)
        calendar_result = {"drafted": 0}
        if self._settings.standalone_calendar_enabled:
            calendar_result = await draft_calendar_items(self._session_factory, self._settings)
        return {"seeded": seeded, "leads": lead_result, "calendar": calendar_result}

    async def _run_publish_cycle(self) -> dict[str, int]:
        return await publish_ready_drafts(
            self._session_factory,
            self._settings,
            reddit_publisher=self._reddit_publisher,
            twitter_publisher=self._twitter_publisher,
        )
