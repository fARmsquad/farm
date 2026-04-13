#!/usr/bin/env python3
import argparse
import json
import re
import sys
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Evaluate Town NPC generations against the TOWN-001 golden set."
    )
    parser.add_argument("--golden", required=True, help="Path to the golden-set JSON file.")
    parser.add_argument(
        "--generations",
        required=True,
        help="Path to a JSONL or JSON generations file keyed by case id.",
    )
    return parser.parse_args()


def load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def load_generations(path: Path) -> dict[str, str]:
    if path.suffix.lower() == ".json":
        return parse_generation_items(load_json(path))

    items: list[dict] = []
    with path.open("r", encoding="utf-8") as handle:
        for line_number, raw_line in enumerate(handle, start=1):
            line = raw_line.strip()
            if not line:
                continue
            try:
                items.append(json.loads(line))
            except json.JSONDecodeError as exc:
                raise ValueError(f"{path}:{line_number}: invalid JSON: {exc}") from exc
    return parse_generation_items(items)


def parse_generation_items(items) -> dict[str, str]:
    if not isinstance(items, list):
        raise ValueError("Generations file must contain a list or JSONL objects.")

    generations: dict[str, str] = {}
    for item in items:
        if not isinstance(item, dict):
            raise ValueError("Each generation item must be an object.")
        case_id = item.get("id")
        output = item.get("output", item.get("text"))
        if not case_id or not isinstance(output, str):
            raise ValueError("Each generation item needs string fields 'id' and 'output' or 'text'.")
        generations[case_id] = output
    return generations


def sentence_count(text: str) -> int:
    stripped = text.strip()
    if not stripped:
        return 0
    parts = [part for part in re.split(r"(?<=[.!?])\s+", stripped) if part.strip()]
    return len(parts) if parts else 1


def contains_any(text: str, phrases: list[str]) -> bool:
    lowered = text.lower()
    return any(phrase.lower() in lowered for phrase in phrases)


def missing_all(text: str, phrases: list[str]) -> list[str]:
    lowered = text.lower()
    return [phrase for phrase in phrases if phrase.lower() not in lowered]


def evaluate_case(case: dict, output: str, global_bans: list[str]) -> list[str]:
    failures: list[str] = []
    if not output.strip():
        return ["output is empty"]

    all_bans = global_bans + case.get("must_not_include", [])
    lowered = output.lower()
    for banned in all_bans:
        if banned.lower() in lowered:
            failures.append(f"contains banned text: {banned}")

    must_include_any = case.get("must_include_any", [])
    if must_include_any and not contains_any(output, must_include_any):
        failures.append(f"missing any of: {', '.join(must_include_any)}")

    must_include_all = case.get("must_include_all", [])
    missing = missing_all(output, must_include_all)
    if missing:
        failures.append(f"missing required text: {', '.join(missing)}")

    max_sentences = case.get("max_sentences")
    if isinstance(max_sentences, int) and sentence_count(output) > max_sentences:
        failures.append(f"exceeds max_sentences={max_sentences}")

    return failures


def main() -> int:
    args = parse_args()
    golden_path = Path(args.golden)
    generations_path = Path(args.generations)

    golden = load_json(golden_path)
    generations = load_generations(generations_path)
    global_bans = golden.get("global_must_not_include", [])

    failures_found = False
    passed = 0
    for case in golden.get("cases", []):
        case_id = case["id"]
        output = generations.get(case_id)
        if output is None:
            failures_found = True
            print(f"FAIL {case_id}")
            print("  - missing generation")
            continue

        failures = evaluate_case(case, output, global_bans)
        if failures:
            failures_found = True
            print(f"FAIL {case_id}")
            for failure in failures:
                print(f"  - {failure}")
            continue

        passed += 1
        print(f"PASS {case_id}")

    total = len(golden.get("cases", []))
    if failures_found:
        print(f"\nSummary: {passed}/{total} cases passed.")
        return 1

    print(f"\nSummary: {passed}/{total} cases passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
