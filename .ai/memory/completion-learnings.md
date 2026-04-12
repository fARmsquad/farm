# Completion Learnings — FarmSim VR

> This file tracks cases where an agent said work was done and the developer
> later found an error, issue, gap, or misunderstanding.
> Log the full case here before or alongside the fix.
> Copy only the durable short rule into `.ai/memory/project-memory.md`.

## When To Write Here
- A developer returns after a "done" claim with a bug, regression, missing piece, or misunderstanding
- A handoff implied stronger verification than what actually happened
- A feature passed tests but failed in the real scene, device, or workflow the agent claimed was complete

## Entry Rules
1. Append a new entry as soon as the post-"done" issue is recognized
2. Name the original task, story, or handoff if known
3. Explain the approach that produced the miss
4. State why verification or wording failed to catch it
5. End with concrete prevention rules for future spec, implementation, verification, or handoff work
6. After the fix lands, update the entry with the actual root cause and the guardrail that was added

## Entry Template
```md
### [Short Title] (YYYY-MM-DD)
- Status: Open | Addressed
- Related story/task:
- Original completion claim:
- Reported issue:
- Failing evidence:
- Approach that produced the miss:
- Why it was mistaken for done:
- What should have been verified or stated differently:
- Prevention rules for future work:
- Follow-up actions:
- Distilled rule added to project-memory.md:
```

## Log

### Scene 7 Farming Tutorial Still Auto-Played After Completion Claim (2026-04-11)
- Status: Addressed
- Related story/task: FarmMain / Scene 7 farming tutorial completion pass
- Original completion claim: Scene 7 was handed off as fixed with real first-watering gating, mission-aware prompts, dead-plant recovery, and a pragmatic playtest guide, while noting that Unity batch tests were blocked by the open editor.
- Reported issue: The developer reported that the crop lifecycle still runs far too quickly and too automatically, and that the scene can end up asking for harvest while the visible crop state no longer makes sense.
- Failing evidence: Developer report after live scene interaction: "it goes through the life cycle way too quick and way too automatically without player doing anything and also goes to nothing and asks me to harvest".
- Approach that produced the miss: The fix corrected germination and prompt filtering but kept an auto-fast-forward growth loop and did not prove that Scene 7 pacing stayed aligned with the visible plot state in the actual player flow.
- Why it was mistaken for done: The handoff over-weighted static coherence and local code review while a scene-level pacing bug remained in the live gameplay loop. The implementation made the tutorial logically stricter, but still effectively auto-played the rest of the crop growth after the first successful action.
- What should have been verified or stated differently: For guided scene flows, "done" must include verification that mission progression, allowed actions, and visible object state stay aligned across the real player interaction loop, not just that the state machine is internally consistent.
- Prevention rules for future work:
  - Do not keep hidden autoplay progression in a guided tutorial step unless the spec explicitly calls for it.
  - When a scene teaches a mechanic, verify that each state transition is caused by an intentional player action or by clearly signaled waiting, not by an opaque background shortcut.
  - For tutorial mission logic, test and review the triad together: mission step, allowed prompt actions, and visible world state.
- Follow-up actions: Added failing tests for tutorial-paced growth and growth-step prompt behavior, then reworked Scene 7 so the hero plot uses per-phase watering gates instead of the old auto-fast-forward coroutine. Root cause was the combination of `TutorialFarmSceneController` auto-advancing the crop in the background and `FarmTutorialMissionService` only exposing watering during wilting, which let the tutorial progress while the player's visible interaction options fell away. The landed guardrail is that guided farming progression must remain tied to explicit player watering checkpoints, and the growth-step prompt must keep exposing the action that advances the tutorial.
- Distilled rule added to project-memory.md: `Tutorial State Progression Must Match Player Actions (2026-04-11)`

### Incomplete Brace Closure In New EditMode Test (2026-04-11)
- Status: Addressed
- Related story/task: Simplify FarmMain into a guided first-harvest mission with dressed farm presentation
- Original completion claim: The FarmMain tutorial rewrite and its new EditMode tests were implemented, with verification limited by the open Unity editor.
- Reported issue: The developer hit `Assets/Tests/EditMode/FarmTutorialGuidanceServiceTests.cs(79,6): error CS1513: } expected`.
- Failing evidence: The new test file ended with the class-closing brace but omitted the namespace-closing brace.
- Approach that produced the miss: A new test file was added by hand and only inspected for content, not for full structural closure at EOF.
- Why it was mistaken for done: Static review focused on test intent and assertions, and missed the trivial parser-level structure error because no compiler pass completed.
- What should have been verified or stated differently: New C# files need an explicit EOF structure check before handoff, especially when Unity compile execution is blocked.
- Prevention rules for future work:
  - After creating a new C# file, confirm its closing braces and namespace/class structure explicitly before handoff.
  - When batch compile is unavailable, perform a quick parser-oriented read of every new file from top to bottom, not only the changed methods.
  - Treat new test files as compile-risky even when the test logic itself is straightforward.
- Follow-up actions: Added the missing namespace-closing brace to `FarmTutorialGuidanceServiceTests.cs` and swept the other newly added tutorial test files plus `FarmTutorialGuidanceService.cs` for the same EOF structure issue. Root cause was a hand-authored new file that never got a full-file closure pass after the last edit. The landed guardrail is the new project-memory rule requiring EOF structure checks on every new C# file before handoff when a fresh Unity compile is unavailable.
- Distilled rule added to project-memory.md: `New C# Files Need EOF Structure Checks (2026-04-11)`

### Missing Namespace Imports In Tutorial/Farming Glue (2026-04-11)
- Status: Addressed
- Related story/task: Simplify FarmMain into a guided first-harvest mission with dressed farm presentation
- Original completion claim: The FarmMain tutorial rewrite was implemented and handed off with targeted static verification, with the main stated limitation being that Unity batch tests could not run while the editor was open.
- Reported issue: The developer hit compiler errors because `FarmPlotInteractionController` referenced `TutorialFlowController` without importing `FarmSimVR.MonoBehaviours.Tutorial`, and `TutorialFarmSceneController` referenced `FarmTutorialObjectiveTracker` without importing `FarmSimVR.Core.Tutorial`.
- Failing evidence: `Assets/_Project/Scripts/MonoBehaviours/Farming/FarmPlotInteractionController.cs(245,50): error CS0246` and `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialFarmSceneController.cs(200,48): error CS0246`.
- Approach that produced the miss: The implementation added new cross-namespace references in MonoBehaviour glue code and relied on local file inspection rather than a fresh compiler pass.
- Why it was mistaken for done: The handoff correctly stated that batch tests were blocked by the open editor, but it still overestimated static confidence and missed a basic namespace-resolution check in the edited files.
- What should have been verified or stated differently: Before calling Unity-facing implementation done, every edited C# file should get a compile-oriented pass for new type references, especially when bridging `Core` and `MonoBehaviours` namespaces. The handoff should also have been stricter that compile validity was not confirmed.
- Prevention rules for future work:
  - After adding new type references, scan edited files for cross-namespace symbols and confirm the required `using` directives are present before handoff.
  - Treat any blocked Unity compile/test run as a hard reason to avoid implying compile confidence beyond direct static checks.
  - When wiring `Core` tutorial types into MonoBehaviour files, explicitly verify namespace imports on both sides of the boundary.
- Follow-up actions: Patched `FarmPlotInteractionController` to import `FarmSimVR.MonoBehaviours.Tutorial`, patched `TutorialFarmSceneController` to import `FarmSimVR.Core.Tutorial`, and re-scanned the nearby tutorial/farming files for the same cross-namespace symbols. Root cause was a hand edit that introduced new references across assemblies without a final import sweep. The guardrail is the new project-memory rule requiring a compile-oriented `using` scan whenever cross-assembly wiring changes land without a fresh Unity compile.
- Distilled rule added to project-memory.md: `Namespace Imports After Cross-Assembly Wiring (2026-04-11)`

### Completion Claim Boundaries (2026-04-11)
- Status: Addressed
- Related story/task: AI workspace completion-learning protocol
- Original completion claim: Agents have been able to report work as done without consistently preserving what verification actually happened or what escaped it.
- Reported issue: The developer asked for every post-"done" error or issue to produce a reusable learning tied to the approach that produced the miss.
- Failing evidence: Repeated developer follow-ups after "done" claims exposed that the workspace had no dedicated ledger for completion misses and no required verified-vs-assumed handoff boundary.
- Approach that produced the miss: Completion relied on ad hoc summaries and general project memory rather than a dedicated post-completion feedback loop.
- Why it was mistaken for done: The system lacked a structured requirement to explain why the work seemed complete, what was actually verified, and what should have been checked before closing it.
- What should have been verified or stated differently: Completion summaries should explicitly separate verified facts from unverified assumptions, and any returned issue should be logged before the fix proceeds.
- Prevention rules for future work:
  - Read relevant completion-learnings before spec, implementation, verification, and finalization
  - Log every post-"done" issue in this file before or alongside the fix
  - Distill durable rules into `project-memory.md`
  - Update tests, checks, or handoff wording so the same escape path is covered next time
- Follow-up actions: Added AGENTS, orchestrator, workflow, skill, and wiring-audit rules that require this loop.
- Distilled rule added to project-memory.md: `Completion Claims (2026-04-11)`

### Scene 7 Tomato Support Was Treated Like a Lifecycle Stage (2026-04-11)
- Status: Addressed
- Related story/task: FarmMain / Scene 7 farming tutorial visual-state tuning
- Original completion claim: Scene 7 was handed off as fixed after the autoplay growth loop was removed and per-phase watering gates were added for the hero crop.
- Reported issue: The developer reported that the wooden plank should stay up after planting because it is a separate support asset, and that the smaller tomato should remain as its own step before the larger ripe tomato appears.
- Failing evidence: Developer report after scene review: "the wooden plank should be always up after seed is planted (thats a separate asset not a lifecycle one that changes), the smaller tomatoe should be a step until the bigger tomato comes out".
- Approach that produced the miss: The scene still treated the support prop as if it belonged to the same one-active-at-a-time lifecycle stage list as the crop meshes, and the verification did not explicitly prove that the fruiting mesh remained visible as a distinct intermediate phase before the ripe mesh.
- Why it was mistaken for done: The previous pass focused on tutorial pacing and interaction state, but it did not separate persistent scene dressing from the crop stage swap system or assert the exact visual sequence for fruit growth.
- What should have been verified or stated differently: The handoff should have confirmed which visuals are persistent props versus lifecycle stages, and should have verified the visible mesh sequence at least through planted, fruiting, and ready.
- Prevention rules for future work:
  - Model persistent crop supports separately from mutually exclusive lifecycle stages.
  - Add scene or EditMode coverage for any visual that must remain active across multiple phases.
  - When a crop has intermediate fruit meshes, verify the ordered visual sequence explicitly instead of assuming timing changes will reveal it correctly.
- Follow-up actions: Added `phaseExtras` support to `CropVisualUpdater`, wired the tomato stick in `FarmMain` as a persistent phase-range visual from `Planted` through `Dead`, and strengthened `FarmingVisualUpdaterTests` to prove the support asset persists while the crop advances and that `Fruiting` shows the smaller tomato before `Ready` swaps to the larger ripe tomato.
- Distilled rule added to project-memory.md: `Persistent Tutorial Props Are Not Lifecycle Stages (2026-04-11)`

### Scene 7 Growth Tuning Kept Moving Before Base Art Signoff (2026-04-11)
- Status: Addressed
- Related story/task: FarmMain / Scene 7 crop lifecycle tuning
- Original completion claim: Scene 7 tomato visuals were being iterated as if the next step was to enrich the growth ladder and maximize use of the crop pack.
- Reported issue: The developer said they were still very unsatisfied with the growth lifecycle and wanted the work scrapped back to a step-by-step review flow starting with soil/base options placed in the scene for selection.
- Failing evidence: Developer direction after review: "im very unsatisfied with the growth lifecycle. please scrap things and lets take it step by step. start with the soil then place different asset options in the scene and i;ll tell you which one to choose".
- Approach that produced the miss: The implementation kept deepening lifecycle logic and prefab sequencing before the developer had signed off on the foundational visual base of the plot itself.
- Why it was mistaken for done: The work assumed that more complete lifecycle mapping would converge on the right answer, but the developer actually needed an incremental art review workflow with discrete scene options.
- What should have been verified or stated differently: Before adding more lifecycle rules, the handoff should have staged base soil/planter options in-scene and gotten a direct choice on the plot foundation first.
- Prevention rules for future work:
  - For art-driven mechanic tuning, get signoff on the base plot/soil presentation before expanding lifecycle sequencing.
  - When the developer asks for step-by-step review, prefer in-scene option galleries over inferred "best" choices.
  - Stop escalating scene logic when the blocking uncertainty is asset selection rather than code behavior.
- Follow-up actions: Reverted the latest lifecycle escalation, restored the simpler tomato visual baseline, and staged a `SoilOptions_Showcase` gallery inside `FarmMain` with field, furrow, and planter candidates for direct review before any more lifecycle mapping work continues.
- Distilled rule added to project-memory.md: `Stage Art Options In-Scene Before Encoding More Lifecycle Logic (2026-04-11)`

### Review Slice Targeted Soil Bases Instead Of Vegetable States (2026-04-11)
- Status: Addressed
- Related story/task: Title-screen review slice for Scene 7 visual tuning
- Original completion claim: A new selectable `FarmSoilSelection` scene was added under the FarmSim VR title-screen launcher so the developer could review options directly in-game.
- Reported issue: The developer clarified that the requested review target was the vegetable growth states, not the soil/base options.
- Failing evidence: Developer follow-up after handoff: "my brother i mean the vehgetable states".
- Approach that produced the miss: The implementation anchored too literally on the earlier "start with the soil" direction and turned the new selectable slice into a soil-base gallery without reconfirming that the latest "create a scene ... i can select" request was still aimed at soil rather than the crop-state visuals being discussed immediately before it.
- Why it was mistaken for done: The handoff assumed the review subject from stale context instead of mirroring the requested target back through the scene name, display name, and slice content.
- What should have been verified or stated differently: Before building a new selection slice for art review, the implementation should explicitly align the slice subject with the most recent noun domain in the request and ensure the scene label/content match that subject.
- Prevention rules for future work:
  - When building a review scene from an ambiguous art-tuning request, reflect the exact review target in the scene name and display name before wiring content.
  - Treat newer clarification nouns like soil, crop states, tomato stages, or planter bases as stronger than older planning context.
  - If the requested slice subject changes, replace the incorrect review content instead of leaving both live by default.
- Follow-up actions: Repurposed the newly added review slice away from soil bases and rebuilt it as a vegetable-state selection scene with crop lifecycle prefabs as the primary review content.
- Distilled rule added to project-memory.md: `Selection Slice Names Must Match The Current Review Target (2026-04-11)`

### FarmStageMinigameDefinition Member Name Collision (2026-04-11)
- Status: Addressed
- Related story/task: Scene 7 tomato task-driven minigame lifecycle
- Original completion claim: The Scene 7 tomato minigame flow was handed off as implemented with targeted static verification, while noting that Unity batch tests were blocked because the editor already had the project open.
- Reported issue: The developer hit a compiler error because `FarmStageMinigameDefinition` declared both a public property and a static factory method named `Sequence`.
- Failing evidence: `Assets/_Project/Scripts/Core/Farming/CropLifecycleProfile.cs(127,51): error CS0102: The type 'FarmStageMinigameDefinition' already contains a definition for 'Sequence'`.
- Approach that produced the miss: The new minigame API introduced a `Sequence` collection property and a `Sequence(...)` factory on the same type, and the verification pass relied on grep and file review rather than a full compile.
- Why it was mistaken for done: The handoff correctly noted that Unity compile execution was blocked, but it still failed to run a same-type public API collision scan on the newly introduced Core type.
- What should have been verified or stated differently: Any new or heavily changed public C# type needs a quick member-name collision scan before handoff when the compiler cannot run. The summary should not imply compile confidence beyond that static pass.
- Prevention rules for future work:
  - Do a same-type member-name collision scan on every new or changed public C# API before handoff when compile verification is blocked.
  - Avoid naming a public collection property and a public factory method with the same identifier.
  - Prefer explicit collection-property names such as `InputSequence` when the factory method keeps the domain name `Sequence(...)`.
- Follow-up actions: Renamed the exposed minigame input-list property to `InputSequence`, updated runtime and overlay call sites, and added an EditMode regression test that proves the sequence factory preserves the intended ordered inputs.
- Distilled rule added to project-memory.md: `C# API Member Name Collision Scan (2026-04-11)`

### Scene 7 Plant Prompt Missing After Minigame Rewrite (2026-04-11)
- Status: Addressed
- Related story/task: Scene 7 tomato task-driven minigame lifecycle
- Original completion claim: The Scene 7 tomato task-driven prompt and minigame flow was handed off as using a single contextual primary interaction from planting through harvest.
- Reported issue: The developer reported that the "Press E to plant" prompt is not appearing.
- Failing evidence: Developer report after live scene use: "the press e to plant stuff isnt comint uo".
- Approach that produced the miss: The tutorial interaction path was reworked around contextual prompts, but `TutorialFarmSceneController` only configured the hero plot once in `Start`, before `FarmSimDriver.Start` had initialized `CropPlotController.State`.
- Why it was mistaken for done: The handoff assumed the hero plot would already have runtime state by the time the tutorial scene controller performed its one-shot setup, and the verification never simulated or guarded against sibling `Start` ordering on the scene bootstrap object.
- What should have been verified or stated differently: The first visible plant prompt from the initial tutorial soil state needed an explicit check that tutorial lifecycle configuration still happens when plot state is initialized after the scene controller's first setup attempt.
- Prevention rules for future work:
  - For guided interaction rewrites, verify the very first available player action and prompt in the live loop, not only later progression steps.
  - Add or maintain targeted coverage for the plant-step prompt whenever tutorial prompt plumbing changes.
  - Do not rely on sibling MonoBehaviour `Start` order for tutorial wiring that needs driver-created runtime state.
  - If prompt or mission code depends on scene bootstrap wiring, add an idempotent `Ensure...Configured()` path that can run before prompts are built.
- Follow-up actions: Added `EnsureHeroPlotConfigured()` to `TutorialFarmSceneController`, retried hero-plot lifecycle wiring during update and before prompt/mission reads, updated `FarmPlotInteractionController` to force that configuration before building the prompt, and added an EditMode regression test that simulates late crop-state initialization.
- Distilled rule added to project-memory.md: `Tutorial Scene Wiring Must Not Depend On Start Order (2026-04-11)`
