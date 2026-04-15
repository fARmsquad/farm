import json
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.generated_minigames import (
    GeneratedMinigameBeatRequest,
    GeneratedMinigameBeatService,
)
from app.main import create_app
from app.minigame_generator_models import MinigameGenerationContext


class GeneratedMinigameBeatServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.service = GeneratedMinigameBeatService(
            package_output_path=self.package_output_path,
        )

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_create_package_writes_materialized_minigame_beat_and_preserves_existing_beats(self) -> None:
        self.package_output_path.parent.mkdir(parents=True, exist_ok=True)
        self.package_output_path.write_text(
            json.dumps(
                {
                    "PackageId": "storypkg_intro_chicken_sample",
                    "SchemaVersion": 1,
                    "PackageVersion": 1,
                    "DisplayName": "Intro Chicken Sample",
                    "Beats": [
                        {
                            "BeatId": "intro_opening",
                            "DisplayName": "Intro Opening",
                            "Kind": "Cutscene",
                            "SceneName": "Intro",
                            "NextSceneName": "ChickenGame",
                            "SequenceSteps": [{"StepType": "Wait", "Duration": 0.5}],
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )

        result = self.service.create_package(build_request())

        self.assertTrue(result.is_valid, result.errors)
        self.assertEqual(result.package_output_path, str(self.package_output_path))
        self.assertEqual(result.materialized_minigame["AdapterId"], "tutorial.plant_rows")
        self.assertEqual(result.materialized_minigame["GeneratorId"], "plant_rows_v1")
        self.assertEqual(result.materialized_minigame["RequiredCount"], 6)
        self.assertEqual(result.materialized_minigame["ResolvedParameters"]["rowCount"], 2)
        self.assertEqual(result.materialized_minigame["ResolvedParameterEntries"][0]["Name"], "cropType")
        self.assertEqual(result.materialized_minigame["ResolvedParameterEntries"][0]["ValueType"], "String")
        self.assertEqual(len(result.unity_package["Beats"]), 2)

        written_package = json.loads(self.package_output_path.read_text(encoding="utf-8"))
        written_beat = written_package["Beats"][1]
        self.assertEqual(written_beat["Kind"], "Minigame")
        self.assertEqual(written_beat["Minigame"]["ObjectiveText"], "Plant 6 carrots in 5 minutes.")
        self.assertEqual(written_package["Beats"][0]["BeatId"], "intro_opening")

    def test_create_package_returns_structured_invalid_result_without_writing_package(self) -> None:
        result = self.service.create_package(
            build_request(
                parameters={"cropType": "tomato", "targetCount": 6, "timeLimitSeconds": 300},
                context=MinigameGenerationContext(
                    fit_tags=["intro"],
                    world_state=["farm_plots_unlocked"],
                    difficulty_band="tutorial",
                ),
            )
        )

        self.assertFalse(result.is_valid)
        self.assertEqual(result.package_output_path, "")
        self.assertEqual(result.fallback_generator_ids, ["plant_rows_tutorial_safe_v1"])
        self.assertIn("cropType=tomato requires world state 'tomatoes_unlocked'.", result.errors)
        self.assertFalse(self.package_output_path.exists())

    def test_create_package_uses_irregular_crop_plural_for_corn(self) -> None:
        result = self.service.create_package(
            build_request(
                parameters={
                    "cropType": "corn",
                    "targetCount": 7,
                    "timeLimitSeconds": 360,
                    "rowCount": 3,
                    "assistLevel": "medium",
                },
                context=MinigameGenerationContext(
                    fit_tags=["intro"],
                    world_state=["farm_plots_unlocked", "corn_unlocked"],
                    difficulty_band="tutorial",
                ),
            )
        )

        self.assertTrue(result.is_valid, result.errors)
        self.assertEqual(result.materialized_minigame["ObjectiveText"], "Plant 7 corn in 6 minutes.")


class GeneratedMinigameBeatEndpointTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        service = GeneratedMinigameBeatService(package_output_path=package_output_path)
        self.client = TestClient(create_app(generated_minigame_service=service))

    def tearDown(self) -> None:
        self.client.close()
        self._temp_dir.cleanup()

    def test_post_generated_minigame_beat_returns_materialized_payload(self) -> None:
        response = self.client.post(
            "/api/v1/generated-minigame-beats",
            json={
                "package_id": "storypkg_intro_chicken_sample",
                "package_display_name": "Intro Chicken Sample",
                "beat_id": "chicken_chase",
                "display_name": "Chicken Chase",
                "scene_name": "ChickenGame",
                "next_scene_name": "Tutorial_PostChickenCutscene",
                "generator_id": "chicken_chase_intro_v1",
                "parameters": {
                    "targetCaptureCount": 1,
                    "chickenCount": 2,
                    "arenaPresetId": "tutorial_pen_small",
                    "timeLimitSeconds": 120,
                },
                "context": {
                    "fit_tags": ["intro"],
                    "world_state": ["chicken_pen_intro_available"],
                    "difficulty_band": "tutorial",
                },
            },
        )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertTrue(payload["is_valid"])
        self.assertEqual(payload["materialized_minigame"]["AdapterId"], "tutorial.chicken_chase")
        self.assertEqual(payload["materialized_minigame"]["RequiredCount"], 1)


def build_request(
    *,
    parameters: dict[str, object] | None = None,
    context: MinigameGenerationContext | None = None,
) -> GeneratedMinigameBeatRequest:
    return GeneratedMinigameBeatRequest(
        package_id="storypkg_intro_chicken_sample",
        package_display_name="Intro Chicken Sample",
        beat_id="plant_rows_intro",
        display_name="Plant Rows Intro",
        scene_name="PlantRowsScene",
        next_scene_name="PostPlantRowsCutscene",
        generator_id="plant_rows_v1",
        parameters=parameters or {
            "cropType": "carrot",
            "targetCount": 6,
            "timeLimitSeconds": 300,
        },
        context=context
        or MinigameGenerationContext(
            fit_tags=["intro"],
            world_state=["farm_plots_unlocked"],
            difficulty_band="tutorial",
        ),
    )


if __name__ == "__main__":
    unittest.main()
