from __future__ import annotations

from dataclasses import dataclass
from datetime import UTC, datetime, timedelta
from hashlib import sha1

import pytest
from requests import Response
from tweepy.errors import HTTPException

from db.models import ControlFlag, Lead
from monitor.keywords import TWITTER_QUERY_MAX_RESULTS, build_twitter_queries
from monitor.reddit import collect_reddit_leads
from monitor.twitter import collect_twitter_leads


@dataclass
class FakeSubmission:
    id: str
    title: str
    selftext: str
    author_name: str
    permalink: str
    created_utc: float
    subreddit_name: str

    @property
    def author(self):
        return type("Author", (), {"name": self.author_name})()


class FakeSubreddit:
    def __init__(self, name: str, search_results: list[FakeSubmission], new_results: list[FakeSubmission]) -> None:
        self.name = name
        self._search_results = search_results
        self._new_results = new_results

    async def search(self, keyword: str, sort: str, time_filter: str, limit: int = 10):
        assert sort == "new"
        assert time_filter == "day"
        for item in self._search_results:
            yield item

    async def new(self, limit: int = 25):
        assert limit == 25
        for item in self._new_results:
            yield item


class FakeReddit:
    def __init__(self, subreddits: dict[str, FakeSubreddit]) -> None:
        self._subreddits = subreddits

    async def subreddit(self, name: str) -> FakeSubreddit:
        return self._subreddits[name]


class FakeTweet:
    def __init__(self, tweet_id: str, text: str, author_id: str, created_at: datetime) -> None:
        self.id = tweet_id
        self.text = text
        self.author_id = author_id
        self.created_at = created_at


class FakeTwitterResponse:
    def __init__(self, data, includes, newest_id: str | None) -> None:
        self.data = data
        self.includes = includes
        self.meta = {"newest_id": newest_id} if newest_id else {}


class FakeTwitterClient:
    def __init__(self, response: FakeTwitterResponse) -> None:
        self.response = response
        self.calls: list[dict[str, object]] = []

    def search_recent_tweets(self, **kwargs):
        self.calls.append(kwargs)
        return self.response


class FailingTwitterClient:
    def search_recent_tweets(self, **kwargs):
        response = Response()
        response.status_code = 402
        response._content = b'{"title":"Payment Required","detail":"credits required"}'
        raise HTTPException(response=response)


@pytest.mark.asyncio
async def test_reddit_monitor_deduplicates_and_skips_old_or_self_authored_posts(
    session_factory,
    settings,
    now,
) -> None:
    fresh = FakeSubmission(
        id="fresh-1",
        title="Best cozy VR games?",
        selftext="Would love a farming sim VR recommendation.",
        author_name="quest_fan",
        permalink="/r/OculusQuest/comments/fresh-1",
        created_utc=(now - timedelta(hours=2)).timestamp(),
        subreddit_name="OculusQuest",
    )
    duplicate = FakeSubmission(
        id="fresh-1",
        title="Best cozy VR games?",
        selftext="Would love a farming sim VR recommendation.",
        author_name="quest_fan",
        permalink="/r/OculusQuest/comments/fresh-1",
        created_utc=(now - timedelta(hours=2)).timestamp(),
        subreddit_name="OculusQuest",
    )
    old = FakeSubmission(
        id="old-1",
        title="Old thread",
        selftext="cozy VR mention",
        author_name="archive_user",
        permalink="/r/OculusQuest/comments/old-1",
        created_utc=(now - timedelta(hours=49)).timestamp(),
        subreddit_name="OculusQuest",
    )
    own_post = FakeSubmission(
        id="self-1",
        title="Our own post",
        selftext="cozy VR mention",
        author_name=settings.reddit_username,
        permalink="/r/OculusQuest/comments/self-1",
        created_utc=(now - timedelta(hours=1)).timestamp(),
        subreddit_name="OculusQuest",
    )
    reddit = FakeReddit(
        {
            "OculusQuest": FakeSubreddit(
                "OculusQuest",
                search_results=[fresh, own_post],
                new_results=[duplicate, old],
            )
        }
    )

    inserted = await collect_reddit_leads(
        session_factory,
        settings,
        reddit=reddit,
        now=now,
        subreddits=["OculusQuest"],
        keywords=["cozy VR", "farming sim VR"],
    )

    with session_factory() as session:
        leads = session.query(Lead).all()

    assert inserted == 1
    assert len(leads) == 1
    assert leads[0].platform_id == "fresh-1"
    assert sorted(leads[0].matched_keywords) == ["cozy VR", "farming sim VR"]
    assert leads[0].status == "new"


@pytest.mark.asyncio
async def test_reddit_monitor_returns_zero_when_credentials_are_missing(session_factory, settings) -> None:
    disabled = settings.model_copy(update={"reddit_client_id": "", "reddit_client_secret": ""})

    inserted = await collect_reddit_leads(session_factory, disabled)

    assert inserted == 0


def test_twitter_monitor_uses_since_id_and_stores_newest_id(session_factory, settings, now) -> None:
    query = '"cozy VR" -is:retweet'
    with session_factory() as session:
        session.add(ControlFlag(key=f"twitter_since::{sha1(query.encode()).hexdigest()}", value="100"))
        session.commit()

    tweet = FakeTweet("101", "Need a cozy VR game on Quest", "user-1", now - timedelta(minutes=5))
    own_tweet = FakeTweet("102", "Our own cozy VR update", "self", now - timedelta(minutes=2))
    response = FakeTwitterResponse(
        data=[tweet, own_tweet],
        includes={"users": [{"id": "user-1", "username": "vrfan"}, {"id": "self", "username": settings.x_username}]},
        newest_id="102",
    )
    client = FakeTwitterClient(response)

    inserted = collect_twitter_leads(
        session_factory,
        settings,
        client=client,
        now=now,
        queries=[query],
    )

    with session_factory() as session:
        leads = session.query(Lead).all()
        flag = session.get(ControlFlag, f"twitter_since::{sha1(query.encode()).hexdigest()}")

    assert inserted == 1
    assert client.calls[0]["since_id"] == "100"
    assert client.calls[0]["max_results"] == TWITTER_QUERY_MAX_RESULTS
    assert leads[0].platform == "twitter"
    assert leads[0].platform_id == "101"
    assert leads[0].author == "vrfan"
    assert leads[0].url == "https://x.com/vrfan/status/101"
    assert leads[0].matched_keywords == [query]
    assert flag.value == "102"


def test_twitter_monitor_returns_zero_when_bearer_token_is_missing(session_factory, settings) -> None:
    disabled = settings.model_copy(update={"x_bearer_token": ""})

    inserted = collect_twitter_leads(session_factory, disabled)

    assert inserted == 0


def test_twitter_monitor_returns_zero_when_x_search_errors(session_factory, settings) -> None:
    inserted = collect_twitter_leads(
        session_factory,
        settings,
        client=FailingTwitterClient(),
        queries=['"cozy VR" -is:retweet'],
    )

    assert inserted == 0


def test_build_twitter_queries_covers_discovery_and_recommendation_intents() -> None:
    queries = build_twitter_queries()

    assert len(queries) >= 12
    combined = " ".join(queries).lower()
    assert "recommend" in combined
    assert "stardew" in combined
    assert "farming sim" in combined or "farm game" in combined or "farming game" in combined
    assert "upcoming farm game" in combined or "new farming game" in combined or "coming soon cozy game" in combined
    assert "#indiedev" not in combined
    assert "vr dev" not in combined

def test_build_twitter_queries_focuses_on_farm_and_stardew_audiences_over_vr_dev_chatter() -> None:
    queries = build_twitter_queries()

    combined = " ".join(queries).lower()
    assert "stardew" in combined
    assert "farm game" in combined or "farming game" in combined or "farming sim" in combined
    assert "vr dev" not in combined
    assert "xr simulator" not in combined
    assert "eye strain" not in combined

def test_twitter_monitor_filters_generic_devlogs_before_storing(session_factory, settings, now) -> None:
    query = '("games like stardew" OR "like stardew valley" OR "similar to stardew") -is:retweet lang:en'
    response = FakeTwitterResponse(
        data=[FakeTweet("300", "Started implementing my quest system today and cleaned up the dialogue UI.", "user-1", now)],
        includes={"users": [{"id": "user-1", "username": "devlog"}]},
        newest_id="300",
    )
    client = FakeTwitterClient(response)

    inserted = collect_twitter_leads(
        session_factory,
        settings,
        client=client,
        now=now,
        queries=[query],
    )

    with session_factory() as session:
        leads = session.query(Lead).all()

    assert inserted == 0
    assert leads == []


def test_twitter_monitor_keeps_stardew_discussion_when_query_matches(session_factory, settings, now) -> None:
    query = '("Stardew Valley" OR Stardew) (wish OR wishes OR update OR mechanic OR feature) -is:retweet lang:en'
    response = FakeTwitterResponse(
        data=[FakeTweet("301", "Stardew really nails the one-more-day loop. More farm games should study that pacing.", "user-2", now)],
        includes={"users": [{"id": "user-2", "username": "farmfan"}]},
        newest_id="301",
    )
    client = FakeTwitterClient(response)

    inserted = collect_twitter_leads(
        session_factory,
        settings,
        client=client,
        now=now,
        queries=[query],
    )

    with session_factory() as session:
        leads = session.query(Lead).all()

    assert inserted == 1
    assert leads[0].author == "farmfan"
    assert leads[0].platform_id == "301"

