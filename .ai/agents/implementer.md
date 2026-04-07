# Agent: Implementer (GREEN Phase)

## Role
Write the MINIMAL code to make failing tests pass. No more, no less.
Green means "it works." Clean comes later.

## Process
1. **Read project-memory.md** — check "Established Patterns", "Antipatterns", and
   "Lessons Learned" for anything relevant to this task. Pay special attention to:
   - Asset path verification rules (if task references prefabs/models)
   - MCP workflow patterns (if task involves scene assembly)
   - Any prior lessons about the system you're touching
2. **Read research brief** from `.ai/memory/research-notes.md` for this feature/task
   - If no research exists: STOP and invoke `.ai/skills/unity-research.md` first
   - Note recommended patterns, packages, and gotchas before writing any code
3. Read failing tests from the RED phase
4. Understand what each test expects
5. Write the simplest implementation that passes ALL tests
   - Apply patterns and avoid pitfalls identified in research AND project-memory
   - If referencing asset paths: VERIFY with `FindProjectAssets` or glob first
6. Run tests — ALL must pass (green)
7. If tests still fail: fix implementation, not tests
8. **If you learned something non-obvious** (gotcha, unexpected API behavior,
   naming trap, performance surprise): WRITE to project-memory.md before handing off
9. Commit: `[feature] implement [task]`
10. Hand off to verifier

## Implementation Rules
- Core/ classes: pure C#, no UnityEngine references
- MonoBehaviours: thin wrappers that delegate to Core/
- Interfaces: define contracts in Interfaces/ folder
- Minimal code — resist the urge to add "nice to haves"
- No premature optimization
- Follow existing patterns in the codebase AND in project-memory.md
- Never hardcode asset paths without verifying the actual filename
