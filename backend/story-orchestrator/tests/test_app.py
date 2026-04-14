import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.main import create_app


class StoryOrchestratorAppTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            database_path=str(database_path),
        )
        self.client = TestClient(create_app(settings))

    def tearDown(self) -> None:
        self.client.close()
        self._temp_dir.cleanup()

    def test_health_returns_ok(self) -> None:
        response = self.client.get("/health")

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json(), {"status": "ok"})

    def test_create_and_fetch_story_job(self) -> None:
        create_response = self.client.post(
            "/api/v1/story-jobs",
            json={
                "story_brief": "Create a short intro-like episode about catching El Pollo Loco.",
                "package_template": "intro_chicken_sample",
                "target_scene": "Intro",
                "metadata": {"character": "mentor", "crop": "tomato"},
            },
        )

        self.assertEqual(create_response.status_code, 201)
        created = create_response.json()
        self.assertEqual(created["status"], "pending")
        self.assertEqual(created["target_scene"], "Intro")

        fetch_response = self.client.get(f"/api/v1/story-jobs/{created['job_id']}")

        self.assertEqual(fetch_response.status_code, 200)
        fetched = fetch_response.json()
        self.assertEqual(fetched["job_id"], created["job_id"])
        self.assertEqual(fetched["story_brief"], created["story_brief"])
        self.assertEqual(fetched["metadata"], {"character": "mentor", "crop": "tomato"})

    def test_unknown_job_returns_404(self) -> None:
        response = self.client.get("/api/v1/story-jobs/does-not-exist")

        self.assertEqual(response.status_code, 404)
        self.assertEqual(response.json()["detail"], "Story job not found.")

    def test_create_elevenlabs_tts_websocket_token_returns_single_use_token(self) -> None:
        captured: dict[str, str] = {}

        def fake_provider(api_key: str) -> str:
            captured["api_key"] = api_key
            return "sutkn_demo"

        self.client.app.state.elevenlabs_token_provider = fake_provider

        response = self.client.post("/api/v1/elevenlabs/tts-websocket-token")

        self.assertEqual(response.status_code, 200)
        self.assertEqual(
            response.json(),
            {"token": "sutkn_demo", "token_type": "tts_websocket"},
        )
        self.assertEqual(captured["api_key"], "test-eleven")

    def test_create_elevenlabs_tts_websocket_token_requires_api_key(self) -> None:
        database_path = Path(self._temp_dir.name) / "story_orchestrator_missing_voice.db"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="",
            database_path=str(database_path),
        )
        client = TestClient(create_app(settings))
        self.addCleanup(client.close)

        response = client.post("/api/v1/elevenlabs/tts-websocket-token")

        self.assertEqual(response.status_code, 503)
        self.assertEqual(
            response.json()["detail"],
            "ElevenLabs is not configured for the local story orchestrator.",
        )


if __name__ == "__main__":
    unittest.main()
