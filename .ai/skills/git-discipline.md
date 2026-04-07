# Skill: Git Discipline (Trunk-Based Development)

## Purpose
Enforce consistent git hygiene. All work happens on main. Commit early,
commit often, push frequently. No branches, no PRs, no rebasing.

## Principles
1. **Never work on stale code** — always `git pull origin main` before starting
2. **Everything on main** — no feature branches
3. **Commit early, commit often** — one commit per logical unit of work
4. **Push after every completed task** — keep remote up to date
5. **Main is always green** — run tests before pushing

---

## Git Operations by Phase

### On Session Start (MANDATORY, before any work)
```
git pull origin main
git status
```
- If uncommitted changes exist: commit or stash before proceeding
- If pull fails (diverged): pull with rebase (`git pull --rebase origin main`)

### Commits During TDD Cycle (Phase 3)
Each agent commits after its step with enforced format:

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

### Push Cadence
- `git push origin main` after every completed task
- If push is rejected (someone else pushed): `git pull --rebase origin main`, re-run tests, push again
- Never force push

### Pre-Push Check
Before every push:
```
./run-tests.sh all    # tests must pass
git push origin main
```

---

## Error Recovery

### Push Rejected (Remote Has New Commits)
1. `git pull --rebase origin main`
2. If conflict: resolve, `git add`, `git rebase --continue`
3. Run tests to verify
4. `git push origin main`

### Accidentally Committed Broken Code
1. Fix the issue immediately
2. Commit the fix: `[fix] fix [what broke]`
3. Push both commits

## Quality Gates
- [ ] On main branch
- [ ] Working tree clean (or changes are staged/committed)
- [ ] All commits follow the `[tag] message` format
- [ ] No `git add .` or `git add -A` in commit history
- [ ] Tests pass before push
