import json
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
from app.generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset
from app.generated_storyboards import GeneratedStoryboardService, TemplateStoryboardPlanner
from app.main import create_app
from app.models import GeneratedStandingSliceJobReviewRequest
from app.store import GeneratedStandingSliceJobStore
from tests.test_generated_standing_slice import build_request, build_seed_package


class GeneratedStandingSliceJobArtifactServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.storyboard_service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=RevisionedImageGenerator(prefix="service-image"),
            speech_generator=RevisionedSpeechGenerator(prefix="service-audio"),
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

    def test_create_job_archives_package_and_asset_snapshots_per_job(self) -> None:
        self.package_output_path.parent.mkdir(parents=True, exist_ok=True)
        self.package_output_path.write_text(json.dumps(build_seed_package(), indent=2), encoding="utf-8")

        record = self.job_service.create_job(build_request())

        package_snapshot_path = Path(record.result.package_output_path)
        self.assertTrue(package_snapshot_path.exists())
        self.assertIn("_job_runs", str(package_snapshot_path))
        self.assertIn(record.job_id, str(package_snapshot_path))
        archived_package = json.loads(package_snapshot_path.read_text(encoding="utf-8"))
        self.assertEqual(archived_package["Beats"][-1]["BeatId"], "pre_farm_bridge")

        image_asset = next(asset for asset in record.assets if asset.asset_type == "image")
        audio_asset = next(asset for asset in record.assets if asset.asset_type == "audio")
        self.assertTrue(Path(image_asset.output_path).exists())
        self.assertTrue(Path(audio_asset.output_path).exists())
        self.assertIn("_job_runs", image_asset.output_path)
        self.assertIn(record.job_id, image_asset.output_path)
        self.assertEqual(Path(image_asset.output_path).read_bytes(), b"service-image-1")
        self.assertIn("source_output_path", image_asset.metadata)
        self.assertNotEqual(image_asset.metadata["source_output_path"], image_asset.output_path)

        self.assertEqual(Path(audio_asset.output_path).read_bytes(), b"service-audio-1")
        self.assertIn("alignment_path", audio_asset.metadata)
        self.assertIn("source_alignment_path", audio_asset.metadata)
        self.assertIn(record.job_id, audio_asset.metadata["alignment_path"])
        self.assertTrue(Path(audio_asset.metadata["alignment_path"]).exists())


class GeneratedStandingSliceJobArtifactEndpointTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.package_output_path = package_output_path
        settings = Settings(
            gemini_api_key="test-gemini",
            elevenlabs_api_key="test-eleven",
            database_path=str(database_path),
        )
        storyboard_service = GeneratedStoryboardService(
            output_root=output_root,
            package_output_path=package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=RevisionedImageGenerator(prefix="revision-image"),
            speech_generator=RevisionedSpeechGenerator(prefix="revision-audio"),
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

    def test_older_job_asset_content_remains_stable_after_newer_regeneration(self) -> None:
        first = self.client.post(
            "/api/v1/generated-standing-slice-jobs",
            json=build_request().model_dump(mode="json"),
        ).json()
        second = self.client.post(
            "/api/v1/generated-standing-slice-jobs",
            json=build_request().model_dump(mode="json"),
        ).json()

        first_asset_id = next(asset["asset_id"] for asset in first["assets"] if asset["asset_type"] == "image")
        second_asset_id = next(asset["asset_id"] for asset in second["assets"] if asset["asset_type"] == "image")
        first_asset_response = self.client.get(
            f"/api/v1/generated-standing-slice-jobs/{first['job_id']}/assets/{first_asset_id}/content"
        )
        second_asset_response = self.client.get(
            f"/api/v1/generated-standing-slice-jobs/{second['job_id']}/assets/{second_asset_id}/content"
        )

        self.assertEqual(first_asset_response.status_code, 200)
        self.assertEqual(second_asset_response.status_code, 200)
        self.assertEqual(first_asset_response.content, b"revision-image-1")
        self.assertEqual(second_asset_response.content, b"revision-image-7")
        self.assertNotEqual(first_asset_response.content, second_asset_response.content)

    def test_publish_requires_approved_job(self) -> None:
        created = self.client.post(
            "/api/v1/generated-standing-slice-jobs",
            json=build_request().model_dump(mode="json"),
        ).json()

        response = self.client.post(
            f"/api/v1/generated-standing-slice-jobs/{created['job_id']}/publish"
        )

        self.assertEqual(response.status_code, 409)
        self.assertEqual(
            response.json()["detail"],
            "Standing slice job must be approved before publish.",
        )

    def test_publish_approved_job_restores_live_package_and_assets(self) -> None:
        first = self.client.post(
            "/api/v1/generated-standing-slice-jobs",
            json=build_request().model_dump(mode="json"),
        ).json()
        self.client.post(
            f"/api/v1/generated-standing-slice-jobs/{first['job_id']}/review",
            json=GeneratedStandingSliceJobReviewRequest(
                approval_status="approved",
                review_notes="Ship this version.",
            ).model_dump(mode="json"),
        )
        second = self.client.post(
            "/api/v1/generated-standing-slice-jobs",
            json=build_request().model_dump(mode="json"),
        ).json()

        first_image = next(asset for asset in first["assets"] if asset["asset_type"] == "image")
        first_audio = next(asset for asset in first["assets"] if asset["asset_type"] == "audio")
        live_image_path = Path(first_image["metadata"]["source_output_path"])
        live_audio_path = Path(first_audio["metadata"]["source_output_path"])
        live_alignment_path = Path(first_audio["metadata"]["source_alignment_path"])
        self.assertEqual(live_image_path.read_bytes(), b"revision-image-7")
        self.assertEqual(live_audio_path.read_bytes(), b"revision-audio-7")

        publish_response = self.client.post(
            f"/api/v1/generated-standing-slice-jobs/{first['job_id']}/publish"
        )

        self.assertEqual(publish_response.status_code, 200)
        published = publish_response.json()
        self.assertEqual(published["publish_status"], "published")
        self.assertTrue(published["published_at"])

        fetch_response = self.client.get(
            f"/api/v1/generated-standing-slice-jobs/{first['job_id']}"
        )
        self.assertEqual(fetch_response.status_code, 200)
        fetched = fetch_response.json()
        self.assertEqual(fetched["publish_status"], "published")
        self.assertTrue(fetched["published_at"])

        archived_package_path = Path(first["result"]["package_output_path"])
        self.assertEqual(
            self.package_output_path.read_text(encoding="utf-8"),
            archived_package_path.read_text(encoding="utf-8"),
        )
        self.assertEqual(live_image_path.read_bytes(), b"revision-image-1")
        self.assertEqual(live_audio_path.read_bytes(), b"revision-audio-1")
        self.assertEqual(
            live_alignment_path.read_text(encoding="utf-8"),
            Path(first_audio["metadata"]["alignment_path"]).read_text(encoding="utf-8"),
        )


class RevisionedImageGenerator:
    def __init__(self, *, prefix: str) -> None:
        self._count = 0
        self._prefix = prefix

    def generate_image(
        self,
        *,
        prompt: str,
        reference_image_paths: list[str],
        output_path: Path,
        aspect_ratio: str,
        image_size: str,
    ) -> GeneratedImageAsset:
        self._count += 1
        payload = f"{self._prefix}-{self._count}".encode("utf-8")
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_bytes(payload)
        return GeneratedImageAsset(
            output_path=output_path,
            mime_type="image/png",
            provider_name="revision-image",
            provider_model="revision-image-v1",
            fallback_used=False,
            source_metadata={"prompt": prompt},
        )


class RevisionedSpeechGenerator:
    def __init__(self, *, prefix: str) -> None:
        self._count = 0
        self._prefix = prefix

    def generate_speech(
        self,
        *,
        text: str,
        voice_id: str,
        output_path: Path,
        previous_text: str,
        next_text: str,
    ) -> GeneratedSpeechAsset:
        self._count += 1
        payload = f"{self._prefix}-{self._count}".encode("utf-8")
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_bytes(payload)
        alignment_path = output_path.with_suffix(".alignment.json")
        alignment_path.write_text(json.dumps({"characters": [str(self._count)]}), encoding="utf-8")
        return GeneratedSpeechAsset(
            output_path=output_path,
            alignment_path=alignment_path,
            duration_seconds=1.0 + self._count,
            mime_type="audio/mpeg",
            provider_name="revision-speech",
            provider_model="revision-speech-v1",
            fallback_used=False,
            source_metadata={"text": text, "voice_id": voice_id},
        )


if __name__ == "__main__":
    unittest.main()
