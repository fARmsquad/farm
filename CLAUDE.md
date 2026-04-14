# CLAUDE.md — fARm (Quest VR Farming Sim)

## Read order

1. **This file** — quick-start rules and pointers
2. **AGENTS.md** (repo root) — iron laws, autonomy model, quality contract
3. **.ai/SINGLE_SOURCE_OF_TRUTH.md** — current project state snapshot
4. **.ai/memory/project-memory.md** — ADRs, patterns, antipatterns, lessons learned
5. Route to the appropriate **.ai/workflows/** file for your task

## Project identity

Unity 6 LTS (6000.4.1f1) · URP · Meta Quest 2+3 · Solo dev (Youssef) + AI agents · Spec-driven TDD

## Scene Map

All project scenes live in `Assets/_Project/Scenes/` (not `Assets/Scenes/`).

| Scene | Purpose |
|---|---|
| `FarmMain.unity` | Main farm gameplay |
| `WorldMain.unity` | Overworld/world map |
| `TitleScreen.unity` | Title/main menu |
| `Intro.unity` | Intro cutscene (Synty farm prefabs, IntroCutsceneManager) |
| `HuntingTest.unity` | Hunting mechanic test |
| `AssetShowcase.unity` | Asset preview |
| `INT001_ScreenEffectsTest.unity` | Screen effects integration test |

> Ignore `Assets/Scenes/SampleScene.unity` — Unity default, unused.

## Testing — NON-NEGOTIABLE

**Iron Law #1: NEVER write implementation without a failing test first.**

This is the most violated rule. Every implementation task MUST follow this cycle:

```
1. RED    — Write a failing test FIRST. Run it. Confirm it fails.
2. GREEN  — Write the minimum code to make it pass. Run it. Confirm it passes.
3. REFACTOR — Clean up while keeping tests green.
```

### What to test and where

| Code location | Test type | Test location | Runner |
|--------------|-----------|---------------|--------|
| `Scripts/Core/` (pure C#) | EditMode unit tests | `Tests/EditMode/` | `.ai/scripts/run-tests.sh editmode` |
| `Scripts/MonoBehaviours/` | PlayMode integration tests | `Tests/PlayMode/` | `.ai/scripts/run-tests.sh playmode` |
| `Editor/` scene builders | EditMode validation tests | `Tests/EditMode/` | `.ai/scripts/run-tests.sh editmode` |

### Test naming convention
```
MethodName_Condition_ExpectedResult
```
Example: `CalculateGrowth_WithDroughtWeather_ReturnsReducedGrowth`

### When you MUST write tests
- **Any new public method in Core/** — always, no exceptions
- **Any new MonoBehaviour with logic** — PlayMode test for key behaviors
- **Any state machine or phase transition** — test all transitions and edge cases
- **Any calculator, tracker, or manager** — test core operations and boundary conditions
- **Bug fixes** — write a test that reproduces the bug FIRST, then fix it

### When tests can be skipped
- Pure data classes (structs/enums with no logic)
- Editor scripts that only place GameObjects (validated by scene builder tests)
- Demo/debug scripts (ScreenEffectsDemo, etc.)

### Running tests
```bash
.ai/scripts/run-tests.sh editmode    # Fast — run after every Core/ change
.ai/scripts/run-tests.sh playmode    # Slow — run after MonoBehaviour changes
.ai/scripts/run-tests.sh all         # Full — report status before push
```

### If you catch yourself writing code without tests
**STOP.** Go back. Write the test first. This is not optional and not a suggestion. The test-first discipline exists because bugs in untested code cost 10x more to fix later. The AGENTS.md Iron Laws are a contract, not guidelines.

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
.ai/scripts/run-tests.sh all         # Full suite status check
.ai/scripts/preflight.sh             # 17-gate merge readiness
.ai/scripts/check_ai_wiring.sh       # Harness integrity audit
.ai/scripts/asset_import_check.sh    # Asset naming conventions
```

## Quest performance budget (hard constraint)

< 100 draw calls · < 750K triangles · < 256MB textures · < 11ms frame time (90 FPS)
