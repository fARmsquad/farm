import json
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.generated_storyboards import (
    GeneratedImageAsset,
    GeneratedSpeechAsset,
    GeneratedStoryboardContext,
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardPlan,
    GeneratedStoryboardPlanShot,
    GeneratedStoryboardService,
)
from app.main import create_app


class GeneratedStoryboardServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=FakeStoryboardPlanner(),
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

        self.assertEqual(result.package_output_path, str(self.package_output_path))
        self.assertEqual(result.unity_package["PackageId"], "storypkg_intro_chicken_sample")
        self.assertEqual(beat["SceneName"], "PostChickenCutscene")
        self.assertEqual(storyboard["StylePresetId"], "farm_storybook_v1")
        self.assertEqual(first_shot["SubtitleText"], "Nice work. The carrot beds are finally ready for you.")
        self.assertTrue((self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "post_chicken_bridge" / "shot_01.png").exists())
        self.assertTrue((self.output_root / "GeneratedStoryboards" / "storypkg_intro_chicken_sample" / "post_chicken_bridge" / "shot_01.mp3").exists())
        self.assertTrue(self.package_output_path.exists())

        written_package = json.loads(self.package_output_path.read_text(encoding="utf-8"))
        self.assertEqual(written_package["Beats"][0]["Storyboard"]["Shots"][2]["ShotId"], "shot_03")


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
            planner=FakeStoryboardPlanner(),
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


def build_request() -> "GeneratedStoryboardCutsceneRequest":
    return GeneratedStoryboardCutsceneRequest(
        package_id="storypkg_intro_chicken_sample",
        package_display_name="Intro Chicken Sample",
        beat_id="post_chicken_bridge",
        display_name="Post Chicken Bridge",
        scene_name="PostChickenCutscene",
        next_scene_name="MidpointPlaceholder",
        story_brief="Bridge the chicken chase into the first planting task.",
        style_preset_id="farm_storybook_v1",
        voice_id="voice-test",
        context=GeneratedStoryboardContext(
            character_name="Old Garrett",
            crop_name="carrots",
            minigame_goal="Plant 3 carrots in 5 minutes",
        ),
    )


class FakeStoryboardPlanner:
    def plan(self, request: "GeneratedStoryboardCutsceneRequest") -> "GeneratedStoryboardPlan":
        return GeneratedStoryboardPlan(
            beat_id=request.beat_id,
            display_name=request.display_name,
            scene_name=request.scene_name,
            next_scene_name=request.next_scene_name or "",
            style_preset_id=request.style_preset_id,
            shots=[
                GeneratedStoryboardPlanShot(
                    shot_id="shot_01",
                    subtitle_text="Nice work. The carrot beds are finally ready for you.",
                    narration_text="Nice work. The carrot beds are finally ready for you.",
                    image_prompt="A warm farm sunrise with Old Garrett turning toward the carrot beds.",
                    duration_seconds=3.2,
                ),
                GeneratedStoryboardPlanShot(
                    shot_id="shot_02",
                    subtitle_text="Plant 3 carrots in 5 minutes, and keep the rows tidy.",
                    narration_text="Plant 3 carrots in 5 minutes, and keep the rows tidy.",
                    image_prompt="Old Garrett points at tidy carrot rows with tools ready beside the soil.",
                    duration_seconds=3.6,
                ),
                GeneratedStoryboardPlanShot(
                    shot_id="shot_03",
                    subtitle_text="Head to the plots. The farm is waiting on your hands.",
                    narration_text="Head to the plots. The farm is waiting on your hands.",
                    image_prompt="A forward-looking farm path leading from the chicken pen to the carrot plots.",
                    duration_seconds=3.0,
                ),
            ],
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
        return GeneratedImageAsset(output_path=output_path, mime_type="image/png")


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
        )
