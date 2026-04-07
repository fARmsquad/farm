# Technical Plan: [Feature Name]

## Research Reference
> **Mandatory**: This section is populated by `.ai/skills/unity-research.md`
> before the plan is written. Do NOT write a technical plan without research.

- **Key pattern adopted**: [pattern name from research brief]
- **Why this approach**: [1-2 sentences referencing community consensus]
- **Recommended packages**: [packages from research brief, or "none — custom implementation"]
- **Sources consulted**: [links to top 3 sources]
- **Deviations from community pattern**: [what we're doing differently and why]

## Memory Reference
> **Mandatory**: This section is populated from `.ai/memory/project-memory.md`.
> Cross-reference the knowledge base before writing the plan.

- **Relevant ADRs**: [list any Architecture Decisions that constrain this plan]
- **Patterns to follow**: [list Established Patterns from project-memory.md that apply]
- **Antipatterns to avoid**: [list specific Antipatterns that could bite this feature]
- **Lessons from past work**: [cite any Lessons Learned entries that are relevant]
- **Asset path note**: If this plan references third-party assets (Synty, etc.),
  all paths MUST be verified at implementation time via `FindProjectAssets` — do NOT
  hardcode assumed paths in the plan. Use descriptive names and mark paths as `[VERIFY]`.

## Architecture
- **Core/ classes**: [what pure C# classes are needed]
- **Interfaces/**: [what contracts are needed]
- **MonoBehaviours/**: [what Unity wrappers are needed]
- **ScriptableObjects/**: [what data containers are needed]

## Data Flow
```
[Input] → [Processing] → [Output]
```

## Dependencies
- Depends on: [existing systems]
- Depended on by: [future systems, if known]

## Testing Strategy
- **EditMode**: [what Core/ logic to test]
- **PlayMode**: [what integration behavior to test]

## Performance Considerations
- [Allocation patterns]
- [Update frequency]
- [Quest-specific optimizations]
- **From research**: [any Quest/mobile gotchas found during research]

## Risks
- [Technical risk 1]
- [Mitigation strategy]
- **From research**: [pitfalls flagged by community sources]
- **From memory**: [antipatterns or lessons learned that apply to this feature]
