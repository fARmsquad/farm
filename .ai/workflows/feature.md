# Workflow: Feature Delivery

## Autonomy Level: FULL
Agents hand off to each other without human gates. The developer is notified
at playtest checkpoint only. The developer CAN intervene at any point via
the inbox or direct interrupt, but the pipeline does not WAIT for them.

## Prerequisites (auto-checked, not human-verified)
- [ ] On main branch
- [ ] `git pull origin main` performed (up to date)
- [ ] Working tree clean
- [ ] No conflicting flights on flight-board.json

## Phase 0: Git Sync (MANDATORY, before anything else)
→ `git pull origin main` — always start from latest
→ If uncommitted changes: commit or stash before proceeding
→ GATE: working tree clean and up to date before entering Phase 1

## Phase 1: Intake
→ **READ .ai/memory/project-memory.md** — load patterns, antipatterns, lessons learned
→ Check .ai/inbox/ for any pending items related to this feature
→ Skill: .ai/skills/story-lookup.md
→ If existing story: load context, resume from last phase
→ If new: create story card from template
→ Auto-proceed to Phase 2

## Phase 2: Spec Package (AUTONOMOUS)
→ **Step 2a: Web Research (MANDATORY)**
→ Skill: .ai/skills/unity-research.md
→ Search how others implement this feature type in Unity
→ Extract patterns, code snippets, gotchas from 3+ sources
→ Save research brief to .ai/memory/research-notes.md
→ This step CANNOT be skipped — no research = no spec

→ **Step 2b: Spec Generation**
→ Skill: .ai/skills/spec-driven-delivery.md
→ **READ .ai/memory/project-memory.md** — check for relevant ADRs, patterns, and antipatterns
→ Cross-reference: does the proposed approach conflict with any established pattern or ADR?
→ Generate: Feature Spec → Technical Plan (with Research Reference) → Task Breakdown
→ Technical Plan MUST reference research findings and adopted patterns
→ Technical Plan MUST reference relevant entries from project-memory.md if they exist
→ Save to Assets/Specs/Features/[feature].md
→ Commit: `[spec] add specification for [feature]`
→ `git push origin main`
→ Auto-proceed to Phase 3
→ NOTE: if the spec involves a NEW system boundary, architectural uncertainty,
  or a decision that's hard to reverse, DROP a note in .ai/inbox/needs-eyes/
  and continue. The developer can review async and steer later.

## Phase 3: TDD Cycle (AUTONOMOUS, agent-to-agent handoff)
→ Skill: .ai/skills/tdd-cycle.md
→ For each task in the breakdown:

### 3a. RED → GREEN → VERIFY → REFACTOR (pure C# logic)

    ┌─────────────────────────────────────────────────┐
    │  MEMORY CHECK (before each task)                │
    │  → READ .ai/memory/project-memory.md            │
    │    Check: patterns, antipatterns, lessons that   │
    │    apply to THIS task (asset paths, naming,      │
    │    API gotchas, performance traps)               │
    │  ──────────────── then ────────────────────────│
    │                                                 │
    │  RESEARCH CHECK (before each task)              │
    │  If task involves a Unity API, pattern, or      │
    │  system the agent hasn't used before:           │
    │  → Skill: .ai/skills/unity-research.md          │
    │  → Quick search (1-2 queries, task-scoped)      │
    │  → Append findings to research-notes.md         │
    │  → Implementer MUST read before writing code    │
    │  ──────────────── then proceed ─────────────── │
    │                                                 │
    │  tdd-agent (RED)                                │
    │  writes failing tests, confirms they fail       │
    │  commits: [tests] add failing tests for [task]  │
    │  ──────────────── hands off to ──────────────── │
    │                                                 │
    │  implementer (GREEN)                            │
    │  reads research brief + project-memory.md       │
    │  writes minimal code, confirms tests pass       │
    │  commits: [feature] implement [task]            │
    │  ──────────────── hands off to ──────────────── │
    │                                                 │
    │  verifier (VERIFY)                              │
    │  checks for overfitting, adds boundary tests    │
    │  if overfitting: loops back to implementer      │
    │  if clean: commits additional tests             │
    │  ──────────────── hands off to ──────────────── │
    │                                                 │
    │  refactorer (REFACTOR)                          │
    │  cleans up, runs tests after each change        │
    │  commits: [refactor] clean up [task]            │
    │  ──────────────── LEARN ──────────────────────│
    │                                                 │
    │  MEMORY WRITE (after each task completes)       │
    │  If anything non-obvious was learned:           │
    │  → WRITE to .ai/memory/project-memory.md        │
    │    - New pattern? → "Established Patterns"      │
    │    - Gotcha hit? → "Antipatterns"               │
    │    - Debugging insight? → "Lessons Learned"     │
    │    - Asset path trap? → "Lessons Learned"       │
    │    - Perf discovery? → "Performance Budgets"    │
    │  Skip if task was routine with no new lessons.  │
    └─────────────────────────────────────────────────┘

    Tests run via MCP run_tests if editor open, CLI fallback if closed.

### 3b. ASSEMBLE (MCP-powered, after REFACTOR)
If this task produced a MonoBehaviour or anything that lives in the scene:
→ Skill: .ai/skills/scene-assembly.md
→ **READ project-memory.md "Asset Paths" antipattern** — verify all prefab/asset paths before use

1. **Check editor state** — read editor_state, verify not compiling/playing
2. **refresh_unity** — ensure new scripts are compiled
3. **read_console** — check for compilation errors before proceeding
4. **Create GameObjects** — manage_gameobject to build the hierarchy
5. **Add components** — manage_components to wire up MonoBehaviours
6. **Configure components** — set SerializeField values, references
7. **Set physics** — manage_physics for collision layers, rigidbodies
8. **Create materials** — manage_material if visual setup needed
9. **Create prefab** — manage_prefabs to save as reusable asset
10. **Verify** — read_console for errors, optionally screenshot
11. **Commit** — `[scene] assemble [task] in editor`

Use batch_execute to chain steps 4-9 when possible.
If editor is closed, queue operations in .ai/coordination/mcp-queue.json.

### 3c. XR WIRING (MCP, Quest-specific, after ASSEMBLE)
If this task involves VR interaction:

1. **Add XR components** — manage_components to add:
   - XRGrabInteractable (for grabbable objects)
   - XRSocketInteractor (for snap points)
   - XRDirectInteractor (on hand controllers)
   - Relevant colliders (trigger for interaction zones)
2. **Configure interaction** — set grab type, throw behavior, attach points
3. **Set physics layers** — manage_physics to configure:
   - "Interactable" layer for grabbable objects
   - "Hand" layer for controllers
   - Layer collision matrix to enable relevant interactions
4. **Test interaction setup** — find_gameobjects to verify components are wired
5. **Commit** — `[xr] wire interaction for [task]`

### 3d. VISUAL POLISH (MCP, optional, after XR WIRING)
If this task has visual elements:

1. **Materials** — manage_material to create/assign
2. **Textures** — manage_texture to set import settings (ASTC, max size)
3. **VFX** — manage_vfx for particle effects (planting burst, water, harvest)
4. **Animation** — manage_animation for growth stages, tool animations
5. **Shaders** — manage_shader if custom shader needed
6. **Commit** — `[visual] add visual polish for [task]`

### 3e. ASSET INGEST (if new models needed for this feature)
If the feature requires new 3D models:
→ Developer provides .glb files (drops into project or gives path)
→ Skill: .ai/skills/glb-ingest.md
→ Full pipeline: detect → optimize → material → prefab → place
→ If model needs a controller MonoBehaviour, loop back to TDD for it
→ Commit: `[asset] ingest [model name]`

    → Repeat for next task in breakdown
    → Between tasks:
      → Check .ai/inbox/ for developer steering
      → **WRITE to project-memory.md** if this task taught something new
      → `git push` (keep remote up to date after each task)


## Phase 4: Finalization (AUTONOMOUS)
→ Auto-triggered after last task completes. No "ship it" prompt needed.
→ Delegate to .ai/agents/finalizer.md

**Step 4a: Preflight**
→ Mark spec acceptance criteria as [x]
→ Run full test suite: `.ai/scripts/run-tests.sh all`
→ Run preflight — if fails, fix and retry (up to 3 attempts)
→ **WRITE to project-memory.md** — add any new patterns/antipatterns discovered during this feature

**Step 4b: Push**
→ `git push origin main`
→ GATE: push must succeed. If rejected (someone else pushed), pull and re-run tests.

**Step 4c: Post-Push Verification**
→ `.ai/scripts/run-tests.sh all` (verify main is still green after push)
→ If red on main: **P0** — investigate immediately, do NOT proceed
→ If green: update SINGLE_SOURCE_OF_TRUTH.md
→ Auto-proceed to Phase 5

## Phase 5: Playtest Checkpoint (HUMAN GATE — the only one)
→ Generate VR playtest guide: .ai/workflows/playtest-checkpoint.md
→ This is where the developer puts on the headset
→ Guide includes:
  - Exact scene to open
  - Build instructions (if Quest build needed vs Link)
  - Step-by-step actions to perform in VR
  - What each interaction should feel like (haptics, audio, visual)
  - Edge cases to try (what happens if you grab two things, etc.)
  - Performance: does it feel smooth? Any frame drops?
  - Comfort: any discomfort, motion issues?
→ STOP HERE. Wait for developer feedback.

## Phase 6: Feedback Loop
→ Developer responds with one of:
  - "Good" / "Approved" → close story, update SSOT, move to next
  - Suggestions → route to .ai/inbox/feedback/[feature].md
    → Triage: is this a tweak (same story) or a new story?
    → If tweak: re-enter Phase 3 with the feedback as a new task
    → If new story: create story card, add to backlog
  - "This doesn't feel right" → deeper conversation
    → Ask: what specifically feels wrong? (interaction, timing, scale, etc.)
    → Generate targeted fix plan
    → Re-enter Phase 3 with fix tasks
  - "What if we also..." → new feature suggestion
    → Create story card in .ai/inbox/ideas/
    → Acknowledge and continue with current story closure

## Memory Touchpoints (Summary)
Agents interact with `.ai/memory/project-memory.md` at these moments:

| When | Action | What |
|------|--------|------|
| Phase 1 (Intake) | **READ** | Load all patterns, antipatterns, lessons before starting |
| Phase 2 (Spec) | **READ** | Check ADRs and patterns that affect the design |
| Phase 3 (each task start) | **READ** | Check relevant antipatterns before implementation |
| Phase 3 (each task end) | **WRITE** | Record new pattern, gotcha, or lesson if any |
| Phase 3b (Assembly) | **READ** | Verify asset paths, check MCP antipatterns |
| Phase 4 (Finalization) | **WRITE** | Consolidate all lessons from this feature |
| Phase 6 (Feedback) | **WRITE** | Record what the playtest revealed (feels wrong = lesson) |

**Rule: if you learned something non-obvious, write it down. If it's routine, skip.**

## Inbox Check Points
Agents check .ai/inbox/ at these moments:
- Start of every new story (Phase 1)
- Between tasks in Phase 3
- After finalization (Phase 4)
- After receiving playtest feedback (Phase 6)
