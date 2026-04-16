from __future__ import annotations

from pydantic import BaseModel, Field


class StylePresetDefinition(BaseModel):
    style_preset_id: str
    display_name: str
    style_descriptor_text: str = Field(
        ...,
        description="Human-readable art direction injected into LLM system prompt and user prompt.",
    )
    image_prompt_suffix: str = Field(
        default="",
        description="Suffix appended to every per-shot image_prompt before sending to image provider. Used in Task A4.",
    )
    preferred_provider: str = Field(
        default="gemini",
        description="ImageGenerator key to route to. Used in Task B3.",
    )


class StylePresetCatalog:
    def __init__(self, presets: dict[str, StylePresetDefinition]) -> None:
        self._presets = presets

    def get(self, style_preset_id: str | None) -> StylePresetDefinition | None:
        if not style_preset_id:
            return None
        return self._presets.get(style_preset_id)

    def all(self) -> list[StylePresetDefinition]:
        return list(self._presets.values())

    @classmethod
    def default(cls) -> "StylePresetCatalog":
        watercolor = StylePresetDefinition(
            style_preset_id="watercolor_intro_v1",
            display_name="Watercolor Intro",
            style_descriptor_text=(
                "Hand-painted watercolor with visible brushwork, warm dawn-toned palette, "
                "soft atmospheric perspective, painterly edges, gentle storybook mood. "
                "Match the look of the existing intro and standing-slice cutscene panels: "
                "warm sunrise lighting, subtle fog, organic line quality, and storybook framing."
            ),
            image_prompt_suffix=(
                ", in warm-toned watercolor storybook style with visible brushwork and "
                "soft atmospheric lighting, matte storybook finish"
            ),
            preferred_provider="gemini",
        )
        return cls({watercolor.style_preset_id: watercolor})
