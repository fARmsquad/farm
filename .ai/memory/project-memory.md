# Project Memory — FarmSim VR

> **This is the shared knowledge base for ALL agents (Claude Code, Codex, Cursor).**
> Both agents MUST read this on startup and append to it when learning something new.
> If you discover a pattern, antipattern, gotcha, or make an architecture decision,
> write it here so the next agent (or the next session) doesn't repeat the mistake.
> Detailed post-"done" misses live in `.ai/memory/completion-learnings.md`.
> When a developer comes back with an issue after a completion claim, log the full
> case there first, then copy the durable rule back here.

---

## Architecture Decisions (ADRs)

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-06 | Pure C# Core + thin MonoBehaviour wrappers | Testability: Core/ has zero UnityEngine refs, can unit-test without editor |
| 2026-04-06 | Target Quest 2 + Quest 3, Unity 6 LTS, URP | Hardware floor = Quest 2; URP for mobile perf; Unity 6 for latest XR toolkit |
| 2026-04-06 | Assembly definitions enforce boundaries | `FarmSimVR.Core` (no engine refs), `FarmSimVR.MonoBehaviours`, `FarmSimVR.Interfaces` (no engine refs), `FarmSimVR.Editor` |
| 2026-04-06 | New Input System only (legacy disabled) | `activeInputHandler: 1` — `UnityEngine.Input` returns 0/false always |
| 2026-04-07 | Codex gets Unity MCP parity via relay binary | Same relay, same tools as CC. Both agents can manipulate scenes. |
| 2026-04-16 | Generative cutscene art style fixed via Gemini reference anchors + `StylePresetCatalog` (no in-engine 3D pivot) | Phase A brainstorming decision: minimal change to runtime pipeline; anchor the 2D storyboard look via reference images + prompt suffix instead of re-authoring cutscenes in 3D |
| 2026-04-16 | `style_preset_id` stays on `GeneratedStoryboardCutsceneRequest` (status quo plumbing) rather than moving to `StoryTypeDefinition` | Minimum churn; preserves backwards compat with existing Unity client; orphan-field smell traded for fewer migration surfaces |

---

## Established Patterns (DO THIS)

<!-- Append new patterns as you discover what works well -->

### Script Organization
- MonoBehaviours go in `Assets/_Project/Scripts/MonoBehaviours/`
- Pure C# core logic goes in `Assets/_Project/Scripts/Core/`
- Editor scripts go in `Assets/_Project/Scripts/Editor/`
- Interfaces go in `Assets/_Project/Scripts/Interfaces/`

### MCP Workflow
- Write ALL `.cs` files first, then wait for domain reload, then do scene wiring
- Poll `Unity_ManageEditor(Action: "GetState")` until `IsCompiling: false` before MCP calls
- For complex scene setup, prefer Editor menu item scripts over individual MCP calls
- `Unity_RunCommand` can't reference project types — use reflection or SerializedObject

### Asset Path Verification
- Before referencing ANY third-party prefab/model/texture in code or scene assembly:
  1. `FindProjectAssets(query: "descriptive name")` or glob `Assets/**/*keyword*`
  2. Use the EXACT path returned — don't construct paths from naming conventions
  3. If multiple matches, pick the best one and log the choice in the commit message
- This applies to Synty packs, Unity starter assets, Asset Store downloads, etc.

### Crop Prefab Selection
- For player-reachable crop plots, prefer the `a`-suffix interactive prefab variants whenever the pack provides them.
- Treat the visible fruit/root material slot as the clearest produce-emergence signal: tomato fruit appears at `Tomato_02/_02a`, carrot root appears at `Carrot_04`, and corn produce appears at `Corn_06`.
- Treat `TomatoStick_01` as a separate support prop, not as a lifecycle replacement mesh.
- Wheat uses paired seeds and `a` alternates as density variants, not as a single strictly linear ladder.

### Testing
- EditMode tests for Core/ pure C# logic
- PlayMode tests for MonoBehaviour integration
- Every public method in Core/ must have a test

### Scene Handoffs
- Use the stable numbered scene map in `.ai/docs/scene-work-map.md` when splitting scene-scoped work across agents or teammates.
- Runtime scene labels and shared scene numbering come from `SceneWorkCatalog`; do not invent ad hoc scene numbers in prompts or docs.

### Tutorial Entry Points
- Title-screen launchers, play-mode start-scene helpers, and scripted Build Settings updates for the tutorial should all read from `SceneWorkCatalog` instead of maintaining separate hardcoded scene lists. If the tutorial sequence changes, update the catalog once and let the editor/runtime entry points follow it.
- Title-screen launchers must resolve canonical tutorial aliases to build-loadable scene names through `SceneWorkCatalog`; routing names like `PostChickenCutscene` are not always the same as the actual scene asset names in the build profile.
- Generative Story Orchestrator progress should keep updating the standing `Generative Story Slice` on the title screen, backed by `StoryPackage_IntroChickenSample`, instead of creating a new ad hoc sample slice for each increment.
- The standing `Generative Story Slice` should launch the current generated proof beat (`PostChickenCutscene` right now), not the authored `Intro` Timeline, unless a spec explicitly calls for an end-to-end intro regression path.
- The standing generated title-screen slice now has separate prepare and play states: `Generate Unique Playthrough` keeps the title screen live and invalidates stale prepared runs, while `Play Unique Playthrough` stays disabled until `StorySequenceRuntimeController` holds a prepared entry scene and active session. Authored scene launches should clear that prepared generated state; the play path must not clear it before the generated scene loads.
- Generated title-screen slices must reconcile button state from `StorySequenceRuntimeController.HasPreparedSequence`, not only from one callback path, so a prepared runtime session cannot leave `Play Unique Playthrough` disabled after the async prepare flow completes.
- The generated title-screen status block should only show package/beat details when the runtime controller actually holds a live generated session. Idle or failed states must not accidentally present the authored fallback package as if it were freshly generated output.
- Package-driven tutorial routing must distinguish between "no package beat exists" and "package beat exists with an empty `NextSceneName`". The latter is an explicit terminal slice boundary and must not fall back into the authored tutorial flow.
- LLM-directed story turns must stay bounded by the minigame generator catalog and active character pool. Schema-valid OpenAI output can choose among those bounded options, but any invalid generator, invalid character, malformed payload, or provider failure must drop all the way back to the local rule-selected turn instead of partially reusing the bad directive.
- Player-facing title-screen UI must not be hidden behind `UNITY_EDITOR` or `DEVELOPMENT_BUILD` guards unless the spec explicitly marks it as debug-only.
- Generated entry cutscenes must not keep installer bypasses for authored slideshow/timeline content once they become runtime package beats; the installer has to bind the runtime storyboard and disable the old authored bridge objects.
- Local AI backends required for first-party dev flows must have a Unity-owned bootstrap path, not a hidden manual terminal prerequisite.

### Scene Art Review
- When the developer is still choosing the look of a mechanic, place curated asset options directly in the scene and get a pick before adding more lifecycle-specific logic or polish.

### Generative Cutscene Style
- `style_preset_id` is the authoritative key across the whole pipeline. `StylePresetCatalog.default()` (`backend/story-orchestrator/app/style_preset_catalog.py`) owns per-preset descriptor text (injected into planner system prompt + user prompt JSON), `image_prompt_suffix` (appended to every shot's image_prompt), and `preferred_provider` (used by the B3 router later). Don't invent parallel style mechanisms.
- `StoryboardReferenceLibrary.references.json` stores `stored_path` **relative** to the library root (e.g., `assets/<uuid>.png`). Resolve at read time via `library.resolve_stored_path(stored_path)` — which handles both the new relative form and any legacy absolute paths. Never commit absolute paths back into the manifest.
- Style anchor PNGs imported via `scripts/seed_style_reference_library.py` must be downscaled to ≤768px max edge before manifest import. Gemini embeds anchors base64 in every call; unconstrained anchor sizes produce 30MB+ request payloads and hit the Quest perf budget indirectly via latency.
- Style anchors and continuity references have **separate budgets**: `max_style_anchors` is additive on top of `max_reference_images` in `StoryboardReferencePathResolver.resolve_paths`. Don't let them share one budget — they serve orthogonal purposes (style teaches visual look; continuity refs anchor character/world identity).

### Title Screen Buttons
- Sibling buttons intended to ship (player-facing) are authored via `Assets/_Project/Editor/CreateTitleScene.cs` so they live in saved `TitleScreen.unity` YAML. Do NOT add them via runtime code in `TitleScreenManager.CreateTutorialSliceLauncher()` — that method is the runtime-only dev launcher panel and its children never persist to the scene asset.
- Regenerate the title scene via Unity batch-mode: `/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "$(pwd)" -executeMethod FarmSimVR.Editor.CreateTitleScene.Create`. This same code path runs in CI and local dev — no "works on my machine" drift from manual menu clicks.

### Integration-Boundary Assertions
- Any Pydantic field that flows through N intermediate layers before emerging in a contract boundary (e.g., `GeneratedStoryboardContext` → `GeneratedStoryboardCutsceneRequest` → planner user_prompt → envelope `cutscene`) needs an end-to-end test that asserts the *value* survives round-trip. Unit tests at each layer miss silent-drop bugs like `_resolve_request_context()` rebuilding the model from scratch and dropping the new field.
- When a hardcoded default determines which catalog entry / anchor set / preset a runtime uses, pin its *live-running value* in a test. Phase A's entire prompt chain was inert at runtime because `runtime_turn_generation.py` hardcoded `style_preset_id="farm_storybook_v1"` and every unit test explicitly set `watercolor_intro_v1` — the production default was never asserted.

### Git
- Feature branches: `feature/<story-id>-<slug>`
- Commit prefix: `[cc]` for Claude Code, `[codex]` for Codex
- No force push. No `--no-verify`. Remove LFS `pre-push` hook if push fails.

---

## Antipatterns (DON'T DO THIS)

<!-- Append antipatterns as you discover what breaks or wastes time -->

### Code
- DON'T put `UnityEngine.Vector3` or any engine type in Core/ or Interfaces/ assemblies
- DON'T use `Debug.Log` in committed code (use conditional compilation)
- DON'T use legacy Input API (`Input.GetKey`, `Input.GetAxis`) — it's dead
- DON'T leave `TODO`/`FIXME` in merge-ready code

### Asset Paths
- DON'T hardcode prefab/asset paths without verifying the actual filename first
- DON'T assume asset pack naming conventions (Synty, etc.) — always glob or `FindProjectAssets`
- DON'T trust spec/plan paths for third-party assets — verify at implementation time

### MCP
- DON'T call MCP scene tools immediately after writing `.cs` files — domain reload will disconnect
- DON'T launch a separate relay process — the Unity editor manages its own relay
- DON'T assume MCP is available — always check editor state first

### Git
- DON'T modify files another agent has locked in `flight-board.json`
- DON'T commit `.ai/memory/session-memory.md` (it's ephemeral)

### Harness
- DON'T let repo-wide legacy debt block every branch. Preflight checks should validate the branch delta, not fail forever because `main` already contains oversized scripts or unchecked planning specs.

### Completion Claims
- DON'T say work is fully done without stating what was directly verified vs what was assumed
- DON'T treat a developer-reported post-"done" issue as routine feedback — log it as a completion learning and update the guardrail
- DON'T assume backend consumers will receive the raw `ResolvedParameters` dictionary. Unity-facing story packages may only carry `ResolvedParameterEntries`, so cross-runtime readers need to support that contract shape too.
- DON'T set a Unity-side timeout for a local proxy call lower than the backend's own upstream provider timeout when Town voice depends on that proxy.
- DON'T present approval in an outbound-content UI as effectively published unless the publish step is actually triggered and visibly tracked.
- DON'T hand off an X write flow based on stale OAuth assumptions. Verify the exact live auth mode and token family that the production publisher will use.
- DON'T let a standing generated slice silently fall back into stale packaged content or publish visibly degraded storyboard frames after a live bootstrap failure.
- DON'T treat `turn.result.is_valid == true` as sufficient for generated-story completion. The storyboard planner must still receive prior context and produce conflict-driven, non-repeating shots.
- DON'T let request-normalization code rebuild `GeneratedStoryboardContext` without copying the extra narrative fields used by prompt construction.
- DON'T hand off a generated-slice fail-closed change without one healthy end-to-end live launch against a running local orchestrator.
- DON'T let an async title-screen launch set transition state unless the underlying request actually started, and don't leave long-running launches without an obvious loading state.
- DON'T keep a cached local orchestrator base URL after a fresh readiness check fails; clear stale readiness immediately so later callers cannot reuse a dead localhost endpoint.
- DON'T hand off a generated-slice backend fix with only `/health` evidence; prove at least one live `next-turn` returns `is_valid=true`.
- DON'T make generated cutscene validity depend only on remote image providers if the local quality gate rejects placeholder fallback; keep a deterministic local image path available.
- DON'T let story-sequence execution rediscover endpoints after readiness already resolved the active backend, and don't attempt local bootstrap for non-local hosts.
- DON'T add a new field to `GeneratedStoryboardContext` (or any Pydantic model reconstructed downstream) without also updating `_resolve_request_context()` in `generated_storyboards.py`. That function rebuilds the context from scratch field-by-field and silently drops anything not explicitly re-listed.
- DON'T declare a Pydantic model `extra="forbid"` and then let a separate WIP pass new fields to it without updating both sides in the same commit. The field will be accepted by static type checks and `git diff` but Pydantic will reject it at runtime, producing deterministic-looking integration test failures that masquerade as unrelated regressions.
- DON'T leave a hardcoded default in a runtime pipeline's request-construction site pointing to a preset/catalog-key that has no matching entries in the downstream catalog or anchor library. The pipeline will silently no-op — unit tests won't catch it because they set the field explicitly.

---

## Tech Debt Log

<!-- Track known debt so agents can opportunistically fix it -->

| Date | Item | Severity | Notes |
|------|------|----------|-------|
| 2026-04-06 | git-lfs not installed locally | Low | LFS hooks error but non-blocking; remove pre-push hook to push |
| 2026-04-06 | TagManager.asset recurring error | Ignore | Pre-existing, harmless — don't try to fix |
| 2026-04-11 | `com.gamelovers.mcp-unity` is embedded under `Packages/` | Low | Embedded from `Library/PackageCache` without `Server~/node_modules` so the missing `Editor/Tests/GetGameObjectResourceTests.cs.meta` can be fixed in-repo and the package can be tracked in source control |

---

## Lessons Learned

<!-- Append hard-won lessons from debugging sessions, failed approaches, etc. -->

### Asset Naming (2026-04-07)
**Synty asset packs use inconsistent naming.** A plan assumed prefab names
like `SM_Prop_HayBale_01` but the actual Synty file was
`SM_Prop_Hay_Bale_Square_01` (extra underscores between words). Similarly
`SM_Prop_LetterBox` vs `SM_Prop_Letterbox` (capitalization difference).
**Rule: Always `FindProjectAssets` or glob for the actual filename before
hardcoding any prefab/asset path in code, specs, or scene assembly.** Never
assume naming conventions from asset packs — verify every path.

### Preflight Scope (2026-04-10)
The repo-root `run-tests.sh` and `preflight.sh` scripts should delegate to the
real harness scripts under `.ai/scripts/`. Also, preflight quality/spec gates
need to evaluate the branch delta instead of the whole repo because current
`main` already contains legacy oversized scripts and planning specs with
unchecked acceptance boxes.

### UI Input Modules (2026-04-11)
When `ProjectSettings.asset` has `activeInputHandler: 1`, any scene or editor
builder that creates an `EventSystem` must use `InputSystemUIInputModule`, not
`StandaloneInputModule`. The legacy module calls `UnityEngine.Input` and will
throw `InvalidOperationException` every frame in play mode once the old input
manager is disabled.

### Completion Claims (2026-04-11)
When the developer reports an issue after an agent said work was done, capture
the full miss in `.ai/memory/completion-learnings.md` before fixing it. The
entry must explain the original claim, the failing behavior, the approach that
produced it, why verification missed it, and what future work must verify or
phrase differently. Completion summaries must also separate verified facts from
unverified assumptions so "done" has a clear boundary.

### Ghost Field Wiring (Phase A, 2026-04-16)
Phase A's whole point was to activate `style_preset_id` across the planner and
Gemini reference resolver. Exploration found the field threaded from
`RuntimeSessionCreateRequest` → `GeneratedStoryboardCutsceneRequest` → image
generator call but never consumed by any prompt. That diagnosis was partly
right: the field existed in `GeneratedStoryboardCutsceneRequest` and was
ignored there. But **`RuntimeSessionCreateRequest` didn't carry it at all**
and `runtime_turn_generation.py:310` hardcoded `"farm_storybook_v1"` — a
preset with no catalog entry and no matching anchors. A1–A9 wired the field
correctly, but because the hardcoded default never matched what Phase A
seeded, the whole chain was inert at runtime. Caught only because the A10
eval harness asserted the field's end-to-end value.

**Rule:** When "wiring a ghost field," audit the full path including every
intermediate **hardcoded default**. Any hardcoded value that names a catalog
key, preset id, generator id, or similar lookup token must be listed in the
relevant catalog/library, or it will silently no-op in production even with
fully green unit tests.

### GSO-026 Completion Interlock (2026-04-16)
Phase A was blocked partway through by 10 `test_runtime_api.py` failures
caused by uncommitted GSO-026 WIP. The WIP added construction-site code that
passed four new fields (`story_purposes`, `story_hook_template`,
`llm_prompt_directives`, `parameter_prompt_hints`) to
`StorySequenceGeneratorOption`, but the model itself (declared
`extra="forbid"`) was never updated to accept them. Every runtime end-to-end
test that exercised the new code path failed with a Pydantic
`extra_forbidden` validation error. Parallel subagents running full-suite
tests gave inconsistent counts because the latent failure was deterministic
but I hadn't run the suite myself to establish ground truth.

**Rule:** Before dispatching parallel subagents on any codebase with
uncommitted WIP, run the full test suite in the main session first to
establish a known baseline. Two parallel "no regressions" reports cannot be
reconciled against each other without external ground truth.

### Town Voice Token Proxy Budgets (2026-04-14)
When Unity asks the local story-orchestrator to mint a Town voice token, the
client timeout and retry policy must account for the backend still needing to
call ElevenLabs. A Unity timeout shorter than the backend provider timeout will
create intermittent text-only fallback even when the happy path works. Keep the
runtime proxy budget at or above the backend's upstream budget, or retry
transient transport/5xx failures explicitly.

### Remote Story Orchestrator Runtime Contract (2026-04-15)
When the generated-story backend is deployed remotely, Unity should prefer the
stable deployed URL before legacy localhost ports, request execution must stay
pinned to the URL that readiness already proved healthy, and the launcher must
never try to start a "local" backend for a non-local host. On the backend,
deterministic `local-reference-remix` images are part of the valid deployed
fallback chain and must remain acceptable to the quality gate, while remote
provider timeouts should stay bounded enough for the Unity request budget.

### Bounded Generated Sessions (2026-04-15)
Keep the primary generated-playthrough contract bounded before attempting
infinite continuation. The current production proof path should persist and
resume a three-turn `cutscene -> minigame` session, complete cleanly after the
third outcome, and avoid reviving completed runs on title-screen restart.

### Gemini Image Defaults (2026-04-15)
Prefer the stable Gemini image model first in production (`gemini-2.5-flash-image`)
and do not rely on preview-model fallback by default. When rejecting generated
images for lower-third caption panels, validate the heuristic against real
provider outputs so dark foreground composition is not misclassified as trash.

### Global Codex Unity Skills (2026-04-11)
When this repo wires external Codex Unity skills into `.ai` workflows, keep
the repo-local `.ai/skills/` files as the authoritative process contract and
document the external skills in `.ai/CODEX_SKILLS.md` plus the exact workflow
entry points that should invoke them. Treat global skills as augmentations, not
hidden requirements. Current Codex-installed Unity skills are
`unity-mcp-orchestrator`, `unity-developer`, `unity-ecs-patterns`, and
`unity-profiler`; `unity-initial-setup` was requested but not exposed as a
Codex-installable skill in either `IvanMurzak/Unity-MCP` or
`devwinsoft/devian` at install time, so workflows must not assume it exists.

### Namespace Imports After Cross-Assembly Wiring (2026-04-11)
When a C# change introduces new references across `Core`, `MonoBehaviours`,
`Editor`, or sibling MonoBehaviour namespaces, do a compile-oriented scan of
every edited file for missing `using` directives before handoff. If Unity
compile/test execution is blocked, do not imply compile confidence beyond what
that static import check actually proved.

### New C# Files Need EOF Structure Checks (2026-04-11)
When creating a brand-new C# file, do a final top-to-bottom structural pass and
confirm the file closes every namespace, type, and method correctly. If Unity
cannot run a fresh compile, this EOF brace/structure check is mandatory before
handoff because parser-level mistakes are otherwise easy to miss.

### C# API Member Name Collision Scan (2026-04-11)
When adding or changing public C# API types, do a same-type member-name
collision scan before handoff, especially if a factory method shares a domain
noun with a data property. Prefer explicit collection property names such as
`InputSequence` when the factory method is named `Sequence(...)`.

### Tutorial Scene Wiring Must Not Depend On Start Order (2026-04-11)
When a tutorial scene depends on runtime-initialized plot state or other data
that another MonoBehaviour sets up in `Start`, do not perform one-shot
configuration only once in `Start`. Retry or ensure the configuration on
demand before prompt building or mission reads so the first interaction
affordance does not vanish because Unity called sibling `Start` methods in an
unexpected order.

### Tutorial State Progression Must Match Player Actions (2026-04-11)
For guided tutorial scenes, do not leave hidden autoplay loops that advance the
core mechanic after the first valid input unless that autoplay behavior is part
of the explicit spec. Verify the mission state, allowed prompt actions, and
visible world state together in the real player loop so the scene never asks
for a later action while the plot still looks wrong or idle.

### Persistent Tutorial Props Are Not Lifecycle Stages (2026-04-11)
When a scene uses support props such as the tomato plank/stake, model them as
persistent visuals that stay active across a phase range instead of forcing
them into the mutually exclusive crop lifecycle mapping. Verify persistent
props and lifecycle-swapped crop meshes separately so the scene does not drop a
support asset or skip an intermediate fruit stage.

### Stage Art Options In-Scene Before Encoding More Lifecycle Logic (2026-04-11)
When a mechanic still feels wrong to the developer at the art direction level,
stop extending lifecycle logic and place a small, reviewable set of candidate
base assets directly in the scene first. Use the chosen base as the anchor for
later sequencing work instead of trying to reason the final look out in code.

### Selection Slice Names Must Match The Current Review Target (2026-04-11)
When building a new title-screen or in-scene review slice during visual tuning,
name and populate the slice after the most recent requested review subject
(soil base, crop state, tomato stage, planter style, and so on), not whichever
earlier asset family happened to be under discussion first.

### Unity Scene Merges Need Missing-Script Sweeps (2026-04-13)
After pulling, rebasing, or replaying stashed Unity scene changes, scan the
touched scenes for stale script GUIDs and duplicate controller blocks before
assuming runtime behavior is preserved. Scene YAML can keep a missing script on
a controller root even when the live C# feature code still exists, and that
kind of merge artifact is easy to miss in source-only review.

### Tutorial Plots Need Unique Names And Spawn-Visible Placement (2026-04-13)
For guided farm scenes, give the special tutorial plot a unique scene name and
place it where it is immediately visible and reachable from the spawn read.
Serialized references alone are not enough; the player must be able to spot the
target patch without hunting through the field.

### Wrapper Prefabs Must Not Depend On Missing Second-Hop Sources (2026-04-13)
When a scene references third-party wrapper prefabs, verify that the wrapper is
itself a healthy standalone asset or that every nested `m_SourcePrefab` target
still exists locally. A `.prefab` file can be present while its import chain is
broken, which will only surface when Unity tries to open the scene.

### Unity Harnesses Need Editor-Lock Fallbacks (2026-04-13)
If a local verification command is expected to run while the Unity editor is

### OpenAI Credentials Must Stay Out Of Scene Data (2026-04-13)
For Town and other live LLM scenes, resolve OpenAI credentials from environment
first and do not leave API keys serialized into Unity scene or prefab YAML.
Add a configuration regression test when a runtime can read both inspector and
environment values so stale local overrides cannot silently win.
already open, the harness should provide a safe fallback path instead of
stopping at the project lock. For batchmode test runs, a disposable copy of the
current on-disk project state is preferable to a skip-only response.

### Cloned Unity Batch Tests Should Own Result Writing (2026-04-13)
When EditMode or PlayMode tests run against a disposable cloned project, do not
assume Unity's built-in `-runTests` CLI path will emit the XML report
reliably. Prefer a repo-owned `-executeMethod` runner that calls
`TestRunnerApi`, saves the NUnit XML explicitly, and only then exits.

### Town Streaming Needs Final-Payload Guardrails (2026-04-13)
When Town NPC dialogue is described as "streaming correctly," verification must
cover both layers of the turn: incremental text deltas during generation and
final payload normalization after completion. A live delta path can still end
with raw JSON on screen if the compatibility parser fails. Add a regression
test for legacy JSON payloads and keep a golden-set eval that rejects raw JSON,
option arrays, code fences, or other non-spoken output formats.

### Responses API History Must Cover Assistant Turns (2026-04-13)
When manually serializing conversation state for the Responses API, verify a
history that includes at least one assistant turn. Text-only history can use
plain string `content`, and that path is safer than hand-building typed content
arrays unless the feature actually needs multimodal items. A request builder
that only tests user messages can still fail at runtime once assistant history
is appended on the second turn.

### Town Dialogue Option Cadence Needs Its Own Guardrail (2026-04-13)
For the Town slice, do not treat button presence as enough. Verify when the
generic fallback buttons appear, when scripted four-choice ladders appear, and
when `Goodbye.` becomes available. A streamed-text fix can still feel wrong if
opening turns fall back to `Continue...`/`Goodbye.` before the intended
conversation ladder takes over.

### Town Dialogue Choices Must Follow The Latest Reply (2026-04-13)
When Town NPC follow-up options are derived locally, do not drive them only
from turn index or a canned ladder. Compose the visible prompts from the
latest parsed reply first so cooking lines yield cooking follow-ups, town
history lines yield story or town prompts, and cadence rules like late
`Goodbye.` unlocks only shape the final set after that response-aware pass.

### Storyboard Controllers Need Unity-Safe Component Bootstrap (2026-04-13)
When a storyboard or cutscene controller lazily creates required runtime
components such as `AudioSource`, resolve them through explicit Unity null
checks before caching or using them. Do not use `GetComponent<T>() ?? AddComponent<T>()`
for Unity objects on player-facing bootstrap paths, because Unity's custom null
semantics can leave you with an invalid component reference that only fails once
playback begins.

### Tutorial Scene Loads Must Resolve Canonical Aliases Before Runtime Transition (2026-04-13)
When tutorial scenes, story-package beats, or generic scene-loading helpers
transition by canonical names such as `MidpointPlaceholder`, resolve them
through `SceneWorkCatalog.GetLoadableSceneName()` before calling
`SceneManager.LoadScene`. Fixing one entry path is not enough; title launchers,
flow controllers, autoplay bridges, and generic loaders all need the same
mapping or aliased bridge scenes will fail at runtime.

### Town Local Backend Integrations Must Not Depend On One Hardcoded Localhost Port (2026-04-14)
When a Unity scene depends on a local backend such as the story-orchestrator,
do not assume a single serialized localhost port will always own that service.
Support an explicit override plus a bounded local fallback path so features like
Town voice token minting still resolve when the backend is moved off the
default dev port.

### Generated Minigame Variants Should Tune Existing Scenes Before Forking Them (2026-04-14)
When the story package asks for a minigame variation, prefer bounded runtime
parameter consumption inside the existing scene controller and manager before
creating a new scene variant. Use safe preset IDs and pure mission services to
carry generated state, and make the config application resilient to Unity
`Start` ordering so the standing `Generative Story Slice` can keep evolving on
one stable test surface.

### Standalone Portal Scenes Must Bootstrap Their Own Runtime (2026-04-14)
If a scene contains `PortalTrigger` components and can be opened directly from
the title screen or editor, do not assume `CoreScene` already loaded the
`PortalManager`. Standalone portal scenes must self-bootstrap a transient
portal runtime or otherwise guarantee the manager exists before the first
trigger entry.

### Backend Bootstrap Failures Must Explain Local Setup Directly (2026-04-14)
When Unity owns startup for a local backend such as the story-orchestrator,
failure messages must point directly at the repo-local env file and venv setup
(`backend/story-orchestrator/.env.local`, `.venv`, `pip install -r requirements.txt`)
instead of only saying the service never became healthy. Do not nudge the
developer toward hardcoded Unity-side secrets.

### Generated Story Reliability Must Survive Focus Loss (2026-04-15)
If a player-facing dev flow depends on long-running local generation or network
requests, do not leave desktop Unity at `runInBackground: 0`. Keep the project
and runtime explicitly background-safe so `Generate Unique Playthrough` can
finish even when the developer clicks away to another app.

### Runtime Create-Session Must Be Truly Async (2026-04-15)
If the runtime API advertises `POST /api/runtime/v1/sessions` as "create a
session and return a job to poll," do not generate the first turn inline in
that request. The create route must return before generation completes, and
startup recovery replay must not consume the same worker lane that fresh live
requests depend on.

### Runtime Generated Playthrough Is Farm-Only For Now (2026-04-15)
The standalone `/api/runtime/v1/*` flow is temporarily constrained to the real
`FarmMain` planting gameplay. Keep runtime generator selection pinned to
`plant_rows_v1` until the non-farm adapters are intentionally brought back.
Current variety should come from bounded planting configuration changes such as
crop rotation, row count, target count, time limit, and assist level, not from
switching over to `FindToolsGame` or `ChickenChaseGame`.

### Generated Readiness Must Be Scoped To The Current Runtime Operation (2026-04-15)
Generated-playthrough readiness is not "any prepared turn exists." Only treat a
turn as playable when the current runtime operation has fully finished. Late
resume callbacks, persisted stale turns, or older async work must never
re-enable `Play Unique Playthrough` during a fresh generation run.

---

## Performance Budgets (Quest 2)

- Draw calls: < 100
- Triangles: < 750K
- Texture memory: < 256MB
- Frame time: < 11ms (90 FPS target)
- See `.ai/docs/quest-perf-budget.md` for full budget


### Approval State Must Expose Publish State (2026-04-14)
For outbound content systems, approval, queued-for-publish, and published are
separate operator states. The UI, automation, and handoff must make those
states explicit, and live verification must confirm that the intended approval
path actually reaches platform publication instead of only changing local DB
status.
