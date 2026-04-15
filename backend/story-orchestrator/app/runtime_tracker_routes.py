from __future__ import annotations

from pathlib import Path

from fastapi import FastAPI, HTTPException, Query
from fastapi.responses import HTMLResponse

from .runtime_models import RuntimeTrackerSessionDetail, RuntimeTrackerSessionSummary
from .runtime_service import RuntimeSessionService


def register_runtime_tracker_routes(
    app: FastAPI,
    *,
    runtime_service: RuntimeSessionService,
    tracker_page_path: Path,
) -> None:
    @app.get("/review/runtime-tracker", response_class=HTMLResponse)
    def get_runtime_tracker_page() -> HTMLResponse:
        if not tracker_page_path.exists():
            raise HTTPException(status_code=404, detail="Runtime tracker page not found.")
        return HTMLResponse(tracker_page_path.read_text(encoding="utf-8"))

    @app.get(
        "/api/runtime/v1/tracker/sessions",
        response_model=list[RuntimeTrackerSessionSummary],
    )
    def list_runtime_tracker_sessions(
        limit: int = Query(12, ge=1, le=50),
    ) -> list[RuntimeTrackerSessionSummary]:
        return runtime_service.list_tracker_sessions(limit)

    @app.get(
        "/api/runtime/v1/tracker/sessions/{session_id}",
        response_model=RuntimeTrackerSessionDetail,
    )
    def get_runtime_tracker_session(session_id: str) -> RuntimeTrackerSessionDetail:
        detail = runtime_service.get_tracker_session(session_id)
        if detail is None:
            raise HTTPException(status_code=404, detail="Runtime tracker session not found.")
        return detail
