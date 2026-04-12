# Workflow: AI Architecture Change

## Trigger
Any modification to:
- AGENTS.md
- Any file in .ai/
- .claude/settings.json or hooks
- Any harness script (run-tests.sh, preflight.sh, finalize.sh, etc.)

## Process
1. Document what changed and why in .ai/memory/project-memory.md
2. Run .ai/scripts/check_ai_wiring.sh
3. Verify all agents still align:
   - claude.md references correct workflows and skills
   - codex.md references correct repo-local and global Codex skills
   - CODEX_SKILLS.md reflects any wired global-skill expectations
   - SINGLE_SOURCE_OF_TRUTH.md is consistent
   - completion-learnings.md exists and both orchestrators reference it
   - All workflow files reference valid agents and skills
   - All skill files reference valid scripts
4. Run full test suite to verify no breakage
5. If wiring audit fails, fix before committing
6. Commit: `[harness] [description of change]`
