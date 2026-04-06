#!/usr/bin/env python3
"""Parse NUnit XML test results and display a summary."""

import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_results(xml_path: str) -> None:
    path = Path(xml_path)
    if not path.exists():
        print(f"ERROR: Results file not found: {xml_path}")
        sys.exit(1)

    tree = ET.parse(path)
    root = tree.getroot()

    # NUnit XML format
    total = int(root.get("total", 0))
    passed = int(root.get("passed", 0))
    failed = int(root.get("failed", 0))
    skipped = int(root.get("skipped", 0))
    inconclusive = int(root.get("inconclusive", 0))
    duration = float(root.get("duration", 0))
    result = root.get("result", "Unknown")

    print(f"\n{'━' * 40}")
    print(f"Test Results: {path.name}")
    print(f"{'━' * 40}")
    print(f"  Total:         {total}")
    print(f"  Passed:        {passed}")
    print(f"  Failed:        {failed}")
    print(f"  Skipped:       {skipped}")
    print(f"  Inconclusive:  {inconclusive}")
    print(f"  Duration:      {duration:.2f}s")
    print(f"  Result:        {result}")
    print(f"{'━' * 40}")

    # Show failed test details
    if failed > 0:
        print("\nFAILED TESTS:")
        for test_case in root.iter("test-case"):
            if test_case.get("result") == "Failed":
                name = test_case.get("fullname", test_case.get("name", "Unknown"))
                failure = test_case.find("failure")
                message = ""
                if failure is not None:
                    msg_elem = failure.find("message")
                    if msg_elem is not None and msg_elem.text:
                        message = msg_elem.text.strip()[:200]
                print(f"\n  FAIL: {name}")
                if message:
                    print(f"        {message}")

    # Exit with non-zero if any tests failed
    sys.exit(0 if failed == 0 else 1)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: parse-test-results.py <results.xml>")
        sys.exit(1)
    parse_results(sys.argv[1])
