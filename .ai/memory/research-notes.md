# Research Notes — FarmSim VR

## How This File Works
Every feature and non-trivial task gets a research brief appended here
BEFORE the technical plan or implementation begins. This is populated by
the `.ai/skills/unity-research.md` skill using WebSearch + WebFetch.

Codex agents (which have no internet) read this file to access research findings.

## Index
<!-- Append entries here as: - [Feature/Task Name](#anchor) — date -->
- [Starter Tool Discovery & Ability Unlocks](#research-starter-tool-discovery--ability-unlocks) — 2026-04-10
- [Tutorial Title Screen Slice Launcher](#research-tutorial-title-screen-slice-launcher) — 2026-04-11
- [Horse Training Title Slice](#research-horse-training-title-slice) — 2026-04-11
- [Town NPC Text Streaming](#research-town-npc-text-streaming) — 2026-04-13
- [Town NPC Voice Streaming](#research-town-npc-voice-streaming) — 2026-04-13
- [Standing Slice Job Submission And Status](#research-standing-slice-job-submission-and-status) — 2026-04-13

---

## Investigations
(will accumulate as technical questions arise)

## Experiments
(will track proof-of-concept results)

---
<!-- Research briefs are appended below this line -->

## Research: Starter Tool Discovery & Ability Unlocks
**Date**: 2026-04-10
**Queries**:
- `site:docs.unity3d.com "Character Controller component reference" Unity`
- `site:docs.unity3d.com "Physics.Raycast" Unity`
- `site:docs.unity3d.com "Input System Quickstart Guide" Unity`

### Recommended Packages
| Package | Source | Why | Status |
|---------|--------|-----|--------|
| `com.unity.inputsystem` | Repo manifest + Unity docs | Already installed and sufficient for the current keyboard/mouse playable slice | In Use |
| `com.unity.xr.interaction.toolkit` | Unity Package Docs | Not required for this feature's current 3D scope; keep as a later follow-up if tool handling moves to XR props | Deferred |
| `com.unity.xr.openxr` | Unity Manual | Also deferred until the feature graduates from the current 3D build to headset-specific interaction | Deferred |

Current repo note: `Packages/manifest.json` already includes `com.unity.inputsystem`, and the playable world already uses a `CharacterController`-based `FirstPersonExplorer` plus raycast-driven `FarmPlotInteractionController`.

### Key Patterns Found
- Keep the current playable slice on `CharacterController` movement instead of Rigidbody-based player physics. Unity's manual explicitly positions `CharacterController` as the right fit for first-person control that should move cleanly without force-driven behavior. Source: https://docs.unity3d.com/Manual/class-CharacterController.html
- Use center-screen or cursor-driven raycasts with distance and layer filtering to determine what tool or plot the player is targeting. Unity's `Physics.Raycast` API supports max distance, layer masks, and hit details, which matches the repo's existing prompt controller pattern. Source: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
- Keep the input stack on the new Input System and formalize actions over time. Unity's Input System quickstart recommends project-wide action assets and grouped action maps, which fits a future cleanup of the current direct keyboard checks without changing feature scope right now. Source: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/QuickStartGuide.html

### Recommended Approach
Model tool collection as a pure Core unlock-and-gating system that exposes farming abilities (`PrepareSoil`, `WaterPlot`, `HarvestCrop`, and so on) to higher-level callers. For the current playable slice, tool discovery should plug into the existing first-person controller, a simple tool pickup prompt, and the current plot-action prompt flow rather than introducing equipped XR props.

This keeps the unlock sequence fully testable in EditMode, preserves the repo's "Core first, wrappers second" architecture, and gives the player a readable 3D farm loop now while leaving carried XR tool interactions for a separate later spec.

### Code Reference
```csharp
if (!toolUnlockState.HasAbility(FarmAbility.WaterPlot))
{
    return ToolGateResult.Blocked(ToolId.WateringCan, "Recover the watering can from the well.");
}

var result = wateringService.Apply(plotId, amount);
return ToolGateResult.FromAbilityResult(result);
```

### Gotchas & Pitfalls
- Do not put unlock checks in scene scripts only. If gating lives only in MonoBehaviours, debug/editor flows and save/load paths will drift from the real rules. Keep unlock truth in Core.
- Limit raycasts deliberately. Unity's `Physics.Raycast` docs emphasize `maxDistance` and `layerMask`; broad scene-wide casts will make tool focus noisy and fragile.
- Tune the existing `CharacterController` rather than replacing it. Unity's manual calls out skin width, min move distance, and step offset as the first controls to fix if the player gets stuck or jitters.
- Avoid input drift. The repo already uses direct `Keyboard.current` checks in places; if this feature adds more keys, document them centrally or move toward an action-map-backed layer instead of scattering bindings further.

### Current 3D Considerations
- Prefer focused raycasts from the camera center and a small interaction radius to keep prompts readable.
- Avoid large always-on HUD overlays; reuse the current prompt strip and lightweight world highlights.
- Keep tool feedback event-driven. No per-frame scene scans for rack hints or unlock effects.

### Sources
1. [Unity Manual: Character Controller component reference](https://docs.unity3d.com/Manual/class-CharacterController.html) — Supports the current first-person exploration model already present in the repo.
2. [Unity Scripting API: Physics.Raycast](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html) — Matches the current camera-to-plot targeting pattern and informs tool pickup focus.
3. [Unity Input System Quickstart Guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/QuickStartGuide.html) — Reinforces staying on the new Input System for the current 3D playable slice.

## Research: Tutorial Title Screen Slice Launcher
**Date**: 2026-04-11
**Queries**:
- `site:docs.unity3d.com SceneManager.LoadScene scene name build settings`
- `site:docs.unity3d.com EditorSceneManager playModeStartScene`
- `site:docs.unity3d.com EditorBuildSettingsScene EditorBuildSettings.scenes`

### Recommended Packages
No packages found — custom implementation required.

### Key Patterns Found
- Keep runtime scene launches on stable scene names already present in Build Settings. Unity's `SceneManager.LoadScene` accepts scene names or paths, but using one shared catalog avoids divergence between UI launchers and scene flow. Source: https://docs.unity3d.com/ru/current/ScriptReference/SceneManagement.SceneManager.LoadScene.html
- Use `EditorSceneManager.playModeStartScene` to force a predictable development entry scene when quick testing matters more than the currently open editor scene. Source: https://docs.unity3d.com/es/2021.1/ScriptReference/SceneManagement.EditorSceneManager-playModeStartScene.html
- Use `EditorBuildSettingsScene` plus `EditorBuildSettings.scenes` to script build-order updates from editor tooling instead of hand-maintaining the Build Settings list. Source: https://docs.unity3d.com/cn/2022.3/ScriptReference/EditorBuildSettingsScene.html and https://docs.unity3d.com/es/ScriptReference/EditorBuildSettings-scenes.html

### Recommended Approach
Keep the ordered tutorial scenes in one shared catalog that carries both scene
names and asset paths. Use that catalog to drive the title-screen slice
launcher, the editor play-mode start scene, and build-settings ordering so the
development launcher and the actual tutorial sequence cannot drift apart.

### Code Reference
```csharp
foreach (var slice in SceneWorkCatalog.TutorialOrderedScenes)
{
    AddButton(slice.NumberLabel, slice.SceneName);
    buildScenes.Add(slice.ScenePath);
}
```

### Gotchas & Pitfalls
- If scene names are duplicated or renamed without updating build settings, `LoadScene` can resolve the wrong scene or fail. Keep build-order updates scripted from the same catalog that feeds the launcher.
- A play-mode start scene that bypasses the title entry makes a slice launcher ineffective for editor testing. If the launcher is the intended dev entry, point `playModeStartScene` at the title scene.

### Quest/Mobile Considerations
- Keep the slice launcher editor/dev only. It is useful for iteration, but it
  should not become required runtime UI for headset players.

### Sources
1. [Unity Scripting API: SceneManager.LoadScene](https://docs.unity3d.com/ru/current/ScriptReference/SceneManagement.SceneManager.LoadScene.html) — Confirms scene-name/path loading behavior and the need for Build Settings alignment.
2. [Unity Scripting API: EditorSceneManager.playModeStartScene](https://docs.unity3d.com/es/2021.1/ScriptReference/SceneManagement.EditorSceneManager-playModeStartScene.html) — Confirms editor-time start-scene override is the supported way to standardize play-mode entry.
3. [Unity Scripting API: EditorBuildSettingsScene](https://docs.unity3d.com/cn/2022.3/ScriptReference/EditorBuildSettingsScene.html) — Confirms scripted build-scene entries are the supported automation path.
4. [Unity Scripting API: EditorBuildSettings.scenes](https://docs.unity3d.com/es/ScriptReference/EditorBuildSettings-scenes.html) — Confirms the build-scene list is writable from editor scripts.

## Research: Horse Training Title Slice
**Date**: 2026-04-11
**Queries**:
- `site:docs.unity3d.com SceneManager.LoadScene Unity 6`
- `site:docs.unity3d.com Character Controller component reference Unity 6`
- `site:docs.unity3d.com Collider.OnTriggerEnter Unity 6`

### Recommended Packages
No new packages found. The repo's current first-person + scene-loading stack is enough for a greybox horse-training slice.

### Key Patterns Found
- Keep the slice on the existing `CharacterController`-style first-person rig for grounded tutorial movement instead of introducing rigidbody locomotion for a one-scene prototype. Unity's manual explicitly positions `CharacterController` for first-person or third-person control that should not rely on rigidbody physics. Source: https://docs.unity3d.com/Manual/class-CharacterController.html
- Launch the slice by stable scene name or path that is already present in Build Settings. Unity's `SceneManager.LoadScene` resolves names from Build Settings and warns that duplicate names should use the full path to avoid ambiguity. Source: https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html
- Use trigger volumes to mark treat pickups, jump rails, and slalom checkpoints. Unity's `Collider.OnTriggerEnter` flow is the built-in pattern for overlap-driven gameplay events without requiring collision impulses. Source: https://docs.unity3d.com/ScriptReference/Collider.OnTriggerEnter.html

### Recommended Approach
Model the horse-training beat as a self-contained greybox scene launched from the title screen's shared scene catalog, but keep it out of the mandatory linear tutorial order. Put the sequence logic in a pure C# service that tracks storyboard phases (`Setup`, `GuidedWalk`, `Jumping`, `Slalom`, `Success`, `Failure`) and let a thin MonoBehaviour scene controller translate trigger hits and button presses into service calls.

This preserves the repo's Core-first architecture, gives the title screen a real launch target, and keeps the storyboarded experience readable without rewriting the existing intro-to-farm sequence.

### Gotchas & Pitfalls
- Do not add the horse slice by hardcoding one more title-screen button in isolation. The launch entry, build-settings inclusion, and scene metadata should come from the same shared catalog so the title screen does not drift from editor tooling.
- Do not put the training progression rules in the MonoBehaviour only. If failure rules, treat counts, or slalom balance live only in scene code, they will be hard to test and harder to tune.
- Avoid force-driven horse or player physics for this slice. The storyboard needs readable gates and failure states, not systemic simulation complexity.

### Sources
1. [Unity Manual: Character Controller component reference](https://docs.unity3d.com/Manual/class-CharacterController.html) — Supports staying on the current grounded first-person rig for the slice.
2. [Unity Scripting API: SceneManager.LoadScene](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html) — Supports title-screen launch wiring through a shared scene catalog and Build Settings.
3. [Unity Scripting API: Collider.OnTriggerEnter](https://docs.unity3d.com/ScriptReference/Collider.OnTriggerEnter.html) — Supports trigger-driven checkpoint and pickup interactions for the training course.

## Research: Town NPC Text Streaming
**Date**: 2026-04-13
**Queries**:
- `OpenAI Responses API streaming text events`
- `OpenAI Responses API structured outputs vs JSON mode`
- `OpenAI Responses API previous_response_id conversation state`
- `Unity DownloadHandlerScript ReceiveData UnityWebRequest`

### Recommended Packages
| Package | Source | Why | Status |
|---------|--------|-----|--------|
| `com.unity.modules.unitywebrequest` | `Packages/manifest.json` | Already installed HTTP transport for the Town scene client | In Use |
| `com.unity.nuget.newtonsoft-json` | `Packages/manifest.json` | Already installed JSON parser better suited than ad hoc string slicing for final turn validation | In Use |
| New OpenAI/streaming Unity package | Unity Registry/OpenUPM/GitHub package search | No clearly necessary package found for this narrow prototype slice; existing UnityWebRequest support is enough | Rejected for this slice |

No new packages found - custom implementation on top of the existing UnityWebRequest and JSON packages is recommended.

### Key Patterns Found
- OpenAI's Responses API exposes explicit streaming lifecycle events for text generation, including `response.created`, `response.output_text.delta`, `response.completed`, and `error`. This is a better fit for a streamed NPC utterance than polling a full raw response buffer. Source: https://developers.openai.com/api/docs/guides/streaming-responses
- OpenAI recommends the Responses API over the older Chat Completions API for new text generation work. Source: https://developers.openai.com/api/docs/guides/text#choosing-models-and-apis
- When structured data is needed, OpenAI recommends Structured Outputs over JSON mode because schema adherence is stronger. Source: https://developers.openai.com/api/docs/guides/structured-outputs#structured-outputs-vs-json-mode
- Unity's `DownloadHandlerScript` is the supported path when data must be handled incrementally as it arrives, and it can use a preallocated buffer to reduce garbage collection. Source: https://docs.unity3d.com/es/2017.4/Manual/UnityWebRequest-CreatingDownloadHandlers.html

### Recommended Approach
Use the Responses API for the visible NPC utterance stream, and parse server-sent events incrementally inside a dedicated Unity transport layer rather than in the UI. Treat the streamed text as one concern and any final structured turn payload as a second concern, so the Town scene never depends on partial JSON scraping to show words on screen.

For this slice, keep the Unity runtime thin: use the already installed UnityWebRequest and JSON packages, keep one in-flight request at a time, and maintain conversation state locally in a bounded history object instead of turning the Town scene into the long-term narrative backend.

### Code Reference
```csharp
request = BuildResponsesRequest(history, stream: true, store: false);

transport.BeginStream(
    request,
    onEvent: evt =>
    {
        switch (evt.Type)
        {
            case StreamEventType.TextDelta:
                conversation.AppendVisibleText(evt.TextDelta);
                break;
            case StreamEventType.Completed:
                conversation.FinalizeTurn(evt.FinalText, evt.ValidatedOptions);
                break;
            case StreamEventType.Error:
                conversation.FailTurn(evt.Message);
                break;
        }
    });
```

### Gotchas & Pitfalls
- Do not drive the visible UI by repeatedly scanning the entire accumulated raw stream for a `"response"` key. That produces avoidable allocations and makes the stream renderer depend on partial JSON syntax.
- If JSON mode is used anywhere in the final turn payload path, the app still has to handle incomplete or malformed JSON. Structured Outputs are preferred when the model must return a typed payload.
- `DownloadHandlerScript` callbacks run on Unity's main thread. Keep the parser lightweight and event-oriented; do not do heavy parsing or repeated full-buffer copies in `ReceiveData`.
- `previous_response_id` is available for threaded conversations, but stored responses persist by default. For this prototype slice, a bounded local history plus an explicit storage choice should be part of the design instead of an accidental side effect.

### Quest/Mobile Considerations
- Text streaming itself is cheap; the real risk is per-token allocation, string rebuilding, and UI churn. Use a preallocated receive buffer and append-only visible text updates.
- Keep only one active NPC stream at a time and cap local conversation history to a small fixed number of turns so request size does not grow without bound.
- Reuse the existing Town dialogue canvas and TMP widgets; this feature should not add new heavy world-space canvases or per-frame scene scans.

### Sources
1. [OpenAI Streaming API responses](https://developers.openai.com/api/docs/guides/streaming-responses) — Defines the streaming lifecycle events to consume for visible text.
2. [OpenAI Text generation: Choosing models and APIs](https://developers.openai.com/api/docs/guides/text#choosing-models-and-apis) — Recommends Responses over Chat Completions for new text-generation work.
3. [OpenAI Structured Outputs vs JSON mode](https://developers.openai.com/api/docs/guides/structured-outputs#structured-outputs-vs-json-mode) — Explains why schema-validated output is safer than raw JSON mode.

## Research: Town NPC Voice Streaming
**Date**: 2026-04-13
**Queries**:
- `ElevenLabs TTS websocket partial text input docs`
- `ElevenLabs single use token tts_websocket docs`
- `ElevenLabs list voices docs`

### Recommended Packages
| Package | Source | Why | Status |
|---------|--------|-----|--------|
| Built-in .NET WebSocket client | Unity / .NET runtime | Enough for a local prototype WebSocket client without adding another Unity package | In Use |
| Existing FastAPI backend scaffold | `backend/story-orchestrator` | Safe place to keep the ElevenLabs API key and mint single-use tokens | In Use |
| New Unity TTS package | Unity Registry/OpenUPM/GitHub package search | Not required for this slice; the official API already supports token-authenticated WebSocket streaming | Rejected for this slice |

### Key Patterns Found
- ElevenLabs' Text-to-Speech WebSocket is the official fit when text is streamed or generated in chunks. The docs explicitly call out partial text input as the target scenario. Source: https://elevenlabs.io/docs/api-reference/text-to-speech/v-1-text-to-speech-voice-id-stream-input
- ElevenLabs provides a single-use token endpoint that supports `tts_websocket`, returning a token that expires after 15 minutes and is consumed on use. This is the correct way to authenticate a frontend/client WebSocket session without exposing the account API key. Source: https://elevenlabs.io/docs/api-reference/tokens/create
- The WebSocket handshake accepts `single_use_token`, `model_id`, `output_format`, and `auto_mode` as query parameters, which is enough to keep Unity runtime configuration small and explicit. Source: https://elevenlabs.io/docs/api-reference/text-to-speech/v-1-text-to-speech-voice-id-stream-input
- The voices listing endpoint returns workspace-available voices and default/premade voices with voice IDs plus labels, which is enough to map Town NPCs onto stable voice profiles. Source: https://elevenlabs.io/docs/api-reference/voices/search

### Recommended Approach
Use the local FastAPI backend as a tiny token broker. Unity should request a
single-use `tts_websocket` token from the backend, then connect directly to
ElevenLabs' TTS WebSocket for the chosen NPC voice. Feed the socket with
sentence- or clause-sized chunks derived from the OpenAI text deltas, and play
the returned PCM audio incrementally through a streaming `AudioClip`.

This keeps the provider secret out of Unity while still letting the Town scene
voice start before the final line completes.

### Gotchas & Pitfalls
- Do not embed `xi-api-key` in Unity scene data or client-side code. The docs
  explicitly provide `single_use_token` for client initiation; use it.
- Do not send every raw token to ElevenLabs. Partial TTS is designed for chunked
  text, but phrase-level chunking is needed to avoid unstable audio and excess
  socket churn.
- Choose a PCM output format if Unity is going to play chunks incrementally.
  MP3 is easier for file-based playback but much more awkward for live chunk

## Research: One-Call Generated Package Assembly
**Date**: 2026-04-13
**Queries**:
- `FastAPI nested body models official docs`
- `FastAPI response_model official docs`
- `Pydantic BaseModel nested models official docs`

### Key Patterns Found
- FastAPI supports arbitrarily nested request bodies through Pydantic
  submodels, which is the cleanest fit for a single package-assembly request
  that contains both a minigame payload and a cutscene payload. Source:
  https://fastapi.tiangolo.com/tutorial/body-nested-models/
- FastAPI `response_model` should own response validation and filtering for
  composed outputs. That keeps the combined endpoint explicit about what the
  operator receives even if the handler internally builds the payload from
  multiple services. Source:
  https://fastapi.tiangolo.com/tutorial/response-model/
- The repo already has the right service split for this assembly step:
  `GeneratedMinigameBeatService` validates and writes adapter-ready minigame
  beats, and `GeneratedStoryboardService` can already derive cutscene context
  from a linked minigame beat. The missing layer is an orchestrator service
  that sequences them in one request.

### Recommended Approach
Add one backend orchestration layer that accepts a nested request with package
metadata, a minigame selection, and a cutscene request. Materialize the
minigame beat first. If that validation fails, return the structured failure
without attempting storyboard generation. If it succeeds, call the storyboard
service second and inject the linked minigame beat ID automatically.

This keeps the existing services single-purpose, removes the current operator
need to make two coordinated writes, and preserves deterministic package JSON
because both steps still write through the same package file contract.

### Gotchas & Pitfalls
- Do not duplicate package IDs or linked beat IDs across two separate operator
  calls when one orchestration request can own them centrally.
- Do not generate the cutscene first. The cutscene planner already depends on
  linked minigame context; ordering matters.
- Do not return an untyped merged `dict` from the endpoint. Keep a dedicated
  response model so FastAPI validates the composed result shape.

### Sources
1. [FastAPI: Body - Nested Models](https://fastapi.tiangolo.com/tutorial/body-nested-models/) — Confirms nested request models are a first-class pattern for one-call composition.
2. [FastAPI: Response Model - Return Type](https://fastapi.tiangolo.com/tutorial/response-model/) — Confirms `response_model` should validate and filter composed outputs.
3. Existing repo services under `backend/story-orchestrator/app/generated_minigames.py` and `backend/story-orchestrator/app/generated_storyboards.py` — Show that the package assembly gap is orchestration, not missing lower-level generation primitives.

## Research: Terminal Package Beats And Resource-Backed Cutscene Continuations
**Date**: 2026-04-13
**Queries**:
- `Unity Resources.Load Scripting API official`
- `Unity SceneManager.LoadScene Scripting API official`

### Key Patterns Found
- `Resources.Load` returns `null` when a resource path is not found, and the
  path must be relative to a `Resources` folder with the extension omitted.
  That matches the repo's current package-storyboard loading model and keeps
  beat-driven cutscene media deterministic at runtime. Source:
  https://docs.unity3d.com/es/current/ScriptReference/Resources.Load.html
- `SceneManager.LoadScene` loads by name or path from Build Settings, and if
  only a scene name is provided Unity will load the first matching scene. That
  reinforces the current repo rule to normalize canonical tutorial aliases into
  build-loadable scene names through `SceneWorkCatalog` before loading. Source:
  https://docs.unity3d.com/kr/560/ScriptReference/SceneManagement.SceneManager.LoadScene.html

### Recommended Approach
Treat the presence of a package beat as separate from whether its
`NextSceneName` is blank. A blank next-scene on an existing package beat should
mean "this slice ends here", not "fall back to tutorial flow defaults".

For the standing generated slice, add a real `Tutorial_PreFarmCutscene` package
beat and let that beat terminate the path explicitly. Keep media loading on the
existing resource-backed storyboard path instead of adding a new runtime
delivery mechanism.

### Gotchas & Pitfalls
- Do not use `string.IsNullOrWhiteSpace(nextScene)` as the only signal for
  whether a package beat has routing intent. That collapses two different
  states: "no package beat exists" and "package beat exists and ends here".
- Do not add a generated pre-farm cutscene beat without also fixing terminal
  flow semantics, or the standing slice will still loop back into `FarmMain`.
- Do not assume all linked storyboard beats are crop-driven. `find_tools`
  follow-up cutscenes need non-crop context handling in the backend planner.

### Sources
1. [Unity Scripting API: Resources.Load](https://docs.unity3d.com/es/current/ScriptReference/Resources.Load.html) — Confirms the runtime contract for loading resource-backed storyboard assets by relative path.
2. [Unity Scripting API: SceneManager.LoadScene](https://docs.unity3d.com/kr/560/ScriptReference/SceneManagement.SceneManager.LoadScene.html) — Confirms scene-loading behavior and supports the existing alias-to-build-name routing rule.

## Research: Standing Slice Regeneration Orchestrator
**Date**: 2026-04-13
**Queries**:
- `FastAPI nested models official docs`
- `FastAPI response_model official docs`
- `Python pathlib read_bytes write_bytes official docs`

### Key Patterns Found
- FastAPI nested models are the right fit for a composite regeneration request
  that contains multiple assembly legs under one typed body. Source:
  https://fastapi.tiangolo.com/tutorial/body-nested-models/
- FastAPI `response_model` should own response validation/filtering for the
  composed regeneration result just as it does for smaller endpoints. Source:
  https://fastapi.tiangolo.com/tutorial/response-model/
- Python `pathlib` documents `write_text()` and `write_bytes()` as normal file
  creation/update primitives. That makes `read_bytes()` / `write_bytes()`
  a reasonable lightweight snapshot-and-restore mechanism for package-manifest
  rollback in this local backend slice. Source:
  https://docs.python.org/uk/3.12/library/pathlib.html

### Recommended Approach
Add one regeneration service above `GeneratedPackageAssemblyService` that:
1. snapshots the current package manifest if it exists
2. runs the first assembly leg
3. runs the second assembly leg
4. restores the original manifest if a later leg fails

This keeps the orchestration layer small and useful right now. It gives the
developer one request that refreshes the standing generated intro slice without
manually sequencing multiple calls, and it avoids leaving the package manifest
half-updated if the second leg fails.

### Gotchas & Pitfalls
- Do not let the second leg mutate the package permanently if it fails
  validation; restore the previous manifest.
- Do not re-implement minigame or storyboard logic in the regeneration layer;
  compose the existing assembly service.
- Asset rollback is a separate concern from manifest rollback. For this slice,
  restore the package manifest first and leave asset garbage collection out of
  scope.

### Sources
1. [FastAPI: Body - Nested Models](https://fastapi.tiangolo.com/tutorial/body-nested-models/) — Supports a typed multi-leg request model.
2. [FastAPI: Response Model - Return Type](https://fastapi.tiangolo.com/tutorial/response-model/) — Supports a typed composed response model.
3. [Python pathlib docs](https://docs.python.org/uk/3.12/library/pathlib.html) — Supports snapshot/restore with path-based file reads and writes.

## Research: Standing Slice Job Submission And Status
**Date**: 2026-04-13
**Queries**:
- `Python sqlite3 connection context manager docs`
- `SQLite foreign_keys pragma docs`
- `FastAPI background tasks caveat docs`

### Key Patterns Found
- Python's `sqlite3.Connection` can act as a context manager that commits on
  success and rolls back on uncaught exceptions, which fits the current repo's
  small SQLite store pattern for job writes. Source:
  https://docs.python.org/3/library/sqlite3.html
- `sqlite3.Row` remains the recommended row factory when named column access is
  needed without bringing in an ORM. Source:
  https://docs.python.org/3/library/sqlite3.html
- SQLite foreign key constraints are disabled by default and must be enabled
  for each connection with `PRAGMA foreign_keys = ON`. Source:
  https://sqlite.org/foreignkeys.html
- FastAPI's own background-task guidance says heavier job systems should move
  to bigger tools later, but small same-process tasks are fine to keep local.
  That supports a synchronous job runner for this slice instead of adding a
  queue right now. Source:
  https://fastapi.tiangolo.com/tutorial/background-tasks/

### Recommended Approach
Add a small standing-slice job layer above the current regeneration service:
1. accept a typed job submission request using the existing
   `GeneratedStandingSliceRequest`
2. persist a job row plus per-step rows in SQLite
3. run the regeneration synchronously in-process
4. store the final job status, per-step outputs, and top-level result
5. expose a fetch endpoint so the operator can inspect one run after the fact

This gives the backend durable visibility without prematurely adding queues,
workers, or a full workflow runtime.

### Gotchas & Pitfalls
- Do not assume SQLite foreign keys are active; enable them on every
  connection before relying on child step rows.
- Do not hide all stage detail in one opaque `result_json`. Persist per-step
  outputs so the operator can tell whether the first leg completed and the
  second leg failed.
- Do not introduce FastAPI background tasks yet just because a job concept now
  exists. For this slice, synchronous execution keeps the state model easier to
  verify and debug.

### Sources
1. [Python sqlite3 docs](https://docs.python.org/3/library/sqlite3.html) — Supports connection context management and row-factory guidance for the store layer.
2. [SQLite foreign key support](https://sqlite.org/foreignkeys.html) — Confirms foreign keys must be enabled per connection.
3. [FastAPI Background Tasks](https://fastapi.tiangolo.com/tutorial/background-tasks/) — Supports keeping this slice same-process and lightweight before introducing a larger workflow system.
  decode in the runtime.
- Keep voice failures non-fatal. The Town text stream already works; audio
  should degrade away without taking the subtitle path down with it.

### Quest/Mobile Considerations
- `eleven_flash_v2_5` is the latency-biased choice for this slice, but the real
  mobile risk is Unity-side buffering and PCM queue starvation, not the network
  call itself.
- One active voice stream at a time matches the current Town conversation model
  and keeps memory/playback state simple.
- This remains an editor-first prototype until it is exercised on Quest hardware.

### Sources
1. [ElevenLabs WebSocket TTS](https://elevenlabs.io/docs/api-reference/text-to-speech/v-1-text-to-speech-voice-id-stream-input) — Official guidance for chunked partial-text speech generation.
2. [ElevenLabs Create Single Use Token](https://elevenlabs.io/docs/api-reference/tokens/create) — Defines the secure client token flow and the `tts_websocket` token type.
3. [ElevenLabs List Voices](https://elevenlabs.io/docs/api-reference/voices/search) — Supplies voice IDs and labels for profile mapping.
4. [ElevenLabs Changelog 2025-11-21](https://elevenlabs.io/docs/changelog/2025/11/21) — Confirms `tts_websocket` was added to the single-use token flow for secure TTS WebSocket sessions.
4. [OpenAI Conversation state](https://developers.openai.com/api/docs/guides/conversation-state#passing-context-from-the-previous-response) — Documents `previous_response_id` and the storage implications of threaded responses.
5. [Unity Manual: Creating DownloadHandlers](https://docs.unity3d.com/es/2017.4/Manual/UnityWebRequest-CreatingDownloadHandlers.html) — Documents `DownloadHandlerScript`, `ReceiveData`, and preallocated buffers for incremental network handling.

## Research: Data-Driven Minigame Generator Contracts
**Date**: 2026-04-13
**Queries**:
- `Unity ScriptableObject data architecture official`
- `Unity OnValidate official`
- `Pydantic validators official docs`

### Key Patterns Found
- Unity positions `ScriptableObject` as a way to separate reusable data from runtime scene objects. That maps well to a generator-definition catalog: authored bounds and tags live as data, while gameplay materialization stays elsewhere. Source: https://docs.unity3d.com/Manual/class-ScriptableObject.html
- Unity's `OnValidate` guidance reinforces that author-time data validation should sit close to the data container itself rather than be deferred to a later runtime failure. Source: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html
- Pydantic's validator model supports field-level and model-level validation, including context-aware checks, which is a good fit for bounded parameter schemas plus cross-field coupling rules. Source: https://pydantic.dev/docs/validation/latest/concepts/validators/

### Recommended Approach
Keep the first generator-definition slice in the backend as typed models plus a catalog/validator service. Validate defaults at model construction time, validate requested parameter values at selection time, and return structured errors plus fallback IDs instead of raising exceptions into the planner path.

For now, keep Unity consuming the already materialized story package shape. Do not couple the Unity runtime directly to the authoring-time generator schema until the planner starts emitting generator selections as part of beat planning.

### Sources
1. [Unity Manual: ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html) — Supports separating authored data from runtime scene logic.
2. [Unity Scripting API: OnValidate](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html) — Reinforces local validation near authored data.
3. [Pydantic Validators](https://pydantic.dev/docs/validation/latest/concepts/validators/) — Provides field/model validation patterns for bounded generator schemas.

## Research: Minigame Beat Materialization Contract
**Date**: 2026-04-13
**Queries**:
- `FastAPI response model official docs`
- `FastAPI body nested models official docs`
- `Pydantic model validators official docs`

### Key Patterns Found
- FastAPI documents response-model typing directly on path operations, including Pydantic models, lists, dictionaries, and scalar values, which fits a service returning either a fully materialized beat or a structured invalid result. Source: https://fastapi.tiangolo.com/tutorial/response-model/
- FastAPI explicitly supports arbitrarily nested request models through Pydantic, which matches a request body carrying package identity, generator selection, bounded parameters, and nested context. Source: https://fastapi.tiangolo.com/tutorial/body-nested-models/
- Pydantic supports both field-level and model-level validators, including whole-model checks after validation. That fits generator-definition integrity checks plus materialization-time parameter validation. Source: https://pydantic.dev/docs/validation/latest/concepts/validators/

### Recommended Approach
Keep materialization as a separate backend service that consumes the generator
catalog instead of folding it into the current storyboard service. Use nested
Pydantic models for request/result shapes, return a structured `is_valid`
result for invalid selections, and only write package JSON after catalog
validation succeeds.

### Sources
1. [FastAPI Response Model - Return Type](https://fastapi.tiangolo.com/tutorial/response-model/) — Supports typed response models for structured materialization results.
2. [FastAPI Body - Nested Models](https://fastapi.tiangolo.com/tutorial/body-nested-models/) — Supports nested request models for generator/context payloads.
3. [Pydantic Validators](https://pydantic.dev/docs/validation/latest/concepts/validators/) — Supports model-level and field-level validation for generator and materialization contracts.

## Research: Unity Runtime Consumption Of Generated Minigame Parameters
**Date**: 2026-04-13
**Queries**:
- `Unity JsonUtility FromJson official docs`
- `Unity TextAsset official docs`
- `Unity SceneManager.sceneLoaded official docs`

### Key Patterns Found
- Unity's `JsonUtility.FromJson` is intentionally limited to fields on
  `[Serializable]` types and does not support arbitrary dictionary-style JSON.
  That means backend-emitted resolved minigame parameters should be mirrored as
  serializable arrays or fixed fields if Unity needs to consume them directly.
  Source: https://docs.unity3d.com/ScriptReference/JsonUtility.FromJson.html
- Unity's `TextAsset` is the expected runtime wrapper for reading imported text
  files such as JSON payloads from `Resources`. That fits the current
  story-package importer path. Source:
  https://docs.unity3d.com/ScriptReference/TextAsset.html
- Unity's `SceneManager.sceneLoaded` callback is the right place to apply
  scene-specific runtime setup after a scene is loaded. That supports using the
  existing tutorial installer/controller path to switch the farm scene into a
  package-driven minigame mode. Source:
  https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html

### Recommended Approach
Keep backend minigame materialization richer than the old Unity contract, but
mirror the runtime-critical generated parameters into a Unity-serializable
shape. For the next slice, drive `FarmMain` from the current package beat using
the existing `TextAsset` importer and tutorial scene controller instead of
adding a separate network/runtime bridge.

### Sources
1. [Unity Scripting API: JsonUtility.FromJson](https://docs.unity3d.com/ScriptReference/JsonUtility.FromJson.html) — Confirms the need for a serializable parameter-entry shape instead of a raw dictionary.
2. [Unity Scripting API: TextAsset](https://docs.unity3d.com/ScriptReference/TextAsset.html) — Matches the current package-import path.
3. [Unity Scripting API: SceneManager.sceneLoaded](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html) — Supports scene-local activation of package-driven minigame setup.

## Research: Unity Runtime Tool-Scavenger Slice Assembly
**Date**: 2026-04-13
**Queries**:
- `Unity GameObject.CreatePrimitive official docs`
- `Unity Physics.OverlapSphere official docs`
- `Unity Renderer.sharedMaterial official docs`
- `Unity OnTriggerEnter official docs`

### Key Patterns Found
- Unity's `GameObject.CreatePrimitive` is the built-in path for quickly creating
  cubes, spheres, capsules, cylinders, and planes at runtime, which is enough
  for a temporary tool-scavenger slice without blocking on authored props.
  Source: https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html
- Unity's `Physics.OverlapSphere` returns all colliders touching a sphere and is
  suitable for local proximity checks, but it allocates an array each call.
  For a tiny fixed pickup count, direct distance checks over a cached pickup
  list are simpler and avoid repeated allocations. This last point is an
  implementation inference from the API shape. Source:
  https://docs.unity3d.com/ScriptReference/Physics.OverlapSphere.html
- Unity's `Renderer.sharedMaterial` references the shared asset-level material,
  while `renderer.material` creates an instantiated material copy. For editor
  and test-built placeholder geometry, that means we should avoid `.material`
  and prefer property blocks or controlled shared-material assignment to avoid
  edit-mode material leak warnings. Source:
  https://docs.unity3d.com/ScriptReference/Renderer-sharedMaterial.html
- `OnTriggerEnter` only fires when both objects have colliders and at least one
  side is a trigger while at least one is a physics body collider. Because the
  current tutorial movement rig is built around `CharacterController`, direct
  distance/proximity collection is lower risk than relying on trigger wiring for
  this slice. The choice of direct proximity is an inference from the docs and
  the current player rig. Source:
  https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter.html

### Recommended Approach
Keep the first `tutorial.find_tools` runtime slice deterministic and cheap:
spawn a bounded set of primitive-based pickup markers from package parameters,
track collection against a cached list, and use simple proximity collection
instead of trigger-heavy physics setup. Keep search-zone layouts explicit and
static, and use material property blocks or shared-material-safe tinting so the
existing EditMode scene tests do not produce new material-instantiation errors.

### Sources
1. [Unity Scripting API: GameObject.CreatePrimitive](https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html) — Supports runtime placeholder pickup geometry.
2. [Unity Scripting API: Physics.OverlapSphere](https://docs.unity3d.com/ScriptReference/Physics.OverlapSphere.html) — Informs proximity-check tradeoffs.
3. [Unity Scripting API: Renderer.sharedMaterial](https://docs.unity3d.com/ScriptReference/Renderer-sharedMaterial.html) — Explains why edit-mode placeholder scene builders should avoid `renderer.material`.
4. [Unity Scripting API: MonoBehaviour.OnTriggerEnter](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter.html) — Clarifies trigger prerequisites and why direct proximity is safer here.
