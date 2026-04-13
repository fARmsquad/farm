#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
OUTPUT_FILE="$(mktemp)"
trap 'rm -f "$OUTPUT_FILE"' EXIT

set +e
FARMSIM_TEST_ALLOW_PROJECT_COPY=1 "$ROOT_DIR/.ai/scripts/run-tests.sh" editmode >"$OUTPUT_FILE" 2>&1
STATUS=$?
set -e

cat "$OUTPUT_FILE"

if grep -q "Permission denied" "$OUTPUT_FILE"; then
    echo "FAIL: run-tests.sh was not executable." >&2
    exit 1
fi

if [ -f "$ROOT_DIR/TestResults/editmode.log" ] \
    && grep -q "Project folder or disk is read only" "$ROOT_DIR/TestResults/editmode.log"; then
    echo "FAIL: disposable project copy was not writable." >&2
    exit 1
fi

if [ "$STATUS" -eq 2 ]; then
    echo "FAIL: run-tests.sh skipped instead of falling back to a disposable project copy." >&2
    exit 1
fi

echo "PASS: run-tests.sh did not skip on editor lock."
