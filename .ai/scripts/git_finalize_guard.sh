#!/bin/bash
# Git Finalization Guard — validates hard gates before PR/merge
set -e

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_PATH"

ERRORS=0

# Clean tree
if [ -n "$(git status --porcelain)" ]; then
    echo "✗ Uncommitted changes"
    ERRORS=$((ERRORS+1))
fi

# Feature branch
BRANCH=$(git branch --show-current)
if [[ "$BRANCH" == "main" || "$BRANCH" == "master" ]]; then
    echo "✗ On $BRANCH"
    ERRORS=$((ERRORS+1))
fi

# Upstream sync
git fetch origin main --quiet 2>/dev/null || true
LOCAL=$(git rev-parse origin/main 2>/dev/null || echo "x")
BASE=$(git merge-base HEAD origin/main 2>/dev/null || echo "y")
if [ "$LOCAL" != "$BASE" ]; then
    echo "✗ Behind origin/main — rebase first"
    ERRORS=$((ERRORS+1))
fi

# No active flights
if [ -f ".ai/coordination/flight-board.json" ]; then
    ACTIVE=$(python3 -c "import json; d=json.load(open('.ai/coordination/flight-board.json')); print(len(d.get('flights',[])))" 2>/dev/null || echo "0")
    if [ "$ACTIVE" != "0" ]; then
        echo "✗ $ACTIVE active flights — wait or resolve"
        ERRORS=$((ERRORS+1))
    fi
fi

# All tests
if .ai/scripts/run-tests.sh all 2>/dev/null; then
    echo "✓ All tests pass"
else
    echo "✗ Tests failing"
    ERRORS=$((ERRORS+1))
fi

if [ $ERRORS -gt 0 ]; then
    echo "Guard FAILED — $ERRORS issues"
else
    echo "Guard PASSED"
fi
exit $ERRORS
