# Agent: Finalizer

## Role
Preflight validation, PR creation, squash-merge pipeline.
The last agent to touch code before it reaches main.

## Process
1. **Git sync**: `.ai/scripts/git_sync.sh sync` — rebase onto latest origin/main
2. **Post-rebase tests**: `.ai/scripts/run-tests.sh all` — MUST be green
3. **Git verify**: `.ai/scripts/git_sync.sh verify` — branch state check
4. Run preflight.sh (9-gate validation + git verify)
5. If any gate fails: diagnose and fix (up to 3 attempts)
6. **WRITE to project-memory.md** — consolidate lessons learned during this feature:
   - Any new patterns established → "Established Patterns"
   - Any gotchas or traps hit → "Antipatterns" or "Lessons Learned"
   - Any new ADRs → "Architecture Decisions"
   - Any new tech debt → "Tech Debt Log"
   - Skip if nothing non-obvious was learned
7. `git push` feature branch to origin (final push after rebase)
8. Create PR with:
   - Title: feature name
   - Body: spec summary, test coverage, files changed, research sources
   - Labels: auto-applied based on workflow type
9. Squash-merge to main (`gh pr merge --squash --delete-branch`)
10. Post-merge: `git checkout main && git pull origin main`
11. Run full test suite on main
12. If main is red: **P0**, investigate immediately, do NOT update SSOT
    - **WRITE to project-memory.md "Lessons Learned"** — what broke and why
13. If main is green: update SINGLE_SOURCE_OF_TRUTH.md, confirm branch deleted

## Preflight Gates (11 total)
1. All EditMode tests pass
2. All PlayMode tests pass
3. No compiler errors
4. No uncommitted changes
5. On feature branch (not main)
6. Branch not behind origin/main (rebased)
7. Branch tracking remote (`git push -u` was used)
8. All commits follow `[tag] message` format
9. Spec acceptance criteria checked off
10. Quality contract met (file size, function size, etc.)
11. No active parallel flights on this branch

## PR Template
```
## [Feature/Fix/Refactor]: [Name]

### Summary
[What this does in plain language]

### Spec
[Link to spec in Assets/Specs/]

### Test Coverage
- EditMode: X tests
- PlayMode: Y tests

### Files Changed
[Grouped by system]

### Quest Performance Impact
[Any perf-relevant notes]
```
