import json
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.generated_minigames import GeneratedMinigameBeatService
from app.generated_package_assembly import GeneratedPackageAssemblyService
from app.generated_standing_slice import (
    GeneratedStandingSliceAssemblyInput,
    GeneratedStandingSliceRequest,
    GeneratedStandingSliceService,
)
from app.generated_storyboard_models import (
    GeneratedImageAsset,
    GeneratedSpeechAsset,
    GeneratedStoryboardContext,
)
from app.generated_storyboards import (
    GeneratedStoryboardService,
    TemplateStoryboardPlanner,
)
from app.main import create_app
from app.minigame_generator_models import MinigameGenerationContext


class GeneratedStandingSliceServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.storyboard_service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=CountingImageGenerator(),
            speech_generator=CountingSpeechGenerator(),
        )
        self.minigame_service = GeneratedMinigameBeatService(
            package_output_path=self.package_output_path,
        )
        self.package_assembly_service = GeneratedPackageAssemblyService(
            minigame_service=self.minigame_service,
            storyboard_service=self.storyboard_service,
        )
        self.service = GeneratedStandingSliceService(
            package_assembly_service=self.package_assembly_service,
            package_output_path=self.package_output_path,
        )

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def _seed_existing_package(self) -> bytes:
        self.package_output_path.parent.mkdir(parents=True, exist_ok=True)
        original_bytes = json.dumps(build_seed_package(), indent=2).encode("utf-8")
        self.package_output_path.write_bytes(original_bytes)
        return original_bytes

    def test_create_package_refreshes_both_assembly_legs_in_one_call(self) -> None:
        self._seed_existing_package()

        result = self.service.create_package(build_request())

        self.assertTrue(result.is_valid, result.errors)
        self.assertFalse(result.restored_original_package)
        self.assertEqual(result.package_output_path, str(self.package_output_path))
        self.assertIsNotNone(result.post_chicken_to_farm_result)
        self.assertIsNotNone(result.find_tools_to_pre_farm_result)
        self.assertTrue(result.post_chicken_to_farm_result.is_valid)
        self.assertTrue(result.find_tools_to_pre_farm_result.is_valid)

        beats = {beat["BeatId"]: beat for beat in result.unity_package["Beats"]}
        self.assertEqual(
            set(beats.keys()),
            {
                "intro_opening",
                "chicken_chase",
                "post_chicken_bridge",
                "plant_rows_intro",
                "find_tools_intro",
                "pre_farm_bridge",
            },
        )
        self.assertEqual(beats["plant_rows_intro"]["Minigame"]["AdapterId"], "tutorial.plant_rows")
        self.assertEqual(beats["find_tools_intro"]["Minigame"]["AdapterId"], "tutorial.find_tools")
        self.assertEqual(
            beats["pre_farm_bridge"]["Storyboard"]["Shots"][0]["SubtitleText"],
            "Nice work. The starter tools are back where they belong.",
        )

        written_package = json.loads(self.package_output_path.read_text(encoding="utf-8"))
        self.assertEqual(len(written_package["Beats"]), 6)
        self.assertEqual(written_package["Beats"][-1]["BeatId"], "pre_farm_bridge")

    def test_create_package_restores_original_manifest_when_second_leg_is_invalid(self) -> None:
        original_bytes = self._seed_existing_package()

        result = self.service.create_package(
            build_request(
                second_leg=GeneratedStandingSliceAssemblyInput(
                    minigame=build_find_tools_minigame(
                        parameters={
                            "targetToolSet": "starter",
                            "toolCount": 3,
                            "searchZone": "yard",
                            "hintStrength": "light",
                            "timeLimitSeconds": 240,
                        }
                    ),
                    cutscene=build_pre_farm_cutscene(),
                )
            )
        )

        self.assertFalse(result.is_valid)
        self.assertTrue(result.restored_original_package)
        self.assertIsNotNone(result.post_chicken_to_farm_result)
        self.assertTrue(result.post_chicken_to_farm_result.is_valid)
        self.assertIsNotNone(result.find_tools_to_pre_farm_result)
        self.assertFalse(result.find_tools_to_pre_farm_result.is_valid)
        self.assertIn(
            "toolCount=3 requires hintStrength to stay at medium or strong.",
            result.errors,
        )
        self.assertEqual(self.package_output_path.read_bytes(), original_bytes)


class GeneratedStandingSliceEndpointTests(unittest.TestCase):
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
        package_assembly_service = GeneratedPackageAssemblyService(
            minigame_service=minigame_service,
            storyboard_service=storyboard_service,
        )
        standing_slice_service = GeneratedStandingSliceService(
            package_assembly_service=package_assembly_service,
            package_output_path=package_output_path,
        )
        package_output_path.parent.mkdir(parents=True, exist_ok=True)
        package_output_path.write_text(
            json.dumps(build_seed_package(), indent=2),
            encoding="utf-8",
        )
        self.client = TestClient(
            create_app(
                settings,
                generated_storyboard_service=storyboard_service,
                generated_minigame_service=minigame_service,
                generated_package_assembly_service=package_assembly_service,
                generated_standing_slice_service=standing_slice_service,
            )
        )

    def tearDown(self) -> None:
        self.client.close()
        self._temp_dir.cleanup()

    def test_post_generated_standing_slice_regeneration_returns_combined_payload(self) -> None:
        response = self.client.post(
            "/api/v1/generated-standing-slice-regenerations",
            json=build_request().model_dump(mode="json"),
        )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["is_valid"])
        self.assertFalse(payload["restored_original_package"])
        self.assertEqual(payload["unity_package"]["Beats"][-1]["BeatId"], "pre_farm_bridge")
        self.assertEqual(
            payload["find_tools_to_pre_farm_result"]["unity_package"]["Beats"][-1]["Storyboard"]["Shots"][0]["ShotId"],
            "shot_01",
        )


def build_request(
    *,
    first_leg: GeneratedStandingSliceAssemblyInput | None = None,
    second_leg: GeneratedStandingSliceAssemblyInput | None = None,
) -> GeneratedStandingSliceRequest:
    return GeneratedStandingSliceRequest(
        package_id="storypkg_intro_chicken_sample",
        package_display_name="Generative Story Slice",
        post_chicken_to_farm=first_leg
        or GeneratedStandingSliceAssemblyInput(
            minigame=build_plant_rows_minigame(),
            cutscene=build_post_chicken_cutscene(),
        ),
        find_tools_to_pre_farm=second_leg
        or GeneratedStandingSliceAssemblyInput(
            minigame=build_find_tools_minigame(),
            cutscene=build_pre_farm_cutscene(),
        ),
    )


def build_plant_rows_minigame() -> object:
    from app.generated_package_assembly import GeneratedPackageAssemblyMinigameInput

    return GeneratedPackageAssemblyMinigameInput(
        beat_id="plant_rows_intro",
        display_name="Plant Rows Intro",
        scene_name="FarmMain",
        next_scene_name="FindToolsGame",
        generator_id="plant_rows_v1",
        parameters={
            "cropType": "carrot",
            "targetCount": 6,
            "timeLimitSeconds": 300,
            "rowCount": 2,
            "assistLevel": "high",
        },
        context=MinigameGenerationContext(
            fit_tags=["intro"],
            world_state=["farm_plots_unlocked"],
            difficulty_band="tutorial",
        ),
    )


def build_find_tools_minigame(*, parameters: dict[str, object] | None = None) -> object:
    from app.generated_package_assembly import GeneratedPackageAssemblyMinigameInput

    return GeneratedPackageAssemblyMinigameInput(
        beat_id="find_tools_intro",
        display_name="Find Tools Intro",
        scene_name="FindToolsGame",
        next_scene_name="Tutorial_PreFarmCutscene",
        generator_id="find_tools_cluster_v1",
        parameters=parameters
        or {
            "targetToolSet": "starter",
            "toolCount": 2,
            "searchZone": "yard",
            "hintStrength": "strong",
            "timeLimitSeconds": 240,
        },
        context=MinigameGenerationContext(
            fit_tags=["bridge"],
            world_state=["tool_search_enabled"],
            difficulty_band="tutorial",
        ),
    )


def build_post_chicken_cutscene() -> object:
    from app.generated_package_assembly import GeneratedPackageAssemblyCutsceneInput

    return GeneratedPackageAssemblyCutsceneInput(
        beat_id="post_chicken_bridge",
        display_name="Post Chicken Bridge",
        scene_name="Tutorial_PostChickenCutscene",
        next_scene_name="FarmMain",
        story_brief="Bridge the chicken chase into the first planting task.",
        style_preset_id="farm_storybook_v1",
        voice_id="voice-test",
        context=GeneratedStoryboardContext(
            character_name="Old Garrett",
            crop_name="",
            minigame_goal="",
        ),
    )


def build_pre_farm_cutscene() -> object:
    from app.generated_package_assembly import GeneratedPackageAssemblyCutsceneInput

    return GeneratedPackageAssemblyCutsceneInput(
        beat_id="pre_farm_bridge",
        display_name="Pre Farm Bridge",
        scene_name="Tutorial_PreFarmCutscene",
        next_scene_name="",
        story_brief="Bridge tool recovery into the first real farm loop.",
        style_preset_id="farm_storybook_v1",
        voice_id="voice-test",
        context=GeneratedStoryboardContext(
            character_name="Old Garrett",
            crop_name="",
            focus_label="",
            minigame_goal="",
        ),
    )


def build_seed_package() -> dict[str, object]:
    return {
        "PackageId": "storypkg_intro_chicken_sample",
        "SchemaVersion": 1,
        "PackageVersion": 1,
        "DisplayName": "Generative Story Slice",
        "Beats": [
            {
                "BeatId": "intro_opening",
                "DisplayName": "Intro Opening",
                "Kind": "Cutscene",
                "SceneName": "Intro",
                "NextSceneName": "ChickenChaseGame",
            },
            {
                "BeatId": "chicken_chase",
                "DisplayName": "Chicken Chase",
                "Kind": "Minigame",
                "SceneName": "ChickenChaseGame",
                "NextSceneName": "Tutorial_PostChickenCutscene",
                "Minigame": {
                    "AdapterId": "tutorial.chicken_chase",
                    "ObjectiveText": "Catch 1 chicken in 2 minutes.",
                    "RequiredCount": 1,
                    "TimeLimitSeconds": 120.0,
                    "GeneratorId": "chicken_chase_intro_v1",
                    "MinigameId": "chicken_chase",
                    "FallbackGeneratorIds": ["chicken_chase_basic_safe_v1"],
                    "ResolvedParameters": {
                        "targetCaptureCount": 1,
                        "chickenCount": 2,
                        "arenaPresetId": "tutorial_pen_small",
                        "timeLimitSeconds": 120,
                        "guidanceLevel": "high",
                    },
                    "ResolvedParameterEntries": [],
                },
            },
        ],
    }


class CountingImageGenerator:
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
        return GeneratedImageAsset(output_path=output_path, mime_type="image/png")


class CountingSpeechGenerator:
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
        )


if __name__ == "__main__":
    unittest.main()
