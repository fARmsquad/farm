import json
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.generated_minigames import GeneratedMinigameBeatRequest, GeneratedMinigameBeatService
from app.generated_storyboards import (
    GeneratedImageAsset,
    GeneratedSpeechAsset,
    GeneratedStoryboardContext,
    GeneratedStoryboardCutsceneRequest,
    GeneratedStoryboardService,
    TemplateStoryboardPlanner,
)
from app.main import create_app
from app.storyboard_media import PlaceholderSpeechGenerator


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
