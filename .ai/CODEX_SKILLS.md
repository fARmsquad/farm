# Codex Skills Catalog — FarmSim VR

## Available Skills
These map to .ai/skills/ files that Codex can invoke:

| Skill | File | Use When |
|-------|------|----------|
| Git Discipline | git-discipline.md | **MANDATORY** at every phase boundary — pull, commit, push to main |
| Unity Research | unity-research.md | **MANDATORY** before every spec & implementation — web search for patterns |
| TDD Cycle | tdd-cycle.md | Writing tests and implementation |
| Story Lookup | story-lookup.md | Finding story context |
| Spec Delivery | spec-driven-delivery.md | Generating spec packages |
| Test Runner | unity-test-runner.md | Running/parsing test results |
| Preflight | preflight.md | Pre-push validation |
| Git Finalize | git-finalization.md | Pre-push validation + push to main |
| Parallel Flight | parallel-flight.md | Multi-agent coordination |
| Asset Pipeline | asset-pipeline.md | 3D asset import validation |

## Global Codex Unity Skills
These are external Codex skills installed outside the repo and used as
adjuncts to the local `.ai/skills/` workflow. Treat the repo-local skills as
the project contract and use these global skills to strengthen Unity-specific
execution where they fit.

| Skill | Source | Use When |
|-------|--------|----------|
| unity-initial-setup | requested external skill (not installed) | Reserved for future Unity MCP/bootstrap recovery if a valid Codex-installable package is identified |
| unity-mcp-orchestrator | installed global Codex skill | Operating the Unity Editor through MCP: scene assembly, GameObject changes, script edits, scene management, tests |
| unity-developer | installed global Codex skill | General Unity implementation work beyond pure Core logic: C#, assets, rendering, scene-side patterns |
| unity-ecs-patterns | installed global Codex skill | DOTS, Jobs, Burst, ECS, or large-entity performance-heavy architecture work |
| unity-profiler | installed global Codex skill | Profiling FPS, memory, draw calls, and validating optimization work with measured evidence |

## Global Unity Skill Status
- Installed in Codex on 2026-04-11: `unity-mcp-orchestrator`,
  `unity-developer`, `unity-ecs-patterns`, `unity-profiler`
- Not installed: `unity-initial-setup`
  - Attempted sources:
    - `IvanMurzak/Unity-MCP`
    - `devwinsoft/devian`
  - Result: neither source exposed a Codex-installable skill by that exact
    name at install time
  - Last verified details:
    - `IvanMurzak/Unity-MCP` exposed `build-cli` and `github-pr-review-fix`
    - `devwinsoft/devian` exposed unrelated utility/game skills, but not
      `unity-initial-setup`
- Do not assume `unity-initial-setup` exists on this machine until the exact
  package path is verified and installed into Codex

## Codex-Specific Notes
- You cannot run tests directly — generate test files and flag for execution
- You cannot access Unity editor — write code that the editor will pick up
- Prefer generating complete files over incremental edits
- Always check flight-board.json before writing to shared paths
- For Unity Editor automation, prefer the global `unity-mcp-orchestrator`
  skill plus the repo's MCP workflow notes rather than ad hoc editor actions
