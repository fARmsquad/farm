# Skill: Story Lookup

## Purpose
Find and load story context for a feature request.

## Process
1. Check SINGLE_SOURCE_OF_TRUTH.md for current story
2. Search Assets/Specs/Features/ for existing specs
3. Search .ai/inbox/ for related items
4. Search .ai/memory/project-memory.md for related decisions
5. Search .ai/memory/research-notes.md for prior research on this feature
6. **Check installed packages** — run `Unity_PackageManager_GetData` to see
   what's already in the project that might cover part of this story

## If Story Exists
- Load the spec from Assets/Specs/Features/
- Check what phase it's in (from SSOT)
- Check if prior research recommended any packages (research-notes.md)
- Resume from that phase

## If New Story
- Create story card from .ai/templates/story/story-card.md
- Update SSOT with new story
- **Trigger unity-research.md skill** — this includes the mandatory package search
- Proceed to spec generation

## Package-First Principle
Before writing custom systems, always ask: "Is there a package that does this?"
A well-maintained package with Quest support beats custom code because:
- Less code to maintain
- Battle-tested by other developers
- Often has editor tooling built in
- Frees agent time for game-specific logic

Flag package suggestions in the story card under a "## Dependencies" section.
