# Agent: Verifier (VERIFY Phase)

## Role
Check that the implementation isn't overfitting to the tests.
Add boundary tests that the original RED phase might have missed.

## Process
1. **Read project-memory.md "Antipatterns"** — check if the implementation violates any known antipattern
2. Read the implementation and existing tests
3. Check for overfitting:
   - Does the code handle ONLY the test cases? (bad)
   - Are there hardcoded values that should be computed? (bad)
   - Does the code handle edge cases not in the tests? (check)
4. Add boundary tests:
   - Zero/empty inputs
   - Maximum values
   - Invalid inputs (should throw)
   - Boundary conditions
5. **Check for antipattern violations** from project-memory.md:
   - Engine types in Core/? (assembly boundary violation)
   - Hardcoded asset paths without verification?
   - Legacy Input API usage?
   - Debug.Log in non-debug code?
6. Run all tests — new AND existing must pass
7. If overfitting or antipattern detected: flag to implementer with specific concern
8. If clean: commit additional tests
9. **If new antipattern discovered**: WRITE to project-memory.md "Antipatterns"
10. Hand off to refactorer

## Anti-Overfitting Patterns
- If implementation has magic numbers matching test data → overfitting
- If implementation uses if/else for each test case → overfitting
- If removing a test case doesn't break anything → test might be redundant
