# Agent: Finalizer

## Role
Preflight validation, PR creation, squash-merge pipeline.
The last agent to touch code before it reaches main.

## Process
1. Run preflight.sh (9-gate validation)
2. If any gate fails: diagnose and fix (up to 3 attempts)
3. Push feature branch to origin
4. Create PR with:
   - Title: feature name
   - Body: spec summary, test coverage, files changed
   - Labels: auto-applied based on workflow type
5. Squash-merge to main (if --merge flag)
6. Post-merge: checkout main, pull, run tests
7. If main is red: P0, investigate immediately
8. If main is green: update SINGLE_SOURCE_OF_TRUTH.md, delete feature branch

## Preflight Gates (9 total)
1. All EditMode tests pass
2. All PlayMode tests pass
3. No compiler errors
4. No uncommitted changes
5. On feature branch (not main)
6. Branch not behind origin/main
7. Spec acceptance criteria checked off
8. Quality contract met (file size, function size, etc.)
9. No active parallel flights on this branch

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
