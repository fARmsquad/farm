# Workflow: Story Handoff

## Purpose
Transition from "code complete" to "developer verified." The handoff is
NOT a gate that blocks the pipeline — finalization already happened.
This is the human experience verification layer.

## Auto-Generated Handoff Package
When finalization completes, automatically generate:

1. **Playtest guide** (from .ai/workflows/playtest-checkpoint.md)
   - VR-specific: what to do with your hands, what to see/feel/hear
   - Edge cases designed around VR quirks (scale, depth, grab zones)

2. **Technical summary** (for the developer's reference, not a gate)
   ```
   ## Story Complete: [Feature Name]

   ### What the Agents Built
   [Plain language summary — not code, not tests, just what it does]

   ### What Was Verified Directly
   - [Tests run, scenes exercised, screenshots checked, commands executed]

   ### What Was Not Verified Directly
   - [Anything inferred, assumed, or not exercised end-to-end]

   ### Remaining Risk
   - [Anything that could still fail or feel wrong despite the completed work]

   ### Architecture Decisions Made
   - [Decision]: [why] (see ADR if created)

   ### Test Coverage
   - EditMode: X tests covering [systems]
   - PlayMode: Y tests covering [interactions]

   ### Files Touched
   [grouped by system, not alphabetical]

   ### Performance Notes
   - [Any perf-relevant observations from the agents]

   ### What the Agents Were Unsure About
   - [Anything flagged in .ai/inbox/needs-eyes/]
   ```

3. **Feedback routing guide**
   ```
   After playtesting, you can:

   Just talk to me
      "The tomato grab feels too snappy" -> I'll create a fix task
      "What if we added crop rotation?" -> I'll create a story card
      "This is wrong, redo it" -> I'll re-enter TDD with your feedback

   Drop a note in .ai/inbox/
      For thoughts that come to you later

   Say "good" or "next"
      Story closes, I move to the next item
   ```

## Feedback Processing
When the developer responds:

| Developer Says | Agent Does |
|---|---|
| "good" / "approved" / "next" | Close story, update SSOT, pick next from backlog |
| "feels off" + description | Create targeted fix tasks, re-enter TDD Phase 3 |
| Bug/error after "done" | Log a completion learning first, then route to bugfix or targeted fix tasks |
| Specific bug report | Route to bugfix workflow immediately |
| New feature idea | Create story card in inbox/ideas/, acknowledge, continue |
| "Stop, let's rethink this" | Pause pipeline, enter collaborative design discussion |
| "Do [other thing] instead" | Archive current, route to new work via steering/ |
| Nothing (silence for >session) | Leave story open, resume when developer returns |

## Post-Completion Correction Protocol
When the developer returns after a "done" claim with an error, issue, or misunderstanding:
1. Append a structured entry to `.ai/memory/completion-learnings.md`
2. Record the original claim, the failing behavior, the approach that produced it, and why verification missed it
3. Distill durable rules into `.ai/memory/project-memory.md`
4. Add the missing test, check, or handoff wording before re-closing the work

## The Developer is Never Blocked
If the developer doesn't respond, the agent:
1. Checks if there's other work in the backlog
2. If yes: starts next story (the current one stays "awaiting playtest")
3. If no: checks inbox for ideas/bugs/feedback to work on
4. If truly nothing: organizes — refactors, improves tests, documents
5. NEVER idles while waiting for human. Always productive.
