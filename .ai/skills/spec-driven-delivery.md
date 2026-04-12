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
3. **READ .ai/memory/completion-learnings.md** — scan for similar cases where prior work was declared done too early or with the wrong verification boundary
4. Research existing architecture (SSOT, project-memory)
5. **Web Research (MANDATORY)** → Skill: `.ai/skills/unity-research.md`
   - Search how others implement this type of feature in Unity
   - Extract patterns, code references, and gotchas
   - Save research brief to `.ai/memory/research-notes.md`
   - This step CANNOT be skipped — even trivial features get one search
6. Generate Feature Spec (from template)
7. Generate Technical Plan (from template) — MUST include:
   - Research Reference section with sources
   - **Memory Reference** section citing relevant project-memory.md entries
   - **Completion Learning Reference** section citing relevant completion-learnings.md entries when applicable
   - Explicit notes on which antipatterns to watch for during implementation
8. Break down into tasks (from template)
9. Save all to Assets/Specs/Features/[feature-name].md
10. Commit: `[spec] add specification for [feature]`
11. **If this spec introduces a new ADR**: WRITE to project-memory.md "Architecture Decisions"

## Spec Quality Gates
- Every acceptance criterion is testable
- No implementation details in the feature spec
- Technical plan references existing systems correctly
- Task breakdown is ordered by dependency
- Quest performance impact assessed
- **Web research performed and saved to research-notes.md**
- **Technical plan includes Research Reference section with sources**
- **Technical plan includes Memory Reference section citing relevant project-memory.md entries**
- **Technical plan includes Completion Learning Reference section when relevant**
- **project-memory.md updated if new ADR introduced**
