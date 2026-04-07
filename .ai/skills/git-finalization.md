# Skill: Git Finalization

## Hard Gates (ALL must pass before push)
1. All tests green: ./run-tests.sh all
2. Working tree clean: no uncommitted changes
3. On main branch
4. Commit format: all recent commits use `[tag] message` format
5. Preflight passed: ./preflight.sh exits 0
6. Spec acceptance criteria: all checked off
7. No parallel flights active

## Process
1. Run `.ai/scripts/run-tests.sh all` (full test suite)
2. Run preflight.sh (validates gates above)
3. If any gate fails: report what failed, fix, retry (up to 3 attempts)
4. `git push origin main`
5. If push rejected: `git pull --rebase origin main`, re-run tests, push again
6. Update SINGLE_SOURCE_OF_TRUTH.md
7. **WRITE to project-memory.md** — consolidate lessons from this feature
