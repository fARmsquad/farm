#!/bin/bash
# Git Sync — fetch, rebase, verify. Called at phase boundaries.
set -e

PROJECT_PATH="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_PATH"

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m'

BRANCH=$(git branch --show-current)
ACTION="${1:-sync}"  # sync | create | verify

echo "━━━ GIT SYNC ━━━"
echo "Branch: $BRANCH"
echo "Action: $ACTION"

case "$ACTION" in

  create)
    # Create a new feature branch from fresh main
    BRANCH_NAME="${2:?Usage: git_sync.sh create <branch-name>}"
    echo -e "\n[1/3] Switching to main and pulling..."
    git checkout main
    git pull origin main

    echo -e "\n[2/3] Creating branch: $BRANCH_NAME"
    git checkout -b "$BRANCH_NAME"

    echo -e "\n[3/3] Pushing branch to origin..."
    git push -u origin "$BRANCH_NAME"

    echo -e "\n${GREEN}Branch $BRANCH_NAME created and pushed${NC}"
    ;;

  sync)
    # Rebase current branch onto latest origin/main
    if [[ "$BRANCH" == "main" || "$BRANCH" == "master" ]]; then
      echo -e "${RED}ERROR: Cannot sync from $BRANCH — create a feature branch first${NC}"
      exit 1
    fi

    echo -e "\n[1/3] Fetching origin/main..."
    git fetch origin main --quiet

    # Check if we're behind
    LOCAL=$(git rev-parse origin/main 2>/dev/null || echo "unknown")
    BASE=$(git merge-base HEAD origin/main 2>/dev/null || echo "unknown")

    if [ "$LOCAL" = "$BASE" ]; then
      echo -e "${GREEN}Already up to date with origin/main${NC}"
    else
      echo -e "${YELLOW}Branch is behind origin/main — rebasing...${NC}"

      echo -e "\n[2/3] Rebasing onto origin/main..."
      if git rebase origin/main; then
        echo -e "${GREEN}Rebase successful${NC}"
      else
        echo -e "${RED}Rebase conflict detected!${NC}"
        echo "Conflicting files:"
        git diff --name-only --diff-filter=U
        echo ""
        echo "Options:"
        echo "  1. Resolve conflicts, then: git add <files> && git rebase --continue"
        echo "  2. Abort: git rebase --abort"
        exit 1
      fi
    fi

    echo -e "\n[3/3] Verifying branch state..."
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

    # On feature branch?
    if [[ "$BRANCH" == "main" || "$BRANCH" == "master" ]]; then
      echo -e "${RED}✗ On $BRANCH — need feature branch${NC}"
      ERRORS=$((ERRORS+1))
    else
      echo -e "${GREEN}✓ On feature branch: $BRANCH${NC}"
    fi

    # Clean tree?
    if [ -n "$(git status --porcelain)" ]; then
      echo -e "${RED}✗ Uncommitted changes${NC}"
      ERRORS=$((ERRORS+1))
    else
      echo -e "${GREEN}✓ Working tree clean${NC}"
    fi

    # Synced with origin/main?
    git fetch origin main --quiet 2>/dev/null || true
    LOCAL=$(git rev-parse origin/main 2>/dev/null || echo "x")
    BASE=$(git merge-base HEAD origin/main 2>/dev/null || echo "y")
    if [ "$LOCAL" != "$BASE" ]; then
      echo -e "${RED}✗ Behind origin/main${NC}"
      ERRORS=$((ERRORS+1))
    else
      echo -e "${GREEN}✓ Synced with origin/main${NC}"
    fi

    # Branch pushed?
    REMOTE_BRANCH=$(git rev-parse --abbrev-ref --symbolic-full-name @{u} 2>/dev/null || echo "")
    if [ -z "$REMOTE_BRANCH" ]; then
      echo -e "${YELLOW}⚠ Branch not tracking a remote — push with -u${NC}"
      ERRORS=$((ERRORS+1))
    else
      echo -e "${GREEN}✓ Tracking $REMOTE_BRANCH${NC}"
    fi

    # Commit format check (last 10 commits on this branch)
    BAD_COMMITS=$(git log origin/main..HEAD --format="%s" 2>/dev/null | grep -cv '^\[' || true)
    if [ "$BAD_COMMITS" -gt 0 ]; then
      echo -e "${YELLOW}⚠ $BAD_COMMITS commits missing [tag] prefix${NC}"
    else
      echo -e "${GREEN}✓ All commits follow [tag] format${NC}"
    fi

    if [ $ERRORS -gt 0 ]; then
      echo -e "\n${RED}Git verify FAILED — $ERRORS issues${NC}"
      exit 1
    else
      echo -e "\n${GREEN}Git verify PASSED${NC}"
    fi
    ;;

  *)
    echo "Usage: git_sync.sh <sync|create|verify> [branch-name]"
    exit 1
    ;;
esac

echo "━━━ GIT SYNC COMPLETE ━━━"
