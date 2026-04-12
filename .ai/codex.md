# Codex Orchestrator — FarmSim VR

You are a secondary development agent. Read AGENTS.md first, then this file.

## Startup Sequence
1. Read AGENTS.md (constitution)
2. Read .ai/SINGLE_SOURCE_OF_TRUTH.md (current state)
3. **Read .ai/memory/project-memory.md (shared knowledge base — patterns, antipatterns, decisions, tech debt)**
4. **Read .ai/memory/completion-learnings.md (post-"done" misses, why they escaped, how to avoid repeating them)**
5. Read .ai/memory/research-notes.md (prior investigation results — your internet substitute)
6. Read .ai/memory/design-philosophy.md (game design pillars)
7. Read .ai/CODEX_SKILLS.md for your skill catalog
8. Read .ai/memory/codex-memory.md for Codex-specific session state
9. Route to appropriate workflow

## Global Unity Skills
Codex may also have global Unity-focused skills installed outside this repo.
When relevant, use them in addition to the repo-local `.ai/skills/` files:

- `unity-mcp-orchestrator` for Unity Editor automation through MCP
- `unity-developer` for day-to-day Unity implementation guidance
- `unity-ecs-patterns` for DOTS / Jobs / Burst / ECS-heavy work
- `unity-profiler` for profiling and performance evidence

`unity-initial-setup` is not currently installed in Codex. If Unity
MCP/bootstrap setup drifts on this machine, fall back to the repo MCP notes in
this file plus `.ai/claude.md` until a valid installable package is identified.

Repo-local `.ai/skills/` remain the authoritative workflow contract. The
global skills are accelerators, not replacements.

## Memory Protocol
- **READ** project-memory.md at the start of every session — it contains hard-won lessons
- **READ** completion-learnings.md at the start of every session — it captures where agents previously over-claimed completion and what verification was missing
- **WRITE** to project-memory.md when you discover something non-obvious:
  - New pattern that worked well → add to "Established Patterns"
  - Approach that failed or wasted time → add to "Antipatterns"
  - Architecture decision → add to "ADRs" table with date + rationale
  - Bug root cause → add to "Lessons Learned"
  - Known debt → add to "Tech Debt Log"
- **WRITE** to completion-learnings.md when the developer reports an error, issue, or misunderstanding after an agent said the work was done:
  - Record the original done claim or handoff context
  - Record the failing behavior and the approach that produced it
  - Explain why the verification or handoff missed it
  - State what should have been verified or phrased differently
  - End with concrete prevention rules for future specs, implementation, and verification
- **DISTILL** any durable rule from completion-learnings.md into project-memory.md so later work can reference the short version quickly
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
For scene/editor execution, prefer loading the global `unity-mcp-orchestrator`
skill before complex MCP-driven Unity work.

## Coordination with Claude Code
- Check .ai/coordination/flight-board.json before starting work
- Claim a flight slot via convention: add your task to codex-memory.md
- Do NOT modify files that Claude Code has actively locked
- Prefer: Claude does scene/XR work, Codex does bulk C# generation

## AGENTS.md File (for AGENTS.md auto-detection)
This project uses AGENTS.md as the policy entrypoint. Always read it first.
