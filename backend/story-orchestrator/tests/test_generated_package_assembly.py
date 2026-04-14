import json
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.generated_minigames import GeneratedMinigameBeatService
from app.generated_package_assembly import (
    GeneratedPackageAssemblyCutsceneInput,
    GeneratedPackageAssemblyMinigameInput,
    GeneratedPackageAssemblyRequest,
    GeneratedPackageAssemblyService,
)
from app.generated_storyboard_models import GeneratedStoryboardContext
from app.generated_storyboards import (
    GeneratedImageAsset,
    GeneratedSpeechAsset,
    GeneratedStoryboardService,
    TemplateStoryboardPlanner,
)
from app.main import create_app
from app.minigame_generator_models import MinigameGenerationContext


class GeneratedPackageAssemblyServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.image_generator = CountingImageGenerator()
        self.speech_generator = CountingSpeechGenerator()
        self.storyboard_service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=self.image_generator,
            speech_generator=self.speech_generator,
        )
        self.minigame_service = GeneratedMinigameBeatService(
            package_output_path=self.package_output_path,
        )
        self.service = GeneratedPackageAssemblyService(
            minigame_service=self.minigame_service,
            storyboard_service=self.storyboard_service,
        )

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_create_package_materializes_minigame_then_storyboard_in_one_call(self) -> None:
        result = self.service.create_package(build_request())

        self.assertTrue(result.is_valid, result.errors)
        self.assertEqual(result.minigame_result.materialized_minigame["AdapterId"], "tutorial.plant_rows")
        self.assertIsNotNone(result.cutscene_result)
        self.assertEqual(len(result.cutscene_result.generated_assets), 6)
        self.assertTrue(self.package_output_path.exists())

        beats = {beat["BeatId"]: beat for beat in result.unity_package["Beats"]}
        self.assertEqual(set(beats.keys()), {"plant_rows_intro", "post_chicken_bridge"})
        self.assertEqual(
            beats["post_chicken_bridge"]["Storyboard"]["Shots"][1]["SubtitleText"],
            "Plant 6 carrots in 5 minutes, and keep the rows tidy.",
        )

        written_package = json.loads(self.package_output_path.read_text(encoding="utf-8"))
        self.assertEqual(len(written_package["Beats"]), 2)
        self.assertEqual(written_package["Beats"][0]["BeatId"], "plant_rows_intro")
        self.assertEqual(written_package["Beats"][1]["BeatId"], "post_chicken_bridge")

    def test_create_package_short_circuits_when_minigame_is_invalid(self) -> None:
        request = build_request(
            minigame=GeneratedPackageAssemblyMinigameInput(
                beat_id="plant_rows_intro",
                display_name="Plant Rows Intro",
                scene_name="FarmMain",
                next_scene_name="Tutorial_PostChickenCutscene",
                generator_id="plant_rows_v1",
                parameters={
                    "cropType": "tomato",
                    "targetCount": 6,
                    "timeLimitSeconds": 300,
                },
                context=MinigameGenerationContext(
                    fit_tags=["intro"],
                    world_state=["farm_plots_unlocked"],
                    difficulty_band="tutorial",
                ),
            )
        )

        result = self.service.create_package(request)

        self.assertFalse(result.is_valid)
        self.assertEqual(result.package_output_path, "")
        self.assertIn("cropType=tomato requires world state 'tomatoes_unlocked'.", result.errors)
        self.assertEqual(self.image_generator.calls, 0)
        self.assertEqual(self.speech_generator.calls, 0)
        self.assertFalse(self.package_output_path.exists())


class GeneratedPackageAssemblyEndpointTests(unittest.TestCase):
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
        storyboard_service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=CountingImageGenerator(),
            speech_generator=CountingSpeechGenerator(),
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

    def test_post_generated_package_assembly_returns_combined_payload(self) -> None:
        response = self.client.post(
            "/api/v1/generated-package-assemblies",
            json=build_request().model_dump(mode="json"),
        )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["is_valid"])
        self.assertEqual(payload["minigame_result"]["materialized_minigame"]["AdapterId"], "tutorial.plant_rows")
        self.assertEqual(len(payload["cutscene_result"]["generated_assets"]), 6)
        self.assertEqual(
            payload["unity_package"]["Beats"][1]["Storyboard"]["Shots"][1]["ShotId"],
            "shot_02",
        )


def build_request(
    *,
    minigame: GeneratedPackageAssemblyMinigameInput | None = None,
) -> GeneratedPackageAssemblyRequest:
    return GeneratedPackageAssemblyRequest(
        package_id="storypkg_intro_chicken_sample",
        package_display_name="Intro Chicken Sample",
        minigame=minigame
        or GeneratedPackageAssemblyMinigameInput(
            beat_id="plant_rows_intro",
            display_name="Plant Rows Intro",
            scene_name="FarmMain",
            next_scene_name="Tutorial_PostChickenCutscene",
            generator_id="plant_rows_v1",
            parameters={
                "cropType": "carrot",
                "targetCount": 6,
                "timeLimitSeconds": 300,
            },
            context=MinigameGenerationContext(
                fit_tags=["intro"],
                world_state=["farm_plots_unlocked"],
                difficulty_band="tutorial",
            ),
        ),
        cutscene=GeneratedPackageAssemblyCutsceneInput(
            beat_id="post_chicken_bridge",
            display_name="Post Chicken Bridge",
            scene_name="PostChickenCutscene",
            next_scene_name="MidpointPlaceholder",
            story_brief="Bridge the chicken chase into the first planting task.",
            style_preset_id="farm_storybook_v1",
            voice_id="voice-test",
            context=GeneratedStoryboardContext(
                character_name="Old Garrett",
                crop_name="",
                minigame_goal="",
            ),
        ),
    )


class CountingImageGenerator:
    def __init__(self) -> None:
        self.calls = 0

    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        self.calls += 1
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


class CountingSpeechGenerator:
    def __init__(self) -> None:
        self.calls = 0

    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> GeneratedSpeechAsset:
        self.calls += 1
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
