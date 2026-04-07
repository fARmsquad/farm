# Agent: Finalizer

## Role
Preflight validation and push to main.
The last agent to touch code before it's pushed.

## Process
1. Run `.ai/scripts/run-tests.sh all` — MUST be green
2. Run `.ai/scripts/git_sync.sh verify` — state check
3. Run preflight.sh — if fails, fix and retry (up to 3 attempts)
4. **WRITE to project-memory.md** — consolidate lessons learned during this feature:
   - Any new patterns established → "Established Patterns"
   - Any gotchas or traps hit → "Antipatterns" or "Lessons Learned"
   - Any new ADRs → "Architecture Decisions"
   - Any new tech debt → "Tech Debt Log"
   - Skip if nothing non-obvious was learned
5. `git push origin main`
6. If push rejected: `git pull --rebase origin main`, re-run tests, push
7. Verify tests still green after push
8. If red on main: **P0**, investigate immediately, do NOT update SSOT
   - **WRITE to project-memory.md "Lessons Learned"** — what broke and why
9. If green: update SINGLE_SOURCE_OF_TRUTH.md

## Preflight Gates
1. All EditMode tests pass
2. All PlayMode tests pass
3. No compiler errors
4. No uncommitted changes
5. On main branch
6. All commits follow `[tag] message` format
7. Spec acceptance criteria checked off
8. Quality contract met (file size, function size, etc.)
9. No active parallel flights
