# Claude Code Orchestrator — FarmSim VR

You are the primary development agent. Read AGENTS.md first, then this file.

## Startup Sequence
1. Read AGENTS.md (constitution)
2. Read .ai/SINGLE_SOURCE_OF_TRUTH.md (current state)
3. **Read .ai/memory/project-memory.md (shared knowledge base — patterns, antipatterns, decisions, tech debt)**
4. **Read .ai/memory/completion-learnings.md (post-"done" misses, escaped verification, prevention rules)**
5. Read .ai/memory/session-memory.md (restore context if exists)
6. Determine task type from developer input
7. Route to appropriate workflow in .ai/workflows/
8. Execute workflow, updating SINGLE_SOURCE_OF_TRUTH.md as you go
9. On completion, update session-memory.md, project-memory.md, and completion-learnings.md if applicable

## Memory Protocol
- **READ** project-memory.md at session start — it has patterns, antipatterns, ADRs, tech debt
- **READ** completion-learnings.md at session start — it captures where previous "done" claims were incomplete or misleading
- **WRITE** to project-memory.md when you learn something non-obvious:
  - New pattern → "Established Patterns"
  - Failed approach → "Antipatterns"
  - Architecture decision → "ADRs" table
  - Bug root cause → "Lessons Learned"
  - Known debt → "Tech Debt Log"
- **WRITE** to completion-learnings.md when the developer comes back after a "done" claim with an error, issue, or misunderstanding:
  - capture the original completion claim
  - capture the issue and the approach that produced it
  - explain why existing verification or wording missed the problem
  - record the prevention rule for future work
- **DISTILL** durable prevention rules from completion-learnings.md into project-memory.md
- **WRITE** to research-notes.md after web research so Codex (no internet) can use it

## Your Capabilities
- Unity editor manipulation via unity-mcp bridge
- CLI test execution via run-tests.sh
- Git operations (branch, commit, push, PR via gh CLI)
- Sub-agent delegation (test-writer, implementer, refactorer, verifier, finalizer)
- Parallel flight coordination via .ai/coordination/

## Settings Reference
See .claude/settings.json for permissions. Key permissions:
- Allowed: test scripts, git operations, gh CLI, file search/grep
- Denied: rm -rf, force push, hard reset, admin merge

## PostToolUse Hooks
Auto-run EditMode tests after any .cs file edit in Core/ or Tests/EditMode/.

## When Uncertain
1. Check .ai/memory/project-memory.md for past decisions
2. Check .ai/memory/research-notes.md for prior investigation
3. If still uncertain, ASK the developer — don't guess on architecture
4. Document the decision in .ai/memory/project-memory.md for next time

## Unity MCP Integration

You have direct access to the Unity Editor via the official Unity MCP bridge.
The relay binary is launched by the Unity editor itself (com.unity.ai.assistant package).
Unity editor must be open with the MCP bridge running
(Edit > Project Settings > AI > Unity MCP → status "Running").

### MCP-First Principle
For anything that touches the Unity Editor, use MCP tools BEFORE falling back
to file writes. The priority order is:
1. MCP tool (direct editor manipulation — preferred)
2. File write + refresh_unity (write .cs, then refresh asset database)
3. CLI batchmode (fallback if Unity editor is closed)

### Check Editor State on Startup
Before any MCP operation, verify the editor is available:
- Call `Unity_ManageEditor(Action: "GetState")` — check compilation status, play mode
- If editor is in play mode, stop it before scene edits
- If compiling, wait for compilation to finish
- If the call fails, Unity is closed — fall back to CLI mode

### CRITICAL: Domain Reload & MCP Disconnection

Writing or modifying `.cs` files triggers a Unity domain reload (recompilation).
During domain reload, the MCP connection DROPS for 10-30 seconds.

**The workflow to avoid getting stuck:**

1. **Write ALL scripts first** before doing any scene wiring via MCP.
   Batch all file creates/edits together, then wait for compilation.
2. **After writing scripts, wait for domain reload to complete:**
   - Poll `Unity_ManageEditor(Action: "GetState")` until `IsCompiling: false`
   - If the call returns "Connection disconnected", wait 10s and retry
   - After 3 consecutive disconnects, check for stale relay processes
3. **Only then do scene assembly** (create GameObjects, add components, wire references)
4. **For complex scene wiring, write an Editor menu item script** (in `Editor/` folder)
   and execute it via `Unity_ManageMenuItem`. This is more reliable than
   individual MCP calls because it runs atomically in one C# method.

**If MCP stays disconnected after domain reload:**
```bash
# Kill stale relay processes (keeps only the editor's own relay)
ps aux | grep relay_mac_arm64 | grep -v grep
# Kill any relay NOT launched by the editor (the editor's relay has --relay --port flags)
kill <stale_pid>
```
Then ask developer to check: Edit > Project Settings > AI > Unity MCP > status.

### RunCommand Limitations

`Unity_RunCommand` compiles in its own dynamic assembly. It CANNOT directly
reference project scripts by `using FarmSimVR.*` namespace. Workarounds:
- Use `System.Type.GetType("FullTypeName, AssemblyName")` for reflection
- Use `SerializedObject` + `FindProperty` to set fields by string name
- Use `GetComponent<Component>()` and check `.GetType().Name == "ClassName"`
- For complex scene setup, prefer an **Editor menu item** (`[MenuItem]` attribute)
  which compiles inside the project and has full access to all project types.

### Visual Verification
After building something in the scene, take a screenshot via
`Unity_SceneView_CaptureMultiAngleSceneView` to verify layout.

### Console Monitoring
After any operation that could produce warnings or errors:
1. Call `Unity_GetConsoleLogs(logTypes: "Error")`
2. If script errors: investigate and fix before proceeding
3. Ignore the recurring TagManager.asset error (pre-existing, harmless)

## MCP Availability Detection

At the start of every session and before any MCP operation:

1. Try: `Unity_ManageEditor(Action: "GetState")`
2. If SUCCESS: MCP mode — use MCP tools for everything
3. If FAIL: CLI mode — editor is closed, use file writes

### What works in CLI mode (editor closed):
- All Core/ pure C# work (write files, run tests via batchmode)
- Git operations
- Spec writing, story management

### What REQUIRES MCP mode (editor open):
- Scene assembly (creating GameObjects, wiring components)
- Prefab creation
- ScriptableObject data asset creation
- Visual setup (materials, VFX, animation)

## Project-Specific Gotchas

### Assembly Definitions
This project uses assembly definitions. Know the boundaries:
- `FarmSimVR.Core` — `noEngineReferences: true`. NO UnityEngine types (Vector3, etc.)
  Pure C# only. Enums, structs, plain classes.
- `FarmSimVR.MonoBehaviours` — references `FarmSimVR.Core` and `Unity.InputSystem`.
  All MonoBehaviours, interfaces using Unity types, ScriptableObjects go here.
- `FarmSimVR.Interfaces` — `noEngineReferences: true`. Pure C# interfaces only.
- `FarmSimVR.Editor` — Editor scripts, menu items, custom inspectors.

**When creating new scripts, check which assembly they land in.**
If a script uses `UnityEngine.Vector3`, it CANNOT go in Core/ or Interfaces/.

### Input System
This project uses **New Input System ONLY** (`activeInputHandler: 1`).
Legacy `UnityEngine.Input` (GetKey, GetAxis, GetMouseButton) is DEAD — returns 0/false.
Always use `UnityEngine.InputSystem`:
```csharp
using UnityEngine.InputSystem;
// Keyboard
var kb = Keyboard.current;
if (kb.eKey.wasPressedThisFrame) { ... }
if (kb.wKey.isPressed) { ... }
// Mouse
var mouse = Mouse.current;
```

### Git LFS
The repo is configured for Git LFS but `git-lfs` is not installed locally.
LFS hooks (pre-push, post-merge, post-commit) will error but are non-blocking
except `pre-push` which blocks `git push`. If push fails:
```bash
rm .git/hooks/pre-push
git push origin main
```
The `pre-push` hook may reappear after pulls. Remove it again as needed.
