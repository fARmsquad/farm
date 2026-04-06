# Skill: Preflight Check

## Purpose
14-gate merge readiness validation. Gates 1-9 are CLI (always available).
Gates 10-14 are MCP (require Unity editor open). ALL available gates must pass.

## CLI Gates (always run)
1. **EditMode tests**: `./run-tests.sh editmode` exits 0
2. **PlayMode tests**: `./run-tests.sh playmode` exits 0
3. **Compiler clean**: no errors in Unity build
4. **Clean tree**: `git status --porcelain` is empty
5. **Feature branch**: not on main/master
6. **Upstream sync**: not behind origin/main
7. **Spec complete**: all acceptance criteria checked off
8. **Quality contract**: file sizes, function sizes within limits
9. **No active flights**: flight-board.json has no active entries

## MCP Gates (run when editor is open)

### Gate 10: Console Clean
→ read_console
→ FAIL if any errors (warnings are OK unless Quest-critical)

### Gate 11: Missing References
→ find_gameobjects with component queries
→ For each MonoBehaviour in the scene: check for missing script references
→ FAIL if any "Missing (Mono Script)" components found

### Gate 12: Physics Layer Integrity
→ manage_physics → get layer collision matrix
→ Verify "Interactable" and "Hand" layers exist and interact correctly
→ WARN if layers not configured (may be pre-XR feature)

### Gate 13: Texture Budget
→ For each texture in Assets/_Project/Art/Textures/:
  → manage_texture → get import settings
  → WARN if any texture > 1024x1024 (except terrain/skybox)
  → FAIL if any texture not set to ASTC compression
  → WARN if mipmaps disabled on 3D textures

### Gate 14: Profiler Snapshot (optional, for perf-sensitive features)
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
- MCP gate failures don't block Core/-only features if editor is closed
