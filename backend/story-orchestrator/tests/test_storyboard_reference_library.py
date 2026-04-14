import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.generated_storyboard_models import GeneratedStoryboardContext, GeneratedStoryboardCutsceneRequest
from app.generated_storyboards import GeneratedImageAsset, GeneratedSpeechAsset, GeneratedStoryboardService, TemplateStoryboardPlanner
from app.main import create_app
from app.storyboard_reference_library import StoryboardReferenceLibrary


class StoryboardReferenceLibraryTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.library = StoryboardReferenceLibrary(self.output_root)

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_import_reference_persists_and_lists_metadata(self) -> None:
        record = self.library.import_reference(
            filename="old-garrett-sheet.png",
            content=b"garrett-reference",
            reference_role="character",
            label="Old Garrett Sheet",
            character_name="Old Garrett",
            tags=["hero", "storybook"],
        )

        listed = self.library.list_references(character_name="Old Garrett")

        self.assertEqual(record.reference_role, "character")
        self.assertEqual(record.character_name, "Old Garrett")
        self.assertTrue(Path(record.stored_path).exists())
        self.assertEqual(len(listed), 1)
        self.assertEqual(listed[0].reference_id, record.reference_id)
        self.assertEqual(listed[0].tags, ["hero", "storybook"])


class StoryboardReferenceContinuityTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.reference_library = StoryboardReferenceLibrary(self.output_root)
        self.service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
            reference_library=self.reference_library,
        )

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_create_package_auto_selects_uploaded_character_and_prior_package_art(self) -> None:
        character_reference = self.reference_library.import_reference(
            filename="old-garrett-sheet.png",
            content=b"garrett-reference",
            reference_role="character",
            label="Old Garrett Sheet",
            character_name="Old Garrett",
            tags=["hero"],
        )
        prior_image = self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "older_bridge" / "shot_01.png"
        prior_image.parent.mkdir(parents=True, exist_ok=True)
        prior_image.write_bytes(b"older-generated-image")

        result = self.service.create_package(build_request())

        first_image_asset = result.generated_assets[0]
        reference_paths = first_image_asset.metadata["reference_image_paths"]

        self.assertIn(character_reference.stored_path, reference_paths)
        self.assertIn(str(prior_image.resolve()), reference_paths)


    def test_create_package_skips_generic_package_sweep_when_explicit_reference_paths_exist(self) -> None:
        character_reference = self.reference_library.import_reference(
            filename="old-garrett-sheet.png",
            content=b"garrett-reference",
            reference_role="character",
            label="Old Garrett Sheet",
            character_name="Old Garrett",
            tags=["hero"],
        )
        explicit_image = self.output_root / "manual_refs" / "old_garrett_turn.png"
        explicit_image.parent.mkdir(parents=True, exist_ok=True)
        explicit_image.write_bytes(b"explicit-generated-image")
        prior_image = self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "older_bridge" / "shot_01.png"
        prior_image.parent.mkdir(parents=True, exist_ok=True)
        prior_image.write_bytes(b"older-generated-image")

        result = self.service.create_package(
            build_request(reference_image_paths=[str(explicit_image.resolve())])
        )

        first_image_asset = result.generated_assets[0]
        reference_paths = first_image_asset.metadata["reference_image_paths"]

        self.assertIn(str(explicit_image.resolve()), reference_paths)
        self.assertIn(character_reference.stored_path, reference_paths)
        self.assertNotIn(str(prior_image.resolve()), reference_paths)


class StoryboardReferenceEndpointTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.reference_library = StoryboardReferenceLibrary(self.output_root)
        self.storyboard_service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
            reference_library=self.reference_library,
        )
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            database_path=str(Path(self._temp_dir.name) / "story_orchestrator.db"),
        )
        self.client = TestClient(
            create_app(
                settings=settings,
                generated_storyboard_service=self.storyboard_service,
                storyboard_reference_library=self.reference_library,
            )
        )

    def tearDown(self) -> None:
        self.client.close()
        self._temp_dir.cleanup()

    def test_upload_reference_endpoint_persists_asset_and_lists_it(self) -> None:
        upload_response = self.client.post(
            "/api/v1/storyboard-reference-assets",
            files={"file": ("old-garrett.png", b"garrett-reference", "image/png")},
            data={
                "reference_role": "character",
                "label": "Old Garrett Sheet",
                "character_name": "Old Garrett",
                "tags": "hero,storybook",
            },
        )

        self.assertEqual(upload_response.status_code, 201)
        created = upload_response.json()
        self.assertEqual(created["reference_role"], "character")
        self.assertEqual(created["character_name"], "Old Garrett")

        list_response = self.client.get("/api/v1/storyboard-reference-assets", params={"character_name": "Old Garrett"})
        self.assertEqual(list_response.status_code, 200)
        payload = list_response.json()
        self.assertEqual(len(payload), 1)
        self.assertEqual(payload[0]["reference_id"], created["reference_id"])


    def test_reference_content_endpoint_streams_uploaded_asset(self) -> None:
        upload_response = self.client.post(
            "/api/v1/storyboard-reference-assets",
            files={"file": ("old-garrett.png", b"garrett-reference", "image/png")},
            data={
                "reference_role": "character",
                "label": "Old Garrett Sheet",
                "character_name": "Old Garrett",
                "tags": "hero,storybook",
            },
        )

        self.assertEqual(upload_response.status_code, 201)
        created = upload_response.json()

        content_response = self.client.get(
            f"/api/v1/storyboard-reference-assets/{created['reference_id']}/content"
        )

        self.assertEqual(content_response.status_code, 200)
        self.assertEqual(content_response.content, b"garrett-reference")
        self.assertEqual(content_response.headers["content-type"], "image/png")


def build_request(
    *,
    reference_image_paths: list[str] | None = None,
) -> GeneratedStoryboardCutsceneRequest:
    return GeneratedStoryboardCutsceneRequest(
        package_id="storypkg_intro_chicken_sample",
        package_display_name="Intro Chicken Sample",
        beat_id="post_chicken_bridge",
        display_name="Post Chicken Bridge",
        scene_name="PostChickenCutscene",
        next_scene_name="MidpointPlaceholder",
        linked_minigame_beat_id=None,
        story_brief="Bridge the chicken chase into the first planting task.",
        style_preset_id="farm_storybook_v1",
        voice_id="voice-test",
        reference_image_paths=list(reference_image_paths or []),
        context=GeneratedStoryboardContext(
            character_name="Old Garrett",
            crop_name="carrots",
            minigame_goal="Plant 3 carrots in 5 minutes",
        ),
    )


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
