# CLAUDE.md — fARm (Quest VR Farming Sim)

## Read order

1. **This file** — quick-start rules and pointers
2. **AGENTS.md** (repo root) — iron laws, autonomy model, quality contract
3. **.ai/SINGLE_SOURCE_OF_TRUTH.md** — current project state snapshot
4. **.ai/memory/project-memory.md** — ADRs, patterns, antipatterns, lessons learned
5. Route to the appropriate **.ai/workflows/** file for your task

## Project identity

Unity 6 LTS (6000.4.1f1) · URP · Meta Quest 2+3 · Solo dev (Youssef) + AI agents · Spec-driven TDD

## Architecture boundaries

| Assembly | Path | UnityEngine? | Depends on |
|----------|------|-------------|------------|
| FarmSimVR.Core | Scripts/Core/ | **NO** (enforced) | nothing |
| FarmSimVR.Interfaces | Scripts/Interfaces/ | **NO** (enforced) | nothing |
| FarmSimVR.MonoBehaviours | Scripts/MonoBehaviours/ | yes | Core, Interfaces |
| FarmSimVR.Editor | Editor/ | yes | Core, MonoBehaviours |
| FarmSimVR.Tests.EditMode | Tests/EditMode/ | yes | everything |
| FarmSimVR.Tests.PlayMode | Tests/PlayMode/ | yes | everything |

## Hard rules

- **New Input System only** — `Keyboard.current`, `Mouse.current` etc. Legacy `UnityEngine.Input` throws at runtime.
- **No UnityEngine in Core/ or Interfaces/** — pure C#, testable without Play mode.
- **No Debug.Log in committed code** — use conditional compilation if needed.
- **No hardcoded asset paths** — always verify with `FindProjectAssets()` or glob first.
- **Max 500 lines/file, 40 lines/function, 3 levels of nesting.**
- **Preflight must pass before push** — run `.ai/scripts/preflight.sh`.
- **Feature branches + PRs** — never push directly to main.

## Auto-learning rule

When fixing a bug caused by a project convention, save the lesson to memory immediately — don't wait to be asked.

## MCP workflow (Unity Editor integration)

1. Write ALL `.cs` files first, batch them together.
2. Wait for domain reload (`GetState` until `IsCompiling: false`).
3. **Only then** do scene wiring via MCP tools.
4. For complex scene setup, prefer `[MenuItem]` Editor scripts over raw MCP calls.

## AI orchestration (`.ai/` directory)

### Agents (`.ai/agents/`)
Role-based sub-agents: architect, implementer, tdd-agent, verifier, refactorer, finalizer, spec-writer, xr-specialist, security-agent.

### Workflows (`.ai/workflows/`)
Task routing pipelines — each runs autonomously:

| Trigger | Workflow |
|---------|----------|
| New feature | `feature.md` — spec → TDD → verify → finalize → playtest |
| Bug report | `bugfix.md` — reproduce → test → fix |
| Performance issue | `performance.md` — profile → optimize → verify |
| Security concern | `security.md` — audit → remediate |
| Build/release | `deployment.md` — build → test → deploy |
| Harness change | `ai-architecture-change.md` — requires audit |

### Skills (`.ai/skills/`)
Reusable operational guides: git-discipline, git-finalization, preflight (17 gates), tdd-cycle, spec-driven-delivery, unity-research, scene-assembly, glb-ingest, asset-pipeline, unity-test-runner, parallel-flight, greybox-world, quest-build-profile.

### Memory (`.ai/memory/`)
- **project-memory.md** — ADRs, patterns, antipatterns, lessons. **Read before every task.**
- **research-notes.md** — web research findings (Claude writes, Codex reads).
- **story-backlog.md** — feature queue and priorities.
- **design-philosophy.md** — game design pillars.
- **asset-manifest.md** — imported asset tracking.

### Templates (`.ai/templates/`)
- `spec/` — feature-spec, technical-plan, task-breakdown, ADR
- `story/` — story-card, handoff-checklist
- `test/` — editmode-test, playmode-test

### Coordination
- `coordination/flight-board.json` — multi-agent file locking. Check before modifying files.
- `inbox/` — async developer input (bugs, feedback, ideas, steering).

## Validation commands

```bash
.ai/scripts/run-tests.sh editmode    # Fast Core/ unit tests
.ai/scripts/run-tests.sh playmode    # Integration tests
.ai/scripts/run-tests.sh all         # Full suite
.ai/scripts/preflight.sh             # 17-gate merge readiness
.ai/scripts/check_ai_wiring.sh       # Harness integrity audit
.ai/scripts/asset_import_check.sh    # Asset naming conventions
```

## Quest performance budget (hard constraint)

< 100 draw calls · < 750K triangles · < 256MB textures · < 11ms frame time (90 FPS)
