from __future__ import annotations

import inspect
import unittest

from app.image_generator_protocol import ImageGenerator
from app.storyboard_provider_clients import GeminiImageGenerator, OpenAIImageGenerator


class ImageGeneratorProtocolTests(unittest.TestCase):
    def test_gemini_image_generator_satisfies_protocol(self) -> None:
        # Placeholder credentials — no network call is made here.
        instance = GeminiImageGenerator(api_key="placeholder", models=["placeholder-model"])
        self.assertIsInstance(instance, ImageGenerator)

    def test_openai_image_generator_satisfies_protocol(self) -> None:
        instance = OpenAIImageGenerator(api_key="placeholder", model="placeholder-model")
        self.assertIsInstance(instance, ImageGenerator)

    def test_protocol_method_signature_matches_concrete_classes(self) -> None:
        proto_sig = inspect.signature(ImageGenerator.generate_image)
        gemini_sig = inspect.signature(GeminiImageGenerator.generate_image)
        openai_sig = inspect.signature(OpenAIImageGenerator.generate_image)

        proto_params = [p for p in proto_sig.parameters if p != "self"]
        gemini_params = [p for p in gemini_sig.parameters if p != "self"]
        openai_params = [p for p in openai_sig.parameters if p != "self"]
        self.assertEqual(proto_params, gemini_params)
        self.assertEqual(proto_params, openai_params)


if __name__ == "__main__":
    unittest.main()
