# AGENTS.md — FarmSim VR Agent Constitution

## Read Order (MANDATORY)
1. This file (AGENTS.md)
2. .ai/SINGLE_SOURCE_OF_TRUTH.md
3. Your orchestrator:
   - Claude Code → .ai/claude.md
   - Codex CLI → .ai/codex.md
   - Cursor/Windsurf → .ai/cursor.md
4. The relevant workflow for your task type (see Task Routing below)

## Project Identity
- **Product**: Quest VR farming simulation game
- **Engine**: Unity 6 LTS (6.3+), Universal Render Pipeline
- **Target**: Meta Quest 2 + Quest 3 (VR first, MR passthrough later)
- **Team**: Solo developer (Youssef), AI agents as collaborators
- **Architecture**: Pure C# Core (no Unity deps) + thin MonoBehaviour wrappers

## The Iron Laws
1. NEVER write implementation without a failing test first
2. Core/ has ZERO UnityEngine references — enforced by noEngineReferences: true
3. Every feature starts with a spec. No spec, no code.
4. Preflight must pass before any push. No exceptions.
5. Main branch is ALWAYS green. A red main is a P0.
6. If you change .ai/, AGENTS.md, or any harness file, run the AI wiring audit.
7. Every story ends with a playtest guide and handoff checklist.
8. Quest performance budget is a hard constraint, not a suggestion.

## Autonomy Model
Agents are FULLY AUTONOMOUS through the pipeline. No human gates between
TDD phases. Spec → RED → GREEN → VERIFY → REFACTOR → push flows
without stopping for permission.

Human touchpoints are INVITATIONS, not gates:
- **Playtest checkpoint**: after finalization, generate a VR playtest guide.
  The developer puts on the headset and tests manually. This is the ONLY
  hard human gate — code is done, but the story isn't closed until the
  developer confirms it feels right in VR.
- **Inbox**: the developer can drop suggestions, feature ideas, bug reports,
  or "this feels wrong" observations into .ai/inbox/ at ANY time.
  Agents check the inbox at the start of every new story and between phases.
- **Steering**: the developer can interrupt any phase with new direction.
  Agents treat interrupts as priority input, not errors.

The pipeline is a river, not a checklist. It flows continuously.
The developer stands on the bank and can redirect the current at any point.

## Task Routing
Route your current task to the appropriate workflow:

| Task Type | Workflow | Entry Gate |
|-----------|----------|------------|
| New feature | .ai/workflows/feature.md | Story lookup → spec → TDD (autonomous) |
| Bug fix | .ai/workflows/bugfix.md | Reproduce → failing test → fix |
| Performance | .ai/workflows/performance.md | Profile → benchmark → optimize |
| Security | .ai/workflows/security.md | Audit → fix → verify → document |
| Deployment | .ai/workflows/deployment.md | Build → test → deploy |
| Add 3D model (.glb) | .ai/skills/glb-ingest.md | Detect → optimize → prefab → place |
| AI/harness change | .ai/workflows/ai-architecture-change.md | Audit required |
| Playtest results | .ai/workflows/playtest-checkpoint.md | Generate VR test guide |
| Story completion | .ai/workflows/story-handoff.md | Handoff + feedback routing |
| Developer suggestion | Check .ai/inbox/ | Triage → route to appropriate workflow |
| Nothing to do | Check inbox → backlog → refactor → document | Never idle |

## Quality Contract
- Max file size: 500 lines (split if larger)
- Max function size: 40 lines (extract if longer)
- Max class responsibilities: 1 (SRP — Single Responsibility)
- Max nesting depth: 3 levels (use guard clauses)
- Test coverage: every public method in Core/ must have tests
- Type hints: all public APIs fully typed, no `object` or `dynamic`
- No `Debug.Log` in committed code (use conditional compilation)
- No `TODO`/`FIXME` in merge-ready code (resolve or create issue)

## Validation Commands
```bash
./run-tests.sh editmode          # Fast: Core/ unit tests
./run-tests.sh playmode          # Slow: XR integration tests
./run-tests.sh all               # Full suite
./preflight.sh                   # Pre-push validation
.ai/scripts/check_ai_wiring.sh  # Harness integrity audit
.ai/scripts/asset_import_check.sh # 3D asset convention check
```

## Agent Identification
When committing, identify yourself:
- Claude Code: `[cc]` prefix in commit body
- Codex: `[codex]` prefix in commit body
- Human: no prefix needed
