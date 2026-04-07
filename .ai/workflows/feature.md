# Workflow: Feature Delivery

## Autonomy Level: FULL
Agents hand off to each other without human gates. The developer is notified
at playtest checkpoint only. The developer CAN intervene at any point via
the inbox or direct interrupt, but the pipeline does not WAIT for them.

## Prerequisites (auto-checked, not human-verified)
- [ ] On a feature branch (not main) — create if needed
- [ ] No conflicting flights on flight-board.json

## Phase 1: Intake
→ Check .ai/inbox/ for any pending items related to this feature
→ Skill: .ai/skills/story-lookup.md
→ If existing story: load context, resume from last phase
→ If new: create story card from template
→ Auto-proceed to Phase 2

## Phase 2: Spec Package (AUTONOMOUS)
→ Skill: .ai/skills/spec-driven-delivery.md
→ Generate: Feature Spec → Technical Plan → Task Breakdown
→ Save to Assets/Specs/Features/[feature].md
→ Commit: `[spec] add specification for [feature]`
→ Auto-proceed to Phase 3
→ NOTE: if the spec involves a NEW system boundary, architectural uncertainty,
  or a decision that's hard to reverse, DROP a note in .ai/inbox/needs-eyes/
  and continue. The developer can review async and steer later.

## Phase 3: TDD Cycle (AUTONOMOUS, agent-to-agent handoff)
→ Skill: .ai/skills/tdd-cycle.md
→ For each task in the breakdown:

### 3a. RED → GREEN → VERIFY → REFACTOR (pure C# logic)

    ┌─────────────────────────────────────────────────┐
    │  tdd-agent (RED)                                │
    │  writes failing tests, confirms they fail       │
    │  commits: [tests] add failing tests for [task]  │
    │  ──────────────── hands off to ──────────────── │
    │                                                 │
    │  implementer (GREEN)                            │
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
    └─────────────────────────────────────────────────┘

    Tests run via MCP run_tests if editor open, CLI fallback if closed.

### 3b. ASSEMBLE (MCP-powered, after REFACTOR)
If this task produced a MonoBehaviour or anything that lives in the scene:
→ Skill: .ai/skills/scene-assembly.md

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
    → Between tasks: check .ai/inbox/ for developer steering

## Phase 4: Finalization (AUTONOMOUS)
→ Auto-triggered after last task completes. No "ship it" prompt needed.
→ Delegate to .ai/agents/finalizer.md
→ Mark spec acceptance criteria as [x]
→ Run preflight (14 gates: 9 CLI + 5 MCP) — if fails, fix and retry (up to 3 attempts)
→ Push branch, create PR with full report
→ Squash-merge to main
→ Run post-merge verification
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

## Inbox Check Points
Agents check .ai/inbox/ at these moments:
- Start of every new story (Phase 1)
- Between tasks in Phase 3
- After finalization (Phase 4)
- After receiving playtest feedback (Phase 6)
