from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient

from web import build_web_app


class FakeRuntime:
    async def start(self) -> None:
        return None

    async def stop(self) -> None:
        return None

    def snapshot(self) -> dict[str, object]:
        return {
            "enabled": True,
            "tasks": {
                "monitor": {"enabled": True, "interval_seconds": 1800},
                "draft": {"enabled": True, "interval_seconds": 300},
                "publish": {"enabled": True, "interval_seconds": 300},
            },
        }


@pytest.mark.asyncio
async def test_web_app_exposes_health_and_runtime_status(session_factory, settings) -> None:
    app = build_web_app(settings=settings, session_factory=session_factory, runtime=FakeRuntime())
    transport = ASGITransport(app=app)

    async with AsyncClient(transport=transport, base_url="http://testserver") as client:
        health_response = await client.get("/health")
        runtime_response = await client.get("/api/runtime")

    assert health_response.status_code == 200
    assert health_response.json() == {
        "status": "ok",
        "app": settings.app_name,
    }

    assert runtime_response.status_code == 200
    payload = runtime_response.json()
    assert payload["enabled"] is True
    assert payload["tasks"]["publish"]["interval_seconds"] == 300
