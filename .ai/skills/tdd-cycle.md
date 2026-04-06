# Skill: TDD Cycle

## Purpose
Full TDD orchestration: RED → GREEN → VERIFY → REFACTOR → ASSEMBLE → XR → VISUAL

## Inputs
- Task description from spec breakdown
- Target system/class to implement
- Acceptance criteria from spec

## Step Sequence

### For Core/ tasks (pure logic — no MCP needed):

1. **RED**: Delegate to .ai/agents/tdd-agent.md
   - Write failing tests
   - Verify tests fail for the right reason
   - Commit: `[tests] add failing tests for [task]`

2. **GREEN**: Delegate to .ai/agents/implementer.md
   - Write minimal passing implementation
   - Verify all tests pass (MCP run_tests if editor open, CLI fallback)
   - Commit: `[feature] implement [task]`

3. **VERIFY**: Delegate to .ai/agents/verifier.md
   - Check for overfitting
   - Add boundary/edge case tests
   - If overfitting: loop back to GREEN
   - Commit: `[tests] add boundary tests for [task]`

4. **REFACTOR**: Delegate to .ai/agents/refactorer.md
   - Clean up while keeping tests green
   - One change at a time, test after each
   - Commit: `[refactor] clean up [task]`

### For MonoBehaviour/ tasks (needs editor — extends above):

5. **ASSEMBLE** (MCP): Skill → .ai/skills/scene-assembly.md
   - Check editor state, refresh_unity, read_console
   - Create GameObjects, add components, wire references
   - Create ScriptableObject data assets
   - Create prefabs
   - Commit: `[scene] assemble [task] in editor`

6. **XR WIRE** (MCP, if interactive):
   - Add XR interaction components
   - Configure physics layers
   - Set up grab/snap/poke interactions
   - Commit: `[xr] wire interaction for [task]`

7. **VISUAL POLISH** (MCP, if visual):
   - Materials, textures, VFX, animation
   - Commit: `[visual] add visual polish for [task]`

8. **EDITOR VERIFY** (MCP):
   - read_console → no errors
   - find_gameobjects → all objects present
   - validate_script → all scripts compile
   - Optional: manage_profiler → quick perf check

## Exit Criteria
- All tests pass (including new boundary tests)
- Code meets Quality Contract
- No overfitting detected
- Implementation is clean and readable
- If MonoBehaviour: scene assembled, prefabs created, console clean
- If XR: interaction components wired, physics layers configured
