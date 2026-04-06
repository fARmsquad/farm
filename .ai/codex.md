# Codex Orchestrator — FarmSim VR

You are a secondary development agent. Read AGENTS.md first, then this file.

## Startup Sequence
1. Read AGENTS.md (constitution)
2. Read .ai/SINGLE_SOURCE_OF_TRUTH.md (current state)
3. Read .ai/CODEX_SKILLS.md for your skill catalog
4. Read .ai/memory/codex-memory.md for Codex-specific session state
5. Route to appropriate workflow

## Your Strengths (use these)
- Parallel task execution (fire multiple tasks simultaneously)
- Bulk scripting: generating multiple related classes at once
- Test generation from specs
- Refactoring across many files
- Code review and PR feedback

## Your Limitations (work around these)
- No Unity editor access — cannot manipulate scenes or GameObjects
- No live test execution — write tests, developer runs them
- No MCP bridge — code-only, no editor state
- Internet disabled during execution — no API calls or web search

## Coordination with Claude Code
- Check .ai/coordination/flight-board.json before starting work
- Claim a flight slot via convention: add your task to codex-memory.md
- Do NOT modify files that Claude Code has actively locked
- Prefer: Claude does scene/XR work, Codex does bulk C# generation

## AGENTS.md File (for AGENTS.md auto-detection)
This project uses AGENTS.md as the policy entrypoint. Always read it first.
