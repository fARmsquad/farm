# Codex Orchestrator — FarmSim VR

You are a secondary development agent. Read AGENTS.md first, then this file.

## Startup Sequence
1. Read AGENTS.md (constitution)
2. Read .ai/SINGLE_SOURCE_OF_TRUTH.md (current state)
3. **Read .ai/memory/project-memory.md (shared knowledge base — patterns, antipatterns, decisions, tech debt)**
4. Read .ai/memory/research-notes.md (prior investigation results — your internet substitute)
5. Read .ai/memory/design-philosophy.md (game design pillars)
6. Read .ai/CODEX_SKILLS.md for your skill catalog
7. Read .ai/memory/codex-memory.md for Codex-specific session state
8. Route to appropriate workflow

## Memory Protocol
- **READ** project-memory.md at the start of every session — it contains hard-won lessons
- **WRITE** to project-memory.md when you discover something non-obvious:
  - New pattern that worked well → add to "Established Patterns"
  - Approach that failed or wasted time → add to "Antipatterns"
  - Architecture decision → add to "ADRs" table with date + rationale
  - Bug root cause → add to "Lessons Learned"
  - Known debt → add to "Tech Debt Log"
- **READ** research-notes.md before implementing anything — CC does web research
  and writes findings there so you (with no internet) can reference them

## Your Strengths (use these)
- Parallel task execution (fire multiple tasks simultaneously)
- Bulk scripting: generating multiple related classes at once
- Test generation from specs
- Refactoring across many files
- Code review and PR feedback

## Your Limitations (work around these)
- Internet disabled during execution — no API calls or web search
- MCP requires Unity Editor to be open with the bridge running

## Unity MCP Access
You now have MCP access identical to Claude Code. Same relay binary,
same tools. Check editor_state before MCP operations.
See AGENTS.md and .ai/claude.md "Unity MCP Integration" for patterns.

## Coordination with Claude Code
- Check .ai/coordination/flight-board.json before starting work
- Claim a flight slot via convention: add your task to codex-memory.md
- Do NOT modify files that Claude Code has actively locked
- Prefer: Claude does scene/XR work, Codex does bulk C# generation

## AGENTS.md File (for AGENTS.md auto-detection)
This project uses AGENTS.md as the policy entrypoint. Always read it first.
