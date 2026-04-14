import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.generated_minigames import GeneratedMinigameBeatService
from app.generated_package_assembly import GeneratedPackageAssemblyService
from app.generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset
from app.generated_storyboards import GeneratedStoryboardService, TemplateStoryboardPlanner
from app.main import create_app
from app.story_sequence_models import StorySequenceSessionCreateRequest
from app.story_sequence_service import StorySequenceSessionService
from app.story_sequence_store import StorySequenceSessionStore


class StorySequenceSessionServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.storyboard_service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
            default_voice_id="voice-test",
        )
        self.minigame_service = GeneratedMinigameBeatService(package_output_path=self.package_output_path)
        self.package_assembly_service = GeneratedPackageAssemblyService(
            minigame_service=self.minigame_service,
            storyboard_service=self.storyboard_service,
        )
        self.store = StorySequenceSessionStore(self.database_path)
        self.service = StorySequenceSessionService(
            package_assembly_service=self.package_assembly_service,
            store=self.store,
            default_voice_id="voice-test",
        )

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_create_session_and_advance_persists_first_turn(self) -> None:
        created = self.service.create_session(StorySequenceSessionCreateRequest())

        self.assertEqual(created.status, "active")
        self.assertEqual(created.state.beat_cursor, 0)
        self.assertEqual(created.package_id, "storypkg_intro_chicken_sample")

        advanced = self.service.advance_session(created.session_id)

        self.assertEqual(advanced.session.state.beat_cursor, 1)
        self.assertEqual(advanced.turn.turn_index, 0)
        self.assertEqual(advanced.turn.generator_id, "plant_rows_v1")
        self.assertEqual(advanced.turn.character_name, "Old Garrett")
        self.assertTrue(advanced.turn.result.is_valid, advanced.turn.result.errors)
        self.assertIn("Plant", advanced.turn.summary)

        detail = self.service.get_session_detail(created.session_id)
        self.assertIsNotNone(detail)
        assert detail is not None
        self.assertEqual(detail.session.state.beat_cursor, 1)
        self.assertEqual(len(detail.turns), 1)
        self.assertEqual(detail.turns[0].generator_id, "plant_rows_v1")
        self.assertIn("tomatoes_unlocked", detail.session.state.world_state)

    def test_advance_session_avoids_immediate_generator_repeat(self) -> None:
        created = self.service.create_session(StorySequenceSessionCreateRequest())

        first = self.service.advance_session(created.session_id)
        second = self.service.advance_session(created.session_id)

        self.assertNotEqual(first.turn.generator_id, second.turn.generator_id)

        detail = self.service.get_session_detail(created.session_id)
        assert detail is not None
        self.assertEqual(
            [turn.generator_id for turn in detail.turns],
            ["plant_rows_v1", "find_tools_cluster_v1"],
        )

    def test_advance_session_persists_and_reuses_continuity_images(self) -> None:
        created = self.service.create_session(
            StorySequenceSessionCreateRequest(character_pool=["Old Garrett"])
        )

        first = self.service.advance_session(created.session_id)
        detail_after_first = self.service.get_session_detail(created.session_id)

        assert detail_after_first is not None
        continuity_images = detail_after_first.session.state.continuity_images
        self.assertEqual(len(continuity_images), 3)
        self.assertTrue(all(item.character_name == "Old Garrett" for item in continuity_images))
        self.assertEqual(
            [item.output_path for item in continuity_images],
            [
                str((self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "sequence_turn_000_cutscene" / "shot_01.png").resolve()),
                str((self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "sequence_turn_000_cutscene" / "shot_02.png").resolve()),
                str((self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "sequence_turn_000_cutscene" / "shot_03.png").resolve()),
            ],
        )

        second = self.service.advance_session(created.session_id)

        self.assertEqual(second.turn.character_name, "Old Garrett")
        self.assertEqual(
            second.turn.request.cutscene.reference_image_paths,
            [continuity_images[2].output_path, continuity_images[1].output_path],
        )


class StorySequenceSessionEndpointTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            database_path=str(database_path),
            elevenlabs_voice_id="voice-test",
        )
        storyboard_service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
            default_voice_id="voice-test",
        )
        minigame_service = GeneratedMinigameBeatService(package_output_path=package_output_path)
        self.client = TestClient(
            create_app(
                settings,
                generated_storyboard_service=storyboard_service,
                generated_minigame_service=minigame_service,
            )
        )

    def tearDown(self) -> None:
        self.client.close()
        self._temp_dir.cleanup()

    def test_story_sequence_session_api_flow(self) -> None:
        create_response = self.client.post("/api/v1/story-sequence-sessions", json={})

        self.assertEqual(create_response.status_code, 201)
        created = create_response.json()
        self.assertEqual(created["status"], "active")

        advance_response = self.client.post(
            f"/api/v1/story-sequence-sessions/{created['session_id']}/next-turn"
        )

        self.assertEqual(advance_response.status_code, 200)
        advanced = advance_response.json()
        self.assertEqual(advanced["turn"]["turn_index"], 0)
        self.assertEqual(advanced["turn"]["generator_id"], "plant_rows_v1")
        self.assertTrue(advanced["turn"]["result"]["is_valid"])
        self.assertEqual(advanced["session"]["state"]["beat_cursor"], 1)

        fetch_response = self.client.get(
            f"/api/v1/story-sequence-sessions/{created['session_id']}"
        )

        self.assertEqual(fetch_response.status_code, 200)
        detail = fetch_response.json()
        self.assertEqual(detail["session"]["session_id"], created["session_id"])
        self.assertEqual(detail["session"]["state"]["beat_cursor"], 1)
        self.assertEqual(len(detail["session"]["state"]["continuity_images"]), 3)
        self.assertEqual(detail["session"]["state"]["continuity_images"][0]["character_name"], "Old Garrett")
        self.assertEqual(len(detail["turns"]), 1)
        self.assertEqual(detail["turns"][0]["generator_id"], "plant_rows_v1")

    def test_unknown_story_sequence_session_returns_404(self) -> None:
        response = self.client.post("/api/v1/story-sequence-sessions/missing/next-turn")

        self.assertEqual(response.status_code, 404)
        self.assertEqual(response.json()["detail"], "Story sequence session not found.")


class FakeImageGenerator:
    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_bytes(b"fake-png")
        return GeneratedImageAsset(
            output_path=output_path,
            mime_type="image/png",
            provider_name="fake-image",
            provider_model="fake-image-v1",
            fallback_used=False,
            source_metadata={
                "prompt": prompt,
                "reference_image_paths": list(reference_image_paths),
                "aspect_ratio": aspect_ratio,
                "image_size": image_size,
            },
        )


class FakeSpeechGenerator:
    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> GeneratedSpeechAsset:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_bytes(b"fake-mp3")
        alignment_path = output_path.with_suffix(".alignment.json")
        alignment_path.write_text('{"characters":["f"]}', encoding="utf-8")
        return GeneratedSpeechAsset(
            output_path=output_path,
            alignment_path=alignment_path,
            duration_seconds=3.0,
            mime_type="audio/mpeg",
            provider_name="fake-speech",
            provider_model="fake-speech-v1",
            fallback_used=False,
            source_metadata={
                "text": text,
                "voice_id": voice_id,
                "previous_text": previous_text,
                "next_text": next_text,
            },
        )


if __name__ == "__main__":
    unittest.main()
