import tempfile
import unittest
from io import BytesIO
from pathlib import Path

from fastapi.testclient import TestClient
from PIL import Image

from app.config import Settings
from app.generated_storyboard_models import GeneratedStoryboardContext, GeneratedStoryboardCutsceneRequest
from app.generated_storyboards import GeneratedImageAsset, GeneratedSpeechAsset, GeneratedStoryboardService, TemplateStoryboardPlanner
from app.main import create_app
from app.storyboard_reference_library import StoryboardReferenceLibrary, StoryboardReferencePathResolver


def _png_bytes() -> bytes:
    buffer = BytesIO()
    Image.new("RGB", (32, 32), (0, 0, 0)).save(buffer, format="PNG")
    return buffer.getvalue()


def _make_request(
    *,
    style_preset_id: str = "farm_storybook_v1",
    character_name: str = "Old Garrett",
    max_style_anchors: int = 0,
    max_reference_images: int = 4,
    reference_image_paths: list[str] | None = None,
    continuity_reference_mode: str = "auto",
) -> GeneratedStoryboardCutsceneRequest:
    request = GeneratedStoryboardCutsceneRequest(
        package_id="storypkg_intro_chicken_sample",
        package_display_name="Intro Chicken Sample",
        beat_id="post_chicken_bridge",
        display_name="Post Chicken Bridge",
        scene_name="PostChickenCutscene",
        next_scene_name="MidpointPlaceholder",
        linked_minigame_beat_id=None,
        story_brief="Bridge the chicken chase into the first planting task.",
        style_preset_id=style_preset_id or "placeholder_preset",
        voice_id="voice-test",
        reference_image_paths=list(reference_image_paths or []),
        continuity_reference_mode=continuity_reference_mode,
        max_reference_images=max_reference_images,
        max_style_anchors=max_style_anchors,
        context=GeneratedStoryboardContext(
            character_name=character_name or "Placeholder",
            crop_name="carrots",
            minigame_goal="Plant 3 carrots in 5 minutes",
        ),
    )
    if not style_preset_id:
        # Pydantic field has min_length=1; mutate post-construction to test the
        # resolver's "no preset" branch without tripping the constructor validator.
        request.style_preset_id = ""
    return request


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
        self.assertTrue(self.library.resolve_stored_path(record.stored_path).exists())
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

        resolved_character_path = str(self.reference_library.resolve_stored_path(character_reference.stored_path))
        self.assertIn(resolved_character_path, reference_paths)
        self.assertIn(str(prior_image.resolve()), reference_paths)


    def test_create_package_character_priority_puts_character_refs_before_generated_continuity(self) -> None:
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

        result = self.service.create_package(
            build_request(
                reference_image_paths=[str(explicit_image.resolve())],
                continuity_reference_mode="character_priority",
            )
        )

        reference_paths = result.generated_assets[0].metadata["reference_image_paths"]

        resolved_character_path = str(self.reference_library.resolve_stored_path(character_reference.stored_path))
        self.assertEqual(reference_paths[0], resolved_character_path)
        self.assertIn(str(explicit_image.resolve()), reference_paths)

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

        resolved_character_path = str(self.reference_library.resolve_stored_path(character_reference.stored_path))
        self.assertIn(str(explicit_image.resolve()), reference_paths)
        self.assertIn(resolved_character_path, reference_paths)
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
    continuity_reference_mode: str = "auto",
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
        continuity_reference_mode=continuity_reference_mode,
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


def test_resolve_paths_prepends_style_anchors_when_style_preset_matches(tmp_path):
    library = StoryboardReferenceLibrary(output_root=tmp_path)
    style_a = library.import_reference(
        filename="style_a.png", content=_png_bytes(), reference_role="style",
        label="style_a", tags=["watercolor_intro_v1", "intro_panels"], mime_type="image/png",
    )
    style_b = library.import_reference(
        filename="style_b.png", content=_png_bytes(), reference_role="style",
        label="style_b", tags=["watercolor_intro_v1", "intro_panels"], mime_type="image/png",
    )
    char_ref = library.import_reference(
        filename="char.png", content=_png_bytes(), reference_role="character",
        label="char", character_name="Garrett", tags=["character"], mime_type="image/png",
    )
    resolver = StoryboardReferencePathResolver(output_root=tmp_path, reference_library=library)
    request = _make_request(
        style_preset_id="watercolor_intro_v1",
        character_name="Garrett",
        max_style_anchors=2,
        max_reference_images=4,
    )

    paths = resolver.resolve_paths(request)

    style_a_path = str(library.resolve_stored_path(style_a.stored_path))
    style_b_path = str(library.resolve_stored_path(style_b.stored_path))
    char_path = str(library.resolve_stored_path(char_ref.stored_path))
    assert len(paths) == 3
    assert paths[0] in {style_a_path, style_b_path}
    assert paths[1] in {style_a_path, style_b_path}
    assert paths[0] != paths[1]
    assert paths[2] == char_path


def test_resolve_paths_omits_style_anchors_when_style_preset_id_empty(tmp_path):
    library = StoryboardReferenceLibrary(output_root=tmp_path)
    library.import_reference(
        filename="style_a.png", content=_png_bytes(), reference_role="style",
        label="style_a", tags=["watercolor_intro_v1"], mime_type="image/png",
    )
    library.import_reference(
        filename="char.png", content=_png_bytes(), reference_role="character",
        label="char", character_name="Garrett", tags=["character"], mime_type="image/png",
    )
    resolver = StoryboardReferencePathResolver(output_root=tmp_path, reference_library=library)
    request = _make_request(
        style_preset_id="",
        character_name="Garrett",
        max_style_anchors=2,
        max_reference_images=4,
    )

    paths = resolver.resolve_paths(request)

    assert len(paths) == 1


def test_resolve_paths_respects_max_style_anchors_config(tmp_path):
    library = StoryboardReferenceLibrary(output_root=tmp_path)
    for i in range(5):
        library.import_reference(
            filename=f"style_{i}.png", content=_png_bytes(), reference_role="style",
            label=f"style_{i}", tags=["watercolor_intro_v1"], mime_type="image/png",
        )
    resolver = StoryboardReferencePathResolver(output_root=tmp_path, reference_library=library)
    request = _make_request(
        style_preset_id="watercolor_intro_v1",
        character_name="",
        max_style_anchors=3,
        max_reference_images=4,
    )

    paths = resolver.resolve_paths(request)

    assert len(paths) == 3


def test_resolve_paths_skips_style_refs_with_non_matching_preset_tag(tmp_path):
    library = StoryboardReferenceLibrary(output_root=tmp_path)
    library.import_reference(
        filename="other_style.png", content=_png_bytes(), reference_role="style",
        label="other", tags=["pixel_art_v1"], mime_type="image/png",
    )
    resolver = StoryboardReferencePathResolver(output_root=tmp_path, reference_library=library)
    request = _make_request(
        style_preset_id="watercolor_intro_v1",
        character_name="",
        max_style_anchors=2,
        max_reference_images=4,
    )

    paths = resolver.resolve_paths(request)

    assert paths == []
