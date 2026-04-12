# Spec Index

This is the central tracker for all feature specs. Before building anything, find it here first.

---

## Spec Map

| ID | Feature | Layer | Status | Assignee | Depends On |
|----|---------|-------|--------|----------|------------|
| L1-001 | [Farm Layout (Greybox)](Assets/Specs/Features/L1-001-farm-layout-greybox.md) | L1 | Done | Youssef | -- |
| L1-002 | [Sky & Lighting](Assets/Specs/Features/L1-002-sky-and-lighting.md) | L1 | Done | Youssef | L1-001 |
| -- | [Crop Growth Calculator](Assets/Specs/Features/crop-growth-calculator.md) | Core | Done | Youssef | -- |
| -- | [Farm Scene Demo](Assets/Specs/Features/farm-scene-demo.md) | Demo | Done | Youssef | Crop Growth Calc |
| L2-008 | [Starter Tool Discovery & Ability Unlocks](Assets/Specs/Features/L2-008-starter-tool-discovery-and-ability-unlocks.md) | L2 | Open | -- | L2-001 thru L2-006 |
| L2-007 | [Hunting Chore (Catch Animals)](Assets/Specs/Features/L2-007-hunting-chore.md) | L2 | Done | AI | L1-001 |
| TUT-001 | [Linear Tutorial Sequence](Assets/Specs/Features/TUT-001-linear-tutorial-sequence.md) | TUT | Open | -- | INT-008, L2-007, L2-000 |
| INT-001 | [Screen Effects](Assets/Specs/Features/INT-001-screen-effects.md) | INT | Done | Youssef | -- |
| INT-002 | [Simple Audio Manager](Assets/Specs/Features/INT-002-simple-audio-manager.md) | INT | Done | Youssef | -- |
| INT-003 | [Dialogue System](Assets/Specs/Features/INT-003-dialogue-system.md) | INT | Done | Youssef | -- |
| INT-004 | [Cinematic Camera](Assets/Specs/Features/INT-004-cinematic-camera.md) | INT | Done | Youssef | -- |
| INT-005 | [Cinematic Sequencer](Assets/Specs/Features/INT-005-cinematic-sequencer.md) | INT | Done | AI | INT-001, INT-002, INT-003, INT-004 |
| INT-006 | [NPC Controller](Assets/Specs/Features/INT-006-npc-controller.md) | INT | Done | Youssef | INT-003 |
| INT-007 | [Mission Manager](Assets/Specs/Features/INT-007-mission-manager.md) | INT | Done | Youssef | INT-001, INT-002 |
| INT-009 | [Lighting Presets & Transitions](Assets/Specs/Features/INT-009-lighting-presets.md) | INT | Done | Youssef | INT-005 |
| INT-010 | [Environment Particle Systems](Assets/Specs/Features/INT-010-particle-effects.md) | INT | Done | Youssef | -- |
| INT-011 | [Comic Text & Speech Bubbles](Assets/Specs/Features/INT-011-comic-text-overlays.md) | INT | Done | Youssef | INT-003 |
| INT-012 | [Cutscene Skip & Auto-Save](Assets/Specs/Features/INT-012-skip-and-autosave.md) | INT | Done | Youssef | INT-005, INT-001 |
| INT-013 | [Intro Props & Ambient NPCs](Assets/Specs/Features/INT-013-intro-props.md) | INT | Done | Youssef | INT-005 |
| INT-008 | [Intro Scene Assembly](Assets/Specs/Features/INT-008-intro-scene-assembly.md) | INT | Open | -- | INT-005 thru INT-013 |
| INT-014 | [Intro Art & Audio Polish](Assets/Specs/Features/INT-014-intro-art-audio.md) | POL | Open | -- | INT-008 |
| POL-001 | [Polish Backlog](Assets/Specs/Features/POL-001-polish-backlog.md) | POL | Open | -- | All gameplay + intro Done |

**Status values:** `Open` | `Claimed` | `In Progress` | `In Review` | `Done`

---

## Layers Explained

Specs are organized in layers. Lower layers are built first.

| Layer | What | Examples |
|-------|------|---------|
| **L1** | Environment & scene setup | Ground plane, skybox, lighting |
| **L2** | Gameplay systems (Core/ C#) | Soil, crops, planting, watering, harvest, inventory |
| **TUT** | Tutorial scene chain & onboarding flow | Intro -> minigame -> cutscene -> farm tutorial |
| **L3** | Polish & effects | Day/night cycle, weather, VFX, audio (not yet specced) |
| **INT** | Intro/Cinematic systems | Screen effects, dialogue, cutscene camera, sequencer |
| **Core** | Foundation logic | Calculators, data types, utilities |
| **Demo** | Playable demos | Quick proof-of-concept scenes |
| **POL** | Polish pass | Replace placeholders, add real assets, tune timing/audio |

**Build order:** Core and L1 have no dependencies. L2 specs may depend on each other — check the "Depends On" column. INT (intro) specs have their own dependency tree.

```
INT Layer Build Order (Intro Cinematic):

  Phase 1 — No dependencies, build in parallel:  ✅ ALL DONE
    INT-001 Screen Effects
    INT-002 Audio Manager
    INT-003 Dialogue System
    INT-004 Cinematic Camera

  Phase 2 — Needs Phase 1:  ✅ ALL DONE
    INT-006 NPC Controller        (needs INT-003)
    INT-007 Mission Manager       (needs INT-001, INT-002)

  Phase 3 — Needs Phase 1 + 2, build in parallel:
    INT-005 Cinematic Sequencer   (needs INT-001 thru INT-004, INT-006, INT-007)
    INT-010 Particle Effects      (no code deps)

  Phase 4 — Needs Phase 3, build in parallel:
    INT-009 Lighting Presets      (needs INT-005 for sequencer integration)
    INT-011 Comic Text Overlays   (needs INT-003)
    INT-012 Skip & Auto-Save      (needs INT-005, INT-001)
    INT-013 Intro Props           (needs INT-005)

  Phase 5 — Assembly (placeholders + existing Synty assets):
    INT-008 Intro Scene Assembly  (needs INT-005 thru INT-013, uses placeholders)

  Phase 6 — Polish (swap placeholders for real assets):
    INT-014 Intro Art & Audio Polish  (needs INT-008 Done first)

L2-007 Hunting Chore (needs Farm Layout) ✅ DONE
```

---

## How to Read a Spec

Every spec file follows the same structure:

```
# Feature Spec: <Name> -- <ID>

## Summary         <-- what this feature does in 2 sentences
## User Story      <-- who needs it and why
## Acceptance Criteria  <-- checkboxes: the feature is done when all are checked
## Edge Cases      <-- gotchas and boundary conditions
## Performance     <-- Quest budget considerations
## Dependencies    <-- what must be built first
## Out of Scope    <-- what this spec intentionally does NOT cover

---
## Technical Plan
### Architecture   <-- which folders/classes are involved
### Build Approach <-- how to implement it
### Testing Strategy  <-- what tests to write

---
## Task Breakdown  <-- numbered tasks, each with type/action/depends/acceptance
```

**The most important sections are Acceptance Criteria and Task Breakdown.** The acceptance criteria tell you when you're done. The task breakdown tells you what to do.

---

## How to Claim a Spec

1. Pick an `Open` spec from the table above (check that its dependencies are `Done`)
2. Update this file: change Status to `Claimed` and put your name in Assignee
3. Copy (or symlink) the spec file into your assignment folder:
   ```
   Assets/Specs/Assignments/<YourName>/
   ```
4. Commit the change: `git commit -m "[spec] claim <spec-id> for <YourName>"`
5. Push so the team can see it

---

## How to Build a Spec

### Step 1: Read the spec
Read the full spec file. Pay attention to:
- **Acceptance Criteria** — this is your definition of done
- **Task Breakdown** — this is your implementation order
- **Dependencies** — make sure these are built and merged first

### Step 2: Create a branch
```bash
git checkout main
git pull
git checkout -b feature/<spec-id>-short-name
# Example: git checkout -b feature/L2-001-soil-system
```

### Step 3: Update status
Edit SPECS.md: change your spec's status to `In Progress`.

### Step 4: Implement
Work through the Task Breakdown in order. Each task has:
- **Type** — what kind of work (scene primitive, C# class, configuration)
- **Action** — what to do
- **Depends on** — which tasks must be done first
- **Acceptance** — how to verify this task is done

General rules:
- Pure logic goes in `Assets/_Project/Scripts/Core/`
- Unity components go in `Assets/_Project/Scripts/MonoBehaviours/`
- Editor tools go in `Assets/_Project/Editor/`
- Write tests in `Assets/Tests/EditMode/` (see CONTRIBUTING.md)

### Step 5: Test
- Run EditMode tests: Unity > Window > General > Test Runner > EditMode > Run All
- Check all Acceptance Criteria checkboxes in the spec
- Visual checks: open the scene, hit Play, verify behavior

### Step 6: Push and PR
```bash
git push -u origin feature/<spec-id>-short-name
gh pr create --title "[feature] implement <spec-id> <name>" --body "Implements <spec link>"
```

### Step 7: Review and merge
- Update SPECS.md: change status to `In Review`
- Get a teammate to review the PR
- After approval, merge to `main`
- Update SPECS.md: change status to `Done`

---

## How to Write a New Spec

1. Use the naming convention: `L<layer>-<number>-<short-name>.md`
   - Example: `L3-001-day-night-cycle.md`
2. Follow the template structure shown in "How to Read a Spec" above
3. A full template is available at `.ai/templates/feature-spec.md` (if using the AI workflow)
4. Save to `Assets/Specs/Features/`
5. Add a row to the Spec Map table in this file
6. Commit: `git commit -m "[spec] add <spec-id> <name>"`
