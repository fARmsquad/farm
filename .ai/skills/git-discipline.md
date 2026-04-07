# Skill: Git Discipline (Mandatory Git Operations)

## Purpose
Enforce consistent git hygiene at every phase boundary: pull before work,
branch per feature, commit per phase, rebase before merge. No code lands
on main without passing through this discipline.

## Principles
1. **Never work on stale code** — always pull/sync before starting
2. **Never work on main** — always on a feature branch
3. **Commit early, commit often** — one commit per logical unit of work
4. **Rebase before merge** — keep history clean, catch conflicts early
5. **Squash-merge to main** — one clean commit per feature on main

---

## Git Operations by Phase

### On Session Start (MANDATORY, before any work)
```
git fetch origin main
git status
```
- If on `main`: create feature branch (see Branch Creation below)
- If on feature branch: sync with origin/main (see Sync below)
- If uncommitted changes exist: stash or commit before proceeding

### Branch Creation (Phase 1: Intake)
When starting a new feature:
```
git checkout main
git pull origin main
git checkout -b feature/[story-id]-[short-name]
```

**Branch naming convention:**
- Features: `feature/L[level]-[id]-[short-name]` (e.g., `feature/L2-007-hunting-chore`)
- Bugfixes: `fix/[id]-[short-name]` (e.g., `fix/B-012-crop-rotation-crash`)
- Refactors: `refactor/[short-name]` (e.g., `refactor/extract-inventory-core`)

The branch MUST be created and pushed before Phase 2 begins:
```
git push -u origin feature/[branch-name]
```

### Sync Before Spec (Phase 2: before research + spec)
```
git fetch origin main
git rebase origin/main
```
- If rebase conflicts: resolve, then continue
- If conflicts are architectural: flag in `.ai/inbox/needs-eyes/`

### Commits During TDD Cycle (Phase 3)
Each agent commits after its step. This is already in the workflow but
the commit message format is now ENFORCED:

| Agent | Commit Format | Example |
|-------|--------------|---------|
| Spec delivery | `[spec] add specification for [feature]` | `[spec] add specification for crop-watering` |
| Research | `[research] add findings for [feature]` | `[research] add findings for crop-watering` |
| TDD agent (RED) | `[tests] add failing tests for [task]` | `[tests] add failing tests for water-level-calc` |
| Implementer (GREEN) | `[feature] implement [task]` | `[feature] implement water-level-calc` |
| Verifier | `[tests] add boundary tests for [task]` | `[tests] add boundary tests for water-level-calc` |
| Refactorer | `[refactor] clean up [task]` | `[refactor] clean up water-level-calc` |
| Scene assembly | `[scene] assemble [task] in editor` | `[scene] assemble water-plot in editor` |
| XR wiring | `[xr] wire interaction for [task]` | `[xr] wire interaction for watering-can` |
| Visual polish | `[visual] add visual polish for [task]` | `[visual] add water splash VFX` |

**Commit rules:**
- Stage only files related to the current task (`git add <specific-files>`)
- Never `git add .` or `git add -A` — prevents accidental inclusion of junk
- Every commit message starts with a `[tag]` prefix
- Commit body (optional) can reference the spec or research

### Mid-Feature Sync (between Phase 3 tasks)
After every 3rd task in the breakdown OR every 2 hours (whichever comes first):
```
git fetch origin main
git rebase origin/main
```
- Catches drift early instead of a painful mega-rebase at the end
- If rebase fails: fix conflicts, run tests, commit resolution

### Pre-Finalization Sync (Phase 4: before preflight)
```
git fetch origin main
git rebase origin/main
# Run full test suite after rebase
.ai/scripts/run-tests.sh all
```
- Tests MUST pass after rebase — if they don't, the rebase introduced a conflict
- Only proceed to preflight after post-rebase tests are green

### Merge to Main (Phase 4: finalization)
```
# Via finalize.sh --merge (uses gh CLI)
gh pr merge --squash --delete-branch
git checkout main
git pull origin main
.ai/scripts/run-tests.sh all
```

### Post-Merge Verification
```
git checkout main
git pull origin main
.ai/scripts/run-tests.sh all
```
- If tests fail on main: **P0 — fix immediately**
- Update `.ai/SINGLE_SOURCE_OF_TRUTH.md` only after green main

---

## Git Sync Script
Use `.ai/scripts/git_sync.sh` for automated sync operations.
It handles fetch, rebase, conflict detection, and test verification.

## Error Recovery

### Rebase Conflict
1. `git status` — identify conflicting files
2. Resolve conflicts (prefer incoming for non-project files, ours for Core/)
3. `git add <resolved-files>`
4. `git rebase --continue`
5. Run tests to verify resolution
6. If still broken: `git rebase --abort` and flag for developer

### Accidentally on Main
1. `git stash` (if uncommitted changes)
2. `git checkout -b feature/[name]`
3. `git stash pop` (if stashed)
4. Continue on feature branch

### Diverged Branch
1. `git fetch origin main`
2. `git rebase origin/main`
3. If too many conflicts (>5 files): consider fresh branch + cherry-pick

## Quality Gates
- [ ] On a feature branch (not main/master)
- [ ] Branch pushed to origin with `-u`
- [ ] Branch not behind origin/main (synced)
- [ ] All commits follow the `[tag] message` format
- [ ] No `git add .` or `git add -A` in commit history
- [ ] Tests pass after most recent rebase
