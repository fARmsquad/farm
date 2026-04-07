# Skill: Unity Research (Mandatory Web Search)

## Purpose
Before designing or implementing any Unity feature, search the web for how
experienced developers implement similar things. Extract concrete excerpts,
patterns, and pitfalls to guide the technical plan and implementation.

## When This Skill Fires
- **Always** during Phase 2 (Spec Package) before generating the Technical Plan
- **Always** during Phase 3a (TDD Cycle) before the implementer writes code
- **On demand** when any agent encounters an unfamiliar Unity API or pattern

## Process

### Step 1: Formulate Search Queries
From the story card / task description, generate 2-4 targeted search queries:

| Query Type | Template | Example |
|-----------|----------|---------|
| Implementation | `"unity [feature] implementation tutorial"` | `"unity crop growth system implementation tutorial"` |
| Architecture | `"unity [pattern] best practice"` | `"unity scriptableobject state machine best practice"` |
| Performance | `"unity [feature] performance optimization quest"` | `"unity particle system performance optimization quest"` |
| XR-specific | `"unity xr toolkit [interaction] how to"` | `"unity xr toolkit grab interactable snap zone how to"` |

Prioritize recent results (last 2 years). Prefer Unity 6 / URP / XR Toolkit 3.x content.

### Step 2: Search for Unity Packages First
Before writing custom code, check if a Unity package already solves the problem:

1. **Unity Registry**: Search `"unity package [feature]"` or check
   [Unity Package Manager docs](https://docs.unity3d.com/Manual/PackagesList.html)
2. **OpenUPM**: Search `"openupm [feature]"` for community packages
3. **Asset Store**: Search `"unity asset store [feature] free"` for vetted assets
4. **GitHub**: Search `"unity [feature] package github"` for open-source options

Use the MCP tool to check what's already installed:
```
Unity_PackageManager_GetData(packageID: "com.unity.*", installedOnly: true)
```

**Evaluate each candidate package against:**
- Is it maintained? (last update < 6 months)
- Does it support Unity 6 / URP?
- Does it work on Quest (Android/ARM64)?
- License compatible? (MIT, Apache, Unity EULA OK — check GPL carefully)
- Does it bloat the build? (check package size vs. what we'd write ourselves)

**Record package recommendations** in the research brief under a new
"### Recommended Packages" section. If a package covers 80%+ of the need,
recommend using it over custom code.

### Step 3: Execute Web Searches
Use `WebSearch` tool for each query. For each result set:
1. Identify the **top 3 most relevant results**
2. Use `WebFetch` to read each page
3. Extract **concrete excerpts** (code patterns, architecture diagrams, gotchas)

### Step 4: Synthesize Findings
Create a research brief with this structure:

```markdown
## Research: [Feature/Task Name]
**Date**: [YYYY-MM-DD]
**Queries**: [list of queries used]

### Recommended Packages
| Package | Source | Why | Status |
|---------|--------|-----|--------|
| [com.unity.xxx or package name] | [Registry/OpenUPM/GitHub] | [what it solves] | [Recommended / Alternative / Rejected (reason)] |

If no suitable package found, state: "No packages found — custom implementation required."
If a package is recommended, the Technical Plan should use it as a dependency,
not reimplement what it provides.

### Key Patterns Found
- [Pattern 1]: [1-2 sentence summary + source URL]
- [Pattern 2]: [1-2 sentence summary + source URL]

### Recommended Approach
Based on [N] sources, the consensus approach is:
[2-3 sentences describing the recommended pattern]

### Code Reference
[Short code snippet or pseudocode distilled from multiple sources — NOT copied verbatim]

### Gotchas & Pitfalls
- [Gotcha 1]: [what to avoid and why, from source URL]
- [Gotcha 2]: [what to avoid and why, from source URL]

### Quest/Mobile Considerations
- [Performance note relevant to Quest 3 / mobile VR]

### Sources
1. [Title](URL) — [one-line relevance note]
2. [Title](URL) — [one-line relevance note]
3. [Title](URL) — [one-line relevance note]
```

### Step 5: Persist Research
- Append the research brief to `.ai/memory/research-notes.md`
- If this is a Phase 2 research, the Technical Plan MUST reference findings
  in a new "## Research Reference" section
- If this is a Phase 3a research, the implementer MUST read the brief
  before writing code
- If a package is recommended, add it to the Technical Plan's dependencies

### Step 6: Validate Against Project Architecture
Cross-check findings against:
- `.ai/SINGLE_SOURCE_OF_TRUTH.md` — does the pattern align with our architecture?
- `.ai/memory/project-memory.md` — have we made decisions that conflict?
- `.ai/memory/design-philosophy.md` — does it fit our design principles?

If conflict found: flag in `.ai/inbox/needs-eyes/` with both the research
findings and the conflicting decision.

## Quality Gates
- [ ] Package search performed (Unity Registry, OpenUPM, GitHub)
- [ ] Package recommendations documented (or "none found" stated)
- [ ] At least 2 web searches performed per feature/task
- [ ] At least 3 sources consulted
- [ ] Excerpts are synthesized, not copy-pasted verbatim
- [ ] Research brief saved to research-notes.md
- [ ] Technical plan or implementation references the research
- [ ] Quest/mobile performance implications noted

## Skip Conditions
Research MAY be abbreviated (1 search, 1 source) when:
- The feature is a trivial CRUD operation with no Unity-specific complexity
- We have prior research in research-notes.md that's < 30 days old for this topic
- The task is pure refactoring with no new patterns

Research CANNOT be skipped entirely. Even trivial features get one search.

## Agent Compatibility
- **Claude Code**: Full access to WebSearch + WebFetch. Primary research agent.
- **Codex**: No internet access. Must READ research from research-notes.md.
  Claude Code performs all research BEFORE handing tasks to Codex.
