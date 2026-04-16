import tempfile
import time
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.generated_minigames import GeneratedMinigameBeatService
from app.generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset
from app.generated_storyboards import GeneratedStoryboardService, TemplateStoryboardPlanner
from app.main import create_app
from app.runtime_turn_generation import RuntimeTurnGenerationService


class RuntimeSessionEndpointTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            openai_api_key="test-openai",
            database_path=str(database_path),
            elevenlabs_voice_id="voice-test",
        )
        storyboard_service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=RuntimeFakeImageGenerator(),
            speech_generator=RuntimeFakeSpeechGenerator(),
            default_voice_id="voice-test",
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

    def test_runtime_session_flow_creates_ready_turn_and_envelope(self) -> None:
        create_response = self.client.post("/api/runtime/v1/sessions", json={})

        self.assertEqual(create_response.status_code, 201)
        created = create_response.json()
        self.assertIn("session_id", created)
        self.assertIn("job_id", created)
        self.assertTrue(created["session_id"])
        self.assertTrue(created["job_id"])

        job = self._wait_for_terminal_job(created["job_id"])
        self.assertEqual(job["status"], "ready")
        self.assertEqual(job["session_id"], created["session_id"])
        self.assertTrue(job["turn_id"])

        envelope_response = self.client.get(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{job['turn_id']}"
        )

        self.assertEqual(envelope_response.status_code, 200)
        envelope = envelope_response.json()
        self.assertEqual(envelope["contract_version"], "runtime/v1")
        self.assertEqual(envelope["session_id"], created["session_id"])
        self.assertEqual(envelope["turn_id"], job["turn_id"])
        self.assertEqual(envelope["status"], "ready")
        self.assertTrue(envelope["entry_scene_name"])
        self.assertTrue(envelope["cutscene"]["shots"])
        self.assertGreaterEqual(len(envelope["cutscene"]["shots"]), 3)
        self.assertLessEqual(len(envelope["cutscene"]["shots"]), 6)
        self.assertTrue(
            all(shot["subtitle_text"] == shot["narration_text"] for shot in envelope["cutscene"]["shots"])
        )
        self.assertTrue(envelope["minigame"]["adapter_id"])
        self.assertGreaterEqual(len(envelope["artifacts"]), 3)
        self.assertEqual(
            envelope["cutscene"]["style_preset_id"],
            "watercolor_intro_v1",
            "Generated cutscene must carry watercolor_intro_v1 so the style anchor library "
            "and StylePresetCatalog descriptor/suffix actually apply at runtime.",
        )

    def test_runtime_configuration_endpoint_returns_story_mode_catalog(self) -> None:
        response = self.client.get("/api/runtime/v1/configuration")

        self.assertEqual(response.status_code, 200)
        payload = response.json()
        self.assertEqual(payload["contract_version"], "runtime-story-mode/v1")
        self.assertEqual(payload["default_story_type_id"], "farm_cozy_starter_v1")
        self.assertEqual(payload["default_prompt_structure_id"], "conflict_escalation_handoff_v1")
        self.assertEqual(
            {item["story_type_id"] for item in payload["story_types"]},
            {
                "farm_cozy_starter_v1",
                "tool_hunt_detour_v1",
                "chicken_ruckus_intro_v1",
                "mixed_tutorial_arc_v1",
            },
        )
        self.assertEqual(
            {item["prompt_structure_id"] for item in payload["prompt_structures"]},
            {
                "conflict_escalation_handoff_v1",
                "character_request_payoff_v1",
                "discovery_pressure_release_v1",
            },
        )
        plant_surface = next(
            item for item in payload["minigame_surfaces"] if item["generator"]["generator_id"] == "plant_rows_v1"
        )
        self.assertEqual(plant_surface["adapter_id"], "tutorial.plant_rows")
        self.assertEqual(plant_surface["scene_name"], "FarmMain")
        self.assertTrue(plant_surface["story_purposes"])
        self.assertTrue(plant_surface["parameter_prompt_hints"])

    def test_runtime_session_detail_reports_active_job_and_last_ready_turn(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()
        job = self._wait_for_terminal_job(created["job_id"])

        session_response = self.client.get(f"/api/runtime/v1/sessions/{created['session_id']}")

        self.assertEqual(session_response.status_code, 200)
        session = session_response.json()
        self.assertEqual(session["session_id"], created["session_id"])
        self.assertEqual(session["active_job"]["job_id"], created["job_id"])
        self.assertEqual(session["active_job"]["status"], "ready")
        self.assertEqual(session["last_ready_turn"]["turn_id"], job["turn_id"])
        self.assertEqual(session["status"], "active")

    def test_runtime_artifact_content_route_streams_asset_bytes(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()
        job = self._wait_for_terminal_job(created["job_id"])
        envelope = self.client.get(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{job['turn_id']}"
        ).json()
        image_asset = next(asset for asset in envelope["artifacts"] if asset["artifact_type"] == "image")

        artifact_response = self.client.get(
            f"/api/runtime/v1/artifacts/{image_asset['asset_id']}/content"
        )

        self.assertEqual(artifact_response.status_code, 200)
        self.assertEqual(artifact_response.headers["content-type"], "image/png")
        self.assertEqual(artifact_response.content, b"fake-png")

    def test_runtime_outcome_creates_next_turn_and_reuses_continuity_images(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()
        first_job = self._wait_for_terminal_job(created["job_id"])
        first_envelope = self.client.get(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{first_job['turn_id']}"
        ).json()
        first_image_asset_ids = [
            artifact["asset_id"]
            for artifact in first_envelope["artifacts"]
            if artifact["artifact_type"] == "image"
        ]

        outcome_response = self.client.post(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{first_job['turn_id']}/outcome",
            json={
                "result": "success",
                "score": 1.0,
                "completed_objective_count": 1,
                "notes": "Completed the objective.",
            },
        )

        self.assertEqual(outcome_response.status_code, 200)
        outcome = outcome_response.json()
        self.assertTrue(outcome["next_job_id"])
        self.assertEqual(outcome["session_state"]["session_id"], created["session_id"])

        next_job = self._wait_for_terminal_job(outcome["next_job_id"])
        self.assertEqual(next_job["status"], "ready")
        self.assertNotEqual(next_job["turn_id"], first_job["turn_id"])

        next_envelope = self.client.get(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{next_job['turn_id']}"
        ).json()

        self.assertEqual(
            next_envelope["continuity"]["reference_image_asset_ids"],
            [first_image_asset_ids[0]],
        )

    def test_runtime_session_can_target_tool_hunt_story_type_and_prompt_structure(self) -> None:
        created = self.client.post(
            "/api/runtime/v1/sessions",
            json={
                "story_type_id": "tool_hunt_detour_v1",
                "prompt_structure_id": "character_request_payoff_v1",
            },
        ).json()

        job = self._wait_for_terminal_job(created["job_id"])
        self.assertEqual(job["status"], "ready")

        session = self.client.get(f"/api/runtime/v1/sessions/{created['session_id']}").json()
        self.assertEqual(session["state"]["story_type_id"], "tool_hunt_detour_v1")
        self.assertEqual(session["state"]["prompt_structure_id"], "character_request_payoff_v1")
        self.assertEqual(session["state"]["allowed_generator_ids"], ["find_tools_cluster_v1"])

        envelope = self.client.get(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{job['turn_id']}"
        ).json()

        self.assertEqual(envelope["minigame"]["generator_id"], "find_tools_cluster_v1")
        self.assertEqual(envelope["minigame"]["scene_name"], "FindToolsGame")
        self.assertEqual(envelope["minigame"]["adapter_id"], "tutorial.find_tools")
        self.assertEqual(envelope["debug"]["story_type_id"], "tool_hunt_detour_v1")
        self.assertEqual(envelope["debug"]["prompt_structure_id"], "character_request_payoff_v1")

    def test_runtime_session_completes_after_third_turn_outcome(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()

        first_job = self._wait_for_terminal_job(created["job_id"])
        second_outcome = self.client.post(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{first_job['turn_id']}/outcome",
            json={
                "result": "success",
                "score": 1.0,
                "completed_objective_count": 1,
                "notes": "First turn complete.",
            },
        ).json()
        second_job = self._wait_for_terminal_job(second_outcome["next_job_id"])

        third_outcome = self.client.post(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{second_job['turn_id']}/outcome",
            json={
                "result": "success",
                "score": 1.0,
                "completed_objective_count": 1,
                "notes": "Second turn complete.",
            },
        ).json()
        third_job = self._wait_for_terminal_job(third_outcome["next_job_id"])

        completion_response = self.client.post(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{third_job['turn_id']}/outcome",
            json={
                "result": "success",
                "score": 1.0,
                "completed_objective_count": 1,
                "notes": "Third turn complete.",
            },
        )

        self.assertEqual(completion_response.status_code, 200)
        completion = completion_response.json()
        self.assertEqual(completion["next_job_id"], "")
        self.assertEqual(completion["session_state"]["status"], "completed")
        self.assertEqual(completion["session_state"]["active_job_id"], "")

        session_response = self.client.get(f"/api/runtime/v1/sessions/{created['session_id']}")
        self.assertEqual(session_response.status_code, 200)
        session = session_response.json()
        self.assertEqual(session["status"], "completed")
        self.assertIsNone(session["active_job"])
        self.assertEqual(session["last_ready_turn"]["turn_id"], third_job["turn_id"])

    def test_runtime_session_uses_farm_minigame_only_with_varied_plant_configs(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()

        turn_jobs = [self._wait_for_terminal_job(created["job_id"])]
        turn_envelopes = [
            self.client.get(
                f"/api/runtime/v1/sessions/{created['session_id']}/turns/{turn_jobs[0]['turn_id']}"
            ).json()
        ]

        for turn_number in range(2):
            outcome = self.client.post(
                f"/api/runtime/v1/sessions/{created['session_id']}/turns/{turn_jobs[-1]['turn_id']}/outcome",
                json={
                    "result": "success",
                    "score": 1.0,
                    "completed_objective_count": 1,
                    "notes": f"Advance to turn {turn_number + 2}.",
                },
            ).json()
            next_job = self._wait_for_terminal_job(outcome["next_job_id"])
            turn_jobs.append(next_job)
            turn_envelopes.append(
                self.client.get(
                    f"/api/runtime/v1/sessions/{created['session_id']}/turns/{next_job['turn_id']}"
                ).json()
            )

        generator_ids = [envelope["minigame"]["generator_id"] for envelope in turn_envelopes]
        scene_names = [envelope["minigame"]["scene_name"] for envelope in turn_envelopes]
        adapter_ids = [envelope["minigame"]["adapter_id"] for envelope in turn_envelopes]
        crop_types = [
            envelope["minigame"]["resolved_parameters"]["cropType"]
            for envelope in turn_envelopes
        ]
        target_counts = [
            envelope["minigame"]["resolved_parameters"]["targetCount"]
            for envelope in turn_envelopes
        ]
        objective_texts = [
            envelope["minigame"]["objective_text"]
            for envelope in turn_envelopes
        ]

        self.assertEqual(generator_ids, ["plant_rows_v1", "plant_rows_v1", "plant_rows_v1"])
        self.assertEqual(scene_names, ["FarmMain", "FarmMain", "FarmMain"])
        self.assertEqual(
            adapter_ids,
            ["tutorial.plant_rows", "tutorial.plant_rows", "tutorial.plant_rows"],
        )
        self.assertEqual(crop_types, ["carrot", "tomato", "corn"])
        self.assertEqual(target_counts, [5, 6, 7])
        self.assertEqual(
            objective_texts,
            [
                "Plant 5 carrots in 5 minutes.",
                "Plant 6 tomatoes in 5 minutes.",
                "Plant 7 corn in 6 minutes.",
            ],
        )

    def test_runtime_tracker_endpoints_return_recent_sessions_and_turn_details(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()
        job = self._wait_for_terminal_job(created["job_id"])

        list_response = self.client.get("/api/runtime/v1/tracker/sessions")
        self.assertEqual(list_response.status_code, 200)
        sessions = list_response.json()
        matching = next(item for item in sessions if item["session_id"] == created["session_id"])
        self.assertEqual(matching["current_stage"], "ready")
        self.assertEqual(matching["ready_turn_count"], 1)

        detail_response = self.client.get(
            f"/api/runtime/v1/tracker/sessions/{created['session_id']}"
        )
        self.assertEqual(detail_response.status_code, 200)
        detail = detail_response.json()
        self.assertEqual(detail["session_id"], created["session_id"])
        self.assertEqual(detail["current_stage"], "ready")
        self.assertEqual(detail["active_job"]["job_id"], created["job_id"])
        self.assertEqual(detail["active_job"]["current_step_name"], "validating")
        self.assertEqual(detail["turns"][0]["turn_id"], job["turn_id"])
        self.assertEqual(detail["turns"][0]["objective_text"], "Plant 5 carrots in 5 minutes.")
        self.assertGreaterEqual(len(detail["turns"][0]["image_asset_ids"]), 1)

    def _wait_for_terminal_job(self, job_id: str) -> dict:
        last_payload = None
        for _ in range(30):
            response = self.client.get(f"/api/runtime/v1/jobs/{job_id}")
            self.assertEqual(response.status_code, 200)
            last_payload = response.json()
            if last_payload["status"] in {"ready", "failed", "cancelled"}:
                return last_payload
            time.sleep(0.05)

        self.fail(f"Runtime job '{job_id}' did not reach a terminal state. Last payload: {last_payload}")


class RuntimeVisualContinuityTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            openai_api_key="test-openai",
            database_path=str(database_path),
            elevenlabs_voice_id="voice-test",
        )
        self.recording_planner = RecordingTemplatePlanner()
        storyboard_service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=self.recording_planner,
            image_generator=RuntimeFakeImageGenerator(),
            speech_generator=RuntimeFakeSpeechGenerator(),
            default_voice_id="voice-test",
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

    def test_second_turn_planner_request_carries_prior_hero_shot_path(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()
        first_job = self._wait_for_terminal_job(created["job_id"])
        first_envelope = self.client.get(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{first_job['turn_id']}"
        ).json()
        first_shot_image_asset_id = first_envelope["cutscene"]["shots"][0]["image_asset_id"]
        first_shot_artifact = next(
            artifact for artifact in first_envelope["artifacts"]
            if artifact["asset_id"] == first_shot_image_asset_id
        )
        first_shot_stored_path = first_shot_artifact["stored_path"]

        outcome_response = self.client.post(
            f"/api/runtime/v1/sessions/{created['session_id']}/turns/{first_job['turn_id']}/outcome",
            json={
                "result": "success",
                "score": 1.0,
                "completed_objective_count": 1,
                "notes": "First turn done.",
            },
        )
        self.assertEqual(outcome_response.status_code, 200)
        next_job = self._wait_for_terminal_job(outcome_response.json()["next_job_id"])
        self.assertEqual(next_job["status"], "ready")

        second_turn_request = self.recording_planner.requests[-1]
        self.assertIn(first_shot_stored_path, second_turn_request.context.prior_hero_shot_paths)

    def test_first_turn_planner_request_has_empty_prior_hero_shot_paths(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()
        first_job = self._wait_for_terminal_job(created["job_id"])
        self.assertEqual(first_job["status"], "ready")

        first_turn_request = self.recording_planner.requests[0]
        self.assertEqual(first_turn_request.context.prior_hero_shot_paths, [])

    def _wait_for_terminal_job(self, job_id: str) -> dict:
        last_payload = None
        for _ in range(60):
            response = self.client.get(f"/api/runtime/v1/jobs/{job_id}")
            self.assertEqual(response.status_code, 200)
            last_payload = response.json()
            if last_payload["status"] in {"ready", "failed", "cancelled"}:
                return last_payload
            time.sleep(0.05)

        self.fail(f"Runtime job '{job_id}' did not reach a terminal state. Last payload: {last_payload}")


class RecordingTemplatePlanner:
    def __init__(self) -> None:
        self._inner = TemplateStoryboardPlanner()
        self.requests: list = []

    def plan(self, request):
        self.requests.append(request)
        return self._inner.plan(request)


class RuntimeSessionCreateContractTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            openai_api_key="test-openai",
            database_path=str(database_path),
            elevenlabs_voice_id="voice-test",
        )
        storyboard_service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=RuntimeFakeImageGenerator(),
            speech_generator=RuntimeFakeSpeechGenerator(),
            default_voice_id="voice-test",
        )
        minigame_service = GeneratedMinigameBeatService(package_output_path=package_output_path)
        runtime_generation_service = DelayedRuntimeGenerationService(
            inner=RuntimeTurnGenerationService(
                runtime_root=(Path(self._temp_dir.name) / "data" / "runtime"),
                storyboard_service=storyboard_service,
                minigame_service=minigame_service,
                default_voice_id="voice-test",
            ),
            delay_seconds=1.0,
        )
        self.client = TestClient(
            create_app(
                settings,
                generated_storyboard_service=storyboard_service,
                generated_minigame_service=minigame_service,
                runtime_generation_service=runtime_generation_service,
            )
        )

    def tearDown(self) -> None:
        self.client.close()
        self._temp_dir.cleanup()

    def test_runtime_session_create_returns_before_generation_finishes(self) -> None:
        started_at = time.perf_counter()
        create_response = self.client.post("/api/runtime/v1/sessions", json={})
        elapsed_seconds = time.perf_counter() - started_at

        self.assertEqual(create_response.status_code, 201)
        self.assertLess(
            elapsed_seconds,
            0.75,
            f"Runtime session creation blocked for {elapsed_seconds:.2f}s instead of returning a queued job immediately.",
        )

        created = create_response.json()
        self.assertIn(created["status"], {"queued", "planning"})

        initial_job = self.client.get(f"/api/runtime/v1/jobs/{created['job_id']}")
        self.assertEqual(initial_job.status_code, 200)
        self.assertIn(initial_job.json()["status"], {"queued", "planning", "generating_images", "generating_audio"})

        job = self._wait_for_terminal_job(created["job_id"])
        self.assertEqual(job["status"], "ready")

    def test_runtime_tracker_detail_reports_live_image_then_audio_phases(self) -> None:
        database_path = Path(self._temp_dir.name) / "story_orchestrator_tracker.db"
        output_root = Path(self._temp_dir.name) / "TrackerAssets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            openai_api_key="test-openai",
            database_path=str(database_path),
            elevenlabs_voice_id="voice-test",
        )
        storyboard_service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=SlowRuntimeFakeImageGenerator(delay_seconds=0.1),
            speech_generator=SlowRuntimeFakeSpeechGenerator(delay_seconds=0.1),
            default_voice_id="voice-test",
        )
        minigame_service = GeneratedMinigameBeatService(package_output_path=package_output_path)
        client = TestClient(
            create_app(
                settings,
                generated_storyboard_service=storyboard_service,
                generated_minigame_service=minigame_service,
            )
        )
        self.addCleanup(client.close)

        created = client.post("/api/runtime/v1/sessions", json={}).json()

        self._wait_for_tracker_stage(
            client,
            created["session_id"],
            {"generating_images", "generating_audio"},
        )
        final_detail = self._wait_for_tracker_ready(client, created["session_id"])
        self.assertEqual(final_detail["current_stage"], "ready")
        self.assertEqual(final_detail["active_job"]["status"], "ready")

    def _wait_for_terminal_job(self, job_id: str) -> dict:
        last_payload = None
        for _ in range(60):
            response = self.client.get(f"/api/runtime/v1/jobs/{job_id}")
            self.assertEqual(response.status_code, 200)
            last_payload = response.json()
            if last_payload["status"] in {"ready", "failed", "cancelled"}:
                return last_payload
            time.sleep(0.05)

        self.fail(f"Runtime job '{job_id}' did not reach a terminal state. Last payload: {last_payload}")

    def _wait_for_tracker_stage(
        self,
        client: TestClient,
        session_id: str,
        expected_stages: set[str],
    ) -> dict:
        last_payload = None
        for _ in range(80):
            response = client.get(f"/api/runtime/v1/tracker/sessions/{session_id}")
            self.assertEqual(response.status_code, 200)
            last_payload = response.json()
            if last_payload["current_stage"] in expected_stages:
                return last_payload
            time.sleep(0.05)

        self.fail(
            f"Tracker session '{session_id}' did not reach any of {expected_stages}. Last payload: {last_payload}"
        )

    def _wait_for_tracker_ready(self, client: TestClient, session_id: str) -> dict:
        last_payload = None
        for _ in range(80):
            response = client.get(f"/api/runtime/v1/tracker/sessions/{session_id}")
            self.assertEqual(response.status_code, 200)
            last_payload = response.json()
            if last_payload["current_stage"] == "ready":
                return last_payload
            time.sleep(0.05)

        self.fail(f"Tracker session '{session_id}' did not reach ready. Last payload: {last_payload}")


class RuntimeFailedJobEndpointTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            openai_api_key="test-openai",
            database_path=str(database_path),
            elevenlabs_voice_id="voice-test",
        )
        storyboard_service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=FailingRuntimeImageGenerator(),
            speech_generator=RuntimeFakeSpeechGenerator(),
            default_voice_id="voice-test",
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

    def test_runtime_job_fails_when_required_artifacts_are_missing(self) -> None:
        created = self.client.post("/api/runtime/v1/sessions", json={}).json()

        job = self._wait_for_terminal_job(created["job_id"])
        self.assertEqual(job["status"], "failed")
        self.assertEqual(job["turn_id"], "")
        self.assertTrue(job["error_message"])

    def _wait_for_terminal_job(self, job_id: str) -> dict:
        last_payload = None
        for _ in range(60):
            response = self.client.get(f"/api/runtime/v1/jobs/{job_id}")
            self.assertEqual(response.status_code, 200)
            last_payload = response.json()
            if last_payload["status"] in {"ready", "failed", "cancelled"}:
                return last_payload
            time.sleep(0.05)

        self.fail(f"Runtime job '{job_id}' did not reach a terminal state. Last payload: {last_payload}")


class RuntimeFakeImageGenerator:
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


class DelayedRuntimeGenerationService:
    def __init__(self, *, inner: RuntimeTurnGenerationService, delay_seconds: float) -> None:
        self._inner = inner
        self._delay_seconds = delay_seconds

    def generate_turn(self, *, session, prior_turns, job_id, progress_callback=None):
        time.sleep(self._delay_seconds)
        return self._inner.generate_turn(
            session=session,
            prior_turns=prior_turns,
            job_id=job_id,
            progress_callback=progress_callback,
        )


class RuntimeFakeSpeechGenerator:
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


class SlowRuntimeFakeImageGenerator(RuntimeFakeImageGenerator):
    def __init__(self, *, delay_seconds: float) -> None:
        self._delay_seconds = delay_seconds

    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        time.sleep(self._delay_seconds)
        return super().generate_image(
            prompt=prompt,
            reference_image_paths=reference_image_paths,
            output_path=output_path,
            aspect_ratio=aspect_ratio,
            image_size=image_size,
        )


class SlowRuntimeFakeSpeechGenerator(RuntimeFakeSpeechGenerator):
    def __init__(self, *, delay_seconds: float) -> None:
        self._delay_seconds = delay_seconds

    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> GeneratedSpeechAsset:
        time.sleep(self._delay_seconds)
        return super().generate_speech(
            text=text,
            voice_id=voice_id,
            output_path=output_path,
            previous_text=previous_text,
            next_text=next_text,
        )


class FailingRuntimeImageGenerator:
    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        raise RuntimeError("forced image generation failure")


if __name__ == "__main__":
    unittest.main()
