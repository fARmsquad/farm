# Workflow: Bug Fix

## Autonomy Level: FULL
Bug fixes follow a streamlined TDD cycle without spec generation.

## Process
1. **Check completion context**: if this bug arrived after an agent said "done," append a structured entry to `.ai/memory/completion-learnings.md` before fixing it
2. **Reproduce**: Identify the bug from inbox, developer report, or test failure
3. **Isolate**: Write a failing test that demonstrates the bug
4. **Fix**: Minimal code change to make the test pass
5. **Verify**: Run full test suite — no regressions
6. **Commit**: `[fix] [description of what was broken]`
7. **Push**: `git push origin main`

## Severity Routing
- **P0** (main is red, crash): Fix immediately, skip inbox check
- **P1** (feature broken): Fix before next story
- **P2** (cosmetic, edge case): Add to backlog, fix when convenient

## Post-Fix
- Update SINGLE_SOURCE_OF_TRUTH.md
- If the bug revealed a missing test category, add tests for that category
- If the bug was architectural, document in project-memory.md
- If this was a post-"done" issue, update the completion-learning entry with the root cause, the fix, and the prevention rule that future work must reference
