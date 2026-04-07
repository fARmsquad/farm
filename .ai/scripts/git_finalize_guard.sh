#!/bin/bash
# Git Finalization Guard — validates hard gates before push
set -e

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_PATH"

ERRORS=0

# Clean tree
if [ -n "$(git status --porcelain)" ]; then
    echo "✗ Uncommitted changes"
    ERRORS=$((ERRORS+1))
else
    echo "✓ Working tree clean"
fi

# On main
BRANCH=$(git branch --show-current)
if [[ "$BRANCH" != "main" ]]; then
    echo "✗ Not on main (on $BRANCH)"
    ERRORS=$((ERRORS+1))
else
    echo "✓ On main"
fi

# No active flights
if [ -f ".ai/coordination/flight-board.json" ]; then
    ACTIVE=$(python3 -c "import json; d=json.load(open('.ai/coordination/flight-board.json')); print(len(d.get('flights',[])))" 2>/dev/null || echo "0")
    if [ "$ACTIVE" != "0" ]; then
        echo "✗ $ACTIVE active flights — wait or resolve"
        ERRORS=$((ERRORS+1))
    else
        echo "✓ No active flights"
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
