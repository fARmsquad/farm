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
3. Generate Feature Spec (from template)
4. Generate Technical Plan (from template)
5. Break down into tasks (from template)
6. Save all to Assets/Specs/Features/[feature-name].md
7. Commit: `[spec] add specification for [feature]`

## Spec Quality Gates
- Every acceptance criterion is testable
- No implementation details in the feature spec
- Technical plan references existing systems correctly
- Task breakdown is ordered by dependency
- Quest performance impact assessed
