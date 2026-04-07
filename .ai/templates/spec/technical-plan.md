# Technical Plan: [Feature Name]

## Research Reference
> **Mandatory**: This section is populated by `.ai/skills/unity-research.md`
> before the plan is written. Do NOT write a technical plan without research.

- **Key pattern adopted**: [pattern name from research brief]
- **Why this approach**: [1-2 sentences referencing community consensus]
- **Sources consulted**: [links to top 3 sources]
- **Deviations from community pattern**: [what we're doing differently and why]

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
