# McCluckin Farm GTM

Automated go-to-market pipeline for [mccluckinfarm.com](https://mccluckinfarm.com).

The app can monitor Reddit and X for relevant conversations, but the default
live setup is now centered on scheduled standalone X posts with human review and
platform guardrails.

## Stack

- Python 3.11+
- FastAPI
- SQLAlchemy + SQLite
- Alembic
- Async PRAW
- Tweepy
- Anthropic SDK
- Typer

## Layout

```text
mccluckin-gtm/
├── README.md
├── .env.example
├── pyproject.toml
├── alembic/
├── db/
├── drafter/
├── monitor/
├── publisher/
├── review/
├── scheduler/
├── templates/
├── tests/
├── cli.py
└── config.py
```

## Setup

```bash
cd mccluckin-gtm
python3 -m venv .venv
source .venv/bin/activate
pip install -e '.[dev]'
cp .env.example .env
```

## Commands

```bash
python cli.py monitor run --once
python cli.py monitor reddit --once
python cli.py monitor twitter --once
python cli.py draft
python cli.py review --port 8000
python cli.py publish
python cli.py calendar seed --weeks 4
python cli.py calendar draft
python cli.py stats
python cli.py run-all
python cli.py pause
python cli.py resume
```

## Railway

`mccluckin-gtm/railway.json` and `mccluckin-gtm/Procfile` run the dashboard and
background automation together through:

```bash
uvicorn web:app --host 0.0.0.0 --port $PORT
```

Recommended Railway variables:

- `DATABASE_URL=sqlite:////data/gtm.db`
- `LOG_PATH=/data/gtm.log`
- `BACKGROUND_JOBS_ENABLED=true`
- `REPLY_MONITOR_ENABLED=false`
- `OUTBOUND_REPLIES_ENABLED=false`
- `STANDALONE_CALENDAR_ENABLED=true`
- `X_BEARER_TOKEN`
- `X_OAUTH2_ACCESS_TOKEN`
- `X_OAUTH2_REFRESH_TOKEN`
- `X_CLIENT_ID`
- `ANTHROPIC_API_KEY`

## Tests

```bash
cd mccluckin-gtm
pytest
```

## Notes

- `AUTO_PUBLISH=false` is the safe default.
- Standalone calendar posts seed automatically for X when `STANDALONE_CALENDAR_ENABLED=true`.
- The default live posture is standalone X posts; reply monitoring and outbound replies stay off unless explicitly enabled.
- Engagement tweets are the only auto-publish path in v0 when `AUTO_PUBLISH=true`.
- Reddit publishing uses a conservative account-age check before any reply is sent.
- X publishing prefers OAuth 2.0 user tokens via `X_OAUTH2_ACCESS_TOKEN`; add
  `X_CLIENT_ID` + `X_OAUTH2_REFRESH_TOKEN` for unattended token refresh.
