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
- [Town Optional Voice Input And Exit Gate](#research-town-optional-voice-input-and-exit-gate) — 2026-04-14
- [Town Adaptive Dialogue HUD](#research-town-adaptive-dialogue-hud) — 2026-04-14
- [Standing Slice Job Submission And Status](#research-standing-slice-job-submission-and-status) — 2026-04-13
- [Standing Slice Review Surface And Asset Provenance](#research-standing-slice-review-surface-and-asset-provenance) — 2026-04-13
- [Standing Slice Review Actions](#research-standing-slice-review-actions) — 2026-04-14
- [Standing Slice Immutable Artifact Snapshots](#research-standing-slice-immutable-artifact-snapshots) — 2026-04-14
- [Standing Slice Approved Publish](#research-standing-slice-approved-publish) — 2026-04-14
- [Sequence Session Context And Auto Next Turn](#research-sequence-session-context-and-auto-next-turn) — 2026-04-14
- [Unity Runtime Sequence Session Bridge](#research-unity-runtime-sequence-session-bridge) — 2026-04-14
- [Tutorial Dev Fast-Complete Shortcut](#research-tutorial-dev-fast-complete-shortcut) — 2026-04-14
- [Reference Library Operator Surface](#research-reference-library-operator-surface) — 2026-04-14
- [Sequence Session Continuity Memory](#research-sequence-session-continuity-memory) — 2026-04-14

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

## Research: Town Optional Voice Input And Exit Gate
**Date**: 2026-04-14
**Queries**:
- `OpenAI speech to text wav transcription gpt-4o-mini-transcribe`
- `Unity Microphone GetPosition AudioClip GetData microphone recording`

### Recommended Packages
| Package | Source | Why | Status |
|---------|--------|-----|--------|
| `com.unity.inputsystem` | Repo manifest + repo usage | Already in use for keyboard-driven Town interactions | In Use |
| `com.unity.modules.audio` | Unity built-in module | Already required for microphone capture and clip access | In Use |
| New speech SDK | Project inspection + official docs | Not needed for a push-to-talk prototype; existing Unity and HTTP support are enough | Rejected for this slice |

### Key Patterns Found
- OpenAI's Audio API supports `wav` uploads to `/v1/audio/transcriptions`, and
  `gpt-4o-mini-transcribe` supports plain `text` output. Source:
  https://developers.openai.com/api/docs/guides/speech-to-text
- For completed recordings, OpenAI explicitly supports a completed-audio
  transcription flow instead of requiring a Realtime session. This matches a
  push-to-talk Town interaction better than always-on streaming. Source:
  https://developers.openai.com/api/docs/guides/speech-to-text
- Unity's `Microphone.GetPosition` returns the current write position in the
  recording buffer, which is the supported way to determine how many frames
  were actually captured before stopping the recording. Source:
  https://docs.unity3d.com/ScriptReference/Microphone.GetPosition.html
- Unity's `AudioClip.GetData` returns interleaved float samples in the range
  `-1.0f` to `1.0f`, which is the right raw input for a custom PCM16 WAV
  encoder. Source:
  https://docs.unity3d.com/ScriptReference/AudioClip.GetData.html

### Recommended Approach
Keep Town voice input as a hold-to-talk, completed-recording feature instead of
opening a second real-time dialogue stack. Record a short clip with Unity's
microphone APIs, encode only the captured frames into a PCM16 WAV payload, and
submit that file to the OpenAI Transcriptions API using `gpt-4o-mini-transcribe`
with `response_format=text`.

Feed the returned transcript through the same Town player-prompt submission path
as the preset dialogue buttons so reply streaming, conversation memory, and
goodbye gating stay unified.

### Gotchas & Pitfalls
- Do not add a second conversation path that bypasses Town memory or option
  composition. Voice input should become just another prompt source.
- Do not assume a recorded `AudioClip` is fully populated up to its maximum
  duration. Use `Microphone.GetPosition` to trim to the actual captured frame
  count before encoding.
- `AudioClip.GetData` returns interleaved samples for multi-channel clips, so
  the WAV encoder has to preserve channel order correctly.
- Push-to-talk is the lower-risk first slice. Realtime transcription would add
  turn detection, ephemeral-session handling, and a more complex failure
  surface before the Town UX has even proven it needs that complexity.

### Sources
1. [OpenAI Docs: Speech to text](https://developers.openai.com/api/docs/guides/speech-to-text) — Confirms `wav` uploads, `gpt-4o-mini-transcribe`, plain-text output, and completed-recording transcription flows.
2. [Unity Scripting API: Microphone.GetPosition](https://docs.unity3d.com/ScriptReference/Microphone.GetPosition.html) — Confirms recorded sample-count access for trimming clips safely.
3. [Unity Scripting API: AudioClip.GetData](https://docs.unity3d.com/ScriptReference/AudioClip.GetData.html) — Confirms interleaved float-sample access and clip-read constraints.
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

## Research: Sequence Session Context And Auto Next Turn
**Date**: 2026-04-14
**Queries**:
- `site:docs.python.org sqlite3 row_factory transaction commit rollback`
- `site:docs.python.org random.Random seed choice sample reproducibility`
- `site:fastapi.tiangolo.com response_model FastAPI`

### Recommended Packages
No new packages found. The current FastAPI + SQLite + Pydantic stack is enough
for the first autonomous sequence-session slice.

### Key Patterns Found
- Keep persistence in explicit SQLite tables with explicit commit/rollback
  boundaries. Python's `sqlite3` docs recommend explicit transaction handling
  and setting `Connection.row_factory` for consistent row access.
- Use isolated `random.Random` instances when deterministic, per-session
  selection behavior matters. Python's `random` docs call out instance-based
  generators and reproducible seeded flows as the correct pattern when state
  should not be shared globally.
- Continue using FastAPI `response_model` on path operations for typed output
  validation and filtering. This matches the current backend route style and
  avoids route-specific ad hoc JSON shaping.

### Recommended Approach
Add a new session store instead of expanding the existing `store.py`. Persist
one session row with a JSON state blob and one ordered turn table with the
request/result payloads for each generated beat. Put selection logic in a
dedicated sequence-session service that:
- reads the current session state
- picks the next generator from the existing catalog
- derives bounded parameters from current unlocks/history
- calls the existing `GeneratedPackageAssemblyService`
- stores the resulting turn plus updated state

This preserves the current package-generation flow while adding the missing
autonomous memory layer above it.

### Gotchas & Pitfalls
- Do not hide the evolving session state only in the live package JSON. The
  package is a rolling output artifact, not the durable planning memory.
- Do not add more unrelated responsibilities to `store.py`; it is already near
  the repo's 500-line cap.
- Do not use module-global randomness if the planner should be reproducible or
  inspectable per session.
- Do not let route handlers own planning logic; keep them thin and return typed
  models through `response_model`.

### Sources
1. [Python `sqlite3` documentation](https://docs.python.org/3/library/sqlite3.html) — Transaction handling and `row_factory` guidance for the session store.
2. [Python `random` documentation](https://docs.python.org/3/library/random.html) — Instance-based generators and reproducibility guidance for turn selection.
3. [FastAPI response model documentation](https://fastapi.tiangolo.com/tutorial/response-model/) — Confirms the existing typed route pattern should stay in place.

## Research: Unity Runtime Sequence Session Bridge
**Date**: 2026-04-14
**Queries**:
- `site:docs.unity3d.com UnityWebRequest scripting API POST`
- `site:docs.unity3d.com JsonUtility.FromJson ScriptReference`
- `site:docs.unity3d.com Resources.Load ScriptReference`
- `site:docs.unity3d.com SceneManager.LoadScene ScriptReference`

### Recommended Packages
No new packages are required. The repo already has:
- `com.unity.modules.unitywebrequest`
- `com.unity.modules.jsonserialize`
- `com.unity.nuget.newtonsoft-json`

The first runtime bridge can be built on top of the existing UnityWebRequest
stack plus lightweight JSON parsing.

### Key Patterns Found
- `UnityWebRequest` is the supported Unity runtime HTTP API. The scripting API
  explicitly calls out upload/download handlers, `SendWebRequest`, headers, and
  `timeout`, which fits a coroutine-driven local orchestrator client.
- `JsonUtility.FromJson` only supports plain serializable classes and structs,
  not `UnityEngine.Object` subclasses. That works for thin DTO wrappers around
  session/turn responses and for deserializing `StoryPackageSnapshot`.
- `Resources.Load<T>` only loads assets already stored under a `Resources`
  folder by relative path. That makes it a fallback package source, not a live
  transport for freshly generated backend payloads.
- `SceneManager.LoadScene` accepts the scene name or path from Build Settings.
  That means the runtime bridge should keep returning logical scene names and
  convert them through `SceneWorkCatalog.GetLoadableSceneName` before loading.

### Recommended Approach
Keep the authored `Resources` story package as the fallback baseline and add an
in-memory runtime override inside `StoryPackageRuntimeCatalog`. Feed that
override from a small Unity HTTP client that talks to the local
story-orchestrator backend and returns:
- active `session_id`
- entry scene name for the new generated turn
- the generated story package JSON to inject into runtime memory

This lets Unity keep using the existing story-package navigator/installers
without requiring an asset refresh every time a new generated turn arrives.

### Gotchas & Pitfalls
- If runtime beat lookup only uses normalized alias matching, a generated
  `PostChickenCutscene` beat can lose to an older authored
  `Tutorial_PostChickenCutscene` beat. Exact scene-name matches must win first.
- If the runtime override is not cleared when falling back to the title screen
  or static slices, unrelated scenes can accidentally keep reading stale
  generated package data.
- Waiting on backend generation inside a transition path can take tens of
  seconds if provider calls do not instantly hit fallback. Runtime failure and
  fallback behavior need to be explicit.
- `SceneManager.LoadScene` resolves duplicate names by the first Build Settings
  match unless a full path is used. Keep one shared scene catalog and always
  normalize logical generated scene names through it before loading.

### Sources
1. [Unity Scripting API: UnityWebRequest](https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html) — Confirms the supported runtime HTTP flow, request handlers, `SendWebRequest`, and `timeout`.
2. [Unity Scripting API: JsonUtility.FromJson](https://docs.unity3d.com/ScriptReference/JsonUtility.FromJson.html) — Confirms plain-class serializer constraints for response DTOs and package snapshots.
3. [Unity Scripting API: Resources.Load](https://docs.unity3d.com/ScriptReference/Resources.Load.html) — Confirms `Resources` assets remain path-based static fallback content.
4. [Unity Scripting API: SceneManager.LoadScene](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html) — Confirms scene-name/path loading behavior and Build Settings resolution.

## Research: Standing Slice Review Actions
**Date**: 2026-04-14
**Queries**:
- `FastAPI request body Pydantic model tutorial`
- `SQLite ALTER TABLE ADD COLUMN documentation`
- `MDN fetch JSON body Content-Type`

### Key Patterns Found
- FastAPI's documented request-body path is to declare a Pydantic model and use
  it as the typed body parameter on a `POST`, `PUT`, or `PATCH` endpoint. That
  matches the existing story-orchestrator route style and keeps the review
  action payload explicit and validated.
- SQLite supports `ALTER TABLE ... ADD COLUMN` directly, and when adding a
  `NOT NULL` column the new column must have a non-`NULL` default. That makes
  a simple `review_notes TEXT NOT NULL DEFAULT ''` migration the lowest-risk
  way to evolve existing local databases without dropping standing-slice jobs.
- The browser fetch pattern for JSON updates is a normal `fetch()` call with a
  JSON-stringified body plus `Content-Type: application/json`, which fits the
  existing single-file operator page.

### Recommended Approach
Add a typed review-update request model in the backend, persist `review_notes`
in the standing-slice jobs table with a small schema migration, and expose a
review action endpoint that the local review page can call with a JSON body.

This keeps review state durable, keeps the page lightweight, and avoids
introducing a second store or client-side shadow state.

### Gotchas & Pitfalls
- Do not add `review_notes` only to the API model. The store schema must evolve
  too, or fetched jobs will silently lose the notes.
- Do not add a `NOT NULL` column in SQLite without a default value. Existing
  rows would fail the migration.
- Do not treat approve/reject as UI-only state. The next fetch of the job must
  reflect the same decision and notes.

### Sources
1. [FastAPI Request Body](https://fastapi.tiangolo.com/tutorial/body/) — FastAPI's documented request-body model pattern.
2. [SQLite ALTER TABLE](https://www.sqlite.org/lang_altertable.html) — SQLite's `ADD COLUMN` rules and `NOT NULL` default requirement.
3. [MDN Using the Fetch API](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch) — JSON body and `Content-Type` header usage for browser requests.

## Research: Standing Slice Immutable Artifact Snapshots
**Date**: 2026-04-14
**Queries**:
- `Python shutil copy2 docs`
- `Python pathlib relative_to docs`

### Key Patterns Found
- Python's `shutil.copy2()` is the standard file-copy path when we want copied
  artifacts to preserve basic metadata alongside the bytes. That is a good fit
  for snapshotting generated media into a job archive directory without
  re-encoding the files.
- `pathlib.Path.relative_to()` is the right standard-library guard for checking
  that an archived file still lives under the configured storyboard output
  root before deriving review paths or serving content.

### Recommended Approach
Archive package JSON and generated media into a job-scoped subtree under the
existing storyboard output root, then rewrite the stored standing-slice job
result to point at those archived paths before persistence.

This preserves the current live generation behavior while making job review
artifacts stable and safe for later review or promotion flows.

### Gotchas & Pitfalls
- Do not persist only metadata while leaving `output_path` on the shared live
  files. That still lets later runs overwrite the reviewed content.
- Do not archive outside the existing storyboard root or the current constrained
  content-serving endpoint will no longer be able to validate paths cleanly.
- Do not forget audio sidecars such as alignment JSON; those are part of the
  artifact set needed for later review and publish flows.

### Sources
1. [Python `shutil` module](https://docs.python.org/3/library/shutil.html) — `copy2()` behavior for copying generated artifacts.
2. [Python `pathlib` module](https://docs.python.org/3/library/pathlib.html) — `Path.relative_to()` for root-bounded path handling.

## Research: Standing Slice Approved Publish
**Date**: 2026-04-14
**Queries**:
- `Python shutil copy2 docs`
- `Python pathlib relative_to docs`

### Key Patterns Found
- `shutil.copy2()` remains the right standard-library primitive for promoting a
  reviewed archived file back into a live path without re-encoding it.
- `Path.relative_to()` remains the simplest root-bounded validation step for
  confirming both archived source files and live publish destinations stay under
  the configured storyboard output root.

### Recommended Approach
Publish should copy the archived package snapshot plus archived media/alignment
files back to the known live standing-slice paths, and it should only run for a
job whose persisted approval state is `approved`.

Persist publish state on the job itself so the local operator page can reflect
which reviewed run is currently live.

### Gotchas & Pitfalls
- Do not publish from mutable shared generation paths; the source of truth must
  be the archived job snapshot.
- Do not let an unapproved job mutate the live package or shared asset paths.
- Do not skip sidecar files such as alignment JSON during publish, or later
  narration tooling will see a partial live asset set.

### Sources
1. [Python `shutil` module](https://docs.python.org/3/library/shutil.html) — copying archived files back into live paths.
2. [Python `pathlib` module](https://docs.python.org/3/library/pathlib.html) — root-bounded path validation for publish sources and destinations.
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

## Research: Standing Slice Review Surface And Asset Provenance
**Date**: 2026-04-13
**Queries**:
- `FastAPI HTMLResponse docs`
- `FastAPI FileResponse docs`
- `Python pathlib resolve relative_to docs`

### Key Patterns Found
- FastAPI can explicitly declare an HTML route with `response_class=HTMLResponse`,
  which is enough for a small operator surface without introducing a separate
  frontend stack. Source:
  https://fastapi.tiangolo.com/advanced/templates/
- FastAPI exposes `FileResponse` for file-serving use cases, which fits a local
  review surface that needs to open generated storyboard images and narration
  files from persisted job assets. Source:
  https://fastapi.tiangolo.com/advanced/custom-response/
- Python's `Path.resolve()` plus `relative_to()` is the documented safe pattern
  when validating that a requested file stays under an approved root. Source:
  https://docs.python.org/3.12/library/pathlib.html

### Recommended Approach
Add a backend-served local operator page for the standing slice instead of
building Unity-side tooling first. The page should:
1. submit the standing-slice job request
2. fetch a stored job by `job_id`
3. render top-level job status, step status, and persisted per-asset details
4. open image/audio content through a constrained backend file route

Persist asset provenance as first-class child rows on the standing-slice job,
not only as nested step JSON. The review page can still consume the nested job
record, but the storage layer should keep assets queryable by job and asset ID.

### Gotchas & Pitfalls
- Do not serve arbitrary filesystem paths directly from a query parameter.
  Validate every asset path against the configured storyboard output root first.
- Do not make the review page depend on unpersisted in-memory results; it
  should render from the same job fetch endpoint a later operator would use.
- Do not stop at step-level JSON once asset provenance is available. Keep asset
  rows queryable so later review/publish flows can target one asset at a time.

### Sources
1. [FastAPI Templates / HTMLResponse](https://fastapi.tiangolo.com/advanced/templates/) — Confirms HTML routes are first-class and suitable for a lightweight operator page.
2. [FastAPI Custom Responses](https://fastapi.tiangolo.com/advanced/custom-response/) — Documents `FileResponse` for serving generated local files.
3. [Python pathlib docs](https://docs.python.org/3.12/library/pathlib.html) — Confirms `resolve()` and `relative_to()` behavior for root-bounded file validation.
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

## Research: Town Adaptive Dialogue HUD
**Date**: 2026-04-14
**Queries**:
- `Unity UI Layout Element preferred flexible size`
- `Unity UI Content Size Fitter preferred size`
- `TextMeshPro UI Text wrapping and overflow`

### Recommended Packages
No new packages found. The repo's existing `uGUI` and `TextMeshPro` stack is
enough for this HUD redesign.

### Key Patterns Found
- Use `LayoutElement` when a layout group needs explicit minimum, preferred, or
  flexible size per child. Unity allocates minimum size first, then preferred
  size, then flexible size, which is a good fit for generated reply buttons
  whose height changes with wrapping. Source:
  https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-LayoutElement.html
- Use `ContentSizeFitter` when a UI element should resize to its preferred
  content size instead of staying fixed. This fits a choice stack or status row
  that should grow when content gets taller. Source:
  https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-ContentSizeFitter.html
- TextMeshPro UI text supports explicit wrapping and overflow behavior, and the
  official docs warn that auto-size is resource intensive for frequently
  changing text. For streamed dialogue and live voice status, fixed font sizes
  plus wrapping are safer than auto-size. Source:
  https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/TMPObjectUIText.html

### Recommended Approach
Keep the existing Town canvas hierarchy, but make the HUD adaptive inside that
structure:
- promote the choice buttons to layout-driven controls with wrapping enabled
- separate the persistent control hint from transient waiting and voice status
- resize or reposition the panel and choice stack based on preferred text
  height instead of assuming one fixed body slot

### Gotchas & Pitfalls
- Do not use TextMeshPro auto-size for fast-changing streamed text or voice
  status; the docs call out that it re-lays out text repeatedly.
- Avoid one-line buttons for generated model text. Wrapping and preferred-size
  layout are more stable than clipping or truncation for Town reply options.
- Keep runtime layout changes conservative because `Town.unity` already uses
  fixed anchors for the panel and choice stack.

### Sources
1. [Unity UI: Layout Element](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-LayoutElement.html) — min / preferred / flexible sizing rules for adaptive controls.
2. [Unity UI: Content Size Fitter](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-ContentSizeFitter.html) — resizing UI elements to preferred content size.
3. [TextMeshPro: UI Text GameObjects](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/TMPObjectUIText.html) — wrapping, overflow, and auto-size guidance for dynamic UI text.

## Research: Runtime Minigame Parameter Consumption In Existing Unity Scenes
**Date**: 2026-04-14
**Queries**:
- `Unity event function execution order sceneLoaded Start Awake`
- `Unity ScriptableObject data driven architecture`
- `Unity open project game architecture overview data layer`

### Key Patterns Found
- Unity documents that `SceneManager.sceneLoaded` fires after `OnEnable` but
  before `Start`, and also warns that you cannot rely on the invocation order
  of the same event function across different GameObjects. That reinforces the
  repo-local rule to avoid one-shot cross-object `Start` ordering assumptions
  when a generated minigame config must reach another runtime component.
  Source: https://docs.unity3d.com/Manual/execution-order.html
- Unity's ScriptableObject guidance keeps game data separate from scene object
  instances. Even though this repo uses serializable story-package snapshots
  instead of ScriptableObject assets for generated turns, the same principle
  applies: treat generated minigame parameters as bounded data consumed by the
  existing scene, not as a reason to fork scene variants.
  Source: https://docs.unity3d.com/Manual/class-ScriptableObject.html
- Unity's Open Project architecture overview explicitly describes a data layer
  driving scene and manager behavior through assets and event-driven systems.
  That pattern supports extending the existing tutorial scene controllers and
  minigame managers with bounded parameter consumption instead of creating new
  minigame scenes for each generated variation. Source:
  https://github.com/UnityTechnologies/open-project-1/wiki/Game-architecture-overview

### Recommended Approach
Keep generated minigame variation inside the standing slice by:
- reading the active story-package beat in the scene controller
- applying bounded runtime config to the existing manager/service pair
- using preset IDs and mission services for safe behavior changes
- avoiding new scene variants unless the current scene cannot express the
  requested range at all

### Gotchas & Pitfalls
- Do not rely on `Start` ordering between the controller and the gameplay
  manager. Package application should be safe whether it happens before or
  after the manager's own `Start`.
- Do not let a layout parameter like `rowCount` explode gameplay surface area.
  Use bounded rounding to shape a grid, not multiplication that silently turns
  a five-plant objective into a ten-plot scene.
- Do not treat generated parameter support as an excuse to create one scene per
  variant. If the scene already supports the mechanic, prefer bounded preset
  tuning in code.

### Sources
1. [Unity Manual: Event function execution order](https://docs.unity3d.com/Manual/execution-order.html) — scene-load timing and cross-object ordering limits.
2. [Unity Manual: ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html) — data-driven separation between runtime objects and authored data.
3. [Unity Open Project 1: Game architecture overview](https://github.com/UnityTechnologies/open-project-1/wiki/Game-architecture-overview) — scene/manager/data-layer architecture reference for bounded runtime consumption.


## Research: Tutorial Dev Fast-Complete Shortcut
**Date**: 2026-04-14
**Queries**:
- `site:docs.unity3d.com SceneManager.LoadScene Unity scripting API`
- `site:docs.unity3d.com Input System Unity manual keyboard package`
- `site:docs.unity3d.com EditorSceneManager.OpenScene Unity scripting API`

### Recommended Packages
No new packages found. The repo already uses the new Input System and runtime
scene routing stack needed for this slice.

### Key Patterns Found
- Keep dev keyboard shortcuts on the Input System package instead of mixing in
  legacy `UnityEngine.Input`. Unity's Input System manual positions the package
  as the supported replacement for the old input manager and the repo already
  depends on that path. Source:
  https://docs.unity3d.com/es/2021.1/Manual/com.unity.inputsystem.html
- Keep scene progression routed through the same runtime `SceneManager.LoadScene`
  path the tutorial already uses. Unity's API loads by Build Settings name or
  path, so the repo's catalog-based indirection remains the right place to
  resolve aliases before loading. Source:
  https://docs.unity3d.com/es/530/ScriptReference/SceneManagement.SceneManager.LoadScene.html
- For EditMode verification, open target scenes explicitly through
  `EditorSceneManager.OpenScene` rather than assuming play-mode start behavior.
  Source:
  https://docs.unity3d.com/ru/2019.4/ScriptReference/SceneManagement.EditorSceneManager.OpenScene.html

### Recommended Approach
Keep `Shift+.` as the brute "next scene" shortcut, but add a separate
`Shift+Enter` fast-complete action that asks the active scene controller to
finish its objective first and only falls back to the existing next-scene flow
when no specialized handler is present. That matches the repo's current
controller-per-scene structure and avoids baking minigame-specific logic into
the shortcut input layer.

### Gotchas & Pitfalls
- Do not collapse "finish the current beat" and "skip to the next scene" into
  one binding; they solve different testing problems.
- Do not bypass `SceneWorkCatalog`/`TutorialFlowController` when fast-complete
  advances a scene, or alias-driven generated beats can regress again.
- Do not add editor-only dependencies to the runtime shortcut path.

### Sources
1. [Unity Manual: Input System](https://docs.unity3d.com/es/2021.1/Manual/com.unity.inputsystem.html) — Confirms the new Input System is the supported keyboard-input path.
2. [Unity Scripting API: SceneManager.LoadScene](https://docs.unity3d.com/es/530/ScriptReference/SceneManagement.SceneManager.LoadScene.html) — Confirms runtime scene loading still needs the existing catalog/loadable-name indirection.
3. [Unity Scripting API: EditorSceneManager.OpenScene](https://docs.unity3d.com/ru/2019.4/ScriptReference/SceneManagement.EditorSceneManager.OpenScene.html) — Supports focused EditMode tests that open real tutorial scenes.


## Research: Reference Library Operator Surface
**Date**: 2026-04-14
**Queries**:
- `site:fastapi.tiangolo.com FastAPI UploadFile File tutorial`
- `site:fastapi.tiangolo.com FastAPI FileResponse docs`
- `site:developer.mozilla.org FormData fetch upload file docs`

### Recommended Packages
No new packages found. The current FastAPI stack already supports multipart
uploads and file streaming, and the operator page can use plain browser
`fetch()` plus `FormData`.

### Key Patterns Found
- FastAPI recommends `UploadFile` for uploaded files, especially for larger
  assets, because it keeps metadata and uses spooled file handling instead of
  forcing the whole file body into memory at once. Source:
  https://fastapi.tiangolo.com/tutorial/request-files/
- FastAPI's `FileResponse` is the right response type for serving stored assets
  and supports explicit `path`, `media_type`, and `filename` handling. Source:
  https://fastapi.tiangolo.com/advanced/custom-response/
- Browser uploads should use `FormData` and should not set the
  `Content-Type` header manually when sending `multipart/form-data`, because the
  browser needs to add the boundary itself. Source:
  https://developer.mozilla.org/en-US/docs/Web/API/XMLHttpRequest_API/Using_FormData_Objects

### Recommended Approach
Keep the existing standing-slice review page as the operator hub and add a
Reference Library section to it. Use the current upload/list endpoints plus a
new constrained content endpoint so uploaded references can be previewed in the
same page. Keep filesystem validation on the server side and keep the client
logic to simple `fetch()` + `FormData` calls.

### Gotchas & Pitfalls
- Do not render previews from raw `stored_path` values. Use a constrained
  backend route instead so the browser never learns arbitrary local filesystem
  paths.
- Do not set `Content-Type` manually on the upload request when using
  `FormData`.
- Do not assume every reference is an image forever; gate inline previews by
  MIME type and fall back to links/metadata for other asset types.

### Sources
1. [FastAPI: Request Files](https://fastapi.tiangolo.com/tutorial/request-files/) — Confirms `UploadFile`, multipart handling, and `python-multipart` requirements.
2. [FastAPI: Custom Response - FileResponse](https://fastapi.tiangolo.com/advanced/custom-response/) — Confirms `FileResponse` usage and arguments for serving stored files.
3. [MDN: Using FormData Objects](https://developer.mozilla.org/en-US/docs/Web/API/XMLHttpRequest_API/Using_FormData_Objects) — Confirms correct browser-side file upload usage and the warning not to override `Content-Type`.

## Research: Sequence Session Continuity Memory
**Date**: 2026-04-14
**Queries**:
- `site:ai.google.dev Gemini image generation image editing multiple image inputs official`
- `site:ai.google.dev Gemini image understanding inline image data multiple images official`
- `site:ai.google.dev Gemini media resolution image inputs official`

### Recommended Packages
No new packages found. The current FastAPI + local filesystem + Gemini request
builder stack is enough for this slice.

### Key Patterns Found
- Gemini image generation supports text-and-image prompting, so generated
  cutscenes can carry forward prior images as direct reference inputs instead
  of relying only on prompt text. Source:
  https://ai.google.dev/gemini-api/docs/image-generation
- Gemini supports multiple image inputs in one prompt, mixing inline image
  data and reusable file references. That means continuity should be treated as
  a small prioritized set of references, not a single previous frame. Source:
  https://ai.google.dev/gemini-api/docs/image-understanding
- Inline image data is appropriate for smaller local requests, while reusable
  file uploads are better when assets are large or reused across many calls.
  For the current local orchestrator slice, a short bounded list of recent
  filesystem-backed references remains the simplest fit. Source:
  https://ai.google.dev/gemini-api/docs/image-understanding
- Gemini 3 also exposes media-resolution controls for multimodal inputs. We do
  not need that yet, but it reinforces keeping the continuity set tight and
  intentional so request cost and latency stay bounded. Source:
  https://ai.google.dev/gemini-api/docs/media-resolution

### Recommended Approach
Persist a bounded list of successful generated storyboard image artifacts in
story-sequence session state, including enough provenance to understand which
turn and character produced each reference. Use that persisted continuity list
to seed the next cutscene request explicitly. When explicit continuity paths
are already present, do not also append a broad package-wide recent-image
sweep, because that dilutes the session's intended continuity signal.

### Gotchas & Pitfalls
- Do not treat all package images as equally useful continuity references.
  Earlier unrelated beats can pollute character consistency if they are mixed
  back in indiscriminately.
- Do not let continuity sets grow unbounded. Gemini supports multiple image
  inputs, but more images are not automatically better for latency, cost, or
  identity stability.
- Do not persist broken or missing filesystem paths into session continuity
  state; the planner should only store successful generated image artifacts.

### Sources
1. [Google AI for Developers: Image generation with Gemini](https://ai.google.dev/gemini-api/docs/image-generation) — Confirms image generation can accept text-plus-image context and return inline image outputs.
2. [Google AI for Developers: Image understanding](https://ai.google.dev/gemini-api/docs/image-understanding) — Confirms inline image inputs, File API guidance, and multiple image parts in a single request.
3. [Google AI for Developers: Media resolution](https://ai.google.dev/gemini-api/docs/media-resolution) — Confirms multimodal image inputs have cost/latency tradeoffs and should stay deliberate.
