#!/bin/bash
# Git Sync — pull and verify. Called at phase boundaries.
# Trunk-based: everything on main, no branches.
set -e

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_PATH"

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m'

BRANCH=$(git branch --show-current)
ACTION="${1:-sync}"  # sync | verify

echo "━━━ GIT SYNC ━━━"
echo "Branch: $BRANCH"
echo "Action: $ACTION"

case "$ACTION" in

  sync)
    # Pull latest main
    if [[ "$BRANCH" != "main" ]]; then
      echo -e "${YELLOW}Warning: not on main — switching to main${NC}"
      git checkout main
    fi

    echo -e "\n[1/2] Pulling origin/main..."
    if git pull --rebase origin main; then
      echo -e "${GREEN}Pull successful${NC}"
    else
      echo -e "${RED}Pull conflict detected!${NC}"
      echo "Conflicting files:"
      git diff --name-only --diff-filter=U
      echo ""
      echo "Options:"
      echo "  1. Resolve conflicts, then: git add <files> && git rebase --continue"
      echo "  2. Abort: git rebase --abort"
      exit 1
    fi

    echo -e "\n[2/2] Verifying state..."
    if [ -n "$(git status --porcelain)" ]; then
      echo -e "${YELLOW}Warning: uncommitted changes present${NC}"
      git status --short
    else
      echo -e "${GREEN}Working tree clean${NC}"
    fi
    ;;

  verify)
    # Quick check: are we in a good state?
    ERRORS=0

    # On main?
    if [[ "$BRANCH" != "main" ]]; then
      echo -e "${RED}✗ Not on main (on $BRANCH)${NC}"
      ERRORS=$((ERRORS+1))
    else
      echo -e "${GREEN}✓ On main${NC}"
    fi

    # Clean tree?
    if [ -n "$(git status --porcelain)" ]; then
      echo -e "${RED}✗ Uncommitted changes${NC}"
      ERRORS=$((ERRORS+1))
    else
      echo -e "${GREEN}✓ Working tree clean${NC}"
    fi

    # Commit format check (last 10 commits)
    BAD_COMMITS=$(git log -10 --format="%s" 2>/dev/null | grep -cv '^\[' || true)
    if [ "$BAD_COMMITS" -gt 0 ]; then
      echo -e "${YELLOW}⚠ $BAD_COMMITS of last 10 commits missing [tag] prefix${NC}"
    else
      echo -e "${GREEN}✓ Recent commits follow [tag] format${NC}"
    fi

    if [ $ERRORS -gt 0 ]; then
      echo -e "\n${RED}Git verify FAILED — $ERRORS issues${NC}"
      exit 1
    else
      echo -e "\n${GREEN}Git verify PASSED${NC}"
    fi
    ;;

  *)
    echo "Usage: git_sync.sh <sync|verify>"
    exit 1
    ;;
esac

echo "━━━ GIT SYNC COMPLETE ━━━"
