# Skill: TDD Cycle

## Purpose
Full TDD orchestration: RED → GREEN → VERIFY → REFACTOR → ASSEMBLE → XR → VISUAL

## Inputs
- Task description from spec breakdown
- Target system/class to implement
- Acceptance criteria from spec

## Pre-Step: Memory Load
**Before starting any TDD cycle:**
1. **READ .ai/memory/project-memory.md** — scan for:
   - Patterns relevant to the system being built
   - Antipatterns to avoid (especially asset paths, MCP workflow, Input System)
   - Lessons learned from similar past work
   - Tech debt that might affect this task
2. **READ .ai/memory/research-notes.md** — check for prior research on this feature

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

## Git Discipline (per-task)
- Each agent commits with the correct `[tag]` prefix after its step
- Stage only specific files (`git add <files>`), never `git add .`
- After every 3rd task: `.ai/scripts/git_sync.sh sync` (rebase onto origin/main)
- `git push` after each completed task to keep remote branch current

## Post-Step: Memory Write
**After completing a full TDD cycle (RED→GREEN→VERIFY→REFACTOR):**
If anything non-obvious was learned during implementation:
- New pattern → WRITE to project-memory.md "Established Patterns"
- Gotcha or trap → WRITE to project-memory.md "Antipatterns" or "Lessons Learned"
- Performance discovery → WRITE to project-memory.md "Performance Budgets"
Skip if the cycle was routine with no new insights.

## Exit Criteria
- All tests pass (including new boundary tests)
- Code meets Quality Contract
- No overfitting detected
- Implementation is clean and readable
- If MonoBehaviour: scene assembled, prefabs created, console clean
- If XR: interaction components wired, physics layers configured
- All commits follow `[tag] message` format
- Branch pushed to origin and synced with main
- project-memory.md updated if new lessons learned
