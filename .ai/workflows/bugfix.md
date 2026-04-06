# Workflow: Bug Fix

## Autonomy Level: FULL
Bug fixes follow a streamlined TDD cycle without spec generation.

## Process
1. **Reproduce**: Identify the bug from inbox, developer report, or test failure
2. **Isolate**: Write a failing test that demonstrates the bug
3. **Fix**: Minimal code change to make the test pass
4. **Verify**: Run full test suite — no regressions
5. **Commit**: `[fix] [description of what was broken]`
6. **Finalize**: If on a feature branch, run preflight + merge

## Severity Routing
- **P0** (main is red, crash): Fix immediately, skip inbox check
- **P1** (feature broken): Fix before next story
- **P2** (cosmetic, edge case): Add to backlog, fix when convenient

## Post-Fix
- Update SINGLE_SOURCE_OF_TRUTH.md
- If the bug revealed a missing test category, add tests for that category
- If the bug was architectural, document in project-memory.md
