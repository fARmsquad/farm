import tempfile
import unittest
from pathlib import Path

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
                            "subtitle_text": "Garrett tips the seed basket toward the first furrow and lowers his voice.",
                            "narration_text": "Garrett tips the seed basket toward the first furrow and lowers his voice.",
                            "image_prompt": "Old Garrett kneels beside the first carrot furrow at sunrise, holding a seed basket with a patient teaching gesture.",
                            "duration_seconds": 3.4,
                        },
                        {
                            "subtitle_text": "Plant 3 carrots in 5 minutes, then follow the neat rhythm of the row.",
                            "narration_text": "Plant 3 carrots in 5 minutes, then follow the neat rhythm of the row.",
                            "image_prompt": "A close farm-row view showing marked carrot plots, hand tools, and a clear inviting path into the planting task.",
                            "duration_seconds": 3.6,
                        },
                        {
                            "subtitle_text": "The morning is ready for your hands. Step in and make the field answer back.",
                            "narration_text": "The morning is ready for your hands. Step in and make the field answer back.",
                            "image_prompt": "A forward-looking sunrise composition from the row edge into the open carrot plots, with warm storybook depth and no UI text.",
                            "duration_seconds": 3.2,
                        },
                    ]
                }
            )
        )

        plan = planner.plan(build_request())

        self.assertEqual(plan.shots[0].subtitle_text, "Garrett tips the seed basket toward the first furrow and lowers his voice.")
        self.assertNotEqual(plan.shots[0].subtitle_text, "Nice work. The carrot beds are finally ready for you.")
        self.assertEqual(plan.shots[1].duration_seconds, 3.6)

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


class FakeStructuredOutputClient:
    def __init__(self, payload: dict) -> None:
        self._payload = payload

    def generate(self, *, response_model, schema_name: str, system_prompt: str, user_prompt: str):
        _ = schema_name, system_prompt, user_prompt
        return response_model.model_validate(self._payload)


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
            character_name="Old Garrett",
            crop_name="carrots",
            minigame_goal="Plant 3 carrots in 5 minutes",
        ),
    )


if __name__ == "__main__":
    unittest.main()
