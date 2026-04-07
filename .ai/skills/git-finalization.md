# Skill: Git Finalization

## Hard Gates (ALL must pass)
1. All tests green: ./run-tests.sh all
2. Working tree clean: no uncommitted changes
3. On feature branch: not main/master
4. Upstream synced: branch rebased onto origin/main
5. Branch tracking remote: `git push -u` was used
6. Commit format: all branch commits use `[tag] message` format
7. Git verify: `.ai/scripts/git_sync.sh verify` exits 0
8. Preflight passed: ./preflight.sh exits 0
9. Spec acceptance criteria: all checked off
10. No parallel flights active on this branch

## Process
1. Run `.ai/scripts/git_sync.sh sync` (final rebase onto origin/main)
2. Run `.ai/scripts/run-tests.sh all` (post-rebase test verification)
3. Run `.ai/scripts/git_sync.sh verify` (validates gates 3-7)
4. Run git_finalize_guard.sh (validates gates 1-2, 10)
5. Run preflight.sh (validates gates 8-9 plus architecture/hygiene)
6. If all pass: proceed to finalize.sh
7. If any fails: report what failed, do NOT proceed

## Post-Merge
1. Checkout main, pull
2. Run full test suite
3. If red: P0, investigate immediately
4. If green: update SINGLE_SOURCE_OF_TRUTH.md
5. Delete feature branch (done by finalize.sh --merge)
