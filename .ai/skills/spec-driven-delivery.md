# Skill: Spec-Driven Delivery

## Purpose
Generate a complete spec package from a story card.

## Outputs
1. **Feature Spec** — what the feature does (user-facing)
2. **Technical Plan** — how to build it (architecture)
3. **Task Breakdown** — granular implementation tasks

## Process
1. Read story card
2. Research existing architecture (SSOT, project-memory)
3. **Web Research (MANDATORY)** → Skill: `.ai/skills/unity-research.md`
   - Search how others implement this type of feature in Unity
   - Extract patterns, code references, and gotchas
   - Save research brief to `.ai/memory/research-notes.md`
   - This step CANNOT be skipped — even trivial features get one search
4. Generate Feature Spec (from template)
5. Generate Technical Plan (from template) — MUST include Research Reference section
6. Break down into tasks (from template)
7. Save all to Assets/Specs/Features/[feature-name].md
8. Commit: `[spec] add specification for [feature]`

## Spec Quality Gates
- Every acceptance criterion is testable
- No implementation details in the feature spec
- Technical plan references existing systems correctly
- Task breakdown is ordered by dependency
- Quest performance impact assessed
- **Web research performed and saved to research-notes.md**
- **Technical plan includes Research Reference section with sources**
