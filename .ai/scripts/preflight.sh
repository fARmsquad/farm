#!/bin/bash
# Preflight Check — 9-gate merge readiness
set -e

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_PATH"

ERRORS=0
WARNINGS=0
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'
SKIPPED_EXIT_CODE=2

echo "━━━ PREFLIGHT CHECK ━━━"

MERGE_BASE=$(git merge-base HEAD origin/main 2>/dev/null || git rev-parse HEAD)
CHANGED_FILES=$(git diff --name-only "$MERGE_BASE"...HEAD)

changed_specs() {
    printf '%s\n' "$CHANGED_FILES" | grep '^Assets/Specs/Features/.*\.md$' || true
}

changed_scripts() {
    printf '%s\n' "$CHANGED_FILES" | grep '^Assets/_Project/Scripts/.*\.cs$' || true
}

echo -e "\n${YELLOW}[1/9] EditMode Tests${NC}"
if .ai/scripts/run-tests.sh editmode; then
    echo -e "  ${GREEN}✓ EditMode tests pass${NC}"
else
    TEST_EXIT=$?
    if [ "$TEST_EXIT" -eq "$SKIPPED_EXIT_CODE" ]; then
        echo -e "  ${YELLOW}⚠ EditMode tests skipped because Unity already has the project open${NC}"
        WARNINGS=$((WARNINGS+1))
    else
        echo -e "  ${RED}✗ EditMode tests failing${NC}"
        ERRORS=$((ERRORS+1))
    fi
fi

echo -e "\n${YELLOW}[2/9] PlayMode Tests${NC}"
if .ai/scripts/run-tests.sh playmode; then
    echo -e "  ${GREEN}✓ PlayMode tests pass${NC}"
else
    TEST_EXIT=$?
    if [ "$TEST_EXIT" -eq "$SKIPPED_EXIT_CODE" ]; then
        echo -e "  ${YELLOW}⚠ PlayMode tests skipped because Unity already has the project open${NC}"
        WARNINGS=$((WARNINGS+1))
    else
        echo -e "  ${YELLOW}⚠ PlayMode tests skipped or failing${NC}"
        WARNINGS=$((WARNINGS+1))
    fi
fi

echo -e "\n${YELLOW}[3/9] Compiler Check${NC}"
echo -e "  ${YELLOW}⚠ Compiler check requires Unity editor — skipping in CLI${NC}"
WARNINGS=$((WARNINGS+1))

echo -e "\n${YELLOW}[4/9] Clean Working Tree${NC}"
if [ -z "$(git status --porcelain)" ]; then
    echo -e "  ${GREEN}✓ Working tree clean${NC}"
else
    echo -e "  ${RED}✗ Uncommitted changes detected${NC}"
    ERRORS=$((ERRORS+1))
fi

echo -e "\n${YELLOW}[5/9] Feature Branch${NC}"
BRANCH=$(git branch --show-current)
if [[ "$BRANCH" != "main" && "$BRANCH" != "master" ]]; then
    echo -e "  ${GREEN}✓ On feature branch: $BRANCH${NC}"
else
    echo -e "  ${RED}✗ On $BRANCH — must be on feature branch${NC}"
    ERRORS=$((ERRORS+1))
fi

echo -e "\n${YELLOW}[6/9] Upstream Sync${NC}"
if git fetch origin main --quiet 2>/dev/null; then
    LOCAL=$(git rev-parse origin/main 2>/dev/null || echo "x")
    BASE=$(git merge-base HEAD origin/main 2>/dev/null || echo "y")
    if [ "$LOCAL" = "$BASE" ]; then
        echo -e "  ${GREEN}✓ Up to date with origin/main${NC}"
    else
        echo -e "  ${RED}✗ Behind origin/main — rebase first${NC}"
        ERRORS=$((ERRORS+1))
    fi
else
    echo -e "  ${YELLOW}⚠ Could not fetch origin — skipping sync check${NC}"
    WARNINGS=$((WARNINGS+1))
fi

echo -e "\n${YELLOW}[7/9] Spec Acceptance Criteria${NC}"
SPECS=$(changed_specs)
if [ -n "$SPECS" ]; then
    UNCHECKED_FOUND=0
    while IFS= read -r spec; do
        [ -z "$spec" ] && continue
        COUNT=$(grep -c '\- \[ \]' "$spec" 2>/dev/null || true)
        COUNT=${COUNT:-0}
        if [ "$COUNT" != "0" ]; then
            echo -e "  ${YELLOW}⚠ $COUNT unchecked acceptance criteria in $spec${NC}"
            WARNINGS=$((WARNINGS+1))
            UNCHECKED_FOUND=1
        fi
    done <<EOF
$SPECS
EOF
    if [ "$UNCHECKED_FOUND" = "0" ]; then
        echo -e "  ${GREEN}✓ Changed specs have all acceptance criteria checked${NC}"
    fi
else
    echo -e "  ${GREEN}✓ No changed feature specs in this branch${NC}"
fi

echo -e "\n${YELLOW}[8/9] Quality Contract${NC}"
LONG_FILES=""
SCRIPTS=$(changed_scripts)
if [ -n "$SCRIPTS" ]; then
    while IFS= read -r script; do
        [ -z "$script" ] && continue
        CURRENT_LINES=$(wc -l < "$script" | tr -d ' ')
        if git cat-file -e "$MERGE_BASE:$script" 2>/dev/null; then
            BASE_LINES=$(git show "$MERGE_BASE:$script" | wc -l | tr -d ' ')
        else
            BASE_LINES=0
        fi

        if [ "$CURRENT_LINES" -gt 500 ] && [ "$CURRENT_LINES" -gt "$BASE_LINES" ]; then
            LONG_FILES="${LONG_FILES}${script} (${BASE_LINES} -> ${CURRENT_LINES})\n"
        fi
    done <<EOF
$SCRIPTS
EOF
fi

if [ -z "$LONG_FILES" ]; then
    echo -e "  ${GREEN}✓ No changed scripts violate size limits${NC}"
else
    echo -e "  ${RED}✗ Changed scripts exceeding 500 lines:${NC}"
    printf '%b' "$LONG_FILES"
    ERRORS=$((ERRORS+1))
fi

echo -e "\n${YELLOW}[9/9] No Active Flights${NC}"
if [ -f ".ai/coordination/flight-board.json" ]; then
    ACTIVE=$(python3 -c "import json; d=json.load(open('.ai/coordination/flight-board.json')); print(len(d.get('flights',[])))" 2>/dev/null || echo "0")
    if [ "$ACTIVE" = "0" ]; then
        echo -e "  ${GREEN}✓ No active flights${NC}"
    else
        echo -e "  ${RED}✗ $ACTIVE active flights — wait or resolve${NC}"
        ERRORS=$((ERRORS+1))
    fi
else
    echo -e "  ${GREEN}✓ No flight board — single agent mode${NC}"
fi

echo -e "\n━━━━━━━━━━━━━━━━━━━━━"
if [ $ERRORS -gt 0 ]; then
    echo -e "${RED}PREFLIGHT FAILED — $ERRORS errors, $WARNINGS warnings${NC}"
    exit 1
else
    echo -e "${GREEN}PREFLIGHT PASSED — $WARNINGS warnings${NC}"
    exit 0
fi
