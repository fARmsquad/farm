from functools import lru_cache
from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    gemini_api_key: str = ""
    gemini_project_name: str = ""
    gemini_project_number: str = ""
    gemini_project_id: str = ""
    elevenlabs_api_key: str = ""
    database_path: str = "data/story_orchestrator.db"

    model_config = SettingsConfigDict(
        env_file=".env.local",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",
    )

    def resolve_database_path(self, base_dir: Path) -> Path:
        path = Path(self.database_path)
        return path if path.is_absolute() else (base_dir / path)


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
