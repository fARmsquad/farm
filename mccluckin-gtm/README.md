# McCluckin Farm GTM

Automated go-to-market pipeline for [mccluckinfarm.com](https://mccluckinfarm.com).

The app monitors Reddit and X for relevant conversations, drafts context-aware
responses with Claude, queues them for human review, and publishes approved
messages with platform guardrails.

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

## Tests

```bash
cd mccluckin-gtm
pytest
```

## Notes

- `AUTO_PUBLISH=false` is the safe default.
- Engagement tweets are the only auto-publish path in v0 when `AUTO_PUBLISH=true`.
- Reddit publishing uses a conservative account-age check before any reply is sent.
