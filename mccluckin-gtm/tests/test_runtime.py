from __future__ import annotations

from runtime import PipelineRuntime


def test_runtime_disables_monitor_loop_when_reply_monitoring_is_off(session_factory, settings) -> None:
    runtime = PipelineRuntime(
        settings.model_copy(
            update={
                "x_bearer_token": "bearer-token",
                "x_oauth2_access_token": "oauth-token",
            }
        ),
        session_factory,
    )

    snapshot = runtime.snapshot()

    assert snapshot["tasks"]["monitor"]["enabled"] is False
    assert snapshot["tasks"]["draft"]["enabled"] is True
    assert snapshot["tasks"]["publish"]["enabled"] is True
