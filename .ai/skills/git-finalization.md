# Skill: Git Finalization

## Hard Gates (ALL must pass before push)
1. Working tree clean: no uncommitted changes
2. On main branch
3. Commit format: all recent commits use `[tag] message` format
4. Preflight passed: ./preflight.sh exits 0
5. Spec acceptance criteria: all checked off
6. No parallel flights active

## Process
1. Run `.ai/scripts/run-tests.sh all` if you want current suite status, but do not block on it
2. Run preflight.sh (validates hard gates above)
3. If any hard gate fails: report what failed, fix, retry (up to 3 attempts)
4. `git push origin main`
5. If push rejected: `git pull --rebase origin main`, optionally re-run tests, push again
6. Update SINGLE_SOURCE_OF_TRUTH.md
7. **WRITE to project-memory.md** — consolidate lessons from this feature
