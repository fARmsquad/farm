from __future__ import annotations

from dataclasses import dataclass

import pytest

from db.models import ContentCalendarItem, Draft, Lead
from drafter.generate import SYSTEM_PROMPT, build_calendar_prompt, build_user_prompt, process_new_leads


@dataclass
class FakeTextBlock:
    text: str


class FakeMessageResponse:
    def __init__(self, text: str) -> None:
        self.content = [FakeTextBlock(text)]


class FakeMessagesAPI:
    def __init__(self, responses: list[str]) -> None:
        self._responses = responses
        self.calls: list[dict[str, object]] = []

    async def create(self, **kwargs):
        self.calls.append(kwargs)
        return FakeMessageResponse(self._responses.pop(0))


class FakeAnthropicClient:
    def __init__(self, responses: list[str]) -> None:
        self.messages = FakeMessagesAPI(responses)


def test_build_user_prompt_includes_platform_specific_context(db_session) -> None:
    lead = Lead(
        platform="reddit",
        platform_id="abc",
        subreddit="OculusQuest",
        author="questfan",
        title="Need cozy VR recommendations",
        body="Would love some upcoming Quest games.",
        url="https://reddit.test/abc",
        matched_keywords=["cozy VR"],
        status="new",
    )

    prompt = build_user_prompt(lead)

    assert "Platform: reddit" in prompt
    assert "Subreddit: r/OculusQuest" in prompt
    assert "Need cozy VR recommendations" in prompt
    assert "Would love some upcoming Quest games." in prompt


@pytest.mark.asyncio
async def test_process_new_leads_skips_irrelevant_items_and_stores_drafts(session_factory, settings) -> None:
    with session_factory() as session:
        session.add_all(
            [
                Lead(
                    platform="reddit",
                    platform_id="skip-me",
                    subreddit="OculusQuest",
                    author="questfan",
                    title="Need shipped games",
                    body="Only released games please.",
                    url="https://reddit.test/skip-me",
                    matched_keywords=["cozy VR"],
                    status="new",
                ),
                Lead(
                    platform="twitter",
                    platform_id="draft-me",
                    subreddit=None,
                    author="vrfan",
                    title=None,
                    body="Any upcoming cozy VR games I should watch?",
                    url="https://x.test/draft-me",
                    matched_keywords=["\"cozy VR\" -is:retweet"],
                    status="new",
                ),
            ]
        )
        session.commit()

    client = FakeAnthropicClient(["SKIP", "Could fit if you're open to in-dev picks: McCluckin Farm is a cozy Quest farming sim we're building."])

    result = await process_new_leads(session_factory, settings, client=client, delay_seconds=0)

    with session_factory() as session:
        leads = {lead.platform_id: lead for lead in session.query(Lead).all()}
        drafts = session.query(Draft).all()

    assert result["processed"] == 2
    assert result["drafted"] == 1
    assert result["skipped"] == 1
    assert leads["skip-me"].status == "skipped"
    assert leads["draft-me"].status == "drafted"
    assert len(drafts) == 1
    assert drafts[0].prompt_hash
    assert drafts[0].model_used == settings.anthropic_model
    assert "Platform: twitter" in client.messages.calls[1]["messages"][0]["content"]


@pytest.mark.asyncio
async def test_process_new_leads_treats_skip_with_explanation_as_skipped(session_factory, settings) -> None:
    with session_factory() as session:
        session.add(
            Lead(
                platform="twitter",
                platform_id="skip-plus-reason",
                subreddit=None,
                author="vrfan",
                title=None,
                body="Another VR developer is promoting their own game on Meta Quest.",
                url="https://x.test/skip-plus-reason",
                matched_keywords=['"cozy VR" -is:retweet'],
                status="new",
            )
        )
        session.commit()

    client = FakeAnthropicClient(
        ["SKIP\n\nThis is another developer promoting their own game, so we should not reply."]
    )

    result = await process_new_leads(session_factory, settings, client=client, delay_seconds=0)

    with session_factory() as session:
        leads = session.query(Lead).all()
        drafts = session.query(Draft).all()

    assert result["processed"] == 1
    assert result["drafted"] == 0
    assert result["skipped"] == 1
    assert leads[0].status == "skipped"
    assert "another developer promoting their own game" in (leads[0].decision_note or "")
    assert drafts == []


@pytest.mark.asyncio
async def test_process_new_leads_skips_generic_twitter_dev_chatter_before_model_call(
    session_factory,
    settings,
) -> None:
    with session_factory() as session:
        session.add(
            Lead(
                platform="twitter",
                platform_id="generic-dev-log",
                subreddit=None,
                author="indiedev",
                title=None,
                body="Started implementing the quest system today and cleaned up my dialogue UI.",
                url="https://x.test/generic-dev-log",
                matched_keywords=['("indie vr" OR "indie VR game" OR "vr indie") (Quest OR "Meta Quest" OR VR) -is:retweet lang:en'],
                status="new",
            )
        )
        session.commit()

    client = FakeAnthropicClient(["This should never be used."])

    result = await process_new_leads(session_factory, settings, client=client, delay_seconds=0)

    with session_factory() as session:
        leads = session.query(Lead).all()
        drafts = session.query(Draft).all()

    assert result["processed"] == 1
    assert result["drafted"] == 0
    assert result["skipped"] == 1
    assert leads[0].status == "skipped"
    assert "generic developer build log" in (leads[0].decision_note or "").lower()
    assert client.messages.calls == []
    assert drafts == []

def test_system_prompt_requires_specific_farm_voice_for_x_replies() -> None:
    prompt = SYSTEM_PROMPT.lower()

    assert "concrete observation" in prompt
    assert "generic openers" in prompt
    assert "farmhand" in prompt
    assert "nuanced take" in prompt
    assert "\"we're working on\"" in prompt


def test_build_calendar_prompt_targets_standalone_repo_star_posts() -> None:
    item = ContentCalendarItem(
        content_type="engagement",
        platform="twitter",
        topic="Why Stardew's morning routine works",
        description="A grounded farm-game take with a soft GitHub star CTA.",
        status="new",
        scheduled_date=None,  # type: ignore[arg-type]
    )

    prompt = build_calendar_prompt(item)

    assert "standalone x post" in prompt.lower()
    assert "not a reply" in prompt.lower()
    assert "star the repo" in prompt.lower()
    assert "do not mention vr" in prompt.lower()
    assert "github.com/fARmsquad/farm" in prompt


@pytest.mark.asyncio
async def test_process_new_leads_allows_stardew_discussion_without_vr_context(session_factory, settings) -> None:
    with session_factory() as session:
        session.add(
            Lead(
                platform="twitter",
                platform_id="stardew-thread",
                subreddit=None,
                author="cozyfarmer",
                title=None,
                body="Stardew really nails that one-more-day loop. I want more farm games that understand pacing like that.",
                url="https://x.test/stardew-thread",
                matched_keywords=['("Stardew Valley" OR Stardew) (farm OR farming OR cozy) -is:retweet lang:en'],
                status="new",
            )
        )
        session.commit()

    client = FakeAnthropicClient(["The one-more-day pacing is the magic trick. We keep talking about how a farm game has to feel like tending a little routine, not speedrunning chores."])

    result = await process_new_leads(session_factory, settings, client=client, delay_seconds=0)

    with session_factory() as session:
        lead = session.query(Lead).filter_by(platform_id="stardew-thread").one()
        drafts = session.query(Draft).all()

    assert result["processed"] == 1
    assert result["drafted"] == 1
    assert result["skipped"] == 0
    assert lead.status == "drafted"
    assert lead.decision_note == "Draft generated for review."
    assert len(client.messages.calls) == 1
    assert len(drafts) == 1
