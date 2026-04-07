# Skill: Git Finalization

## Hard Gates (ALL must pass)
1. All tests green: ./run-tests.sh all
2. Working tree clean: no uncommitted changes
3. On feature branch: not main/master
4. Upstream synced: branch not behind origin/main
5. Preflight passed: ./preflight.sh exits 0
6. Spec acceptance criteria: all checked off
7. No parallel flights active on this branch

## Process
1. Run git_finalize_guard.sh (validates gates 1-4, 7)
2. Run preflight.sh (validates gates 5-6 plus architecture/hygiene)
3. If both pass: proceed to finalize.sh
4. If either fails: report what failed, do NOT proceed

## Post-Merge
1. Checkout main, pull
2. Run full test suite
3. If red: P0, investigate immediately
4. If green: update SINGLE_SOURCE_OF_TRUTH.md
5. Delete feature branch (done by finalize.sh --merge)
