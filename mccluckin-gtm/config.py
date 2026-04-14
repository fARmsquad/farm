from __future__ import annotations

from functools import lru_cache
from pathlib import Path

from dotenv import load_dotenv
from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict

load_dotenv()


class Settings(BaseSettings):
    reddit_client_id: str = ""
    reddit_client_secret: str = ""
    reddit_username: str = ""
    reddit_password: str = ""
    reddit_user_agent: str = "mccluckin-gtm:v0.1"

    x_bearer_token: str = ""
    x_api_key: str = ""
    x_api_secret: str = ""
    x_access_token: str = ""
    x_access_token_secret: str = ""
    x_username: str = ""

    anthropic_api_key: str = ""
    anthropic_model: str = "claude-sonnet-4-20250514"

    database_url: str = "sqlite:///./gtm.db"
    review_port: int = 8000
    auto_publish: bool = False
    log_path: str = "gtm.log"
    reddit_daily_reply_limit: int = 5
    reddit_daily_post_limit: int = 1
    twitter_daily_reply_limit: int = 10
    twitter_daily_post_limit: int = 3
    reddit_subreddit_cooldown_hours: int = 24
    reddit_account_min_age_days: int = 7
    default_seed_weeks: int = 4
    draft_delay_seconds: float = 1.0
    reddit_publish_delay_min_minutes: int = 2
    reddit_publish_delay_max_minutes: int = 10
    twitter_publish_delay_min_minutes: int = 5
    twitter_publish_delay_max_minutes: int = 15
    review_refresh_seconds: int = 30
    app_name: str = "McCluckin GTM"

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",
    )

    @property
    def database_path(self) -> Path:
        if self.database_url.startswith("sqlite:///"):
            return Path(self.database_url.removeprefix("sqlite:///"))
        raise ValueError("Only sqlite:/// DATABASE_URL values are supported in v0.")

    @property
    def log_file_path(self) -> Path:
        return Path(self.log_path)

    @property
    def reddit_enabled(self) -> bool:
        required = (
            self.reddit_client_id,
            self.reddit_client_secret,
            self.reddit_username,
            self.reddit_password,
            self.reddit_user_agent,
        )
        return all(bool(value.strip()) for value in required)

    @property
    def x_monitor_enabled(self) -> bool:
        return bool(self.x_bearer_token.strip())

    @property
    def x_publish_enabled(self) -> bool:
        required = (
            self.x_api_key,
            self.x_api_secret,
            self.x_access_token,
            self.x_access_token_secret,
        )
        return all(bool(value.strip()) for value in required)


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
