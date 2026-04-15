import tempfile
import unittest
from pathlib import Path

from app.generated_minigames import GeneratedMinigameBeatService
from app.generated_package_assembly import GeneratedPackageAssemblyService
from app.generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset
from app.generated_storyboards import GeneratedStoryboardService, TemplateStoryboardPlanner
from app.story_sequence_models import StorySequenceSessionCreateRequest
from app.story_sequence_service import StorySequenceSessionService
from app.story_sequence_store import StorySequenceSessionStore
from app.story_sequence_turn_director import StorySequenceTurnDirective


class StorySequenceTurnDirectorIntegrationTests(unittest.TestCase):
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

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_advance_session_uses_valid_llm_directed_turn_choice(self) -> None:
        service = StorySequenceSessionService(
            package_assembly_service=self.package_assembly_service,
            store=self.store,
            default_voice_id="voice-test",
            turn_director=FakeTurnDirector(
                StorySequenceTurnDirective(
                    generator_id="find_tools_cluster_v1",
                    character_name="Miss Clara",
                    cutscene_display_name="Clara Sends You Searching",
                    story_brief="Miss Clara notices the yard is missing key tools and sends the player into a quick search before the planting rhythm starts.",
                )
            ),
        )

        created = service.create_session(StorySequenceSessionCreateRequest())
        advanced = service.advance_session(created.session_id)

        self.assertEqual(advanced.turn.generator_id, "find_tools_cluster_v1")
        self.assertEqual(advanced.turn.character_name, "Miss Clara")
        self.assertEqual(advanced.turn.request.cutscene.display_name, "Clara Sends You Searching")
        self.assertIn("missing key tools", advanced.turn.request.cutscene.story_brief)

    def test_advance_session_falls_back_when_llm_choice_is_invalid(self) -> None:
        service = StorySequenceSessionService(
            package_assembly_service=self.package_assembly_service,
            store=self.store,
            default_voice_id="voice-test",
            turn_director=FakeTurnDirector(
                StorySequenceTurnDirective(
                    generator_id="made_up_generator",
                    character_name="Ghost Farmer",
                    cutscene_display_name="Broken Choice",
                    story_brief="This should not survive validation.",
                )
            ),
        )

        created = service.create_session(StorySequenceSessionCreateRequest())
        advanced = service.advance_session(created.session_id)

        self.assertEqual(advanced.turn.generator_id, "plant_rows_v1")
        self.assertEqual(advanced.turn.character_name, "Old Garrett")
        self.assertEqual(advanced.turn.request.cutscene.display_name, "Sequence Bridge 1")
        self.assertNotEqual(advanced.turn.request.cutscene.story_brief, "This should not survive validation.")


class FakeTurnDirector:
    def __init__(self, directive: StorySequenceTurnDirective) -> None:
        self._directive = directive

    def choose_turn(self, **_: object) -> StorySequenceTurnDirective:
        return self._directive


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
