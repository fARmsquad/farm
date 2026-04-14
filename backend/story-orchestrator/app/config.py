from functools import lru_cache
from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    gemini_api_key: str = ""
    gemini_project_name: str = ""
    gemini_project_number: str = ""
    gemini_project_id: str = ""
    gemini_image_model: str = "gemini-3.1-flash-image-preview"
    gemini_image_fallback_model: str = "gemini-2.5-flash-image"
    elevenlabs_api_key: str = ""
    elevenlabs_voice_id: str = ""
    elevenlabs_model_id: str = "eleven_flash_v2_5"
    database_path: str = "data/story_orchestrator.db"
    generated_storyboard_output_root: str = "../../Assets/_Project/Resources"
    generated_storyboard_package_path: str = "../../Assets/_Project/Resources/StoryPackages/StoryPackage_IntroChickenSample.json"

    model_config = SettingsConfigDict(
        env_file=".env.local",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",
    )

    def resolve_database_path(self, base_dir: Path) -> Path:
        return self.resolve_path(base_dir, self.database_path)

    def resolve_path(self, base_dir: Path, raw_path: str) -> Path:
        path = Path(raw_path)
        return path if path.is_absolute() else (base_dir / path)


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
