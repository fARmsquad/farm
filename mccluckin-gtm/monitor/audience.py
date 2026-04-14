from __future__ import annotations

import re

TARGET_AUDIENCE_PATTERNS = (
    re.compile(r"\bstardew(?: valley)?\b"),
    re.compile(r"\bfarm(?:ing)?\s(?:game|games|sim|sims)\b"),
    re.compile(r"\bcozy(?:\s(?:game|games|farm|farming))?\b"),
    re.compile(r"\blife sim\b"),
    re.compile(r"\bharvest moon\b"),
    re.compile(r"\bstory of seasons\b"),
    re.compile(r"\brune factory\b"),
    re.compile(r"\bone[- ]more[- ]day\b"),
)

GENERIC_DEVLOG_PATTERNS = (
    re.compile(r"#indiedev\b"),
    re.compile(r"#gamedev\b"),
    re.compile(r"#screenshotsaturday\b"),
    re.compile(r"\bvr dev\b"),
    re.compile(r"\bxr simulator\b"),
    re.compile(r"\bquest system\b"),
    re.compile(r"\bday \d+ of building\b"),
    re.compile(r"\b(?:started|working|implemented|implementing|building|shipped|shipping)\b.*\b(?:my|our)\s+game\b"),
    re.compile(r"\b(?:started|working|implemented|implementing)\b.*\b(?:quest system|dialogue ui|skill tree|combat system|shader)\b"),
)


def normalize_candidate_text(body: str, matched_keywords: list[str] | None = None) -> str:
    return " ".join([body, *(matched_keywords or [])]).casefold()


def has_target_audience_context(text: str) -> bool:
    return any(pattern.search(text) for pattern in TARGET_AUDIENCE_PATTERNS)


def is_generic_devlog(text: str) -> bool:
    return any(pattern.search(text) for pattern in GENERIC_DEVLOG_PATTERNS)


def should_store_twitter_lead(body: str, matched_keywords: list[str] | None = None) -> bool:
    normalized = normalize_candidate_text(body, matched_keywords)
    return has_target_audience_context(normalized) and not is_generic_devlog(normalized)
