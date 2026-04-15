import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

from fastapi.testclient import TestClient
from PIL import Image, ImageDraw

from app.config import Settings
from app.generated_minigames import GeneratedMinigameBeatRequest, GeneratedMinigameBeatService
from app.generated_storyboards import (
    GeneratedImageAsset,
    GeneratedSpeechAsset,
    GeneratedStoryboardContext,
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardPlan,
    GeneratedStoryboardPlanShot,
    GeneratedStoryboardService,
    TemplateStoryboardPlanner,
)
from app.main import create_app
from app.storyboard_media import LocalReferenceImageGenerator, PlaceholderSpeechGenerator
from app.storyboard_provider_clients import (
    OpenAIImageGenerator,
    OpenAISpeechGenerator,
    REQUEST_TIMEOUT_SECONDS,
)


class GeneratedStoryboardServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
        )

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_create_package_writes_unity_manifest_and_media_assets(self) -> None:
        result = self.service.create_package(build_request())

        beat = result.unity_package["Beats"][0]
        storyboard = beat["Storyboard"]
        first_shot = storyboard["Shots"][0]
        first_asset = result.generated_assets[0]

        self.assertEqual(result.package_output_path, str(self.package_output_path))
        self.assertEqual(result.unity_package["PackageId"], "storypkg_intro_chicken_sample")
        self.assertEqual(beat["SceneName"], "PostChickenCutscene")
        self.assertEqual(storyboard["StylePresetId"], "farm_storybook_v1")
        self.assertEqual(first_shot["SubtitleText"], "Nice work. The carrot beds are finally ready for you.")
        self.assertGreaterEqual(len(storyboard["Shots"]), 3)
        self.assertLessEqual(len(storyboard["Shots"]), 6)
        self.assertEqual(len(result.generated_assets), 6)
        self.assertEqual(first_asset.asset_type, "image")
        self.assertEqual(first_asset.provider_name, "fake-image")
        self.assertFalse(first_asset.fallback_used)
        self.assertEqual(first_asset.shot_id, "shot_01")
        self.assertEqual(first_asset.metadata["aspect_ratio"], "16:9")
        self.assertTrue((self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "post_chicken_bridge" / "shot_01.png").exists())
        self.assertTrue((self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "post_chicken_bridge" / "shot_01.mp3").exists())
        self.assertTrue(self.package_output_path.exists())

        written_package = json.loads(self.package_output_path.read_text(encoding="utf-8"))
        self.assertEqual(written_package["Beats"][0]["Storyboard"]["Shots"][2]["ShotId"], "shot_03")

    def test_create_package_generates_speech_from_exact_subtitle_text_for_every_shot(self) -> None:
        service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=MismatchedNarrationPlanner(),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
        )

        result = service.create_package(build_request())

        storyboard_shots = result.unity_package["Beats"][0]["Storyboard"]["Shots"]
        audio_assets = [asset for asset in result.generated_assets if asset.asset_type == "audio"]

        self.assertEqual(len(storyboard_shots), 3)
        for index, shot in enumerate(storyboard_shots):
            self.assertEqual(shot["NarrationText"], shot["SubtitleText"])
            self.assertEqual(audio_assets[index].metadata["text"], shot["SubtitleText"])

    def test_create_package_writes_prompt_with_explicit_no_text_constraints(self) -> None:
        result = self.service.create_package(build_request())

        first_asset = result.generated_assets[0]
        prompt = first_asset.metadata["prompt"].lower()

        self.assertIn("do not draw subtitle boxes", prompt)
        self.assertIn("readable text", prompt)
        self.assertIn("speech bubbles", prompt)
        self.assertIn("do not copy camera angle", prompt)
        self.assertIn("fresh composition", prompt)
        self.assertIn("full-bleed image", prompt)
        self.assertIn("bottom third of the image", prompt)

    def test_create_package_preserves_story_context_in_resolved_image_prompt(self) -> None:
        request = build_request(
            context=GeneratedStoryboardContext(
                character_name="Miss Clara",
                crop_name="carrots",
                minigame_goal="Recover the missing watering kit before planting begins",
                prior_story_summary="Old Garrett just calmed the chicken pen, but a crash came from the shed.",
                world_state=["tomatoes_unlocked", "watering_tools_unlocked"],
                present_character_names=["Miss Clara", "Old Garrett"],
                selected_generator_id="find_tools_cluster_v1",
                selected_generator_display_name="Find Lost Tools",
                mission_configuration_summary="Recover 2 watering tools around the field path with medium hints.",
            )
        )

        result = self.service.create_package(request)

        prompt = result.generated_assets[0].metadata["prompt"]
        self.assertIn("Previous context: Old Garrett just calmed the chicken pen", prompt)
        self.assertIn("World state: tomatoes_unlocked, watering_tools_unlocked", prompt)
        self.assertIn("Present characters: Miss Clara, Old Garrett", prompt)
        self.assertIn("Mission configuration: Recover 2 watering tools around the field path with medium hints.", prompt)

    def test_create_package_can_derive_minigame_goal_from_linked_materialized_beat(self) -> None:
        minigame_service = GeneratedMinigameBeatService(package_output_path=self.package_output_path)
        minigame_service.create_package(
            GeneratedMinigameBeatRequest(
                package_id="storypkg_intro_chicken_sample",
                package_display_name="Intro Chicken Sample",
                beat_id="plant_rows_intro",
                display_name="Plant Rows Intro",
                scene_name="PlantRowsScene",
                next_scene_name="PostPlantRowsCutscene",
                generator_id="plant_rows_v1",
                parameters={
                    "cropType": "carrot",
                    "targetCount": 6,
                    "timeLimitSeconds": 300,
                },
                context={
                    "fit_tags": ["intro"],
                    "world_state": ["farm_plots_unlocked"],
                    "difficulty_band": "tutorial",
                },
            )
        )

        request = build_request(
            context=GeneratedStoryboardContext(
                character_name="Old Garrett",
                crop_name="",
                minigame_goal="",
            ),
            linked_minigame_beat_id="plant_rows_intro",
        )

        result = self.service.create_package(request)

        storyboard_shot = result.unity_package["Beats"][1]["Storyboard"]["Shots"][1]
        self.assertEqual(
            storyboard_shot["SubtitleText"],
            "Plant 6 carrots in 5 minutes, and keep the rows tidy.",
        )

        written_package = json.loads(self.package_output_path.read_text(encoding="utf-8"))
        self.assertEqual(len(written_package["Beats"]), 2)
        self.assertEqual(written_package["Beats"][0]["Minigame"]["ObjectiveText"], "Plant 6 carrots in 5 minutes.")
        self.assertEqual(written_package["Beats"][1]["BeatId"], "post_chicken_bridge")

    def test_create_package_can_derive_non_crop_context_from_linked_find_tools_beat(self) -> None:
        minigame_service = GeneratedMinigameBeatService(package_output_path=self.package_output_path)
        minigame_service.create_package(
            GeneratedMinigameBeatRequest(
                package_id="storypkg_intro_chicken_sample",
                package_display_name="Intro Chicken Sample",
                beat_id="find_tools_intro",
                display_name="Find Tools Intro",
                scene_name="FindToolsGame",
                next_scene_name="Tutorial_PreFarmCutscene",
                generator_id="find_tools_cluster_v1",
                parameters={
                    "targetToolSet": "starter",
                    "toolCount": 2,
                    "searchZone": "yard",
                    "hintStrength": "strong",
                    "timeLimitSeconds": 240,
                },
                context={
                    "fit_tags": ["bridge"],
                    "world_state": ["tool_search_enabled"],
                    "difficulty_band": "tutorial",
                },
            )
        )

        request = GeneratedStoryboardCutsceneRequest(
            package_id="storypkg_intro_chicken_sample",
            package_display_name="Intro Chicken Sample",
            beat_id="pre_farm_bridge",
            display_name="Pre-Farm Bridge",
            scene_name="Tutorial_PreFarmCutscene",
            next_scene_name="",
            linked_minigame_beat_id="find_tools_intro",
            story_brief="Bridge tool recovery into the first real farm loop.",
            style_preset_id="farm_storybook_v1",
            voice_id="voice-test",
            context=GeneratedStoryboardContext(
                character_name="Old Garrett",
                crop_name="",
                minigame_goal="",
            ),
        )

        result = self.service.create_package(request)

        storyboard_shot = result.unity_package["Beats"][1]["Storyboard"]["Shots"][0]
        self.assertEqual(
            storyboard_shot["SubtitleText"],
            "Nice work. The starter tools are back where they belong.",
        )


class GeneratedStoryboardEndpointTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            database_path=str(database_path),
        )
        service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
        )
        self.client = TestClient(create_app(settings, generated_storyboard_service=service))

    def tearDown(self) -> None:
        self.client.close()
        self._temp_dir.cleanup()

    def test_post_generated_storyboard_cutscene_returns_unity_package(self) -> None:
        response = self.client.post(
            "/api/v1/generated-storyboards/cutscene",
            json={
                "package_id": "storypkg_intro_chicken_sample",
                "package_display_name": "Intro Chicken Sample",
                "beat_id": "post_chicken_bridge",
                "display_name": "Post Chicken Bridge",
                "scene_name": "PostChickenCutscene",
                "next_scene_name": "MidpointPlaceholder",
                "story_brief": "Bridge the chicken chase into the first planting task.",
                "style_preset_id": "farm_storybook_v1",
                "voice_id": "voice-test",
                "context": {
                    "character_name": "Old Garrett",
                    "crop_name": "carrots",
                    "minigame_goal": "Plant 3 carrots in 5 minutes"
                }
            },
        )

        self.assertEqual(response.status_code, 201)
        payload = response.json()
        self.assertEqual(payload["unity_package"]["Beats"][0]["Storyboard"]["Shots"][1]["ShotId"], "shot_02")
        self.assertEqual(payload["unity_package"]["Beats"][0]["Storyboard"]["Shots"][1]["ImageResourcePath"], "GeneratedStoryboards/storypkg_intro_chicken_sample/post_chicken_bridge/shot_02")
        self.assertEqual(len(payload["generated_assets"]), 6)
        self.assertEqual(payload["generated_assets"][1]["asset_type"], "audio")


class StoryboardImageQualityGateTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name)

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_quality_gate_rejects_placeholder_fallback_assets(self) -> None:
        from app.storyboard_quality import StoryboardImageQualityGate

        output_path = self.output_root / "placeholder.png"
        _write_quality_test_image(output_path, include_caption_panel=False)
        asset = GeneratedImageAsset(
            output_path=output_path,
            mime_type="image/png",
            provider_name="placeholder-image",
            provider_model="placeholder-image-v1",
            fallback_used=True,
            source_metadata={},
        )

        result = StoryboardImageQualityGate().evaluate(asset)

        self.assertFalse(result.accepted)
        self.assertEqual(result.reason, "provider_fallback")

    def test_quality_gate_accepts_local_reference_remix_when_image_is_clean(self) -> None:
        from app.storyboard_quality import StoryboardImageQualityGate

        output_path = self.output_root / "reference-remix.png"
        _write_quality_test_image(output_path, include_caption_panel=False)
        asset = GeneratedImageAsset(
            output_path=output_path,
            mime_type="image/png",
            provider_name="local-reference-remix",
            provider_model="local-reference-remix-v1",
            fallback_used=True,
            source_metadata={},
        )

        result = StoryboardImageQualityGate().evaluate(asset)

        self.assertTrue(result.accepted)
        self.assertEqual(result.reason, "")

    def test_quality_gated_generator_retries_rejected_caption_panel_asset(self) -> None:
        from app.storyboard_quality import QualityGatedImageGenerator

        output_path = self.output_root / "retry.png"
        sequence = SequenceImageGenerator([True, False])
        generator = QualityGatedImageGenerator(sequence, max_attempts=2)

        asset = generator.generate_image(
            prompt="test prompt",
            reference_image_paths=[],
            output_path=output_path,
            aspect_ratio="16:9",
            image_size="2K",
        )

        self.assertEqual(sequence.calls, 2)
        self.assertEqual(sequence.path_exists_at_call_start, [False, False])
        self.assertEqual(asset.provider_name, "gemini-image")
        self.assertTrue(output_path.exists())

    def test_quality_gate_accepts_dark_foreground_scene_without_caption_panel(self) -> None:
        from app.storyboard_quality import StoryboardImageQualityGate

        output_path = self.output_root / "dark-foreground-scene.png"
        _write_dark_foreground_quality_test_image(output_path)
        asset = GeneratedImageAsset(
            output_path=output_path,
            mime_type="image/png",
            provider_name="gemini-image",
            provider_model="gemini-2.5-flash-image",
            fallback_used=False,
            source_metadata={},
        )

        result = StoryboardImageQualityGate().evaluate(asset)

        self.assertTrue(result.accepted, result.reason)
        self.assertEqual(result.reason, "")


class SettingsDefaultsTests(unittest.TestCase):
    def test_default_settings_prefer_gemini_2_5_without_preview_fallback(self) -> None:
        settings = Settings(_env_file=None)

        self.assertEqual(settings.gemini_image_model, "gemini-2.5-flash-image")
        self.assertEqual(settings.gemini_image_fallback_model, "")


class StoryboardProviderClientTimeoutTests(unittest.TestCase):
    def test_provider_request_timeout_stays_bounded_for_remote_story_sequence_turns(self) -> None:
        self.assertLessEqual(REQUEST_TIMEOUT_SECONDS, 30)


class PlaceholderSpeechGeneratorTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name)

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_generate_speech_removes_stale_sibling_audio_assets(self) -> None:
        generator = PlaceholderSpeechGenerator()
        stale_mp3 = self.output_root / "shot_01.mp3"
        stale_meta = self.output_root / "shot_01.mp3.meta"
        stale_mp3.write_bytes(b"old-mp3")
        stale_meta.write_text("old-meta", encoding="utf-8")

        result = generator.generate_speech(
            text="Fresh fallback narration.",
            voice_id="placeholder",
            output_path=stale_mp3,
            previous_text="",
            next_text="",
        )

        self.assertEqual(result.output_path.suffix, ".wav")
        self.assertTrue(result.output_path.exists())
        self.assertFalse(stale_mp3.exists())
        self.assertFalse(stale_meta.exists())


class OpenAIProviderClientTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name)

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    @patch("app.storyboard_provider_clients.httpx.post")
    def test_openai_image_generator_writes_png_from_base64_payload(self, post_mock: Mock) -> None:
        output_path = self.output_root / "shot_01.png"
        response = Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"data": [{"b64_json": _create_base64_png()}]}
        post_mock.return_value = response

        asset = OpenAIImageGenerator(api_key="test-key", model="gpt-image-1").generate_image(
            prompt="Paint Old Garrett in the field at dawn.",
            reference_image_paths=[],
            output_path=output_path,
            aspect_ratio="16:9",
            image_size="1536x1024",
        )

        self.assertEqual(asset.provider_name, "openai-image")
        self.assertEqual(asset.provider_model, "gpt-image-1")
        self.assertTrue(output_path.exists())
        self.assertGreater(output_path.stat().st_size, 0)

    @patch("app.storyboard_provider_clients.httpx.post")
    def test_openai_speech_generator_writes_audio_and_alignment(self, post_mock: Mock) -> None:
        output_path = self.output_root / "shot_01.mp3"
        response = Mock()
        response.raise_for_status.return_value = None
        response.content = b"fake-mp3-data"
        post_mock.return_value = response

        asset = OpenAISpeechGenerator(
            api_key="test-key",
            model_id="gpt-4o-mini-tts",
            voice="alloy",
        ).generate_speech(
            text="The rows are ready, and the field is yours now.",
            voice_id="ignored-elevenlabs-voice",
            output_path=output_path,
            previous_text="",
            next_text="",
        )

        self.assertEqual(asset.provider_name, "openai-speech")
        self.assertEqual(asset.provider_model, "gpt-4o-mini-tts")
        self.assertTrue(output_path.exists())
        self.assertTrue(asset.alignment_path.exists())
        alignment = json.loads(asset.alignment_path.read_text(encoding="utf-8"))
        self.assertEqual(
            "".join(alignment["characters"]),
            "The rows are ready, and the field is yours now.",
        )


class LocalReferenceImageGeneratorTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name)

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_local_reference_image_generator_remixes_reference_without_caption_panel(self) -> None:
        from app.storyboard_quality import StoryboardImageQualityGate

        reference_path = self.output_root / "reference.png"
        _write_quality_test_image(reference_path, include_caption_panel=False)
        output_path = self.output_root / "shot_01.png"

        asset = LocalReferenceImageGenerator().generate_image(
            prompt="Old Garrett points toward the carrot rows at sunrise.",
            reference_image_paths=[str(reference_path)],
            output_path=output_path,
            aspect_ratio="16:9",
            image_size="2K",
        )

        result = StoryboardImageQualityGate().evaluate(asset)

        self.assertTrue(result.accepted, result.reason)
        self.assertEqual(asset.provider_name, "local-reference-remix")
        self.assertTrue(output_path.exists())


def build_request(
    *,
    context: "GeneratedStoryboardContext | None" = None,
    linked_minigame_beat_id: str | None = None,
) -> "GeneratedStoryboardCutsceneRequest":
    return GeneratedStoryboardCutsceneRequest(
        package_id="storypkg_intro_chicken_sample",
        package_display_name="Intro Chicken Sample",
        beat_id="post_chicken_bridge",
        display_name="Post Chicken Bridge",
        scene_name="PostChickenCutscene",
        next_scene_name="MidpointPlaceholder",
        linked_minigame_beat_id=linked_minigame_beat_id,
        story_brief="Bridge the chicken chase into the first planting task.",
        style_preset_id="farm_storybook_v1",
        voice_id="voice-test",
        context=context
        or GeneratedStoryboardContext(
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
    ) -> "GeneratedImageAsset":
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


class MismatchedNarrationPlanner:
    def plan(self, request: GeneratedStoryboardCutsceneRequest):
        return GeneratedStoryboardPlan(
            beat_id=request.beat_id,
            display_name=request.display_name,
            scene_name=request.scene_name,
            next_scene_name=request.next_scene_name or "",
            style_preset_id=request.style_preset_id,
            shots=[
                GeneratedStoryboardPlanShot(
                    shot_id="shot_01",
                    subtitle_text="Garrett checks the first row.",
                    narration_text="Garrett studies the weather vane instead.",
                    image_prompt="Old Garrett checks the first carrot row at sunrise, cautious posture, warm storybook light.",
                    duration_seconds=3.0,
                ),
                GeneratedStoryboardPlanShot(
                    shot_id="shot_02",
                    subtitle_text="The seed cart catches on broken wood.",
                    narration_text="The cart wheel slips against broken wood.",
                    image_prompt="A seed cart jams against a broken wooden handle beside the field path.",
                    duration_seconds=3.1,
                ),
                GeneratedStoryboardPlanShot(
                    shot_id="shot_03",
                    subtitle_text="Clear it and take the field.",
                    narration_text="Step forward and start the planting.",
                    image_prompt="The field opens ahead as the player is waved toward the prepared plots.",
                    duration_seconds=3.2,
                ),
            ],
        )


def _write_quality_test_image(output_path: Path, *, include_caption_panel: bool) -> None:
    width, height = 1280, 720
    image = Image.new("RGB", (width, height), "#87b36b")
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, width, int(height * 0.58)), fill="#d8b46b")
    draw.rectangle((0, int(height * 0.58), width, height), fill="#5f8c52")
    if include_caption_panel:
        draw.rounded_rectangle(
            (40, height - 220, width - 40, height - 35),
            radius=24,
            fill=(10, 10, 10),
        )
    output_path.parent.mkdir(parents=True, exist_ok=True)
    image.save(output_path, format="PNG")


def _write_dark_foreground_quality_test_image(output_path: Path) -> None:
    width, height = 1280, 720
    image = Image.new("RGB", (width, height), "#87b36b")
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, width, int(height * 0.58)), fill="#d8b46b")
    draw.polygon(
        [
            (0, height),
            (0, int(height * 0.73)),
            (int(width * 0.16), int(height * 0.69)),
            (int(width * 0.32), int(height * 0.79)),
            (int(width * 0.52), int(height * 0.67)),
            (int(width * 0.76), int(height * 0.82)),
            (width, int(height * 0.71)),
            (width, height),
        ],
        fill=(8, 12, 9),
    )
    output_path.parent.mkdir(parents=True, exist_ok=True)
    image.save(output_path, format="PNG")


def _create_base64_png() -> str:
    import base64
    from io import BytesIO

    image = Image.new("RGB", (16, 16), "#7aa35a")
    buffer = BytesIO()
    image.save(buffer, format="PNG")
    return base64.b64encode(buffer.getvalue()).decode("ascii")


class SequenceImageGenerator:
    def __init__(self, caption_panel_sequence: list[bool]) -> None:
        self._caption_panel_sequence = list(caption_panel_sequence)
        self.calls = 0
        self.path_exists_at_call_start: list[bool] = []

    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> "GeneratedImageAsset":
        self.path_exists_at_call_start.append(output_path.exists())
        include_caption_panel = self._caption_panel_sequence[self.calls]
        self.calls += 1
        _write_quality_test_image(output_path, include_caption_panel=include_caption_panel)
        return GeneratedImageAsset(
            output_path=output_path,
            mime_type="image/png",
            provider_name="gemini-image",
            provider_model="gemini-test",
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
    ) -> "GeneratedSpeechAsset":
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
