import tempfile
import unittest
from pathlib import Path

from app.generated_minigames import GeneratedMinigameBeatService
from app.generated_package_assembly import GeneratedPackageAssemblyService
from app.generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset
from app.generated_storyboards import GeneratedStoryboardService, TemplateStoryboardPlanner
from app.story_sequence_models import StorySequenceSessionCreateRequest
from app.story_sequence_service import StorySequenceSessionService
from app.story_sequence_store import StorySequenceSessionStore
from app.story_sequence_turn_director import (
    OpenAIStorySequenceTurnDirector,
    StorySequenceGeneratorOption,
    StorySequenceTurnDirective,
)


class OpenAIStorySequenceTurnDirectorTests(unittest.TestCase):
    def test_choose_turn_prompt_demands_contextual_conflict_and_mission_handoff(self) -> None:
        client = CapturingStructuredOutputClient(
            {
                "generator_id": "find_tools_cluster_v1",
                "character_name": "Miss Clara",
                "cutscene_display_name": "Clara Hears the Shed Rattle",
                "story_brief": (
                    "Miss Clara hears a crash from the shed roof and spots muddy tracks near the missing watering kit. "
                    "She asks the player to recover the scattered tools before the first planting lesson can begin."
                ),
            }
        )
        director = OpenAIStorySequenceTurnDirector(client=client)

        directive = director.choose_turn(
            narrative_seed="The farm is waking up after a messy first morning.",
            beat_cursor=1,
            fit_tags=["tutorial", "cozy", "farming"],
            world_state=["tomatoes_unlocked", "watering_tools_unlocked"],
            difficulty_band="intro",
            last_minigame_goal="Catch the last runaway chicken in the pen",
            recent_turn_summaries=["Old Garrett: Catch the last runaway chicken in the pen."],
            candidate_generators=[
                StorySequenceGeneratorOption(
                    generator_id="find_tools_cluster_v1",
                    display_name="Find Lost Tools",
                    minigame_id="find_tools",
                    fit_tags=["search", "tutorial"],
                    preview_text_template="Recover missing tools around the farmyard.",
                )
            ],
            candidate_character_names=["Old Garrett", "Miss Clara"],
            default_generator_id="find_tools_cluster_v1",
            default_character_name="Miss Clara",
        )

        self.assertEqual(directive.generator_id, "find_tools_cluster_v1")
        assert client.last_system_prompt is not None
        assert client.last_user_prompt is not None
        self.assertIn("concrete conflict or change in the world", client.last_system_prompt)
        self.assertIn("mad-libs style variation", client.last_user_prompt)
        self.assertIn("introduce a concrete problem", client.last_user_prompt)
        self.assertIn('"recent_turn_summaries": [', client.last_user_prompt)
        self.assertIn('"candidate_generators": [', client.last_user_prompt)
        self.assertIn('"default_character_name": "Miss Clara"', client.last_user_prompt)


class StorySequenceTurnDirectorIntegrationTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_dir = tempfile.TemporaryDirectory()
        self.database_path = Path(self._temp_dir.name) / "story_orchestrator.db"
        self.output_root = Path(self._temp_dir.name) / "Assets" / "_Project" / "Resources"
        self.package_output_path = self.output_root / "StoryPackages" / "StoryPackage_IntroChickenSample.json"
        self.storyboard_service = GeneratedStoryboardService(
            output_root=self.output_root,
            package_output_path=self.package_output_path,
            planner=TemplateStoryboardPlanner(),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
            default_voice_id="voice-test",
        )
        self.minigame_service = GeneratedMinigameBeatService(package_output_path=self.package_output_path)
        self.package_assembly_service = GeneratedPackageAssemblyService(
            minigame_service=self.minigame_service,
            storyboard_service=self.storyboard_service,
        )
        self.store = StorySequenceSessionStore(self.database_path)

    def tearDown(self) -> None:
        self._temp_dir.cleanup()

    def test_advance_session_uses_valid_llm_directed_turn_choice(self) -> None:
        service = StorySequenceSessionService(
            package_assembly_service=self.package_assembly_service,
            store=self.store,
            default_voice_id="voice-test",
            turn_director=FakeTurnDirector(
                StorySequenceTurnDirective(
                    generator_id="find_tools_cluster_v1",
                    character_name="Miss Clara",
                    cutscene_display_name="Clara Sends You Searching",
                    story_brief="Miss Clara notices the yard is missing key tools and sends the player into a quick search before the planting rhythm starts.",
                )
            ),
        )

        created = service.create_session(StorySequenceSessionCreateRequest())
        advanced = service.advance_session(created.session_id)

        self.assertEqual(advanced.turn.generator_id, "find_tools_cluster_v1")
        self.assertEqual(advanced.turn.character_name, "Miss Clara")
        self.assertEqual(advanced.turn.request.cutscene.display_name, "Clara Sends You Searching")
        self.assertIn("missing key tools", advanced.turn.request.cutscene.story_brief)

    def test_advance_session_falls_back_when_llm_choice_is_invalid(self) -> None:
        service = StorySequenceSessionService(
            package_assembly_service=self.package_assembly_service,
            store=self.store,
            default_voice_id="voice-test",
            turn_director=FakeTurnDirector(
                StorySequenceTurnDirective(
                    generator_id="made_up_generator",
                    character_name="Ghost Farmer",
                    cutscene_display_name="Broken Choice",
                    story_brief="This should not survive validation.",
                )
            ),
        )

        created = service.create_session(StorySequenceSessionCreateRequest())
        advanced = service.advance_session(created.session_id)

        self.assertEqual(advanced.turn.generator_id, "plant_rows_v1")
        self.assertEqual(advanced.turn.character_name, "Old Garrett")
        self.assertEqual(advanced.turn.request.cutscene.display_name, "Sequence Bridge 1")
        self.assertNotEqual(advanced.turn.request.cutscene.story_brief, "This should not survive validation.")

    def test_advance_session_passes_story_context_and_mission_shape_into_cutscene_request(self) -> None:
        service = StorySequenceSessionService(
            package_assembly_service=self.package_assembly_service,
            store=self.store,
            default_voice_id="voice-test",
            turn_director=FakeTurnDirector(
                StorySequenceTurnDirective(
                    generator_id="find_tools_cluster_v1",
                    character_name="Miss Clara",
                    cutscene_display_name="Clara Hears the Shed Rattle",
                    story_brief=(
                        "Miss Clara hears a crash from the shed roof and realizes the watering kit is missing. "
                        "She sends the player after the scattered tools before the first planting lesson can begin."
                    ),
                )
            ),
        )

        created = service.create_session(StorySequenceSessionCreateRequest())
        advanced = service.advance_session(created.session_id)
        cutscene_context = advanced.turn.request.cutscene.context

        self.assertEqual(cutscene_context.character_name, "Miss Clara")
        self.assertEqual(cutscene_context.selected_generator_id, "find_tools_cluster_v1")
        self.assertEqual(cutscene_context.selected_generator_display_name, "Find Tools Cluster V1")
        self.assertIn("starter tools", cutscene_context.mission_configuration_summary)
        self.assertIn("yard", cutscene_context.mission_configuration_summary.lower())
        self.assertIn("Old Garrett", cutscene_context.present_character_names)
        self.assertIn("Miss Clara", cutscene_context.present_character_names)
        self.assertIn("earliest_tutorial_bridge", cutscene_context.world_state)
        self.assertEqual(
            cutscene_context.prior_story_summary,
            "No previous turn summary yet. Use the existing farm state and story brief to create the next conflict.",
        )


class FakeTurnDirector:
    def __init__(self, directive: StorySequenceTurnDirective) -> None:
        self._directive = directive

    def choose_turn(self, **_: object) -> StorySequenceTurnDirective:
        return self._directive


class CapturingStructuredOutputClient:
    def __init__(self, payload: dict) -> None:
        self._payload = payload
        self.last_system_prompt: str | None = None
        self.last_user_prompt: str | None = None

    def generate(self, *, response_model, schema_name: str, system_prompt: str, user_prompt: str):
        _ = schema_name
        self.last_system_prompt = system_prompt
        self.last_user_prompt = user_prompt
        return response_model.model_validate(self._payload)


class FakeImageGenerator:
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


class FakeSpeechGenerator:
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


if __name__ == "__main__":
    unittest.main()
