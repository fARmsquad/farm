from __future__ import annotations

import re

SUBREDDITS = [
    "OculusQuest",
    "MetaQuestVR",
    "VRGaming",
    "virtualreality",
    "CozyGamers",
    "indiegames",
    "gamedev",
    "oculus",
    "SteamVR",
    "Quest3",
]

KEYWORDS = [
    "cozy VR",
    "farming sim VR",
    "Quest game recommendation",
    "games like Stardew VR",
    "upcoming VR games",
    "VR farming",
    "cozy Quest games",
    "indie VR game",
    "VR game recommendation",
    "Meta Quest cozy",
    "VR simulation game",
    "chicken game",
]

TWITTER_QUERY_MAX_RESULTS = 50

TWITTER_QUERY_BUCKETS: dict[str, list[str]] = {
    "recommendations": [
        '("farm game" OR "farming game" OR "farming sim") (recommend OR recommendations OR favorite) -is:retweet lang:en',
        '("cozy game" OR "cozy games") (farm OR farming OR stardew OR "life sim") (recommend OR recommendations) -is:retweet lang:en',
        '("games like stardew" OR "like stardew valley" OR "similar to stardew") -is:retweet lang:en',
    ],
    "stardew_discourse": [
        '("Stardew Valley" OR Stardew) (wish OR wishes OR update OR mechanic OR feature) -is:retweet lang:en',
        '("Stardew Valley" OR Stardew) (favorite OR love OR obsessed OR addicted OR playing) -is:retweet lang:en',
        '("Stardew Valley" OR Stardew) ("one more day" OR "one-more-day" OR pacing OR cozy) -is:retweet lang:en',
    ],
    "cozy_farming": [
        '("farm game" OR "farming game" OR "farming sim") (cozy OR wholesome OR relaxing) -is:retweet lang:en',
        '("cozy farming" OR "cozy farm game" OR "cozy farming game") -is:retweet lang:en',
        '("Harvest Moon" OR "Story of Seasons" OR "Rune Factory") (farm OR cozy OR recommendation) -is:retweet lang:en',
    ],
    "upcoming_farm_games": [
        '("upcoming farm game" OR "new farming game" OR "new farm game") -is:retweet lang:en',
        '("upcoming cozy game" OR "coming soon cozy game") (farm OR farming OR stardew) -is:retweet lang:en',
        '("in development" OR "coming soon") ("farm game" OR "farming sim" OR "cozy game") -is:retweet lang:en',
    ],
}


def build_twitter_queries() -> list[str]:
    queries: list[str] = []
    seen: set[str] = set()
    for bucket in TWITTER_QUERY_BUCKETS.values():
        for query in bucket:
            if query in seen:
                continue
            seen.add(query)
            queries.append(query)
    return queries


QUERIES = build_twitter_queries()


def find_matching_keywords(text: str, keywords: list[str] | None = None) -> list[str]:
    haystack = text.casefold()
    matches: list[str] = []
    for keyword in keywords or KEYWORDS:
        if keyword.casefold() in haystack:
            matches.append(keyword)
            continue
        pattern = r"\b" + re.sub(r"\s+", r"\\s+", re.escape(keyword.casefold())) + r"\b"
        if re.search(pattern, haystack):
            matches.append(keyword)
    return matches
