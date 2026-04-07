# Project Memory — FarmSim VR

> **This is the shared knowledge base for ALL agents (Claude Code, Codex, Cursor).**
> Both agents MUST read this on startup and append to it when learning something new.
> If you discover a pattern, antipattern, gotcha, or make an architecture decision,
> write it here so the next agent (or the next session) doesn't repeat the mistake.

---

## Architecture Decisions (ADRs)

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-06 | Pure C# Core + thin MonoBehaviour wrappers | Testability: Core/ has zero UnityEngine refs, can unit-test without editor |
| 2026-04-06 | Target Quest 2 + Quest 3, Unity 6 LTS, URP | Hardware floor = Quest 2; URP for mobile perf; Unity 6 for latest XR toolkit |
| 2026-04-06 | Assembly definitions enforce boundaries | `FarmSimVR.Core` (no engine refs), `FarmSimVR.MonoBehaviours`, `FarmSimVR.Interfaces` (no engine refs), `FarmSimVR.Editor` |
| 2026-04-06 | New Input System only (legacy disabled) | `activeInputHandler: 1` — `UnityEngine.Input` returns 0/false always |
| 2026-04-07 | Codex gets Unity MCP parity via relay binary | Same relay, same tools as CC. Both agents can manipulate scenes. |

---

## Established Patterns (DO THIS)

<!-- Append new patterns as you discover what works well -->

### Script Organization
- MonoBehaviours go in `Assets/_Project/Scripts/MonoBehaviours/`
- Pure C# core logic goes in `Assets/_Project/Scripts/Core/`
- Editor scripts go in `Assets/_Project/Scripts/Editor/`
- Interfaces go in `Assets/_Project/Scripts/Interfaces/`

### MCP Workflow
- Write ALL `.cs` files first, then wait for domain reload, then do scene wiring
- Poll `Unity_ManageEditor(Action: "GetState")` until `IsCompiling: false` before MCP calls
- For complex scene setup, prefer Editor menu item scripts over individual MCP calls
- `Unity_RunCommand` can't reference project types — use reflection or SerializedObject

### Testing
- EditMode tests for Core/ pure C# logic
- PlayMode tests for MonoBehaviour integration
- Every public method in Core/ must have a test

### Git
- Feature branches: `feature/<story-id>-<slug>`
- Commit prefix: `[cc]` for Claude Code, `[codex]` for Codex
- No force push. No `--no-verify`. Remove LFS `pre-push` hook if push fails.

---

## Antipatterns (DON'T DO THIS)

<!-- Append antipatterns as you discover what breaks or wastes time -->

### Code
- DON'T put `UnityEngine.Vector3` or any engine type in Core/ or Interfaces/ assemblies
- DON'T use `Debug.Log` in committed code (use conditional compilation)
- DON'T use legacy Input API (`Input.GetKey`, `Input.GetAxis`) — it's dead
- DON'T leave `TODO`/`FIXME` in merge-ready code

### MCP
- DON'T call MCP scene tools immediately after writing `.cs` files — domain reload will disconnect
- DON'T launch a separate relay process — the Unity editor manages its own relay
- DON'T assume MCP is available — always check editor state first

### Git
- DON'T modify files another agent has locked in `flight-board.json`
- DON'T commit `.ai/memory/session-memory.md` (it's ephemeral)

---

## Tech Debt Log

<!-- Track known debt so agents can opportunistically fix it -->

| Date | Item | Severity | Notes |
|------|------|----------|-------|
| 2026-04-06 | git-lfs not installed locally | Low | LFS hooks error but non-blocking; remove pre-push hook to push |
| 2026-04-06 | TagManager.asset recurring error | Ignore | Pre-existing, harmless — don't try to fix |

---

## Lessons Learned

<!-- Append hard-won lessons from debugging sessions, failed approaches, etc. -->

(will accumulate through development)

---

## Performance Budgets (Quest 2)

- Draw calls: < 100
- Triangles: < 750K
- Texture memory: < 256MB
- Frame time: < 11ms (90 FPS target)
- See `.ai/docs/quest-perf-budget.md` for full budget
