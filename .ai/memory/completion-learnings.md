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

### Generated Play Button Re-enabled From Stale Runtime State During A Fresh Generation Request (2026-04-15)
- Status: Addressed
- Related story/task: generated runtime tracker and title-screen generated playthrough flow
- Original completion claim: The generated runtime flow was handed off with a working tracker, persisted resume, and title-screen generate/play gating.
- Reported issue: The developer reported that the game-side generated slice became playable before the current generation run had actually finished.
- Failing evidence: Developer report after tracker validation: the title-screen/game UI automatically enabled the playable generated slice before the in-flight generation completed.
- Approach that produced the miss: I combined startup resume, persisted prepared-turn recovery, and fresh generation launch in the same runtime controller without proving that old async resume work could no longer repopulate readiness after a new generate request had already started.
- Why it was mistaken for done: Verification focused on tracker visibility, backend job progress, and the happy-path enablement of Play once a turn is prepared, but it did not cover the race where stale prepared state appears while a fresh request is still running.
- What should have been verified or stated differently: The handoff should have included an explicit regression for "generate in progress plus stale prepared turn present" and it should have stated whether startup resume work is cancelled or ignored when the player requests a fresh run.
- Prevention rules for future work:
  - Any resumable generated-playthrough flow must scope readiness to the current request, not just to any prepared turn visible in runtime state.
  - Title-screen play enablement must stay locked while a fresh generation request is active, even if older prepared state still exists locally.
- Follow-up actions:
  - Added `TitleScreenManager_Update_DoesNotEnablePlayWhileFreshGenerationIsStillRunning`.
  - Versioned async runtime operations so late resume/request callbacks are ignored after a clear or fresh generate starts.
  - Tightened `HasPreparedSequence` so a turn is only considered ready when no runtime operation is still active.
- Actual root cause after fix: Startup resume and fresh generation shared the same controller state without operation scoping. A late resume/request path could repopulate prepared-turn state while the current generation was still active, and the title screen trusted any prepared runtime turn as immediately playable.
- Guardrail added: The runtime controller now versions async operations, ignores stale callbacks after a fresh operation starts or the sequence is cleared, exposes pending-operation state, and only reports `HasPreparedSequence` when no runtime work is still in flight. Edit-mode setup now also clears persisted generative-runtime prefs so title-screen readiness tests do not inherit stale session state across cases.
- Distilled rule added to project-memory.md: `Generated-playthrough readiness must be scoped to the current runtime operation; late resume callbacks and stale prepared turns must never re-enable Play during a fresh generation run (2026-04-15)`

### Runtime Story Generation Still Fell Into Fallback Or Rejected Valid Gemini Frames (2026-04-15)
- Status: Addressed
- Related story/task: standalone generative runtime service and generated playthrough media pipeline
- Original completion claim: The runtime service was handed off as producing service-backed generated turns with Gemini/ElevenLabs media generation and Railway deployment working.
- Reported issue: The developer reported that generated runs were still showing fallback-looking frames, and later asked to constrain the system because an endless sequence would keep running into provider limits.
- Failing evidence: Railway deploy logs showed repeated Gemini `429`/timeout failures followed by `image generator fallback engaged provider=local-reference-remix`, while a later live probe showed `gemini-2.5-flash-image` succeeding but the runtime rejecting the result with `storyboard image rejected attempt=1/2 reason=caption_panel_detected provider=gemini-image`.
- Approach that produced the miss: I optimized for keeping a turn alive through provider failures, left `gemini-3.1-flash-image-preview` first in the production chain even after it proved brittle, and treated the current caption-panel detector as sufficiently precise without validating it against real Gemini outputs or a finite sequence cap.
- Why it was mistaken for done: The handoff focused on transport and provider reachability, but it did not prove that production runs were using true generated images end to end instead of remix fallback, nor that the runtime bounded provider exposure to a realistic session length.
- What should have been verified or stated differently: The handoff should have distinguished between "a session can finish" and "the session is using accepted first-party generated media under a bounded runtime contract." It also should have included a live check that real Gemini frames survive the quality gate.
- Prevention rules for future work:
  - Production generated-playthrough handoff must prove which provider actually produced the final accepted image assets.
  - Quality-gate heuristics for generated art must be checked against real provider outputs, not only synthetic placeholder fixtures.
  - Infinite continuation should stay out of the primary contract until the bounded finite-sequence version is stable under real provider limits.
- Follow-up actions:
  - Reordered the production Gemini image chain around the working model and removed the preview-model fallback from the default runtime path.
  - Tightened image prompts and caption-panel detection so clean dark-foreground Gemini frames are accepted while explicit panel-like lower thirds still fail.
  - Capped the runtime session to a persisted 3-turn sequence and ended the session cleanly after the third minigame outcome, with Unity clearing completed runs instead of reviving them on restart.
- Actual root cause after fix: The deployed runtime still prioritized a brittle preview Gemini model, accepted remix fallback as a way to keep turns alive, and used a caption-panel heuristic that could reject legitimate Gemini shots with dark foreground composition. On top of that, the endless continuation contract kept provider exposure open-ended even though the stable user-facing slice only needed a bounded proof run.
- Guardrail added: The default image path now starts with `gemini-2.5-flash-image`, the prompt explicitly requires a full-bleed frame with real scene content in the bottom third, the caption-panel detector now requires both a sharp horizontal drop and low lower-third variance before rejecting an image, and the runtime contract now defaults to a persisted 3-turn session that marks itself completed after the third minigame outcome.
- Distilled rule added to project-memory.md: `Use the stable Gemini image model first, validate caption-panel heuristics against real provider outputs, and keep the primary generated-playthrough contract bounded before attempting infinite continuation (2026-04-15)`

### Generated Story Sequence Re-Probed Dead Fallback Ports After A Healthy Backend Had Already Been Resolved (2026-04-15)
- Status: Addressed
- Related story/task: generated playthrough local backend bootstrap and runtime sequence session bridge
- Original completion claim: The generated-story runtime path was handed off as using the Unity-owned local backend bootstrap and the healthy local story-orchestrator on `127.0.0.1:8012`.
- Reported issue: The developer reported renewed generated-story failures and asked for the fix plus the backend to be brought up, with Unity logs showing a timed-out `next-turn` on `127.0.0.1:8012` followed by failed create-session probes to `127.0.0.1:8000` and `127.0.0.1:8011`.
- Failing evidence: Runtime logs showed `StorySequenceServiceClient` timing out on `http://127.0.0.1:8012` for `next-turn`, then emitting `Cannot connect to destination host` warnings while trying session creation on the dead fallback ports. A live shell check still showed the backend healthy on `http://127.0.0.1:8012/health`.
- Approach that produced the miss: The runtime controller resolved a healthy orchestrator base URL first, but `StorySequenceServiceClient` still rebuilt the full localhost candidate list for create and advance requests instead of staying pinned to the resolved backend.
- Why it was mistaken for done: Verification proved backend bootstrap, healthy endpoint resolution, and live generated turns, but it did not treat a slow request on the resolved backend as a separate case from endpoint discovery. That left the request layer free to fan back out across dead fallback ports and muddy the real failure.
- What should have been verified or stated differently: The handoff should have verified that once Unity resolves a healthy orchestrator base URL, story-sequence requests remain pinned to that URL and do not retry unrelated localhost ports.
- Prevention rules for future work:
  - Story-sequence request execution must stay pinned to the backend URL that readiness just proved healthy.
  - Endpoint discovery and request retry are separate concerns; a slow request on the resolved backend must not silently turn back into multi-port endpoint probing.
- Follow-up actions:
  - Add a regression that locks `StorySequenceServiceClient` to the resolved backend URL instead of rebuilding candidate localhost ports.
  - Patch the runtime request path and re-verify the backend with a live session-create plus next-turn call.
- Actual root cause after fix: `StorySequenceServiceClient` treated execution like discovery and rebuilt the full localhost candidate set even after `EnsureLocalOrchestratorReady()` had already resolved the live backend. For the deployed Railway path, the runtime also still assumed any unresolved target could be bootstrapped locally, and the deterministic `local-reference-remix` image fallback was still marked as a rejected provider fallback even though it was the intended valid last-resort image path.
- Guardrail added: Story-sequence requests now stay pinned to the resolved backend URL, Unity's generated-story timeout budget now exceeds the old 240-second ceiling, the local launcher refuses to "bootstrap" non-local hosts, the deployed Railway service runs from its own Procfile with persistent `/data` storage and provider env vars, provider request budgets are capped at 30 seconds, and the image quality gate now accepts clean `local-reference-remix` outputs while still rejecting placeholder assets. The deployed Railway service at `https://story-orchestrator-production.up.railway.app` passed `/health`, and Railway runtime logs recorded a live `POST /api/v1/story-sequence-sessions/{id}/next-turn` finishing `valid=True`.
- Distilled rule added to project-memory.md: `Once readiness resolves a story-orchestrator URL, execution must stay pinned to it; remote services are not candidates for local bootstrap, and deterministic local-reference remix images must remain quality-gate valid on deployed backends (2026-04-15)`

### Generated Story Slice Produced Repetitive Storyboards Even When The Backend Returned A Valid Turn (2026-04-15)
- Status: Addressed
- Related story/task: generated story-sequence narrative quality and storyboard novelty
- Original completion claim: The generated story slice was handed off as working end to end after the live backend turn path and media fallbacks were fixed.
- Reported issue: The developer reported that the generated cutscene was technically working but kept playing the same art over and over instead of advancing the story with distinct shots.
- Failing evidence: Live gameplay feedback showed the cutscene repeating visually similar frames and generic handoff beats even though Unity successfully received and played a generated package.
- Approach that produced the miss: I optimized for transport validity and provider fallback, but I did not verify that the storyboard planner still received prior-turn context after request resolution, and I left the turn/storyboard prompts too generic to force conflict, progression, and shot variety.
- Why it was mistaken for done: I treated `turn.result.is_valid == true` as equivalent to a satisfactory narrative result, even though story quality depended on context propagation and prompt guardrails that were not yet pinned by tests.
- What should have been verified or stated differently: The handoff should have stated that endpoint health and valid media output do not prove narrative continuity or visual variety. It also should have included a check that prior context, mission configuration, and shot-diversity rules survive the full request path.
- Prevention rules for future work:
  - Generated-story verification must cover narrative validity as well as transport validity.
  - Any request-normalization layer must preserve storyboard context fields used by prompt construction.
  - Prompt-based cutscene generation needs explicit guardrails for conflict, shot progression, and non-repeating compositions.
- Follow-up actions:
  - Preserved enriched storyboard context in `GeneratedStoryboardService._resolve_request_context`.
  - Added mission-configuration, world-state, prior-summary, and present-character context to the cutscene request builder.
  - Tightened turn-director and storyboard-planner prompts around conflict, handoff, and 2-4 second distinct shots.
  - Added focused tests for prompt content and context propagation.
- Actual root cause after fix: The richer cutscene context was being built upstream but stripped during storyboard request resolution, and the surviving prompts were still permissive enough to generate generic repeated bridges.
- Guardrail added: Context-preservation and prompt-guardrail tests now lock the intended narrative behavior, and the live media pipeline still falls back cleanly when provider auth drifts.
- Distilled rule added to project-memory.md: `A valid generated turn is not done until the storyboard planner proves it sees prior context and is constrained to distinct conflict-driven shots (2026-04-15)`

### Generated Story Slice Reused A Stale Healthy Backend Endpoint After The Backend Went Away (2026-04-15)
- Status: Addressed
- Related story/task: local backend bootstrap and generated playthrough preparation
- Original completion claim: The generated slice runtime bridge and local backend bootstrap were handed off as fail-closed with clearer startup guidance.
- Reported issue: The developer reported repeated `Cannot connect to destination host` warnings while the generated slice tried dead localhost endpoints during session creation.
- Failing evidence: Unity logs from `StorySequenceServiceClient.CreateSessionAndAdvance` showed connection attempts against `http://127.0.0.1:8000` and `http://127.0.0.1:8011`, followed by `StorySequenceRuntimeController` rejecting the payload and resetting session state.
- Approach that produced the miss: The runtime controller cached the last healthy orchestrator base URL and reused that readiness state while concurrent callers waited on an in-flight health check, but it did not prove the cached endpoint was still healthy when the current readiness pass failed.
- Why it was mistaken for done: Verification covered healthy bootstrap and clearer launcher diagnostics, but it did not exercise the case where a previously healthy local backend goes away and a waiting generated-sequence request inherits that stale readiness cache.
- What should have been verified or stated differently: The handoff should have included a regression for stale cached readiness after a failed re-check, especially because the title screen starts background readiness work before the generate button is pressed.
- Prevention rules for future work:
  - A cached local-backend readiness URL must be cleared as soon as a fresh readiness check fails.
  - Any caller that waits on an in-flight readiness pass must receive the actual result of that pass, not a synthesized stale-success fallback.
- Follow-up actions:
  - Add a focused EditMode regression around stale readiness cache invalidation in `StorySequenceRuntimeBridgeTests`.
  - Patch `StorySequenceRuntimeController` so failed readiness checks cannot feed stale healthy endpoints into generated-sequence requests.
- Actual root cause after fix: `StorySequenceRuntimeController.EnsureLocalOrchestratorReady()` preserved `_lastOrchestratorReadyBaseUrl` after a failed readiness pass, so later callers could inherit a stale localhost endpoint and proceed into `CreateSessionAndAdvance` against a dead backend.
- Guardrail added: `StorySequenceRuntimeBridgeTests.StorySequenceRuntimeController_EnsureLocalOrchestratorReady_ClearsStaleHealthyBaseUrlWhenCurrentCheckFails` now locks the stale-cache invalidation behavior, and the controller clears the cached healthy base URL whenever the current readiness pass fails.
- Distilled rule added to project-memory.md: `Clear cached local-backend readiness immediately when a fresh orchestrator health check fails (2026-04-15)`

### Generated Story Slice Still Failed End To End Because Remote Media Providers Were Brittle (2026-04-15)
- Status: Addressed
- Related story/task: live generated playthrough bootstrap and autonomous story-sequence turn generation
- Original completion claim: The local story-orchestrator bootstrap and generated playthrough request path were handed off as working/fail-closed once the backend port drift was fixed.
- Reported issue: The developer asked whether the API was really launching and working alongside Unity because generated playthroughs still failed in practice.
- Failing evidence: Live backend inspection showed `uvicorn` healthy on `127.0.0.1:8012`, but prior generated turns still failed through Gemini image timeouts/empty-image responses and ElevenLabs `401 Unauthorized`, which caused the whole turn to come back invalid.
- Approach that produced the miss: The system depended on remote image/audio providers being healthy on every run, with placeholder image fallback intentionally rejected by the quality gate and no deterministic local image provider capable of keeping the cutscene valid.
- Why it was mistaken for done: Transport/bootstrap verification was treated as equivalent to end-to-end playthrough viability even though media generation still had provider-specific failure modes.
- What should have been verified or stated differently: The handoff should have included at least one live `POST /api/v1/story-sequence-sessions/{id}/next-turn` success proving `turn.result.is_valid == true`, not only a healthy backend and a launched process.
- Prevention rules for future work:
  - A healthy local backend process is not enough; generated-slice fixes must prove one live valid turn through the actual media pipeline.
  - Standing generated playthroughs need a deterministic local media path that preserves validity when remote providers drift or lose auth.
- Follow-up actions:
  - Added OpenAI-backed speech/image provider clients plus a deterministic `LocalReferenceImageGenerator`.
  - Reordered generated storyboard media selection so the runtime no longer depends on Gemini/ElevenLabs success to return a valid turn.
  - Verified two live session-create + next-turn runs against `127.0.0.1:8012`, both returning `is_valid=true`.
- Actual root cause after fix: The backend process was reachable, but the media stack still relied on brittle remote providers. Gemini image generation could stall or return unusable payloads, ElevenLabs narration was unauthorized, and the only image fallback was a placeholder intentionally rejected by the quality gate.
- Guardrail added: The generated slice now has a deterministic local-reference image path and OpenAI speech path, backed by new provider tests plus repeated live next-turn verification.
- Distilled rule added to project-memory.md: `Generated-slice handoff requires one live valid turn through the real media pipeline, and that pipeline needs a deterministic local fallback for images (2026-04-15)`

### Generated Story Slice Button Became Inert After Title-Screen UX Change (2026-04-14)
- Status: Addressed
- Related story/task: generated-slice title-screen loading UX fix
- Original completion claim: The generated slice launch path was updated to keep the title screen visible while live generation runs.
- Reported issue: The developer reported that clicking the generated slice button now does nothing.
- Failing evidence: Developer report after retrying the standing slice: clicking the button produces no visible transition into the generated scene.
- Approach that produced the miss: The title-screen flow was changed so the generated branch exits before fade, but that path was not re-verified for the case where the runtime controller refuses to start or the title manager remains locked in transition state without a visible scene change.
- Why it was mistaken for done: Verification covered backend reachability and a live HTTP generation call, but not the exact Unity click-to-request state machine after the branch rewrite.
- What should have been verified or stated differently: The handoff should have included one explicit click-path verification for request-start success, request-in-flight refusal, and callback reset behavior.
- Prevention rules for future work:
  - Any title-screen async launch path must prove both ''request started'' and ''UI unlocked again if request did not start.''
  - A backend-health check is not a substitute for verifying the Unity-side click state machine.
- Follow-up actions:
  - Added `TitleScreenManager_GeneratedSliceButtonClick_ShowsVisibleLoadingStateImmediately` so title-screen clicks have a focused regression.
  - Updated `TitleScreenManager` to raise a full-screen loading overlay immediately, disable the generated slice button during bootstrap, and restore the title surface when bootstrap is refused or unavailable.
- Actual root cause after fix: The generated branch no longer faded away immediately, but the remaining feedback was only a small status label. For a live bootstrap that can take around a minute, that looked like a dead click even when the request had started.
- Guardrail added: The title-screen generated slice now exposes an obvious blocking loading state plus a busy/unavailable recovery path instead of relying on subtle footer text.
- Distilled rule added to project-memory.md: `Async title-screen launch flows must show an obvious loading state immediately and recover visibly if the request never starts (2026-04-14)`

### Generated Story Slice Returned To Title Screen After Fail-Closed Bootstrap (2026-04-14)
- Status: Open
- Related story/task: GSO-017 generated slice launch gating and storyboard quality gates
- Original completion claim: The title-screen generated slice was handed off as failing closed on backend/bootstrap errors instead of loading stale fallback content.
- Reported issue: The developer reported that playing the slice now goes to black and returns to the title screen.
- Failing evidence: Developer report after running the title-screen slice: fade to black, then immediate return to title instead of a playable generated beat.
- Approach that produced the miss: The fail-closed title flow was added before the live backend availability and quality-gated generation path were re-verified end to end in the actual title-screen launch loop.
- Why it was mistaken for done: Verification covered focused backend tests and source review of the Unity callback path, but it did not prove that a real title-screen launch still had a healthy backend/generation path to land on.
- What should have been verified or stated differently: The handoff should have separated ''fail-closed path is working'' from ''live generated slice still succeeds end to end right now.''
- Prevention rules for future work:
  - Any fail-closed runtime path must be re-verified with a healthy live backend before handoff so ''returns safely'' does not mask ''never launches successfully.''
  - Title-screen generated slice changes need one explicit end-to-end live launch check, not only unit/backend coverage.
- Follow-up actions:
  - Confirm whether the local story-orchestrator is reachable on the configured port.
  - Reproduce the bootstrap failure directly against the backend and patch the actual failing dependency.
- Distilled rule added to project-memory.md: `Generated-slice fail-closed changes still need one healthy live launch verification before handoff (2026-04-14)`

### Generative Story Slice Fell Back Into Trash Content When Live Bootstrap Failed (2026-04-14)
- Status: Open
- Related story/task: Standing `Generative Story Slice` runtime bridge and live storyboard generation
- Original completion claim: The standing title-screen slice was described as using the live story-sequence backend path, with the baked package only as a fallback.
- Reported issue: The developer reported that the `Generative Story Slice` looked like trash.
- Failing evidence: The standing slice launched into stale packaged/generated content when the local backend was unavailable, and the shared generated storyboard folder also contained live Gemini frames with a large dark caption box baked into the bottom of the image.
- Approach that produced the miss: The Unity bootstrap path treated generated-sequence failure as a reason to load the fallback scene, and the backend accepted any returned image artifact without a quality gate beyond transport success.
- Why it was mistaken for done: Verification proved that the title button existed and that the generated path could work when the backend was healthy, but it did not treat backend unavailability and obviously degraded image output as first-class handoff cases.
- What should have been verified or stated differently: The handoff should have stated that the standing slice was only trustworthy while the backend was up and that storyboard output still lacked rejection logic for caption-box or placeholder frames.
- Prevention rules for future work:
  - A standing generated slice must never silently load stale fallback content after live bootstrap failure.
  - Generated asset pipelines must gate on visible output quality and provider provenance, not only HTTP success and file creation.
  - Handoffs for live generation paths must distinguish "live path can succeed" from "fallback and degraded-output cases are contained."
- Follow-up actions:
  - Add a title-screen bootstrap regression that proves failure does not load a fallback scene.
  - Add a storyboard image quality gate with retry and rejected-asset cleanup.
  - Surface generated-slice availability state directly on the title-screen slice launcher.
- Distilled rule added to project-memory.md: `Standing generated slices must fail closed on bootstrap errors and reject visibly degraded storyboard frames (2026-04-14)`

### GTM Approval Flow Did Not Publish Approved X Replies (2026-04-14)
- Status: Addressed
- Related story/task: McCluckin Farm GTM X-only monitor + review dashboard
- Original completion claim: The X-only pipeline was described as working end to end for discovery and drafting, with the dashboard acting as the operator console for live monitoring and review.
- Reported issue: The developer approved multiple X drafts in the dashboard and none of them were actually posted on X.
- Failing evidence: The live GTM database contained 10 drafts with `reviewer_action in ('approved','edited')` and no matching row in `published`, while the active `gtm-x-monitor` automation only ran monitor + draft work and never invoked the publish path.
- Approach that produced the miss: The implementation treated approval as a state transition only, left publishing to a separate CLI path, created monitoring automations that summarized queue activity without actually driving approved replies through the publisher, and continued using an OAuth 1.0a publish client even after the live X setup moved to OAuth 2.0 PKCE user tokens.
- Why it was mistaken for done: Verification proved the mechanical pieces independently, but it did not verify the operator workflow the developer would actually use: approve in the dashboard, then expect the post to appear on X or at least become visibly ready or pending publication.
- What should have been verified or stated differently: The handoff should have explicitly said that approval alone does not publish and that a separate publish run or automation was still required. The dashboard should also have exposed approved-but-unpublished items as a first-class operator state, and the publish handoff should have proven the exact live X auth mode used by the app instead of assuming older OAuth 1.0a credentials still matched the endpoint.
- Prevention rules for future work:
  - When a review UI includes an approval action for outbound content, verify whether approval publishes immediately, schedules publication, or only marks readiness, and state that behavior explicitly in the handoff.
  - For automation-backed publishing systems, verify the live automation prompts against the intended operator workflow instead of assuming a separate command will be run later.
  - Make approved-but-unpublished content visible in the operator surface so queue state cannot masquerade as published state.
  - For X publishing, verify the exact user-context auth path that production will use and do not hand off a write flow until that live auth mode succeeds against the real app configuration.
- Follow-up actions:
  - Add focused tests for approved-but-unpublished dashboard visibility and publish actions.
  - Wire the dashboard and/or automation flow so approved X replies can actually publish through the intended operator path.
  - Add clearer publish logging so operator history distinguishes review approval from platform publication.
  - Add an OAuth 2.0 PKCE publish path for X, including refresh-token rotation and persistence for unattended runs.
- Actual root cause after fix: The dashboard review flow was only marking drafts approved, while the active automation never invoked the publish path. After the dashboard gained an explicit publish action, the live publish client still failed because it used OAuth 1.0a tokens even though the active X app credentials had shifted to OAuth 2.0 PKCE user tokens. The publisher now prefers OAuth 2.0 access tokens, retries once after a 401 by refreshing via `POST /2/oauth2/token`, and persists the rotated tokens for later scheduled runs.
- Guardrail added: GTM X publishing now has focused regression coverage for OAuth 2.0 `user_auth=False` publish calls and 401 refresh-and-retry handling.
- Distilled rule added to project-memory.md: `Approval flows for outbound content must make publish state explicit and X write handoffs must verify the exact live auth mode in use (2026-04-15)`


### Town Voice Streaming Fell Back To Text On Intermittent Token Mint Delays (2026-04-14)
- Status: Addressed
- Related story/task: TOWN-002 Town NPC voice streaming
- Original completion claim: The Town scene was handed off as speaking reliably through ElevenLabs while text streamed.
- Reported issue: The developer reported that Town voice sometimes streamed and sometimes fell back to text-only with `[TownVoice] Voice streaming is unavailable; continuing with text only.`
- Failing evidence: Runtime warning emitted from `TownNpcVoiceStreamController.Update()` after `TownVoiceTokenServiceClient.RequestToken()` returned an unsuccessful result.
- Approach that produced the miss: The Unity runtime treated local token minting as a very short-lived request with a 3-second timeout and no retry, even though the local backend still had to call ElevenLabs before returning the single-use token.
- Why it was mistaken for done: Earlier verification proved the happy path when the local backend and ElevenLabs answered quickly, but it did not treat token-mint latency variance as part of the real Town voice-stream reliability surface.
- What should have been verified or stated differently: The handoff should have called out that the Unity-side token timeout was much shorter than the backend provider timeout and therefore vulnerable to intermittent fallback under normal network jitter.
- Prevention rules for future work:
  - Any Unity client calling a local proxy for third-party tokens must allow at least as much time as the proxy's upstream provider timeout, or explicitly retry transient failures.
  - Treat "token mint succeeds once" and "voice path is resilient to startup latency" as separate verification axes.
  - When a local backend proxies an external provider, the runtime warning should preserve enough endpoint/error context to distinguish misconfiguration from transient latency.
- Follow-up actions:
  - Increase the Unity token request budget and add a focused retry path for transport and upstream 5xx failures.
  - Add regression coverage that locks the Unity-side timeout above the old 3-second budget.
- Actual root cause after fix: `TownVoiceTokenServiceClient` only allowed 3 seconds for `/api/v1/elevenlabs/tts-websocket-token`, but the backend's own ElevenLabs token mint call uses a much longer timeout. Under intermittent provider or network latency, Unity gave up first and dropped the voice session into text-only fallback.
- Guardrail added: Unity token requests now use a longer timeout plus transient retries, and EditMode coverage locks the timeout above the old fragile threshold.
- Distilled rule added to project-memory.md: `Unity proxy timeouts must not undercut the backend provider timeout for Town voice token minting (2026-04-14)`

### Scene Loader Alias Fix Missed The Core Tutorial Namespace Import (2026-04-13)
- Status: Addressed
- Related story/task: Tutorial alias-resolution fix for post-chicken -> midpoint runtime transitions
- Original completion claim: The runtime scene-loading paths were updated to resolve canonical tutorial aliases through `SceneWorkCatalog`.
- Reported issue: Unity compile failed with `Assets/_Project/Scripts/MonoBehaviours/SceneLoader.cs(18,19): error CS0103: The name 'SceneWorkCatalog' does not exist in the current context`.
- Failing evidence: Developer-reported compiler error in `SceneLoader.cs` after the alias-resolution patch landed.
- Approach that produced the miss: The implementation updated `SceneLoader` to use `SceneWorkCatalog.GetLoadableSceneName()` but did not add the required `using FarmSimVR.Core.Tutorial;` import to the MonoBehaviours file.
- Why it was mistaken for done: The patch was validated by local source inspection and `git diff --check`, but not by a fresh compile, and the changed file did not get a final cross-assembly import sweep.
- What should have been verified or stated differently: Any cross-assembly symbol added to a MonoBehaviour file needs a final namespace-import scan before handoff, especially when broader Unity test execution is already known to be blocked elsewhere.
- Prevention rules for future work:
  - When adding `Core` symbols into `MonoBehaviours`, do a file-level import audit before calling the patch done.
  - Reuse the existing `Namespace Imports After Cross-Assembly Wiring` rule explicitly when a compile run is unavailable or blocked.
- Follow-up actions:
  - Add the missing `using FarmSimVR.Core.Tutorial;` import to `SceneLoader.cs`.
  - Re-scan the edited files for unresolved `SceneWorkCatalog` references.
- Actual root cause after fix: `SceneLoader.cs` referenced `SceneWorkCatalog` from `FarmSimVR.Core.Tutorial` without importing that namespace.
- Guardrail added: No new project-memory rule; this is covered by the existing `Namespace Imports After Cross-Assembly Wiring (2026-04-11)` rule, which should have been applied to this patch.
- Distilled rule added to project-memory.md: `Namespace Imports After Cross-Assembly Wiring (2026-04-11)`

### Tutorial Flow Loaded Canonical Next-Scene Aliases Instead Of Build-Loadable Names (2026-04-13)
- Status: Addressed
- Related story/task: Generated storyboard tutorial slice spanning the post-chicken bridge into the midpoint placeholder
- Original completion claim: The story-package slice was handed off as playable through the post-chicken cutscene into the next tutorial bridge.
- Reported issue: Completing the post-chicken storyboard cutscene threw `Scene 'MidpointPlaceholder' couldn't be loaded` because `TutorialFlowController` attempted to load the canonical tutorial alias instead of the actual build-profile scene name.
- Failing evidence: Runtime log from `TutorialFlowController.CompleteCurrentSceneAndLoadNext()` at `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialFlowController.cs:92` calling `SceneManager.LoadScene("MidpointPlaceholder")` while the build profile contains `Tutorial_MidpointPlaceholder`.
- Approach that produced the miss: The earlier alias fix only covered the title-screen launcher path. The follow-up tutorial runtime path still loaded `NextSceneName` values from the story package and tutorial flow service directly without resolving them through `SceneWorkCatalog`.
- Why it was mistaken for done: Verification focused on entering the generated cutscene and playing the storyboard assets, but it did not directly exercise the runtime handoff from one aliased bridge scene to the next.
- What should have been verified or stated differently: A tutorial handoff must validate both entry and exit transitions for any scene chain that uses canonical routing aliases different from actual scene asset names.
- Prevention rules for future work:
  - Treat every Unity scene-loading path as a separate integration surface; fixing one launcher does not prove tutorial transitions are safe.
  - Centralize canonical-scene-to-build-scene resolution before every `SceneManager.LoadScene` call that touches tutorial routing.
  - Add a regression around story-package next-scene resolution whenever a package references canonical aliases.
- Follow-up actions:
  - Add a focused regression covering loadable resolution of the post-chicken package next-scene alias.
  - Route tutorial flow scene loads through `SceneWorkCatalog.GetLoadableSceneName`.
- Actual root cause after fix: `TutorialFlowController.CompleteCurrentSceneAndLoadNext()` loaded `packageNextScene` and tutorial flow outputs directly, and `SceneLoader` also passed resolved tutorial aliases straight into `SceneManager.LoadScene`. Both paths bypassed the existing `SceneWorkCatalog.GetLoadableSceneName()` mapping that the title-screen launcher already used.
- Guardrail added: `TutorialFlowController.ResolveLoadableSceneRequest()` now resolves canonical tutorial aliases to build-loadable scene names, `TutorialFlowController` routes all tutorial scene loads through that mapping, and `SceneLoader` falls back to the same catalog mapping when no controller is present. `TutorialSceneConfigurationTests.TutorialFlowController_ResolveLoadableSceneRequest_UsesBuildProfileNameForStoryPackageNextScene` locks the post-chicken -> midpoint alias conversion in source.
- Distilled rule added to project-memory.md: `All tutorial scene-loading paths must resolve canonical aliases through SceneWorkCatalog before calling SceneManager.LoadScene (2026-04-13)`

### Storyboard Cutscene Playback Assumed An AudioSource Existed (2026-04-13)
- Status: Addressed
- Related story/task: Generated storyboard cutscene slice on the post-chicken bridge scene
- Original completion claim: The title-screen story package slice was handed off as testable with generated Gemini stills and narration-backed cutscene playback.
- Reported issue: Entering the post-chicken cutscene threw `MissingComponentException` because `TutorialCutsceneSceneController` tried to stop an `AudioSource` on a game object that did not have one yet.
- Failing evidence: Runtime log from `TutorialCutsceneSceneController.PlayAudio` at `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialCutsceneSceneController.cs:219` after loading `Tutorial_PostChickenCutscene`.
- Approach that produced the miss: The controller cached audio setup through `GetComponent<AudioSource>() ?? AddComponent<AudioSource>()` and the handoff focused on generated assets and story routing without exercising the actual storyboard playback path in Unity.
- Why it was mistaken for done: Verification confirmed that the package, images, and audio assets existed, but it did not directly play the cutscene scene to prove the controller could bootstrap its own runtime audio component.
- What should have been verified or stated differently: A storyboard cutscene handoff must verify that the controller can start playback from a clean scene object with no pre-attached `AudioSource`, or state explicitly that the scene requires one to be authored in advance.
- Prevention rules for future work:
  - Add a focused regression test for cutscene playback bootstrap whenever a controller lazily creates required components at runtime.
  - Avoid Unity null-coalescing shortcuts for component creation when runtime safety depends on Unity's custom null semantics.
  - Treat "asset generation complete" and "scene playback verified" as separate completion claims.
- Follow-up actions:
  - Add a targeted test around `TutorialCutsceneSceneController` playback with no pre-existing `AudioSource`.
  - Patch the controller to resolve or create a valid audio component before calling `Stop()` or `Play()`.
- Actual root cause after fix: `TutorialCutsceneSceneController` used `gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>()`, and that null-coalescing path was not safe against Unity's custom null semantics for missing components. The controller could cache an invalid `AudioSource` reference and then throw `MissingComponentException` on `_audioSource.Stop()` during storyboard playback.
- Guardrail added: `StoryPackageRuntimeCatalogTests.StoryboardController_PlayAudio_CreatesAudioSource_WhenMissing` now locks the bootstrap behavior in source, and `TutorialCutsceneSceneController` resolves the component through explicit Unity null checks before caching or using it. Direct Unity test execution was still blocked in the disposable copy by unrelated `TownConversationMemoryTests` compile errors, so runtime verification remains limited to source inspection plus the reported failure path.
- Distilled rule added to project-memory.md: `Storyboard cutscene controllers must resolve runtime-required components with explicit Unity null checks, not null-coalescing shortcuts (2026-04-13)`

### Title Screen Slice Launcher Used Canonical Tutorial Aliases As Loadable Scene Names (2026-04-13)
- Status: Addressed
- Related story/task: Generated storyboard cutscene slice on the post-chicken bridge scene
- Original completion claim: The post-chicken bridge was handed off as testable from the title screen through the existing slice launcher.
- Reported issue: Launching the post-chicken slice from the title screen failed with `Scene 'PostChickenCutscene' couldn't be loaded`.
- Failing evidence: Runtime log from `TitleScreenManager/<TransitionToGame>d__12` calling `SceneManager.LoadScene("PostChickenCutscene")` even though the build profile contains `Tutorial_PostChickenCutscene`.
- Approach that produced the miss: The implementation reused the canonical scene names from `SceneWorkCatalog` for both story routing and Unity scene loading without verifying that every canonical tutorial alias matched an actual scene asset name.
- Why it was mistaken for done: Verification covered package routing, generated assets, and button presence, but it did not exercise the title-screen launch path for aliased bridge scenes.
- What should have been verified or stated differently: A title-screen handoff must verify that each launcher button resolves to a scene name that exists in the build profile, especially when runtime routing uses normalized aliases.
- Prevention rules for future work:
  - Keep story-routing scene aliases separate from Unity loadable scene names whenever the asset names still carry migration prefixes.
  - Add a regression test for title-screen launcher resolution whenever `SceneWorkCatalog` exposes canonical names that differ from scene asset names.
  - Treat launch-path verification as distinct from tutorial-flow verification; a scene being routable does not prove it is directly loadable from the menu.
- Follow-up actions:
  - Added `SceneWorkCatalog.GetLoadableSceneName()` and routed `TitleScreenManager` through it before `SceneManager.LoadScene`.
  - Added `TutorialSceneConfigurationTests.SceneWorkCatalog_GetLoadableSceneName_ResolvesBuildProfileSceneNames` to lock the alias-to-build-name mapping for the aliased bridge scenes.
  - Root cause was mixing canonical tutorial routing names with actual scene asset names in the title-screen launcher path.
- Distilled rule added to project-memory.md: `Title-screen launchers must resolve canonical tutorial aliases to build-loadable scene names through SceneWorkCatalog (2026-04-13)`

### Generative Story Slice Launched The Tutorial Intro Instead Of The Generated Beat (2026-04-13)
- Status: Addressed
- Related story/task: Standing `Generative Story Slice` launcher on the title screen
- Original completion claim: The standing title-screen slice was handed off as the stable test surface for ongoing Generative Story Orchestrator work.
- Reported issue: The developer reported that clicking `Generative Story Slice` still starts from `Intro`, which is the authored tutorial opening rather than the generated slice.
- Failing evidence: Developer report after using the title-screen launcher: "the `generative story slice` just starts from the intro which it shouldnt lol".
- Approach that produced the miss: The launcher label and package backing were updated, but the slice entry scene remained hardcoded to `TutorialSceneCatalog.IntroSceneName`.
- Why it was mistaken for done: Verification focused on the presence of the standing slice button and package naming, but it did not validate that the button targeted the generated storyboard beat described in the GSO specs.
- What should have been verified or stated differently: The handoff should have stated the exact entry scene and checked that it matched the generated-slice target beat, not just that a package-backed launcher existed.
- Prevention rules for future work:
  - A standing slice launcher must verify both label and entry beat; a renamed button is not enough.
  - When multiple specs describe the same slice, reconcile the entry scene against the most specific generated-slice spec before handoff.
  - Add a source regression that locks the standing slice to the intended generated beat scene.
- Follow-up actions:
  - Added a failing regression for the standing slice entry scene in `TutorialSceneConfigurationTests.TitleScreenManager_StartBuildsTutorialSliceLauncherFromSharedSceneCatalogAndStoryPackageSampleEntry`.
  - Retargeted `TitleScreenManager` from `Intro` to `PostChickenCutscene`, which is the current generated storyboard proof beat.
  - Updated the slice spec and project memory so future increments preserve the correct entry point.
- Actual root cause after fix: `TitleScreenManager` still hardcoded `StoryPackageSampleSceneName` to `TutorialSceneCatalog.IntroSceneName`, and the earlier GSO-003 slice spec repeated that mismatch even though the more specific generated-slice spec already targeted `PostChickenCutscene`.
- Guardrail added: The standing slice test now asserts the launcher's internal scene constant resolves to `TutorialSceneCatalog.PostChickenCutsceneSceneName`, and project memory now records that the standing generative slice must launch the current generated proof beat instead of the authored intro unless a spec explicitly says otherwise.
- Distilled rule added to project-memory.md: `The standing Generative Story Slice should launch the current generated proof beat, not the authored Intro Timeline (2026-04-13)`

### Town Scene Shipped With A Stale Serialized OpenAI Key (2026-04-13)
- Status: Addressed
- Related story/task: Town NPC text streaming and voice-streaming handoff
- Original completion claim: The Town scene was handed off as ready for local streamed NPC conversations with OpenAI text and ElevenLabs voice layered on top.
- Reported issue: The developer hit a runtime 401 from OpenAI because the scene was still sending an invalid serialized API key.
- Failing evidence: Runtime log from `LLMConversationController` / `OpenAIClient`: `OpenAI stream failed (401)` with `Incorrect API key provided`.
- Approach that produced the miss: The implementation focused on request shape, stream parsing, and voice integration, but it did not audit the serialized `OpenAIClient` configuration already present in `Town.unity`.
- Why it was mistaken for done: Verification covered focused tests and new runtime wiring, but not the actual credential source that the Town scene would use at play time.
- What should have been verified or stated differently: A handoff for any live LLM scene must explicitly verify whether credentials come from environment, backend, or serialized scene data, and must call out any unverified local secret dependency.
- Prevention rules for future work:
  - Never leave provider API keys serialized into Unity scenes or prefabs.
  - Add configuration coverage for credential resolution precedence whenever a runtime can read both inspector and environment values.
  - Treat local secret sourcing as a first-class handoff item for AI features, not an implementation afterthought.
- Follow-up actions:
  - Add EditMode tests that fail if `Town.unity` contains a serialized OpenAI API key.
  - Add EditMode tests that prove `OPENAI_API_KEY` wins over any stale inspector override.
  - Remove the stale serialized key and update the resolver accordingly.
- Actual root cause after fix: `Town.unity` still serialized a stale `apiKey` on `OpenAIClient`, and `ResolveApiKey()` preferred that inspector value over `OPENAI_API_KEY`, so the runtime kept sending the bad scene key.
- Guardrail added: `OpenAIClientConfigurationTests` now verifies both environment-first key resolution and that `Town.unity` does not serialize an OpenAI key.
- Distilled rule added to project-memory.md: `OpenAI credentials must come from environment-first runtime config, never serialized Town scene data (2026-04-13)`

### Town Early-Turn Choice Flow Fell Back To Continue/Goodbye Too Soon (2026-04-13)
- Status: Addressed
- Related story/task: Town direct text streaming and follow-up option flow
- Original completion claim: The Town scene was handed off as streaming correctly with usable post-response dialogue options.
- Reported issue: The developer reported that `Continue...` and `Goodbye.` still show up too early, and that `Goodbye.` should instead arrive as one of the four follow-up choices after a few steps.
- Failing evidence: Developer report after using the updated Town flow: the opening turns still surface the generic fallback buttons instead of a staged four-choice conversation ladder.
- Approach that produced the miss: The streaming fix focused on spoken-text rendering and payload parsing, but left the post-turn option source on the generic fallback path whenever the model returned plain text instead of structured options.
- Why it was mistaken for done: Verification proved the text path and request shape, but did not treat the option cadence itself as a first-class player-facing behavior that needed dedicated regression coverage.
- What should have been verified or stated differently: A Town dialogue handoff must explicitly verify the early-turn choice cadence, not just that some buttons appear after a line completes.
- Prevention rules for future work:
  - Add option-flow coverage that checks opening turns, mid turns, and the point where `Goodbye.` becomes available.
  - When live model output is text-only, source interactive options from a deliberate conversation ladder instead of defaulting to generic fallback buttons.
  - Treat response latency tuning and option cadence as separate UX concerns with their own checks.
- Follow-up actions: Added `TownConversationFlowTests` to verify that the opening turns do not surface `Continue...` or `Goodbye.`, that later turns unlock `Goodbye.` inside a four-choice set, and that the Town scene uses the intended fast model budget. Reduced the Town scene output budget from 300 to 140 tokens and aligned the `OpenAIClient` default with that cap to tighten response time without changing the model. A later refinement replaced the temporary staged ladder with a response-aware composer so the option text itself now follows the streamed reply instead of a canned sequence.
- Distilled rule added to project-memory.md: `Town dialogue tests must cover option cadence, not only streamed text (2026-04-13)`

### Town Dialogue Choices Felt Scripted Against The Actual Reply (2026-04-13)
- Status: Addressed
- Related story/task: Town direct text streaming and follow-up option flow refinement
- Original completion claim: The Town scene was handed off with early-turn `Continue...` and `Goodbye.` removed in favor of a staged four-choice ladder.
- Reported issue: The developer reported that the flow still felt off and asked for choices that read naturally against whatever the NPC had just said.
- Failing evidence: Developer follow-up after the cadence fix: the fallback flow still felt wrong even when the generic `Continue...` and early `Goodbye.` buttons were gone.
- Approach that produced the miss: The first option-cadence fix replaced generic fallback buttons with a scripted turn ladder, but that ladder still ignored the content of the streamed reply and could surface prompts that felt disconnected from the current line.
- Why it was mistaken for done: Verification proved the turn cadence and `Goodbye.` timing, but did not treat the option copy itself as a response-aware UX surface that needed content-sensitive coverage.
- What should have been verified or stated differently: A Town dialogue handoff should not imply the follow-up flow feels natural unless the four choices are checked against the actual reply text, not just against the turn number.
- Prevention rules for future work:
  - Build Town follow-up options from the streamed reply text first, then layer cadence rules like late `Goodbye.` unlocks on top.
  - Add regression coverage that proves keyword-heavy replies such as cooking, town-history, and gossip beats produce matching follow-up prompts.
  - Treat "buttons appear in the right phase" and "buttons read naturally for this reply" as separate verification axes.
- Follow-up actions: Added a pure C# `TownDialogueOptionComposer` in `Core/` and routed `LLMConversationController` through it so displayed choices now prefer meaningful model options, derive topic-aware follow-ups from the parsed reply text, rotate NPC-specific evergreen prompts, and only inject a natural goodbye line after later turns. Kept the existing Town conversation flow tests for goodbye cadence and used them to lock in Garrett cooking and history follow-ups against the new composer.
- Distilled rule added to project-memory.md: `Town dialogue choices must be composed from the latest reply, not only the turn index (2026-04-13)`

### Town Responses Request Used The Wrong Content Type For Assistant History (2026-04-13)
- Status: Addressed
- Related story/task: Town direct text streaming via the OpenAI Responses API
- Original completion claim: The Town scene was handed off as streaming through the Responses API with local conversation history passed back into subsequent turns.
- Reported issue: The developer hit a 400 from OpenAI: `Invalid value: 'input_text'. Supported values are: 'output_text' and 'refusal'.`
- Failing evidence: Runtime log from `LLMConversationController` showing the request failed at `input[1].content[0]` when a prior assistant turn was included in the conversation history.
- Approach that produced the miss: `OpenAIClient.BuildRequestJson()` encoded every non-system history item as a typed content array with `type: "input_text"`, including assistant messages.
- Why it was mistaken for done: The request-shape verification only covered a system plus user turn and never exercised a multi-turn history that included an assistant reply, so the invalid assistant serialization path stayed invisible.
- What should have been verified or stated differently: Any manual conversation-state implementation for the Responses API must verify both user and assistant history serialization against the documented multi-turn format before handoff.
- Prevention rules for future work:
  - Add request-builder coverage that includes at least one assistant history turn whenever conversation state is serialized manually.
  - Prefer the documented string `content` message form for text-only history unless typed content items are required by the feature.
  - Treat request-shape validation and stream parsing as separate guardrails; one passing does not imply the other is sound.
- Follow-up actions: Updated `OpenAIClient.BuildRequestJson()` to serialize text-only history as plain message strings instead of typed `input_text` content arrays, which aligns with the official Responses API conversation-state examples for alternating user and assistant turns. Expanded the request-builder regression to include an assistant message and assert that no `input_text` item leaks into the serialized payload.
- Distilled rule added to project-memory.md: `Responses API history tests must cover assistant turns (2026-04-13)`

### Town Streaming Finalized With Raw JSON Still Visible (2026-04-13)
- Status: Addressed
- Related story/task: Town title-screen slice plus direct NPC text streaming in `Town.unity`
- Original completion claim: The Town scene was handed off as having direct text streaming through the OpenAI Responses API, with the UI no longer scraping partial JSON from the raw stream.
- Reported issue: The developer reported that the visible dialogue still showed the final JSON payload instead of only the NPC's spoken line.
- Failing evidence: Town scene screenshot showing `Old Garrett` with the literal payload `{"response":"Oh, bless my soul!...","options":[...]}` rendered in the dialogue body while fallback buttons remained visible.
- Approach that produced the miss: The transport and UI were moved onto direct text deltas, but the end-of-turn compatibility parser still fell back to the full raw payload when the model returned a legacy JSON object and the final parser path did not reliably extract `response` and `options`.
- Why it was mistaken for done: Verification focused on the streaming path itself and did not include a regression that exercised the final payload parser with a JSON-shaped completion. The handoff overestimated end-to-end confidence from the delta path alone.
- What should have been verified or stated differently: A "streaming works" claim for Town NPC dialogue must cover both the incremental delta display and the final payload normalization path, including legacy JSON completions and option extraction.
- Prevention rules for future work:
  - Add a parser regression test for any legacy JSON completion shape that can still arrive from the model or prompt drift.
  - Add a golden-set eval that fails when outputs leak raw JSON, option arrays, code fences, or other non-spoken payload formatting.
  - Treat stream rendering and final turn normalization as separate verification axes in the handoff.
- Follow-up actions: Added an EditMode regression that invokes `LLMConversationController.TryParseResponse()` with the legacy JSON payload shape from the Town failure and asserts that only the spoken `response` plus structured `options` survive. Reworked the compatibility parser in `LLMConversationController` so it extracts `response` and `options` manually before falling back, which keeps direct streaming intact while preventing raw JSON from reaching the dialogue body. Added a repo-local Town golden-set eval under `docs/evals/` plus `tools/evals/town_llm_golden_eval.py` so prompt or model changes fail quickly if they leak payload formatting back into the scene.
- Distilled rule added to project-memory.md: `Town streaming verification must cover both delta rendering and final payload normalization (2026-04-13)`

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

### FarmMain Scene Merge Left A Stale Tutorial Script On FarmController (2026-04-13)
- Status: Addressed
- Related story/task: Scene 7 tomato task-driven minigame lifecycle after teammate scene/tool merge
- Original completion claim: The tomato stage-minigame flow was previously handed off as present in Scene 7, and later pull/integration work was summarized as keeping local farming changes intact while syncing teammate updates.
- Reported issue: The developer reported that the farming-step minigames no longer show.
- Failing evidence: Repo inspection of [`Assets/_Project/Scenes/FarmMain.unity`](/Users/youss/My%20project/Assets/_Project/Scenes/FarmMain.unity) found `FarmController` carrying a stale `MonoBehaviour` entry with missing script GUID `1cf442307b7db40ffa99802cf172a9bc` ahead of the live `TutorialFarmSceneController`.
- Approach that produced the miss: Scene integration focused on code paths and stash contents, but did not perform a scene-level integrity check for missing scripts after the `FarmMain.unity` merge.
- Why it was mistaken for done: The underlying minigame code still existed in `CropLifecycleProfile`, `CropPlotState`, and `FarmPlotInteractionController`, which made the feature look intact from a code diff alone while the scene root still contained stale merged wiring.
- What should have been verified or stated differently: After merging or restoring Unity scenes, verification should include a missing-script sweep on the relevant controller roots, not just source-file inspection.
- Prevention rules for future work:
  - After any Unity scene merge, stash replay, or large scene pull, scan the touched scenes for stale script GUIDs and duplicate controller components.
  - Treat controller-root missing scripts as P1 regressions for tutorial scenes because they can invalidate runtime behavior while leaving feature code apparently intact.
  - Add scene-integrity tests for critical tutorial scenes so merged YAML regressions fail fast in EditMode.
- Follow-up actions: Added a `FarmMainScene_FarmControllerHasNoMissingScripts` edit-mode regression test and removed the stale `FarmController` tutorial-script block from `FarmMain.unity`. Unity batch tests remained blocked because the editor already had the project open.
- Distilled rule added to project-memory.md: `Unity Scene Merges Need Missing-Script Sweeps (2026-04-13)`

### FarmMain Scene Integrity Test Missed FarmSimDriver Namespace Import (2026-04-13)
- Status: Addressed
- Related story/task: FarmMain scene-integrity regression test for missing scripts
- Original completion claim: The stale `FarmController` script block was removed and a regression test was added to catch the issue going forward.
- Reported issue: The developer hit `Assets/Tests/EditMode/TutorialSceneConfigurationTests.cs(242,53): error CS0246: The type or namespace name 'FarmSimDriver' could not be found`.
- Failing evidence: The new test referenced `FarmSimDriver` without importing `FarmSimVR.MonoBehaviours.Farming`.
- Approach that produced the miss: The scene fix added a new type assertion to an existing test file, but the final compile-oriented import sweep only focused on the scene YAML change and not the new test's namespace dependencies.
- Why it was mistaken for done: The test logic itself was correct, so source review over-weighted the behavioral assertion and missed the missing namespace import while Unity compile execution was blocked by the open editor.
- What should have been verified or stated differently: Any new test reference to a MonoBehaviour outside the current namespace set needs the same explicit `using` scan as production code when compile verification is unavailable.
- Prevention rules for future work:
  - Include test files in compile-oriented namespace/import sweeps after adding new typed assertions.
  - When Unity compile is blocked, do not treat a new test as complete until every referenced project type is matched to an explicit `using` or fully qualified name.
- Follow-up actions: Added the missing `using FarmSimVR.MonoBehaviours.Farming;` import to `TutorialSceneConfigurationTests.cs` and re-scanned the file header for the remaining referenced namespaces.
- Distilled rule added to project-memory.md: `Namespace Imports After Cross-Assembly Wiring (2026-04-11)`

### Tutorial Tomato Patch Was Present But Hidden From The Spawn Read (2026-04-13)
- Status: Addressed
- Related story/task: Scene 7 tomato minigame accessibility in `FarmMain`
- Original completion claim: The minigame regression was summarized as fixed after scene-controller cleanup because the tutorial tomato hero plot still existed and remained wired to `TutorialFarmSceneController`.
- Reported issue: The developer still could not see a tomato patch in the scene to try the minigames on.
- Failing evidence: `FarmMain.unity` still placed the hero tomato plot prefab override at local position `(0, 0.01, 4)` under `FarmPlots` and named it `CropPlot_0`, which left it outside the obvious spawn read and indistinguishable from the generic plot row during inspection.
- Approach that produced the miss: Verification focused on missing scripts and prompt wiring, but did not confirm that the dedicated tutorial plot was physically visible and uniquely identifiable from the starting play space.
- Why it was mistaken for done: The scene contained a valid hero plot reference, so source-level checks suggested the feature existed even though the player-facing read of the space still failed.
- What should have been verified or stated differently: The scene handoff needed an explicit "from spawn, can I immediately spot and reach the tutorial patch?" check, plus a unique scene name for the hero plot.
- Prevention rules for future work:
  - Critical tutorial interactables need unique scene names so merge review and tests can target them directly.
  - Scene verification for playable loops must include physical visibility and reachability from the spawn area, not only serialized references.
  - When a tutorial patch is special-purpose, prefer a dedicated lane position over leaving it mixed into ambiguous generic naming.
- Follow-up actions: Renamed the hero plot to `CropPlot_TutorialTomato`, moved it into the front-right patch lane near `PlayerSpawn`, and added an EditMode regression test that asserts the tutorial controller still points at that visible plot.
- Distilled rule added to project-memory.md: `Tutorial Plots Need Unique Names And Spawn-Visible Placement (2026-04-13)`

### FarmMain Tool Props Used Wrapper Prefabs With Missing Second-Hop Sources (2026-04-13)
- Status: Addressed
- Related story/task: `FarmMain` scene-open failures after the tutorial tomato/minigame scene repairs
- Original completion claim: The scene-level handoff focused on tutorial plot visibility and prompt/minigame wiring, with the expectation that `FarmMain` would remain openable after the scene edits.
- Reported issue: Unity reported missing prefab assets while opening `Assets/_Project/Scenes/FarmMain.unity` for `SpadingFork`, `Bucket`, `Spade`, `Hoe`, and `Sickle`.
- Failing evidence: The five prefabs under `Assets/ResilientLogicGames/ToolsLowPolyPackLite/Prefabs/` still existed by GUID, but each file was only a `PrefabInstance` wrapper whose `m_SourcePrefab` pointed at a missing inner source GUID (`6b58c11b87f85e144aeaa25c638a21f3`, `b78750eed6179e54f85d89fd770e6cc9`, `b83de2dc15ae7724d813d14ee94d8522`, `8e0274d0422891640b7ee8811ae16a5f`, `70dc12851c11b444aa5573d6ea1e3292`).
- Approach that produced the miss: Verification stopped at scene YAML references and did not inspect whether the referenced decorative prefabs themselves still resolved to healthy imported assets.
- Why it was mistaken for done: The outer prefab GUIDs were present in the repo, which made the scene references look valid in text search even though the actual asset import chain was broken one level deeper.
- What should have been verified or stated differently: Scene-open validation needed an asset-health pass on referenced prefabs, especially third-party wrappers, instead of assuming that a present `.prefab` file implies a valid importable asset.
- Prevention rules for future work:
  - When a scene depends on wrapper prefabs, verify the wrapper is a standalone/importable asset or that every nested source GUID resolves locally.
  - Treat scene-open editor errors as a separate verification axis from C# compile/test health.
  - Add regression coverage for third-party decorative prefabs that the scene relies on so missing second-hop sources fail before manual scene-open.
- Follow-up actions: Replaced the five broken wrapper prefab files with standalone prefab assets backed by in-repo prop meshes, preserving the existing wrapper GUIDs and root object IDs so `FarmMain` and demo scenes can keep their current references.
- Distilled rule added to project-memory.md: `Wrapper Prefabs Must Not Depend On Missing Second-Hop Sources (2026-04-13)`

### Run-Tests Harness Treated The Open Editor As A Terminal Failure (2026-04-13)
- Status: Addressed
- Related story/task: EditMode verification for the Scene 7 farming/minigame fixes
- Original completion claim: Test verification was repeatedly reported as blocked because the Unity editor already had the project open.
- Reported issue: The developer pointed out that the test runner design was faulty and should be redesigned to complete its intended use instead of just skipping.
- Failing evidence: `.ai/scripts/run-tests.sh` returned `SKIPPED` with exit code `2` whenever batchmode hit `another Unity instance is running with this project open`, so `./run-tests.sh editmode` could not perform the requested verification while the editor was open.
- Approach that produced the miss: The harness only attempted to run batchmode on the live project path and treated Unity's project lock as the end of the workflow instead of a condition the harness should work around.
- Why it was mistaken for done: The prior handoff focused on accurately describing the lock, but that still left the command unable to do the one thing it exists to do: run tests.
- What should have been verified or stated differently: The test runner itself needed a fallback execution path, not just a note that the primary path was blocked.
- Prevention rules for future work:
  - Verification harnesses should degrade into a workable alternative path when a common local constraint is predictable and automatable.
  - Do not stop at "environment blocked" when the harness can safely run against a disposable copy of on-disk state.
  - Add a regression around the fallback path so future harness refactors do not silently reintroduce skip-only behavior.
- Follow-up actions: Added `.ai/tests/run-tests-lock-fallback.sh` as a regression, redesigned `.ai/scripts/run-tests.sh` to detect a live editor and run Unity against a disposable cloned project, and replaced the flaky CLI `-runTests` path with a repo-owned `BatchmodeTestRunner` execute-method that saves NUnit XML before exit. The harness now runs the real root command while the editor stays open and surfaces actual failing tests instead of skipping.
- Distilled rule added to project-memory.md: `Unity Harnesses Need Editor-Lock Fallbacks (2026-04-13)`

### Town Voice Streaming Was Wired But Still Pointed At A Dead Local Port (2026-04-14)
- Status: Addressed
- Related story/task: `TOWN-002` Town NPC voice streaming
- Original completion claim: The Town slice already had the ElevenLabs voice path wired through Unity, the story-orchestrator token broker, and per-character voice profiles.
- Reported issue: The live Town scene still could not mint a token because `TownNpcVoiceStreamController` was serialized to `http://127.0.0.1:8000` while the actual local story-orchestrator was running on `http://127.0.0.1:8011`.
- Failing evidence: `curl http://127.0.0.1:8000/health` returned no listener, `curl http://127.0.0.1:8011/health` returned `200`, and `Assets/_Project/Scenes/Town.unity` still had `tokenServiceBaseUrl: http://127.0.0.1:8000`.
- Approach that produced the miss: Verification focused on the existence of the text stream, the backend token route, and the scene-side voice controller, but it did not exercise the actual runtime token endpoint resolution path against the active local backend.
- Why it was mistaken for done: The architecture was present in source, so the feature looked complete on inspection even though the player-facing token fetch was pinned to a stale local port.
- What should have been verified or stated differently: A voice-streaming completion claim needed one end-to-end check that the Town scene could actually reach the active local token broker, not just that the broker and client components both existed.
- Prevention rules for future work:
  - Local backend integrations in Unity must support an override or fallback path instead of assuming one hardcoded localhost port.
  - When a feature depends on a local service, verify the scene/client hits the currently running endpoint before calling the workflow complete.
  - Distinguish "integration pieces exist" from "the live endpoint resolves successfully" in the handoff.
- Follow-up actions:
  - Add a tested token-service endpoint resolver for Town voice streaming.
  - Update the runtime to try the active local story-orchestrator endpoint instead of trusting one stale serialized port.
- Actual root cause after fix: `TownNpcVoiceStreamController` trusted one serialized `tokenServiceBaseUrl` value from `Town.unity` and never tried an environment override or the alternate local orchestrator port. On this machine the backend was healthy on `127.0.0.1:8011`, so the scene-side token fetch failed before voice streaming could start.
- Guardrail added: `TownVoiceTokenServiceEndpointResolver` now builds ordered candidate base URLs from an explicit environment override, the serialized scene value, and bounded local fallbacks (`8000`, `8011`). `TownVoiceTokenServiceClient` probes those candidates before giving up, and `TownVoiceStreamingTests` locks the override ordering plus local fallback behavior.
- Distilled rule added to project-memory.md: `Town local backend integrations must not depend on one hardcoded localhost port (2026-04-14)`


### Generate Unique Playthrough Finished But Never Enabled Play (2026-04-14)
- Status: Addressed
- Related story/task: GSO-021 LLM-directed story sequence generation
- Original completion claim: The real generative title-screen flow was handed off as wired to the live story-sequence backend with a generated first turn landing in the standing slice package.
- Reported issue: The developer reported that `Generate Unique Playthrough` completes but `Play Unique Playthrough` remains disabled.
- Failing evidence: Developer report from the title screen after generation finishes: the prepare step ends, but the play button never becomes interactable.
- Approach that produced the miss: The backend live smoke was verified through direct API/package output, but the exact Unity-side prepared-state handoff from generation completion into button interactivity was not re-verified in the title-screen state machine.
- Why it was mistaken for done: Verification proved the providers could generate assets and update the package, but it did not prove that Unity stored the prepared generated session and refreshed the button state after the async flow returned.
- What should have been verified or stated differently: The handoff should have separated backend generation success from title-screen readiness success and included one explicit UI-state verification that `Play Unique Playthrough` becomes enabled after a successful prepare.
- Prevention rules for future work:
  - Backend generation success is not enough; generated-slice handoffs must verify the title-screen prepared-state transition and button interactivity.
  - Async title-screen flows must log each phase transition so a stuck disabled button can be diagnosed from one play-mode run.
- Follow-up actions:
  - Added `TitleScreenManager_Update_RestoresPreparedGeneratedPlaythroughStateFromRuntimeController` to lock recovery when runtime state is prepared but the UI has not re-enabled play yet.
  - Added conditional generated-slice diagnostics across the title-screen manager, runtime controller, and Unity HTTP client so one play-mode run shows create, advance, payload-apply, ready, failure, and load transitions.
- Actual root cause after fix: `TitleScreenManager` only enabled `Play Unique Playthrough` on the direct prepared callback path. If the runtime controller already held a prepared session and entry scene but the UI state had not been advanced to `Ready`, the button stayed disabled because nothing reconciled the title-screen controls back from the runtime state.
- Guardrail added: `TitleScreenManager.Update()` now reconciles the generate/play buttons and ready status from `StorySequenceRuntimeController.HasPreparedSequence`, and the generated-slice diagnostics logs show each phase so callback/state mismatches are visible immediately.
- Distilled rule added to project-memory.md: `Generated title-screen slices must reconcile button state from runtime prepared state, not only from one callback path (2026-04-14)`

### Better Title Screen Was Hidden Behind Editor-Only Compilation (2026-04-14)
- Status: Addressed
- Related story/task: generated title-screen slice UX refresh
- Original completion claim: The updated title-screen flow with separate generate/play actions and diagnostics was treated as the current improved title-screen experience.
- Reported issue: The developer reported that after pulling changes, the better title screen was not present.
- Failing evidence: Git history showed the larger title-screen overhaul existed only as local changes, and source inspection showed `TitleScreenManager.CreateTutorialSliceLauncher()` was wrapped in `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, so the improved launcher never appeared in a normal build even when the local code was present.
- Approach that produced the miss: The improved title-screen surface was implemented as runtime-generated UI for fast iteration, but it was left behind an editor/development compile guard without an explicit shipping decision or regression against that gate.
- Why it was mistaken for done: Editor-side verification always saw the new launcher, which masked the fact that a normal build compiled the whole surface out.
- What should have been verified or stated differently: The handoff needed to distinguish editor-only tooling from the player-facing title screen and state whether the improved launcher was intended to ship.
- Prevention rules for future work:
  - Any player-facing title-screen improvement must be verified in shipping compilation conditions, not only in the editor.
  - Runtime-generated menu UI should not live behind editor-only compile guards unless the spec explicitly marks it as debug tooling.
  - When a UX refresh is considered the better default, add a regression that fails if a compile guard removes it from non-development builds.
- Follow-up actions:
  - Add a regression test that fails if `CreateTutorialSliceLauncher()` is guarded by `UNITY_EDITOR` or `DEVELOPMENT_BUILD`.
  - Remove the compile guard so the improved title-screen launcher ships in normal builds.
- Actual root cause after fix: The improved title-screen surface was implemented as runtime-generated UI but accidentally left inside an editor/development-only compile block, so editor verification always showed the better launcher while normal builds compiled it out.
- Guardrail added: `TitleScreenManager_CreateTutorialSliceLauncher_IsNotEditorOnly` now fails if the launcher is wrapped in the old compile guard, and `CreateTutorialSliceLauncher()` now runs in all builds.
- Distilled rule added to project-memory.md: `Player-facing title-screen UI must not be hidden behind UNITY_EDITOR or DEVELOPMENT_BUILD guards unless it is explicitly debug-only (2026-04-14)`


### Generated Playthrough Loaded Authored Post-Chicken Cutscene Instead Of Runtime Storyboard (2026-04-14)
- Status: Addressed
- Related story/task: GSO-021 LLM-directed story sequence generation
- Original completion claim: The generated playthrough was handed off as using the live generated first cutscene beat through the standing title-screen slice.
- Reported issue: The developer reported that `Play Unique Playthrough` still shows the old authored `Once caught, the chicken quickly became docile...` cutscene instead of the generated storyboard.
- Failing evidence: Developer playthrough report plus source inspection of `TutorialSceneInstaller` showing `PostChickenCutscene` explicitly skips story-package-driven installation.
- Approach that produced the miss: The backend/runtime package generation path was wired and the title-screen launch target was retargeted, but the specific post-chicken scene remained on its old scene-owned slideshow/timeline content path.
- Why it was mistaken for done: Verification proved generation and package writing, but it did not verify that the loaded scene actually consumed the runtime override package for the first generated bridge beat.
- What should have been verified or stated differently: The handoff should have explicitly checked that the first generated cutscene scene uses `StoryPackageRuntimeCatalog` content rather than only launching the correct scene name.
- Prevention rules for future work:
  - A generated scene launch is not enough; verify the loaded scene binds runtime package content instead of scene-authored fallback content.
  - Any installer special-case that bypasses package-driven cutscene setup must be revisited when that scene becomes a generated entry beat.
- Follow-up actions:
  - Add a regression proving PostChicken cutscene installation consumes runtime storyboard data.
  - Patch the installer or scene binding to replace the authored bridge content with the generated storyboard for runtime package launches.
- Actual root cause after fix: `StorySequenceRuntimeController` was preparing and importing the generated runtime package correctly, but `TutorialSceneInstaller` still special-cased `PostChickenCutscene` as a scene-owned slideshow/timeline and never created a `TutorialCutsceneSceneController` from the runtime package. The authored `SlideshowPanel` / `SlideshowDirector` path therefore remained active and surfaced the old baked cutscene.
- Guardrail added: `TutorialSceneConfigurationTests.Installer_PostChickenCutscene_UsesRuntimeStoryboardAndDisablesAuthoredSlideshow_WhenGeneratedBeatExists` now proves the generated storyboard is bound for PostChicken, `StoryPackageRuntimeCatalogTests.Installer_InjectsStoryboardCutscene_OnPostChickenScene` locks the installer contract, and `TutorialSceneInstaller` now disables the authored slideshow objects when it installs the runtime cutscene controller for that scene.
- Distilled rule added to project-memory.md: `Generated entry cutscenes must not keep installer bypasses for authored slideshow/timeline content once they become runtime package beats (2026-04-14)`

### Generated Story And Town Voice Required Manual Local Backend Startup (2026-04-14)
- Status: Addressed
- Related story/task: generated story slice bootstrap and local story-orchestrator integration
- Original completion claim: The generated-story flow and Town voice path were handed off as usable with the local story-orchestrator running on `127.0.0.1:8012`.
- Reported issue: The developer asked for the backend to be launched with the game so the earlier `Cannot connect to destination host` failure cannot recur as a manual startup miss.
- Failing evidence: Earlier Unity logs showed generated-story requests cycling through local ports and failing to connect until the orchestrator was started by hand outside Unity.
- Approach that produced the miss: The implementation assumed the local Python orchestrator would already be running and treated backend reachability as an operator concern instead of part of the runtime bootstrap contract.
- Why it was mistaken for done: Verification proved that the feature works with a healthy backend, but it did not close the loop on how that backend gets started for a normal dev play session.
- What should have been verified or stated differently: The handoff should have said explicitly that the local orchestrator still required manual startup, or the runtime should have owned that startup itself.
- Prevention rules for future work:
  - Local AI backends that are required for a first-party game flow must either auto-start from the game in development or surface a built-in bootstrap action, not rely on hidden terminal steps.
  - Backend-health verification is incomplete unless the runtime path covers the server lifecycle the developer is expected to use.
- Follow-up actions:
  - Add a regression proving the generated-story request path ensures local orchestrator readiness before the HTTP request runs.
  - Add a dev launcher path that warms the local story-orchestrator when the game starts and before story/voice requests.
- Actual root cause after fix: The generated-story and Town voice clients only knew how to call the local Python orchestrator after it was already running. No Unity-side bootstrap existed, so a fresh editor/game session could hit `Cannot connect to destination host` until the developer launched `uvicorn` manually.
- Guardrail added: `TitleScreenManager` now warms the local story-orchestrator in the background on startup, `StorySequenceRuntimeController` blocks generated-story requests until local orchestrator readiness succeeds, `TownVoiceTokenServiceClient` runs the same readiness bootstrap before requesting ElevenLabs voice tokens, and the repo-owned `backend/story-orchestrator/start_local_backend.sh` script gives Unity one stable launch path to invoke. `StorySequenceRuntimeBridgeTests.StorySequenceRuntimeController_BeginSequencePreparationRoutine_WaitsForLocalOrchestratorReadinessBeforeRequest` locks the request ordering, and `TutorialSceneConfigurationTests.TitleScreenManager_Start_WarmsLocalStoryOrchestratorInBackground_FromSource` guards the title-screen warm-up hook.
- Distilled rule added to project-memory.md: `Local AI backends required for first-party dev flows must have a Unity-owned bootstrap path, not a hidden manual terminal prerequisite (2026-04-14)`

### Standalone Portal Scenes Assumed CoreScene Was Already Loaded (2026-04-14)
- Status: Addressed
- Related story/task: portal traversal from title-screen standalone scenes
- Original completion claim: Town, FarmMain, and related portal scenes were left launchable/usable outside the additive CoreScene flow.
- Reported issue: The developer hit `[PortalTrigger] PortalManager.Instance is null. Is PortalManager in the Core scene?` when entering a portal trigger.
- Failing evidence: Runtime stack trace from `PortalTrigger.OnTriggerEnter` at `Assets/_Project/Scripts/MonoBehaviours/Portal/PortalTrigger.cs:32`.
- Approach that produced the miss: The portal runtime assumed `CoreScene` would always be the bootstrap entry and never added a fallback path for scenes opened directly from the title screen or editor.
- Why it was mistaken for done: Verification covered additive portal wiring with `CoreScene`, but it did not prove that standalone portal scenes could self-host the required runtime manager.
- What should have been verified or stated differently: The handoff should have called out that portal scenes depended on `CoreScene`, or the runtime should have created that dependency automatically when the scene was loaded standalone.
- Prevention rules for future work:
  - Any scene that contains a `PortalTrigger` and is launchable standalone must either include its own portal runtime bootstrap or prove `CoreScene` is always loaded first.
  - Scene-level smoke coverage must include the title-screen/editor direct-launch path for standalone gameplay slices, not only the intended additive flow.
- Follow-up actions:
  - Added `PortalRuntimeBootstrapTests` to lock the standalone-bootstrap contract.
  - Added a runtime bootstrap path that creates a transient `PortalManager` for already-loaded portal scenes when no manager exists yet.
- Actual root cause after fix: `PortalManager` only existed in `CoreScene`, while standalone portal scenes could load with either a known controller type or only a generic `CharacterController`. The bootstrap path was too narrow about which player rigs it recognized, and the singleton registration was not explicitly locked in the edit-mode regression harness, so the scene could still reach a `PortalTrigger` with no live manager.
- Guardrail added: `PortalRuntimeBootstrap` now falls back to any loaded `CharacterController` when the known portal-scene controller types are absent, and `PortalRuntimeBootstrapTests` now prove the bootstrap works for known controller rigs, generic controller rigs, and the no-trigger no-op path.
- Distilled rule added to project-memory.md: `Standalone scenes with PortalTrigger components must self-bootstrap portal runtime when CoreScene is not part of the entry path (2026-04-14)`

### Story-Orchestrator Bootstrap Failures Were Too Opaque (2026-04-14)
- Status: Addressed
- Related story/task: generated playthrough local backend bootstrap
- Original completion claim: Unity-owned local backend bootstrap was handed off as the fix for manual story-orchestrator startup.
- Reported issue: The developer still saw "unable to start story-orchestrator" and asked whether tokens needed to be hardcoded somewhere.
- Failing evidence: Runtime generated-playthrough status only reported that the local story-orchestrator did not become healthy and pointed at `/tmp/story-orchestrator-8012.log`, without surfacing missing venv or `.env.local` guidance in the Unity-side error path.
- Approach that produced the miss: The launcher automation was added, but its failure reporting stopped at health polling and delegated all real diagnosis to a log file that the in-game message did not summarize.
- Why it was mistaken for done: Verification proved the happy path with a healthy local backend, but it did not verify that a broken local bootstrap would explain where provider keys and Python setup actually belong.
- What should have been verified or stated differently: The handoff should have included one failure-path check proving Unity explains the local venv and `backend/story-orchestrator/.env.local` contract directly, without nudging the developer toward hardcoded secrets.
- Prevention rules for future work:
  - Local backend bootstrap failures must surface actionable setup guidance in-product, not only a raw "not healthy" timeout plus a log path.
  - Secret-dependent local flows must point at repo-local env files and explicitly avoid hardcoded Unity-scene secrets.
- Follow-up actions:
  - Added `LocalStoryOrchestratorLauncherTests` to lock source-level guidance for launcher logging and `.env.local` messaging.
  - Updated the launcher script to log startup preflight, load `.env.local`, and warn about missing provider keys.
  - Updated Unity bootstrap failure messages to point at `.env.local`, `pip install -r requirements.txt`, and relevant log summaries.
- Actual root cause after fix: The backend launcher could fail for ordinary local setup reasons, but the Unity-side message collapsed those into a generic health failure. That made the operator guess about hardcoding keys instead of checking the repo-local backend env and venv setup.
- Guardrail added: `LocalStoryOrchestratorLauncher` now builds actionable failure messages with `backend/story-orchestrator/.env.local` guidance, venv bootstrap instructions, and log summaries, while `start_local_backend.sh` writes its own startup diagnostics into the launcher log.
- Distilled rule added to project-memory.md: `Unity-owned local backend bootstraps must explain env-file and venv setup directly when startup fails; do not leave secret/config diagnosis hidden in logs (2026-04-14)`

### Generated Playthrough Could Stall When Unity Lost Focus (2026-04-15)
- Status: Addressed
- Related story/task: generated playthrough title-screen reliability
- Original completion claim: The generated playthrough path was handed off as reliable once Unity owned local backend bootstrap and prepared-state recovery.
- Reported issue: The developer asked for the flow to remain reliable even if they click away from the game window.
- Failing evidence: Source inspection showed `ProjectSettings/ProjectSettings.asset` still had `runInBackground: 0`, and `TitleScreenManager.Start()` did not explicitly opt the app into background execution. That leaves long-running title-screen generation vulnerable to focus-loss stalls on desktop.
- Approach that produced the miss: Earlier fixes focused on backend readiness, provider fallbacks, and prepared-state recovery, but they did not verify the desktop player-loop behavior while the app was unfocused.
- Why it was mistaken for done: The happy path was exercised with the Unity window in focus, which masked the fact that losing focus could pause the request/coroutine loop even with a healthy backend.
- What should have been verified or stated differently: The handoff should have included one explicit background-safety check for the title-screen generation path or called out that focus had to stay on the app.
- Prevention rules for future work:
  - If a developer-facing generation flow may take multiple seconds, verify it survives desktop focus loss.
  - Do not rely only on `ProjectSettings.asset`; add a runtime guard on the actual entry path for the flow.
- Follow-up actions:
  - Added `GeneratedPlaythroughBackgroundReliabilityTests` to lock both the runtime guard and the project setting.
  - Set `Application.runInBackground = true;` in `TitleScreenManager.Start()`.
  - Flipped `ProjectSettings.asset` to `runInBackground: 1`.
- Actual root cause after fix: The generated title-screen flow had become backend-safe but not background-safe. Unity still defaulted to pausing when unfocused, so generation could appear stuck if the developer clicked away mid-request.
- Guardrail added: The project now enables background execution globally and the generated-story title-screen entry path reasserts that behavior at runtime.
- Distilled rule added to project-memory.md: `Generated story reliability must survive focus loss (2026-04-15)`

### Runtime Session Creation Still Blocked The First Turn Inside The HTTP Request (2026-04-15)
- Status: Addressed
- Related story/task: standalone Railway runtime service for generated playthroughs
- Original completion claim: The standalone runtime API was handed off as a queued-job contract where Unity would `POST /api/runtime/v1/sessions`, receive a `job_id`, then poll `/jobs/{job_id}` until the first turn was ready.
- Reported issue: A live Unity-to-Railway smoke timed out for the full request budget on `POST /api/runtime/v1/sessions`, even though the service later completed the turn server-side.
- Failing evidence: The Unity smoke test failed after `330005 ms` with `Curl error 28` and no bytes received, while Railway HTTP logs showed the same route eventually returning `201` only after the generation work had finished. Source-level inspection of `RuntimeSessionService.create_session()` showed `_create_next_job()` calling `_run_job()` inline.
- Approach that produced the miss: The public API shape was changed to look asynchronous, but the service implementation still generated the first turn synchronously in the request thread and only returned once the work was done.
- Why it was mistaken for done: Earlier verification focused on backend health, payload validity, and a successful generated turn, but it did not prove that the runtime contract returned immediately and let Unity move into a poll-based state.
- What should have been verified or stated differently: The handoff should have included one explicit regression proving that `POST /api/runtime/v1/sessions` returns a queued job before a deliberately slow generation service finishes, plus one live check that the deployed service behaves the same way over real HTTP.
- Prevention rules for future work:
  - If an API contract says "enqueue and poll," the create endpoint must be verified to return before generation completes.
  - A local passing generation flow is not enough for Unity-facing runtime APIs; verify the live HTTP timing contract separately from the final media payload.
  - Recovery replay must not consume the same worker capacity that fresh live requests depend on.
- Follow-up actions:
  - Added `RuntimeSessionCreateContractTests.test_runtime_session_create_returns_before_generation_finishes`.
  - Moved runtime turn execution onto background workers in `RuntimeSessionService`.
  - Split live job workers from recovery replay workers, and limited startup replay to interrupted non-queued jobs while queued jobs re-enter execution when polled.
  - Redeployed the Railway service and verified that `POST /api/runtime/v1/sessions` now returns immediately with `status: queued`.
- Actual root cause after fix: The runtime service had a false-async boundary. `create_session()` persisted the session and job, then executed `_run_job()` inline, which kept the HTTP request open for the entire first-turn generation. Startup replay also let older interrupted work consume the same worker lane that new live jobs needed.
- Guardrail added: The runtime service now returns queued jobs immediately, executes turns in background workers, keeps recovery replay off the live worker lane, and has a backend test that fails if session creation blocks on generation again.
- Distilled rule added to project-memory.md: `Runtime create-session endpoints must prove they return before generation completes, and recovery replay must not block fresh live jobs (2026-04-15)`
