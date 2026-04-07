# Skill: Spec-Driven Delivery

## Purpose
Generate a complete spec package from a story card.

## Outputs
1. **Feature Spec** — what the feature does (user-facing)
2. **Technical Plan** — how to build it (architecture)
3. **Task Breakdown** — granular implementation tasks

## Process
1. Read story card
2. **READ .ai/memory/project-memory.md** — check ALL sections:
   - ADRs: does this feature touch an existing architectural boundary?
   - Patterns: what established patterns should the spec adopt?
   - Antipatterns: what traps must the spec explicitly avoid?
   - Lessons Learned: has a similar feature been attempted before?
   - Tech Debt: does this feature overlap with known debt?
3. Research existing architecture (SSOT, project-memory)
4. **Web Research (MANDATORY)** → Skill: `.ai/skills/unity-research.md`
   - Search how others implement this type of feature in Unity
   - Extract patterns, code references, and gotchas
   - Save research brief to `.ai/memory/research-notes.md`
   - This step CANNOT be skipped — even trivial features get one search
5. Generate Feature Spec (from template)
6. Generate Technical Plan (from template) — MUST include:
   - Research Reference section with sources
   - **Memory Reference** section citing relevant project-memory.md entries
   - Explicit notes on which antipatterns to watch for during implementation
7. Break down into tasks (from template)
8. Save all to Assets/Specs/Features/[feature-name].md
9. Commit: `[spec] add specification for [feature]`
10. **If this spec introduces a new ADR**: WRITE to project-memory.md "Architecture Decisions"

## Spec Quality Gates
- Every acceptance criterion is testable
- No implementation details in the feature spec
- Technical plan references existing systems correctly
- Task breakdown is ordered by dependency
- Quest performance impact assessed
- **Web research performed and saved to research-notes.md**
- **Technical plan includes Research Reference section with sources**
- **Technical plan includes Memory Reference section citing relevant project-memory.md entries**
- **project-memory.md updated if new ADR introduced**
