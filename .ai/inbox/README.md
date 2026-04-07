# Developer Inbox

Drop files here anytime. Agents check between tasks and at story boundaries.
No special format required — just write what you're thinking.

## Folders
- **ideas/**: "What if crops could cross-pollinate?" — new feature concepts
- **feedback/**: "The watering gesture feels too precise" — playtest observations
- **bugs/**: "Tomatoes clip through the soil when planted" — things that are broken
- **needs-eyes/**: Agents put items here when they made a decision they're unsure about
- **steering/**: "Stop working on weather, prioritize the harvest system" — priority shifts

## How Agents Process This
1. Read all files in inbox/ at the start of each story
2. Triage:
   - Ideas → add to backlog in SINGLE_SOURCE_OF_TRUTH.md
   - Feedback → attach to relevant story or create fix task
   - Bugs → route to .ai/workflows/bugfix.md immediately if critical
   - Needs-eyes → present to developer for decision
   - Steering → re-prioritize current work
3. Move processed files to .ai/inbox/processed/ with timestamp
4. Never delete inbox files — archive them

## You Can Also Just Talk
If you're in a CC session, just say what you're thinking. The inbox is for
async thoughts — things that come to you while playtesting, showering,
or falling asleep. Jot them down, agents will pick them up.
