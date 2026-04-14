# Skill: Preflight Check

## Purpose
17-gate merge readiness validation. Gates 1-12 are CLI (always available).
Gates 13-17 are MCP (require Unity editor open). ALL available gates must pass.

## CLI Gates (always run)
1. **EditMode test status**: `./run-tests.sh editmode` should be reported, but failure is advisory
2. **PlayMode test status**: `./run-tests.sh playmode` should be reported, but failure is advisory
3. **Compiler clean**: no errors in Unity build
4. **Clean tree**: `git status --porcelain` is empty
5. **Feature branch**: not on main/master
6. **Upstream sync**: not behind origin/main (rebased)
7. **Branch tracking**: remote tracking set up (`git push -u`)
8. **Commit format**: all branch commits use `[tag] message` format
9. **Git verify**: `.ai/scripts/git_sync.sh verify` exits 0
10. **Spec complete**: all acceptance criteria checked off
11. **Quality contract**: file sizes, function sizes within limits
12. **No active flights**: flight-board.json has no active entries

## MCP Gates (run when editor is open)

### Gate 13: Console Clean
→ read_console
→ FAIL if any errors (warnings are OK unless Quest-critical)

### Gate 14: Missing References
→ find_gameobjects with component queries
→ For each MonoBehaviour in the scene: check for missing script references
→ FAIL if any "Missing (Mono Script)" components found

### Gate 15: Physics Layer Integrity
→ manage_physics → get layer collision matrix
→ Verify "Interactable" and "Hand" layers exist and interact correctly
→ WARN if layers not configured (may be pre-XR feature)

### Gate 16: Texture Budget
→ For each texture in Assets/_Project/Art/Textures/:
  → manage_texture → get import settings
  → WARN if any texture > 1024x1024 (except terrain/skybox)
  → FAIL if any texture not set to ASTC compression
  → WARN if mipmaps disabled on 3D textures

### Gate 17: Profiler Snapshot (optional, for perf-sensitive features)
→ manage_profiler → take memory snapshot
→ WARN if total texture memory > 200MB (Quest 2 budget)
→ Log frame timing baseline for comparison

## Usage
```bash
# CLI gates only
./preflight.sh

# MCP gates run automatically when editor_state is available
# They are checked by the orchestrator after CLI gates pass
```

## On Failure
- Report which gate(s) failed
- Attempt automatic fix (up to 3 times)
- If unfixable: report to developer
- Test-status failures are warnings unless the developer explicitly asks to block on them
- MCP gate failures don't block Core/-only features if editor is closed
