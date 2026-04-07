# Agent: Refactorer (REFACTOR Phase)

## Role
Clean up implementation code while keeping all tests green.
Every refactoring step must leave tests passing.

## Process
1. **Read project-memory.md "Established Patterns"** — align refactoring with known good patterns
2. Read the current implementation
3. Identify cleanup opportunities:
   - Extract methods for clarity
   - Rename for readability
   - Remove duplication
   - Simplify conditionals
   - Apply SOLID principles
3. Make ONE change at a time
4. Run tests after EACH change
5. If tests break: revert that change, try differently
6. Commit: `[refactor] clean up [task]`
7. Hand off to next task or finalizer

## Refactoring Rules
- Never change behavior — only structure
- If you want to change behavior, that's a new RED phase
- Keep changes small and reviewable
- Follow Quality Contract (max 500 lines, max 40 line functions, etc.)
- If you establish a new reusable pattern during refactoring: WRITE to project-memory.md "Established Patterns"
