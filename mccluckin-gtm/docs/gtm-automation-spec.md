# Feature Spec: McCluckin Farm GTM Automation Pipeline

## Summary
Build a self-contained Python service that finds relevant Reddit and X
conversations about cozy VR, farming sims, and Meta Quest game
recommendations; drafts context-aware replies with Claude; queues every draft
for review; and publishes only when the platform rules and local safety
guardrails allow it.

## User Story
As the McCluckin Farm team, we want a lightweight GTM pipeline that helps us
join the right conversations without sounding like a bot, so we can stay
present in the community while keeping humans in control of what gets posted.

## Acceptance Criteria
- [ ] The project is isolated under `mccluckin-gtm/` and runs independently of
      the Unity game.
- [ ] Reddit and X monitoring persist deduplicated leads into SQLite.
- [ ] Claude draft generation marks irrelevant leads as skipped and stores
      approved candidates as drafts.
- [ ] A FastAPI review dashboard supports approve, edit, reject, and stats.
- [ ] Publishing enforces duplicate protection, rate limits, subreddit
      cooldowns, a pause flag, and manual-review requirements.
- [ ] The content calendar can seed upcoming items and draft them into the same
      review queue.
- [ ] The CLI exposes the monitor, draft, review, publish, calendar, stats,
      pause, resume, and run-all commands described in the brief.
- [ ] Unit and integration tests cover monitor matching, drafting, publishing,
      and the review flow with mocked external APIs.

## Technical Plan

### Research Reference
- **Async PRAW**: official `Subreddit.search(...)`, `Subreddit.new(...)`, and
  stream/listing docs confirm async listing generators for subreddit search and
  new submissions:
  [asyncpraw.readthedocs.io](https://asyncpraw.readthedocs.io/en/latest/code_overview/models/subreddit.html)
- **Tweepy / X API v2**: official Tweepy docs for
  `Client.search_recent_tweets(...)` and `Client.create_tweet(...)` support the
  query + `since_id` pull model and reply publishing via
  `in_reply_to_tweet_id`:
  [docs.tweepy.org](https://docs.tweepy.org/en/stable/client.html)
- **Anthropic Messages API**: official docs for
  `client.messages.create(...)` match the required system + user prompt flow:
  [docs.anthropic.com](https://docs.anthropic.com/en/api/messages)
- **FastAPI testing**: official docs recommend the built-in ASGI testing path
  via HTTPX/TestClient:
  [fastapi.tiangolo.com](https://fastapi.tiangolo.com/tutorial/testing/)
- **SQLAlchemy + SQLite**: official 2.0/2.1 docs confirm annotated declarative
  models, `mapped_column()`, context-managed sessions, and SQLite-backed
  `DateTime`/JSON handling:
  [docs.sqlalchemy.org ORM Quick Start](https://docs.sqlalchemy.org/en/21/orm/quickstart.html),
  [docs.sqlalchemy.org SQLite dialect](https://docs.sqlalchemy.org/en/20/dialects/sqlite.html)
- **Alembic**: official tutorial for migration environment + version scripts:
  [alembic.sqlalchemy.org](https://alembic.sqlalchemy.org/en/latest/tutorial.html)
- **Typer**: official docs support nested command groups matching the requested
  CLI shape:
  [typer.tiangolo.com](https://typer.tiangolo.com/)

### Memory / Repo Reference
- Keep the GTM system isolated from the active Unity and story-orchestrator
  worktree state; do not edit the existing dirty backend files.
- Respect the repo’s spec-first and test-first rules from
  `/Users/youss/My project/AGENTS.md`.
- Borrow backend conventions from
  `/Users/youss/My project/backend/story-orchestrator/README.md` and its
  FastAPI + SQLite + local-template pattern, but keep all new code under its
  own top-level project.

### Architecture
- `config.py`: environment-driven settings
- `db/models.py`: SQLAlchemy ORM models for leads, drafts, publications,
  calendar items, quotas, and control flags
- `db/session.py`: engine/session helpers
- `monitor/`: Reddit/X monitoring logic
- `drafter/generate.py`: Claude prompt construction and draft persistence
- `review/app.py`: FastAPI dashboard + JSON API
- `publisher/`: platform-specific publish clients and policy checks
- `scheduler/content_calendar.py`: seeding and draft generation for proactive
  content
- `cli.py`: Typer entry point

### Data Flow
`Monitor -> leads -> drafter -> drafts -> review queue -> publisher -> published`

### Risks
- Reddit/X API credentials and approval level are not available yet, so live
  publishing remains assumed until the user supplies keys.
- Reddit “account age filter per subreddit” is not a direct API primitive. V0
  will enforce a conservative global Reddit-account-age guard unless a
  subreddit-specific policy source is added later.
- X API access tier limits may constrain live search volume; the code should
  degrade cleanly when queries return no data or hit rate limits.

## Task Breakdown

### Task 1: Project Scaffold
- **Files**: `pyproject.toml`, `.env.example`, `README.md`, `config.py`,
  `db/models.py`, `alembic/*`
- **Acceptance**: local install path exists and database schema is defined

### Task 2: Monitor Layer
- **Files**: `monitor/reddit.py`, `monitor/twitter.py`, `tests/test_monitor.py`
- **Acceptance**: mocked monitor runs store deduplicated, filtered leads

### Task 3: Drafter Layer
- **Files**: `drafter/generate.py`, `tests/test_drafter.py`
- **Acceptance**: mocked Claude calls create drafts or skip leads correctly

### Task 4: Review Dashboard
- **Files**: `review/app.py`, `templates/index.html`, `tests/test_pipeline.py`
- **Acceptance**: API supports queue, approve/edit/reject, and stats

### Task 5: Publisher + Guardrails
- **Files**: `publisher/*.py`, `tests/test_publisher.py`
- **Acceptance**: publishing respects quotas, cooldowns, duplicate checks, and
  pause/manual-review rules

### Task 6: Content Calendar + CLI
- **Files**: `scheduler/content_calendar.py`, `cli.py`
- **Acceptance**: calendar seed/draft works and the CLI exposes the requested
  commands

