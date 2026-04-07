# Codex Skills Catalog — FarmSim VR

## Available Skills
These map to .ai/skills/ files that Codex can invoke:

| Skill | File | Use When |
|-------|------|----------|
| Git Discipline | git-discipline.md | **MANDATORY** at every phase boundary — pull, branch, commit, merge |
| Unity Research | unity-research.md | **MANDATORY** before every spec & implementation — web search for patterns |
| TDD Cycle | tdd-cycle.md | Writing tests and implementation |
| Story Lookup | story-lookup.md | Finding story context |
| Spec Delivery | spec-driven-delivery.md | Generating spec packages |
| Test Runner | unity-test-runner.md | Running/parsing test results |
| Preflight | preflight.md | Pre-merge validation |
| Git Finalize | git-finalization.md | PR + merge pipeline |
| Parallel Flight | parallel-flight.md | Multi-agent coordination |
| Asset Pipeline | asset-pipeline.md | 3D asset import validation |

## Codex-Specific Notes
- You cannot run tests directly — generate test files and flag for execution
- You cannot access Unity editor — write code that the editor will pick up
- Prefer generating complete files over incremental edits
- Always check flight-board.json before writing to shared paths
