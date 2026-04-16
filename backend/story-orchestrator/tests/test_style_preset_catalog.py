from __future__ import annotations

from app.style_preset_catalog import StylePresetCatalog, StylePresetDefinition


def test_default_catalog_contains_watercolor_intro_v1():
    catalog = StylePresetCatalog.default()
    preset = catalog.get("watercolor_intro_v1")
    assert preset is not None
    assert preset.display_name == "Watercolor Intro"


def test_watercolor_descriptor_includes_painterly_keywords():
    descriptor = StylePresetCatalog.default().get("watercolor_intro_v1").style_descriptor_text.lower()
    for term in ("watercolor", "warm", "painterly"):
        assert term in descriptor, f"missing keyword: {term}"


def test_watercolor_image_prompt_suffix_is_non_empty_and_starts_with_separator():
    preset = StylePresetCatalog.default().get("watercolor_intro_v1")
    assert preset.image_prompt_suffix.startswith(", "), "suffix must lead with ', ' so it concatenates cleanly"
    assert "watercolor" in preset.image_prompt_suffix.lower()


def test_watercolor_preferred_provider_defaults_to_gemini():
    preset = StylePresetCatalog.default().get("watercolor_intro_v1")
    assert preset.preferred_provider == "gemini"


def test_get_returns_none_for_unknown_or_empty_preset_id():
    catalog = StylePresetCatalog.default()
    assert catalog.get("unknown_preset") is None
    assert catalog.get("") is None
    assert catalog.get(None) is None


def test_definition_model_round_trips():
    defn = StylePresetDefinition(
        style_preset_id="custom_v1",
        display_name="Custom",
        style_descriptor_text="anything",
    )
    assert defn.image_prompt_suffix == ""
    assert defn.preferred_provider == "gemini"
