# Spec Index

This is the central tracker for all feature specs. Before building anything, find it here first.

---

## Spec Map

| ID | Feature | Layer | Status | Assignee | Depends On |
|----|---------|-------|--------|----------|------------|
| L1-001 | [Farm Layout (Greybox)](Assets/Specs/Features/L1-001-farm-layout-greybox.md) | L1 | Done | Youssef | -- |
| L1-002 | [Sky & Lighting](Assets/Specs/Features/L1-002-sky-and-lighting.md) | L1 | Done | Youssef | L1-001 |
| L2-001 | [Soil System](Assets/Specs/Features/L2-001-soil-system.md) | L2 | Open | -- | -- |
| L2-002 | [Crop Growth System](Assets/Specs/Features/L2-002-crop-growth-system.md) | L2 | Open | -- | Crop Growth Calc |
| L2-003 | [Planting System](Assets/Specs/Features/L2-003-planting-system.md) | L2 | Open | -- | L2-001, L2-006 |
| L2-004 | [Watering System](Assets/Specs/Features/L2-004-watering-system.md) | L2 | Open | -- | L2-001 |
| L2-005 | [Harvest System](Assets/Specs/Features/L2-005-harvest-system.md) | L2 | Open | -- | L2-002, L2-006 |
| L2-006 | [Inventory System](Assets/Specs/Features/L2-006-inventory-system.md) | L2 | Open | -- | -- |
| -- | [Crop Growth Calculator](Assets/Specs/Features/crop-growth-calculator.md) | Core | Done | Youssef | -- |
| -- | [Farm Scene Demo](Assets/Specs/Features/farm-scene-demo.md) | Demo | Done | Youssef | Crop Growth Calc |

**Status values:** `Open` | `Claimed` | `In Progress` | `In Review` | `Done`

---

## Layers Explained

Specs are organized in layers. Lower layers are built first.

| Layer | What | Examples |
|-------|------|---------|
| **L1** | Environment & scene setup | Ground plane, skybox, lighting |
| **L2** | Gameplay systems (Core/ C#) | Soil, crops, planting, watering, harvest, inventory |
| **L3** | Polish & effects | Day/night cycle, weather, VFX, audio (not yet specced) |
| **Core** | Foundation logic | Calculators, data types, utilities |
| **Demo** | Playable demos | Quick proof-of-concept scenes |

**Build order:** Core and L1 have no dependencies. L2 specs may depend on each other — check the "Depends On" column. A good starting order for L2:

```
L2-006 Inventory (zero deps)
  --> L2-001 Soil System (zero deps)
    --> L2-004 Watering (needs Soil)
    --> L2-003 Planting (needs Soil + Inventory)
      --> L2-002 Crop Growth (needs Crop Growth Calc)
        --> L2-005 Harvest (needs Crop Growth + Inventory)
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
