from __future__ import annotations

import json
import tempfile
import time
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from app.generated_minigames import GeneratedMinigameBeatService
from app.generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset
from app.generated_storyboards import GeneratedStoryboardService, TemplateStoryboardPlanner
from app.runtime_service import RuntimeSessionService
from app.runtime_store import RuntimeSessionStore
from app.runtime_turn_generation import RuntimeTurnGenerationService

from scripts.eval_chicken_ruckus_slice import run


class _FakeImageGenerator:
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


class _FakeSpeechGenerator:
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


def _build_fake_runtime_service(tmp_root: Path) -> RuntimeSessionService:
    database_path = tmp_root / "story_orchestrator.db"
    output_root = tmp_root / "Assets" / "_Project" / "Resources"
    package_output_path = output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"

    storyboard_service = GeneratedStoryboardService(
        output_root=output_root,
        package_output_path=package_output_path,
        planner=TemplateStoryboardPlanner(),
        image_generator=_FakeImageGenerator(),
        speech_generator=_FakeSpeechGenerator(),
        default_voice_id="voice-test",
    )
    minigame_service = GeneratedMinigameBeatService(package_output_path=package_output_path)

    runtime_root = tmp_root / "data" / "runtime"
    generation_service = RuntimeTurnGenerationService(
        runtime_root=runtime_root,
        storyboard_service=storyboard_service,
        minigame_service=minigame_service,
        default_voice_id="voice-test",
    )
    store = RuntimeSessionStore(database_path)
    return RuntimeSessionService(store=store, generation_service=generation_service)


class EvalChickenRuckusSliceTests(unittest.TestCase):
    def test_harness_runs_three_turns_and_writes_manifests(self) -> None:
        with TemporaryDirectory() as service_tmp, TemporaryDirectory() as out_tmp:
            fake_service = _build_fake_runtime_service(Path(service_tmp))
            try:
                output_dir = Path(out_tmp) / "eval_out"
                summaries = run(
                    output_dir=output_dir,
                    story_type_id="chicken_ruckus_intro_v1",
                    style_preset_id="watercolor_intro_v1",
                    runtime_service=fake_service,
                    max_turns=3,
                    poll_interval_seconds=0.0,
                    poll_timeout_seconds=30.0,
                )
                self.assertEqual(len(summaries), 3)
                for i in (1, 2, 3):
                    turn_dir = output_dir / f"turn_{i:03d}"
                    self.assertTrue(turn_dir.is_dir(), f"missing {turn_dir}")
                    self.assertTrue((turn_dir / "manifest.json").is_file())
                top = json.loads((output_dir / "manifest.json").read_text(encoding="utf-8"))
                self.assertEqual(top["story_type_id"], "chicken_ruckus_intro_v1")
                self.assertEqual(top["style_preset_id"], "watercolor_intro_v1")
                self.assertEqual(len(top["turns"]), 3)
            finally:
                fake_service.shutdown()

    def test_harness_copies_artifact_files_into_turn_dir(self) -> None:
        with TemporaryDirectory() as service_tmp, TemporaryDirectory() as out_tmp:
            fake_service = _build_fake_runtime_service(Path(service_tmp))
            try:
                output_dir = Path(out_tmp) / "eval_out"
                run(
                    output_dir=output_dir,
                    story_type_id="chicken_ruckus_intro_v1",
                    style_preset_id="watercolor_intro_v1",
                    runtime_service=fake_service,
                    max_turns=1,
                    poll_interval_seconds=0.0,
                    poll_timeout_seconds=30.0,
                )
                turn_dir = output_dir / "turn_001"
                copied_files = [
                    p.name
                    for p in turn_dir.iterdir()
                    if p.is_file() and p.suffix in {".png", ".wav", ".mp3", ".json"}
                ]
                self.assertIn("manifest.json", copied_files)
                self.assertTrue(
                    any(name.endswith(".png") for name in copied_files),
                    f"expected at least one PNG in {turn_dir}; found {copied_files}",
                )
            finally:
                fake_service.shutdown()


if __name__ == "__main__":
    unittest.main()
