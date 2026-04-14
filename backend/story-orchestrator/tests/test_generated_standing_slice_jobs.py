import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from app.config import Settings
from app.generated_minigames import GeneratedMinigameBeatService
from app.generated_package_assembly import GeneratedPackageAssemblyService
from app.generated_standing_slice import GeneratedStandingSliceService
from app.generated_standing_slice_artifacts import GeneratedStandingSliceArtifactArchive
from app.generated_standing_slice_publish import GeneratedStandingSlicePublisher
from app.generated_standing_slice_jobs import GeneratedStandingSliceJobService
from app.generated_storyboards import GeneratedStoryboardService, TemplateStoryboardPlanner
from app.main import create_app
from app.models import GeneratedStandingSliceJobReviewRequest
from app.store import GeneratedStandingSliceJobStore
from tests.test_generated_standing_slice import (
    CountingImageGenerator,
    CountingSpeechGenerator,
    build_find_tools_minigame,
    build_pre_farm_cutscene,
    build_request,
    build_seed_package,
)


class GeneratedStandingSliceJobServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.storyboard_service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=CountingImageGenerator(),
            speech_generator=CountingSpeechGenerator(),
        )
        self.minigame_service = GeneratedMinigameBeatService(package_output_path=self.package_output_path)
        self.package_assembly_service = GeneratedPackageAssemblyService(
            minigame_service=self.minigame_service,
            storyboard_service=self.storyboard_service,
        )
        self.standing_slice_service = GeneratedStandingSliceService(
            package_assembly_service=self.package_assembly_service,
            package_output_path=self.package_output_path,
        )
        self.job_store = GeneratedStandingSliceJobStore(self.database_path)
        self.job_service = GeneratedStandingSliceJobService(
            standing_slice_service=self.standing_slice_service,
            job_store=self.job_store,
            artifact_archive=GeneratedStandingSliceArtifactArchive(output_root=self.output_root),
            publisher=GeneratedStandingSlicePublisher(
                output_root=self.output_root,
                live_package_path=self.package_output_path,
            ),
        )

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def _seed_existing_package(self) -> bytes:
        self.package_output_path.parent.mkdir(parents=True, exist_ok=True)
        original_bytes = json.dumps(build_seed_package(), indent=2).encode("utf-8")
        self.package_output_path.write_bytes(original_bytes)
        return original_bytes

    def test_create_job_persists_completed_record_with_step_outputs(self) -> None:
        self._seed_existing_package()

        record = self.job_service.create_job(build_request())

        self.assertEqual(record.status, "completed")
        self.assertEqual(record.approval_status, "not_requested")
        self.assertEqual(record.review_notes, "")
        self.assertEqual(record.publish_status, "not_published")
        self.assertEqual(record.published_at, "")
        self.assertTrue(record.result.is_valid)
        self.assertEqual([step.status for step in record.steps], ["completed", "completed"])
        self.assertEqual(len(record.assets), 12)
        self.assertEqual(record.steps[0].step_id, "post_chicken_to_farm")
        self.assertEqual(record.steps[1].step_id, "find_tools_to_pre_farm")
        self.assertEqual(
            record.steps[1].output.unity_package["Beats"][-1]["BeatId"],
            "pre_farm_bridge",
        )
        self.assertEqual(record.assets[0].provider_name, "fake-image")

        fetched = self.job_store.get_job(record.job_id)
        self.assertIsNotNone(fetched)
        assert fetched is not None
        self.assertEqual(fetched.job_id, record.job_id)
        self.assertEqual(fetched.status, "completed")
        self.assertEqual(fetched.review_notes, "")
        self.assertEqual(fetched.publish_status, "not_published")
        self.assertEqual([step.status for step in fetched.steps], ["completed", "completed"])
        self.assertEqual(len(fetched.assets), 12)

    def test_update_review_persists_status_and_notes(self) -> None:
        self._seed_existing_package()
        record = self.job_service.create_job(build_request())

        updated = self.job_service.update_review(
            record.job_id,
            GeneratedStandingSliceJobReviewRequest(
                approval_status="approved",
                review_notes="Ready for the standing slice handoff.",
            ),
        )

        self.assertIsNotNone(updated)
        assert updated is not None
        self.assertEqual(updated.approval_status, "approved")
        self.assertEqual(updated.review_notes, "Ready for the standing slice handoff.")

        fetched = self.job_store.get_job(record.job_id)
        self.assertIsNotNone(fetched)
        assert fetched is not None
        self.assertEqual(fetched.approval_status, "approved")
        self.assertEqual(fetched.review_notes, "Ready for the standing slice handoff.")

    def test_create_job_persists_failed_second_step_and_restored_manifest(self) -> None:
        original_bytes = self._seed_existing_package()

        record = self.job_service.create_job(
            build_request(
                second_leg=build_request().find_tools_to_pre_farm.model_copy(
                    update={
                        "minigame": build_find_tools_minigame(
                            parameters={
                                "targetToolSet": "starter",
                                "toolCount": 3,
                                "searchZone": "yard",
                                "hintStrength": "light",
                                "timeLimitSeconds": 240,
                            }
                        ),
                        "cutscene": build_pre_farm_cutscene(),
                    }
                )
            )
        )

        self.assertEqual(record.status, "failed")
        self.assertFalse(record.result.is_valid)
        self.assertTrue(record.result.restored_original_package)
        self.assertEqual([step.status for step in record.steps], ["completed", "failed"])
        self.assertEqual(len(record.assets), 6)
        self.assertIn(
            "toolCount=3 requires hintStrength to stay at medium or strong.",
            record.steps[1].errors,
        )
        self.assertEqual(self.package_output_path.read_bytes(), original_bytes)

    def test_create_job_marks_second_step_skipped_when_first_step_fails(self) -> None:
        record = self.job_service.create_job(
            build_request(
                first_leg=build_request().post_chicken_to_farm.model_copy(
                    update={
                        "minigame": build_request().post_chicken_to_farm.minigame.model_copy(
                            update={
                                "parameters": {
                                    "cropType": "tomato",
                                    "targetCount": 6,
                                    "timeLimitSeconds": 300,
                                    "rowCount": 2,
                                    "assistLevel": "high",
                                }
                            }
                        )
                    }
                )
            )
        )

        self.assertEqual(record.status, "failed")
        self.assertFalse(record.result.is_valid)
        self.assertEqual(record.steps[0].status, "failed")
        self.assertEqual(record.steps[1].status, "skipped")

    def test_store_migrates_existing_jobs_with_default_review_notes(self) -> None:
        migration_path = Path(self._temp_dir.name) / "migration_story_orchestrator.db"
        request_json = json.dumps(build_request().model_dump(mode="json"), sort_keys=True)
        with sqlite3.connect(migration_path) as connection:
            connection.execute(
                """
                CREATE TABLE generated_standing_slice_jobs (
                    job_id TEXT PRIMARY KEY,
                    status TEXT NOT NULL,
                    approval_status TEXT NOT NULL,
                    request_json TEXT NOT NULL,
                    result_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
                """
            )
            connection.execute(
                """
                INSERT INTO generated_standing_slice_jobs (
                    job_id,
                    status,
                    approval_status,
                    request_json,
                    result_json,
                    created_at,
                    updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    "job-legacy",
                    "completed",
                    "pending_review",
                    request_json,
                    "null",
                    "2026-04-14T00:00:00+00:00",
                    "2026-04-14T00:00:00+00:00",
                ),
            )

        migrated_store = GeneratedStandingSliceJobStore(migration_path)
        fetched = migrated_store.get_job("job-legacy")

        self.assertIsNotNone(fetched)
        assert fetched is not None
        self.assertEqual(fetched.approval_status, "pending_review")
        self.assertEqual(fetched.review_notes, "")
        self.assertEqual(fetched.publish_status, "not_published")
        self.assertEqual(fetched.published_at, "")


class GeneratedStandingSliceJobEndpointTests(unittest.TestCase):
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
        job_store = GeneratedStandingSliceJobStore(database_path)
        job_service = GeneratedStandingSliceJobService(
            standing_slice_service=standing_slice_service,
            job_store=job_store,
            artifact_archive=GeneratedStandingSliceArtifactArchive(output_root=output_root),
            publisher=GeneratedStandingSlicePublisher(
                output_root=output_root,
                live_package_path=package_output_path,
            ),
        )
        package_output_path.parent.mkdir(parents=True, exist_ok=True)
        package_output_path.write_text(json.dumps(build_seed_package(), indent=2), encoding="utf-8")
        self.client = TestClient(
            create_app(
                settings,
                generated_storyboard_service=storyboard_service,
                generated_minigame_service=minigame_service,
                generated_package_assembly_service=package_assembly_service,
                generated_standing_slice_service=standing_slice_service,
                generated_standing_slice_job_store=job_store,
                generated_standing_slice_job_service=job_service,
            )
        )

    def tearDown(self) -> None:
        self.client.close()
        self._temp_dir.cleanup()

    def test_create_and_fetch_generated_standing_slice_job(self) -> None:
        create_response = self.client.post(
            "/api/v1/generated-standing-slice-jobs",
            json=build_request().model_dump(mode="json"),
        )

        self.assertEqual(create_response.status_code, 201)
        created = create_response.json()
        self.assertEqual(created["status"], "completed")
        self.assertEqual(created["review_notes"], "")
        self.assertEqual(created["publish_status"], "not_published")
        self.assertEqual(created["steps"][0]["status"], "completed")
        self.assertEqual(created["steps"][1]["status"], "completed")
        self.assertEqual(len(created["assets"]), 12)

        fetch_response = self.client.get(
            f"/api/v1/generated-standing-slice-jobs/{created['job_id']}"
        )

        self.assertEqual(fetch_response.status_code, 200)
        fetched = fetch_response.json()
        self.assertEqual(fetched["job_id"], created["job_id"])
        self.assertEqual(fetched["review_notes"], "")
        self.assertEqual(fetched["publish_status"], "not_published")
        self.assertEqual(fetched["result"]["unity_package"]["Beats"][-1]["BeatId"], "pre_farm_bridge")

        asset_id = created["assets"][0]["asset_id"]
        asset_response = self.client.get(
            f"/api/v1/generated-standing-slice-jobs/{created['job_id']}/assets/{asset_id}/content"
        )

        self.assertEqual(asset_response.status_code, 200)
        self.assertEqual(asset_response.content, b"fake-png")
        self.assertEqual(asset_response.headers["content-type"], "image/png")

    def test_update_generated_standing_slice_job_review(self) -> None:
        create_response = self.client.post(
            "/api/v1/generated-standing-slice-jobs",
            json=build_request().model_dump(mode="json"),
        )
        created = create_response.json()

        review_response = self.client.post(
            f"/api/v1/generated-standing-slice-jobs/{created['job_id']}/review",
            json={
                "approval_status": "rejected",
                "review_notes": "Continuity breaks after the tool beat.",
            },
        )

        self.assertEqual(review_response.status_code, 200)
        updated = review_response.json()
        self.assertEqual(updated["approval_status"], "rejected")
        self.assertEqual(updated["review_notes"], "Continuity breaks after the tool beat.")
        self.assertNotEqual(updated["updated_at"], created["updated_at"])

        fetch_response = self.client.get(
            f"/api/v1/generated-standing-slice-jobs/{created['job_id']}"
        )
        self.assertEqual(fetch_response.status_code, 200)
        fetched = fetch_response.json()
        self.assertEqual(fetched["approval_status"], "rejected")
        self.assertEqual(fetched["review_notes"], "Continuity breaks after the tool beat.")

    def test_unknown_generated_standing_slice_job_returns_404(self) -> None:
        response = self.client.get("/api/v1/generated-standing-slice-jobs/does-not-exist")

        self.assertEqual(response.status_code, 404)
        self.assertEqual(response.json()["detail"], "Standing slice job not found.")

    def test_review_update_for_unknown_job_returns_404(self) -> None:
        response = self.client.post(
            "/api/v1/generated-standing-slice-jobs/does-not-exist/review",
            json={
                "approval_status": "pending_review",
                "review_notes": "Need another look.",
            },
        )

        self.assertEqual(response.status_code, 404)
        self.assertEqual(response.json()["detail"], "Standing slice job not found.")

    def test_review_page_loads(self) -> None:
        response = self.client.get("/review/standing-slice")

        self.assertEqual(response.status_code, 200)
        self.assertIn("Standing Slice Review", response.text)
        self.assertIn("Approve Job", response.text)
        self.assertIn("Publish Approved Job", response.text)
        self.assertIn("reviewNotes", response.text)

if __name__ == "__main__":
    unittest.main()
