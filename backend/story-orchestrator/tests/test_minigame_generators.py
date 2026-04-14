import unittest

from fastapi.testclient import TestClient

from app.main import create_app
from app.minigame_generator_models import MinigameParameterDefinition, MinigameParameterType
from app.minigame_generators import MinigameGenerationContext, MinigameGeneratorCatalog


class MinigameGeneratorCatalogTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = MinigameGeneratorCatalog.default()

    def test_default_catalog_exposes_expected_v1_generators(self) -> None:
        generator_ids = {definition.generator_id for definition in self.catalog.list_definitions()}

        self.assertEqual(
            generator_ids,
            {"plant_rows_v1", "find_tools_cluster_v1", "chicken_chase_intro_v1"},
        )

        plant_rows = self.catalog.get_definition("plant_rows_v1")
        assert plant_rows is not None
        self.assertEqual(plant_rows.fit_tags, ["intro", "teaching", "crop-focused", "calm"])
        self.assertEqual(plant_rows.defaults["cropType"], "carrot")
        self.assertEqual(plant_rows.fallback_generator_ids, ["plant_rows_tutorial_safe_v1"])

    def test_validate_selection_applies_defaults_for_valid_plant_rows_request(self) -> None:
        result = self.catalog.validate_selection(
            "plant_rows_v1",
            parameters={"targetCount": 6},
            context=MinigameGenerationContext(
                fit_tags=["intro"],
                world_state=["farm_plots_unlocked"],
                difficulty_band="tutorial",
            ),
        )

        self.assertTrue(result.is_valid, result.errors)
        self.assertEqual(result.resolved_parameters["cropType"], "carrot")
        self.assertEqual(result.resolved_parameters["rowCount"], 2)
        self.assertEqual(result.resolved_parameters["targetCount"], 6)

    def test_validate_selection_rejects_plant_rows_coupling_rule_violation(self) -> None:
        result = self.catalog.validate_selection(
            "plant_rows_v1",
            parameters={"rowCount": 3, "targetCount": 5},
            context=MinigameGenerationContext(
                fit_tags=["teaching"],
                world_state=["farm_plots_unlocked"],
                difficulty_band="tutorial",
            ),
        )

        self.assertFalse(result.is_valid)
        self.assertIn("rowCount >= 3 requires targetCount >= 6.", result.errors)
        self.assertEqual(result.fallback_generator_ids, ["plant_rows_tutorial_safe_v1"])

    def test_validate_selection_rejects_plant_rows_tomato_before_unlock(self) -> None:
        result = self.catalog.validate_selection(
            "plant_rows_v1",
            parameters={"cropType": "tomato"},
            context=MinigameGenerationContext(
                fit_tags=["intro"],
                world_state=["farm_plots_unlocked"],
                difficulty_band="tutorial",
            ),
        )

        self.assertFalse(result.is_valid)
        self.assertIn("cropType=tomato requires world state 'tomatoes_unlocked'.", result.errors)

    def test_validate_selection_rejects_find_tools_cluster_light_hints_for_three_tools(self) -> None:
        result = self.catalog.validate_selection(
            "find_tools_cluster_v1",
            parameters={"toolCount": 3, "hintStrength": "light"},
            context=MinigameGenerationContext(
                fit_tags=["bridge"],
                world_state=["tool_search_enabled"],
                difficulty_band="tutorial",
            ),
        )

        self.assertFalse(result.is_valid)
        self.assertIn("toolCount=3 requires hintStrength to stay at medium or strong.", result.errors)
        self.assertEqual(result.fallback_generator_ids, ["find_tools_linear_safe_v1"])

    def test_validate_selection_rejects_chicken_capture_target_above_available_chickens(self) -> None:
        result = self.catalog.validate_selection(
            "chicken_chase_intro_v1",
            parameters={"targetCaptureCount": 3, "chickenCount": 2},
            context=MinigameGenerationContext(
                fit_tags=["intro"],
                world_state=["chicken_pen_intro_available"],
                difficulty_band="tutorial",
            ),
        )

        self.assertFalse(result.is_valid)
        self.assertIn("targetCaptureCount may not exceed chickenCount.", result.errors)
        self.assertEqual(result.fallback_generator_ids, ["chicken_chase_basic_safe_v1"])

    def test_validate_selection_returns_invalid_result_for_unknown_generator(self) -> None:
        result = self.catalog.validate_selection(
            "missing_generator",
            parameters={"anything": 1},
            context=MinigameGenerationContext(),
        )

        self.assertFalse(result.is_valid)
        self.assertEqual(result.errors, ["Unknown minigame generator 'missing_generator'."])
        self.assertEqual(result.fallback_generator_ids, [])

    def test_parameter_definition_supports_float_and_bool_types(self) -> None:
        speed = MinigameParameterDefinition(
            name="speedMultiplier",
            type=MinigameParameterType.FLOAT,
            description="Applies a floating-point speed modifier.",
            minimum=0.5,
            maximum=2.0,
            default=1.25,
        )
        enabled = MinigameParameterDefinition(
            name="spawnCompanion",
            type=MinigameParameterType.BOOL,
            description="Turns an assist helper on or off.",
            default=True,
        )

        normalized_speed, speed_errors = speed.validate_and_normalize(1.5)
        normalized_enabled, enabled_errors = enabled.validate_and_normalize(False)

        self.assertEqual(normalized_speed, 1.5)
        self.assertEqual(speed_errors, [])
        self.assertFalse(normalized_enabled)
        self.assertEqual(enabled_errors, [])


class MinigameGeneratorEndpointTests(unittest.TestCase):
    def setUp(self) -> None:
        self.client = TestClient(create_app())

    def tearDown(self) -> None:
        self.client.close()

    def test_list_generators_returns_expected_catalog_entries(self) -> None:
        response = self.client.get("/api/v1/minigame-generators")

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        generator_ids = {item["generator_id"] for item in payload}
        self.assertEqual(
            generator_ids,
            {"plant_rows_v1", "find_tools_cluster_v1", "chicken_chase_intro_v1"},
        )

    def test_validate_generator_returns_errors_and_fallback_ids(self) -> None:
        response = self.client.post(
            "/api/v1/minigame-generators/plant_rows_v1/validate",
            json={
                "parameters": {
                    "rowCount": 3,
                    "targetCount": 5,
                },
                "context": {
                    "fit_tags": ["intro"],
                    "world_state": ["farm_plots_unlocked"],
                    "difficulty_band": "tutorial",
                },
            },
        )

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertFalse(payload["is_valid"])
        self.assertEqual(payload["fallback_generator_ids"], ["plant_rows_tutorial_safe_v1"])
        self.assertIn("rowCount >= 3 requires targetCount >= 6.", payload["errors"])


if __name__ == "__main__":
    unittest.main()
