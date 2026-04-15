import tempfile
import unittest
from pathlib import Path

from pydantic import ValidationError

from app.generated_storyboard_models import GeneratedImageAsset, GeneratedSpeechAsset, GeneratedStoryboardContext, GeneratedStoryboardCutsceneRequest
from app.generated_storyboards import GeneratedStoryboardService, TemplateStoryboardPlanner
from app.storyboard_llm_planner import OpenAIStoryboardPlanner, StoryboardPlannerChain


class OpenAIStoryboardPlannerTests(unittest.TestCase):
    def test_openai_storyboard_planner_emits_generated_shots(self) -> None:
        planner = OpenAIStoryboardPlanner(
            client=FakeStructuredOutputClient(
                {
                    "shots": [
                        {
                            "subtitle_text": "Garrett lowers the seed basket over the first furrow.",
                            "narration_text": "Garrett lowers the seed basket over the first furrow.",
                            "image_prompt": "Old Garrett kneels beside the first carrot furrow at sunrise, holding a seed basket with a patient teaching gesture.",
                            "duration_seconds": 3.4,
                        },
                        {
                            "subtitle_text": "Plant 3 carrots and keep the row steady.",
                            "narration_text": "Plant 3 carrots and keep the row steady.",
                            "image_prompt": "A close farm-row view showing marked carrot plots, hand tools, and a clear inviting path into the planting task.",
                            "duration_seconds": 3.6,
                        },
                        {
                            "subtitle_text": "The morning is ready. Step in and answer the field.",
                            "narration_text": "The morning is ready. Step in and answer the field.",
                            "image_prompt": "A forward-looking sunrise composition from the row edge into the open carrot plots, with warm storybook depth and no UI text.",
                            "duration_seconds": 3.2,
                        },
                    ]
                }
            )
        )

        plan = planner.plan(build_request())

        self.assertEqual(plan.shots[0].subtitle_text, "Garrett lowers the seed basket over the first furrow.")
        self.assertNotEqual(plan.shots[0].subtitle_text, "Nice work. The carrot beds are finally ready for you.")
        self.assertEqual(plan.shots[1].duration_seconds, 3.6)

    def test_openai_storyboard_planner_includes_story_guardrails_in_prompt(self) -> None:
        client = CapturingStructuredOutputClient(
            {
                "shots": [
                    {
                        "subtitle_text": "A rake clatters from the shed roof beside Clara.",
                        "narration_text": "A rake clatters from the shed roof beside Clara.",
                        "image_prompt": "Miss Clara recoils beneath a rattling shed roof as a rake slides loose overhead, wide shot, windy afternoon, dynamic motion.",
                        "duration_seconds": 3.0,
                    },
                    {
                        "subtitle_text": "Fresh mud tracks point toward the missing watering kit.",
                        "narration_text": "Fresh mud tracks point toward the missing watering kit.",
                        "image_prompt": "Low-angle tracking shot of muddy boot prints curving from the shed toward the field path, abandoned watering tools in the distance.",
                        "duration_seconds": 3.2,
                    },
                    {
                        "subtitle_text": "Clara points down the trail and sends you after them.",
                        "narration_text": "Clara points down the trail and sends you after them.",
                        "image_prompt": "Over-the-shoulder view behind Miss Clara as she points toward a cluttered field path, urgent but cozy farm atmosphere, no text.",
                        "duration_seconds": 3.4,
                    },
                ]
            }
        )
        planner = OpenAIStoryboardPlanner(client=client)

        planner.plan(build_request())

        assert client.last_system_prompt is not None
        assert client.last_user_prompt is not None
        self.assertIn("actual story beat", client.last_system_prompt)
        self.assertIn("distinct visual moment", client.last_system_prompt)
        self.assertIn("Every shot must last between 2 and 4 seconds.", client.last_user_prompt)
        self.assertIn("Write 3 to 6 storyboard shots.", client.last_user_prompt)
        self.assertIn("Narration text must exactly match the subtitle text", client.last_user_prompt)
        self.assertIn("never more than 12 words", client.last_user_prompt)
        self.assertIn("never more than 14 words", client.last_user_prompt)
        self.assertIn("Do not repeat the same camera angle", client.last_user_prompt)
        self.assertIn("not by showing a tutorial card", client.last_user_prompt)
        self.assertIn('"mission_configuration_summary": "Recover 2 watering tools around the field path with medium hints."', client.last_user_prompt)
        self.assertIn('"prior_story_summary": "Old Garrett settled the chicken pen, but the tool rack is still in disarray."', client.last_user_prompt)

    def test_storyboard_planner_chain_falls_back_to_template_when_llm_planner_fails(self) -> None:
        service = GeneratedStoryboardService(
            output_root=Path(tempfile.mkdtemp()) / "Assets" / "_Project" / "Resources",
            package_output_path=Path(tempfile.mkdtemp()) / "Assets" / "_Project" / "Resources" / "StoryPackages" / "StoryPackage_IntroChickenSample.json",
            planner=StoryboardPlannerChain(
                planners=[
                    OpenAIStoryboardPlanner(client=FailingStructuredOutputClient()),
                    TemplateStoryboardPlanner(),
                ]
            ),
            image_generator=FakeImageGenerator(),
            speech_generator=FakeSpeechGenerator(),
            default_voice_id="voice-test",
        )

        result = service.create_package(build_request())
        first_shot = result.unity_package["Beats"][0]["Storyboard"]["Shots"][0]

        self.assertEqual(first_shot["SubtitleText"], "Nice work. The carrot beds are finally ready for you.")

    def test_openai_storyboard_planner_rejects_lines_that_are_too_long_for_short_beats(self) -> None:
        planner = OpenAIStoryboardPlanner(
            client=FakeStructuredOutputClient(
                {
                    "shots": [
                        {
                            "subtitle_text": "Garrett urgently explains that the first seed basket must be lowered carefully before anything else happens today.",
                            "narration_text": "Garrett urgently explains that the first seed basket must be lowered carefully before anything else happens today.",
                            "image_prompt": "Old Garrett at the furrow, sunrise, urgent expression.",
                            "duration_seconds": 3.2,
                        },
                        {
                            "subtitle_text": "The row is waiting.",
                            "narration_text": "The row is waiting.",
                            "image_prompt": "A tidy carrot row at dawn.",
                            "duration_seconds": 3.0,
                        },
                        {
                            "subtitle_text": "Step in now.",
                            "narration_text": "Step in now.",
                            "image_prompt": "A forward-looking farm path into the plots.",
                            "duration_seconds": 3.1,
                        },
                    ]
                }
            )
        )

        with self.assertRaises(ValidationError):
            planner.plan(build_request())

    def test_openai_storyboard_planner_rejects_mismatched_narration_text(self) -> None:
        planner = OpenAIStoryboardPlanner(
            client=FakeStructuredOutputClient(
                {
                    "shots": [
                        {
                            "subtitle_text": "Garrett lifts the lantern toward the first row.",
                            "narration_text": "Garrett scans the barn before the planting starts.",
                            "image_prompt": "Old Garrett lifts a lantern over the first carrot row at dawn, cautious posture, warm storybook light.",
                            "duration_seconds": 3.2,
                        },
                        {
                            "subtitle_text": "A broken handle blocks the seed cart.",
                            "narration_text": "A broken handle blocks the seed cart.",
                            "image_prompt": "The seed cart leans sideways in the dirt path with a snapped wooden handle, medium shot.",
                            "duration_seconds": 3.1,
                        },
                        {
                            "subtitle_text": "Clear the path and take the field.",
                            "narration_text": "Clear the path and take the field.",
                            "image_prompt": "The farm path opens toward the ready plots as Old Garrett signals the player forward.",
                            "duration_seconds": 3.0,
                        },
                    ]
                }
            )
        )

        with self.assertRaises(ValidationError):
            planner.plan(build_request())


class FakeStructuredOutputClient:
    def __init__(self, payload: dict) -> None:
        self._payload = payload

    def generate(self, *, response_model, schema_name: str, system_prompt: str, user_prompt: str):
        _ = schema_name, system_prompt, user_prompt
        return response_model.model_validate(self._payload)


class CapturingStructuredOutputClient(FakeStructuredOutputClient):
    def __init__(self, payload: dict) -> None:
        super().__init__(payload)
        self.last_system_prompt: str | None = None
        self.last_user_prompt: str | None = None

    def generate(self, *, response_model, schema_name: str, system_prompt: str, user_prompt: str):
        self.last_system_prompt = system_prompt
        self.last_user_prompt = user_prompt
        return super().generate(
            response_model=response_model,
            schema_name=schema_name,
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )


class FailingStructuredOutputClient:
    def generate(self, *, response_model, schema_name: str, system_prompt: str, user_prompt: str):
        _ = response_model, schema_name, system_prompt, user_prompt
        raise RuntimeError("planner unavailable")


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


def build_request() -> GeneratedStoryboardCutsceneRequest:
    return GeneratedStoryboardCutsceneRequest(
        package_id="storypkg_intro_chicken_sample",
        package_display_name="Intro Chicken Sample",
        beat_id="post_chicken_bridge",
        display_name="Post Chicken Bridge",
        scene_name="PostChickenCutscene",
        next_scene_name="MidpointPlaceholder",
        linked_minigame_beat_id=None,
        story_brief="Old Garrett turns the player from the chicken pen toward the first planting task.",
        style_preset_id="farm_storybook_v1",
        voice_id="voice-test",
        context=GeneratedStoryboardContext(
            character_name="Miss Clara",
            crop_name="carrots",
            minigame_goal="Recover the missing watering kit before planting begins",
            prior_story_summary="Old Garrett settled the chicken pen, but the tool rack is still in disarray.",
            world_state=["tomatoes_unlocked", "watering_tools_unlocked"],
            present_character_names=["Miss Clara", "Old Garrett"],
            selected_generator_id="find_tools_cluster_v1",
            selected_generator_display_name="Find Lost Tools",
            mission_configuration_summary="Recover 2 watering tools around the field path with medium hints.",
        ),
    )


if __name__ == "__main__":
    unittest.main()
