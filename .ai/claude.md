# Claude Code Orchestrator — FarmSim VR

You are the primary development agent. Read AGENTS.md first, then this file.

## Startup Sequence
1. Read AGENTS.md (constitution)
2. Read .ai/SINGLE_SOURCE_OF_TRUTH.md (current state)
3. Read .ai/memory/session-memory.md (restore context if exists)
4. Determine task type from developer input
5. Route to appropriate workflow in .ai/workflows/
6. Execute workflow, updating SINGLE_SOURCE_OF_TRUTH.md as you go
7. On completion, update session-memory.md and project-memory.md

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
The relay binary at `~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64`
is launched by CC as an MCP server (configured in .claude/settings.json).
Unity editor must be open with the MCP bridge running
(Edit > Project Settings > AI > Unity MCP → status "Running").

### MCP Server Config (for .claude/settings.json)
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64",
      "args": ["--mcp"]
    }
  }
}
```
First connection requires approval in Unity: Project Settings > AI > Unity MCP > Pending Connections > Accept.

### MCP-First Principle
For anything that touches the Unity Editor, use MCP tools BEFORE falling back
to file writes. The priority order is:
1. MCP tool (direct editor manipulation — preferred)
2. File write + refresh_unity (write .cs, then refresh asset database)
3. CLI batchmode (fallback if Unity editor is closed)

### Check Editor State on Startup
Before any MCP operation, verify the editor is available:
- Read `editor_state` resource — check compilation status, play mode
- If editor is in play mode, wait or ask developer to stop
- If compiling, wait for compilation to finish
- If editor_state fails, Unity is closed — fall back to CLI mode

### The batch_execute Pattern
For complex operations, chain MCP calls to avoid round-trip latency:
```
batch_execute([
  { tool: "manage_gameobject", action: "create", name: "TomatoPlot", position: [2, 0, 3] },
  { tool: "manage_components", action: "add", gameObject: "TomatoPlot", component: "CropPlotController" },
  { tool: "manage_components", action: "add", gameObject: "TomatoPlot", component: "XRGrabInteractable" },
  { tool: "manage_physics", action: "set_layer", gameObject: "TomatoPlot", layer: "Interactable" },
  { tool: "manage_prefabs", action: "create", gameObject: "TomatoPlot", path: "Assets/_Project/Prefabs/Farm/TomatoPlot.prefab" }
])
```

### Visual Verification
After building something in the scene, take a screenshot via manage_editor
to verify it looks correct. If something looks wrong, fix it before proceeding.
This is the MCP equivalent of "looking at your work."

### Console Monitoring
After any operation that could produce warnings or errors:
1. Call read_console
2. If errors: investigate and fix before proceeding
3. If warnings: log to session-memory.md, continue if non-critical

### run_tests via MCP (preferred over CLI)
Use the MCP `run_tests` tool instead of `./run-tests.sh` when the editor is open.
It's faster (no batchmode boot), gives real-time results, and doesn't lock
the editor. Fall back to CLI only when Unity is closed.

## MCP Availability Detection

At the start of every session and before any MCP operation:

1. Try: read editor_state resource
2. If SUCCESS: MCP mode — use MCP tools for everything
3. If FAIL: CLI mode — editor is closed, use file writes + batchmode

### What works in CLI mode (editor closed):
- All Core/ pure C# work (write files, run tests via batchmode)
- Git operations
- Spec writing, story management
- Preflight gates 1-9

### What REQUIRES MCP mode (editor open):
- Scene assembly (creating GameObjects, wiring components)
- Prefab creation
- ScriptableObject data asset creation
- XR component wiring
- Visual setup (materials, VFX, animation)
- Profiling
- Preflight gates 10-14
- Quick-turnaround test runs

### Hybrid Pattern
CC can do ALL Core/ work with editor closed, then batch the MCP work
when the editor opens:
1. Developer closes Unity, leaves CC running overnight
2. CC completes all Core/ TDD cycles (spec, tests, implementation)
3. CC queues MCP operations in .ai/coordination/mcp-queue.json
4. Developer opens Unity next morning
5. CC detects editor, drains the MCP queue, assembles everything
6. Developer gets a playtest guide covering all overnight work

### mcp-queue.json Schema
```json
{
  "pending": [
    {
      "story": "crop-watering",
      "operations": [
        { "tool": "manage_gameobject", "action": "create", "args": {} },
        { "tool": "manage_components", "action": "add", "args": {} }
      ],
      "queued_at": "2026-04-06T23:00:00Z"
    }
  ],
  "completed": []
}
```
