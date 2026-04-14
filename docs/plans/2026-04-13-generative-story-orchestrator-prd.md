# PRD: Generative Story Orchestrator
Date: 2026-04-13
Status: Draft for decision
Owner: Youssef
Product: FarmSim VR

## 1. Executive Summary
FarmSim VR should add a backend-first generative content platform that turns structured game data, approved references, and narrative goals into reviewed `StoryPackage` artifacts for Unity. The platform should generate coherent intro-like episodes made of cutscene beats, dialogue, voice, supporting audio, and parameterized minigame configurations. It should not attempt live end-to-end generation in-headset.

The strongest product shape is a reviewed asynchronous pipeline:
- planner creates structured beat JSON
- specialized workers generate images, dialogue, VO, and SFX
- a minigame adapter layer configures existing gameplay slices
- QA and moderation gate publishing
- Unity consumes approved package versions deterministically

This approach matches the repo’s architecture and the best patterns visible in comparable systems: structured memory, modular content contracts, style-governed media generation, human review, and durable orchestration.

## 2. Problem Statement
Creating a new intro-quality story sequence currently requires manual work across:
- narrative design
- character selection and continuity
- visual direction
- dialogue writing
- VO and audio production
- minigame tuning
- scene assembly

That cost blocks scalable story content such as:
- tutorial variants
- seasonal episodes
- character arcs
- limited-time events
- localization-ready narrative packs

The challenge is not "call an image model." It is orchestrating a reliable content system that can:
- preserve tone and continuity
- stay visually on-model
- choose and tune fitting minigames
- degrade gracefully when a provider fails
- remain lightweight at Quest runtime

## 3. Product Vision
Create a story operating system for FarmSim VR that composes:
- a world model
- a cast model
- a minigame capability model
- a reference asset library
- a generation workflow with QA, safety, and fallbacks

The end result is a reusable backend platform that can produce authored-feeling story episodes from data, rather than one-off prompt outputs.

## 4. Repo-Grounded Constraints
The PRD must fit the current project, not an abstract AI demo.

Observed local anchors:
- `SceneWorkCatalog` already defines an ordered intro/tutorial chain.
- `CinematicSequencer` is registry-driven and suited to data-fed sequence playback.
- `CropLifecycleProfile` already models crop-stage minigame definitions.
- `WorldPenGameCatalog` shows the project already uses catalog-style content assets.
- A direct Unity-side OpenAI client exists, but it is a prototype pattern and should not become the production architecture.

Hard constraints from project constitution and memory:
- Unity runtime should stay thin and deterministic.
- Core domain logic should stay structured and testable.
- Quest performance budget is non-negotiable.
- FarmSim’s tone is contemplative and tactile; time pressure should be used carefully, not as the default pattern.
- Completion quality must separate verified facts from assumptions and residual risk.

## 5. External Reference Synthesis
The best adjacent references do not rely on unconstrained generation. They combine memory, modular content, governance, and review.

### 5.1 Hidden Door
Why it matters:
- story-first AI product with a clear game frame
- narrator-led experience rather than raw prose generation
- platform posture around content and safety, not just prompting

What to borrow:
- narrator-led structure
- generated NPC/item/location outputs as first-class objects
- platform-level safety and IP posture

What not to copy:
- endless open-ended story play as the default loop
- text-first delivery without authored beats

Source:
- [Hidden Door press kit](https://www.hiddendoor.co/press)

### 5.2 AI Dungeon / Latitude
Why it matters:
- long-running reference for memory management, publishing, and moderation in generative narrative

What to borrow:
- story cards / world cards as modular continuity memory
- memory summaries instead of replaying entire histories
- draft-versus-published separation
- substitution paths when generation fails

What not to copy:
- unconstrained free-form prompting as the primary authoring interface
- public publishing without stronger brand and art governance

Sources:
- [AI Dungeon Story Cards](https://help.aidungeon.com/faq/story-cards)
- [AI Dungeon Memory System](https://help.aidungeon.com/faq/the-memory-system)
- [AI Dungeon visibility and rating flow](https://help.aidungeon.com/faq/visibility)
- [AI Dungeon moderation flow](https://help.aidungeon.com/faq/how-does-content-moderation-work)

### 5.3 Scenario
Why it matters:
- strongest reference here for production-grade AI asset workflows for game teams

What to borrow:
- reference-driven style governance
- private training / ownership posture
- workflow-centric generation instead of isolated prompt calls
- Unity-aware delivery

What not to copy:
- platform sprawl in V1
- artist-tool complexity before story generation works end to end

Sources:
- [Scenario platform](https://www.scenario.com/)
- [Scenario case study: Mighty Bear Games](https://www.scenario.com/case-studies/mighty-bear-games)
- [Scenario guide: cohesive assets from sketches](https://help.scenario.com/en/articles/turning-sketches-into-cohesive-isometric-assets/)

### 5.4 Inworld
Why it matters:
- useful schema model for characters, goals, knowledge, and safety in a Unity-adjacent system

What to borrow:
- structured character data instead of prompt blobs
- explicit goal activation rules
- server-side orchestration with Unity as client

What not to copy:
- real-time conversation as the center of the system
- heavy live orchestration inside Unity

Sources:
- [Inworld CharacterData](https://docs.inworld.ai/Unity/runtime/runtime-reference/DataClasses/CharacterData)
- [Inworld GoalData](https://docs.inworld.ai/Unity/runtime/runtime-reference/DataClasses/GoalData)
- [Inworld multimodal companion template](https://docs.inworld.ai/node/templates/multimodal-companion)

### 5.5 Convai
Why it matters:
- practical Quest/VR reference for animation libraries selected by AI context

What to borrow:
- future gesture library captured on Quest
- authored animations selected by context, not fully AI-generated motion

What not to copy:
- making freeform conversational AI the core content system before episodes work

Sources:
- [Convai docs](https://docs.convai.com/api-docs)
- [Convai XR Animation Capture App](https://docs.convai.com/api-docs/no-code-experiences/convai-xr-animation-capture-app)

### 5.6 Academic References
Useful synthesis:
- PANGeA supports the designer-guided procedural narrative framing.
- Language as Reality supports co-creative workflows where generated visuals and story are part of the same experience layer.

Sources:
- [PANGeA paper](https://arxiv.org/abs/2404.19721)
- [Language as Reality paper](https://arxiv.org/abs/2308.12915)

## 6. Vendor Capability Baseline
These product decisions must match current provider capabilities as of 2026-04-13.

### 6.1 Gemini
Confirmed relevant capabilities:
- `gemini-2.5-flash` supports structured outputs, function calling, and long-context planning.
- `gemini-2.5-flash-image` supports text+image input and text+image output for generation and editing.
- Gemini image workflows support iterative, reference-informed edits.
- Gemini Files API is suited to larger media inputs and reusable references.

Implication:
- use a planner model for JSON contracts and orchestration decisions
- use separate image workers for media generation

Sources:
- [Gemini 2.5 Flash](https://ai.google.dev/gemini-api/docs/models/gemini-2.5-flash)
- [Gemini 2.5 Flash Image](https://ai.google.dev/gemini-api/docs/models/gemini-2.5-flash-image)
- [Gemini image generation guide](https://ai.google.dev/gemini-api/docs/image-generation)
- [Gemini structured outputs](https://ai.google.dev/gemini-api/docs/structured-output)
- [Gemini function calling](https://ai.google.dev/gemini-api/docs/function-calling)
- [Gemini Files API](https://ai.google.dev/gemini-api/docs/files)

### 6.2 ElevenLabs
Confirmed relevant capabilities:
- text-to-speech for single-voice clips
- text-to-dialogue for multi-speaker scenes
- voice design from text descriptions and optional references
- short sound effect generation

Confirmed constraints:
- text-to-dialogue is suited to pre-generated scenes, not live conversational runtime
- dialogue requests support up to 10 unique voice IDs
- sound effects are capped at short-form generation
- seeding is available for improved consistency

Implication:
- ElevenLabs fits pre-generated story package audio well
- it should not be the primary live runtime dialogue engine

Sources:
- [ElevenLabs TTS](https://elevenlabs.io/docs/api-reference/text-to-speech/convert)
- [ElevenLabs text-to-dialogue](https://elevenlabs.io/docs/api-reference/text-to-dialogue/convert)
- [ElevenLabs text-to-dialogue overview](https://elevenlabs.io/docs/overview/capabilities/text-to-dialogue)
- [ElevenLabs voice design](https://elevenlabs.io/docs/api-reference/text-to-voice/design)
- [ElevenLabs sound effects](https://elevenlabs.io/docs/overview/capabilities/sound-effects)

## 7. Product Decision
Build a reviewed, backend-orchestrated `StoryPackage` pipeline.

Do not build in V1:
- live end-to-end AI generation inside Unity
- a pure chatbot experience
- AI-invented new minigame mechanics
- video-first cutscene generation

Build instead:
- a backend planner that emits structured beat JSON
- media workers for images, dialogue, VO, and SFX
- a minigame adapter contract for existing gameplay templates
- a review-and-publish workflow
- a Unity importer/player for approved package versions

## 8. Core Principles
1. FarmSim tone beats novelty.
2. Structured data beats prompt-only memory.
3. Reviewable packages beat live improvisation.
4. Existing minigame templates beat AI-invented mechanics.
5. Style consistency beats maximum variety.
6. Fallbacks are mandatory.
7. Quest runtime stays lightweight.

## 9. Users and Jobs To Be Done
Primary users:
- Youssef as creative director and final approver
- future internal content operator

Secondary users:
- players consuming published episodes

Jobs to be done:
- "Given a character, crop, and narrative objective, create a playable episode."
- "Generate visuals that stay on-model for approved characters."
- "Tune a fitting minigame without creating a new mechanic."
- "Preview, edit, approve, and publish a package into Unity."
- "Recover cleanly when one step of generation fails."

## 10. Goals
- Generate intro-like story episodes from backend data and references.
- Personalize character, crop, and minigame selection.
- Produce cutscene-ready visual sequences with stable style.
- Produce dialogue, subtitles, VO, and supporting audio.
- Parameterize minigames through stable schemas.
- Publish deterministic packages into Unity.
- Support moderation, review, traceability, and rollback.

## 11. Non-Goals
- full 3D scene generation in V1
- complex AI animation synthesis in V1
- live player-facing freeform storytelling in-headset
- replacing authored story design entirely
- full procedural sandbox generation
- mandatory countdown-style pressure as the default gameplay modifier

## 12. Scope by Phase
### V1
- 2-3 approved characters
- 1 crop family
- 2-3 minigame adapters
- storyboard-panel cutscenes or pan-and-zoom still sequences
- subtitles, VO, short SFX
- one operator review flow
- Unity importer for one generated episode class

### V1.5
- localization-ready subtitle pipeline
- continuity memory bank
- stronger operator editing tools
- more characters, crops, and minigame tags
- gesture/animation library triggers for NPC delivery

### V2
- seasonal content operations
- personalization by player segment or profile
- richer cutscene assembly
- cautious experiments with limited motion/video
- internal episode-authoring tools at scale

## 13. Functional Requirements
### 13.1 Story Authoring Input
The platform must accept:
- story brief
- target characters
- world/season/location state
- crop or item focus
- allowed minigame tags
- target episode length
- difficulty profile
- approved references

### 13.2 Content Modules
The backend must store:
- `CharacterModule`
- `MinigameCatalog`
- `NarrativeWorldState`
- `ReferenceAssetLibrary`
- `VoiceProfileLibrary`
- `StoryTemplateLibrary`

Recommended structure:
- `CharacterModule`: identity, look references, role, voice, lore, exclusions, continuity facts
- `MinigameCatalog`: capability tags, config schema, generator definitions, tuning bounds, fallbacks, preview data
- `NarrativeWorldState`: season, farm status, unlocked content, relationship state, prior episode facts
- `ReferenceAssetLibrary`: approved poses, costume references, environment references, visual red lines
- `VoiceProfileLibrary`: voice IDs, seeds, speaking style constraints, fallback mapping
- `StoryTemplateLibrary`: beat archetypes, transition rules, tone constraints, reusable episode scaffolds

### 13.3 Story Planning
The planner must output structured JSON for:
- beat list
- cutscene/gameplay transitions
- character participation
- emotional arc
- asset requests
- dialogue requests
- audio requests
- minigame placement
- fallback candidates

### 13.4 Minigame Adapter Model
Each minigame must expose an MCP-like backend tool contract:
- supported capabilities
- input schema
- generator definitions
- validation rules
- tuning bounds
- preview payload
- fallback substitutions

This is not a literal MCP server per minigame. It is a backend contract that lets the planner reason about minigames as reliable tools.

### 13.4.1 Per-Minigame Generator Pattern
Each supported minigame should also expose one or more `MinigameGeneratorDefinition` entries. This is the layer the AI planner actually selects and tweaks.

The rule is simple:
- the planner does not invent raw gameplay config from scratch
- it chooses a known generator
- it fills bounded parameters
- the adapter validates and materializes the final minigame config

Each generator definition should include:
- `generatorId`
- narrative fit tags such as `intro`, `teaching`, `harvest`, `timed`, `calm`, `character-led`
- supported difficulty bands
- required world-state preconditions
- parameter schema
- allowed ranges or enums
- default values
- parameter coupling rules
- preview text template
- fallback generator IDs

Example shape:
```json
{
  "minigameId": "planting",
  "generatorId": "plant_rows_v1",
  "fitTags": ["intro", "teaching", "crop-focused"],
  "parameters": {
    "cropType": { "type": "enum", "values": ["carrot", "tomato", "corn"] },
    "targetCount": { "type": "int", "min": 3, "max": 12, "default": 5 },
    "timeLimitSeconds": { "type": "int", "min": 120, "max": 600, "default": 300 },
    "rowCount": { "type": "int", "min": 1, "max": 4, "default": 2 },
    "assistLevel": { "type": "enum", "values": ["high", "medium", "low"] }
  },
  "constraints": [
    "intro episodes may not use assistLevel=low",
    "targetCount must scale with rowCount",
    "tomato is not valid until tomatoes are unlocked"
  ],
  "fallbackGenerators": ["plant_rows_tutorial_safe_v1"]
}
```

This matters because it gives the system a clean boundary between:
- narrative planning
- minigame selection
- minigame customization
- runtime validation

For FarmSim VR, this should become the standard model for every AI-tunable minigame:
- planting generators
- harvesting generators
- find-tools generators
- chicken-herding or chase generators
- future horse-training generators

The generator layer is what makes each minigame feel like its own controllable "tool" without requiring a separate service per game.

V1 should start with exactly three generator definitions:
- `plant_rows_v1`
- `find_tools_cluster_v1`
- `chicken_chase_intro_v1`

That gives the system one crop-focused generator, one search/recovery generator,
and one light-pressure action generator without widening the initial planning
surface too early.

### 13.5 Story Package Output
Each generated episode must produce a versioned `StoryPackage` with:
- package metadata
- story seed
- planner version
- dependency versions
- beat-by-beat payload
- media asset references
- subtitle script
- voice/audio references
- minigame configs
- QA status
- publish status

Suggested top-level schema:
```json
{
  "packageId": "storypkg_intro_variant_001",
  "version": 1,
  "seed": "abc123",
  "continuityDeps": [],
  "beats": [],
  "media": [],
  "dialogue": [],
  "minigameConfigs": [],
  "qa": {},
  "publish": {}
}
```

### 13.6 Review and Publish
The operator must be able to:
- preview beat sheets
- inspect continuity inputs
- regenerate one failed step
- swap a minigame
- replace an image with a fallback
- approve or reject a version
- publish a selected version to Unity

## 14. Non-Functional Requirements
- asynchronous generation with durable retry
- traceability of every generated asset
- provider abstraction for failover
- asset provenance and rights tracking
- moderation gates before publish
- deterministic Unity runtime consumption
- Quest-safe asset budgets
- auditability of prompts, seeds, versions, outputs, and provider usage

## 15. Proposed Architecture
### 15.1 Services
- Content Service
- Story Planner Service
- Media Orchestrator
- Image Worker
- Dialogue Worker
- Voice Worker
- Audio FX Worker
- QA/Moderation Service
- Asset Registry
- Unity Delivery Service

### 15.2 Orchestration Pattern
Use a durable workflow or graph runtime for long-running jobs, retries, step replay, and human checkpoints.

Practical candidates:
- Temporal for durable execution and retry semantics
- LangGraph for explicit state graphs and human-interruptible flows

Sources:
- [Temporal docs](https://docs.temporal.io/)
- [LangGraph overview](https://docs.langchain.com/oss/python/langgraph/overview)
- [LangGraph workflow patterns](https://docs.langchain.com/oss/python/langgraph/workflows-agents)

### 15.3 Unity Responsibilities
Unity should:
- download approved `StoryPackage` versions
- map beats to scene templates
- feed dialogue, camera, and audio data into existing cutscene systems
- pass config payloads into minigame controllers

Unity should not:
- hold provider secrets
- own provider retries
- be the primary orchestration engine
- depend on live generation for normal episode playback

## 16. End-to-End Generation Workflow
1. Operator submits story brief.
2. Platform resolves continuity modules and approved references.
3. Planner creates beat JSON and fallback candidates.
4. Validator checks beat feasibility against the minigame catalog.
5. Image worker creates cutscene panels or shot assets from references.
6. Dialogue worker writes scene lines and subtitles.
7. Voice worker renders speech or dialogue.
8. Audio worker renders supporting SFX.
9. Assembler builds a versioned `StoryPackage`.
10. QA runs schema, moderation, style, and runtime checks.
11. Operator reviews, edits, regenerates, or approves.
12. Unity ingests the approved package.

## 17. Fallback Strategy
### Narrative
- primary: generated beat sheet
- fallback: template-driven beat sheet with limited personalization

### Images
- primary: Gemini reference-driven generation/editing
- fallback 1: regenerate one shot with tighter controls
- fallback 2: use canonical approved art
- fallback 3: use storyboard card plus subtitles only

### Voice
- primary: ElevenLabs multi-speaker dialogue
- fallback 1: line-by-line TTS
- fallback 2: single narrator voice
- fallback 3: subtitle-only sequence

### Minigames
- primary: requested adapter
- fallback 1: substitute a minigame with the same capability tag
- fallback 2: shorten the episode and remove the failed gameplay beat

### Publish
- block publish when quality gates fail
- allow degraded publish only when explicitly marked acceptable by the operator

## 18. Safety, Rights, and Quality
The platform must enforce:
- approved-reference-only visual generation for protected characters
- rights validation for uploaded references
- AI rating/moderation before publish
- human approval before release
- full audit trails for prompts, versions, seeds, outputs, and provider usage

The key external lesson is simple: private drafting and public publishing need different trust models.

## 19. Roadmap
### Phase 0: Contracts and Governance
- define schemas
- define package versioning
- define review states
- define asset rights model
- define continuity memory model

### Phase 1: Backend Foundation
- content database
- object storage for references and outputs
- workflow engine
- operator dashboard skeleton
- auth and secrets model

### Phase 2: Planner and Catalogs
- `CharacterModule`
- `MinigameCatalog`
- `NarrativeWorldState`
- `StoryTemplateLibrary`
- planner JSON contract
- validation layer against gameplay constraints

### Phase 3: Media Generation
- Gemini image worker
- ElevenLabs voice/dialogue worker
- SFX worker
- per-step retries
- asset provenance logging

### Phase 4: Unity Assembly
- package importer
- cutscene mapping into current sequence systems
- minigame config application
- preview scene or debug playback flow

### Phase 5: QA and Publish
- moderation pipeline
- style checks
- schema checks
- package diffing
- publish and rollback controls

### Phase 6: Scale and Personalization
- content ops dashboards
- analytics
- release scheduling
- multilingual support
- player segmentation or personalization rules

## 20. Success Metrics
- time from brief to previewable package
- publishable package rate
- manual edit rate per package
- failed generation step rate
- cost per approved package
- continuity defect rate
- generated-content-induced Quest performance defects

## 21. Risks and Mitigations
### Style drift
Mitigation:
- approved references
- style templates
- package-level visual QA

### Continuity drift
Mitigation:
- story/world cards
- memory summaries
- dependency-aware planner inputs

### Provider instability
Mitigation:
- separate workers
- retries
- substitution graph
- durable orchestration

### Unsafe or off-brand output
Mitigation:
- moderation gates
- AI rating
- human approval
- provenance logging

### Runtime instability on Quest
Mitigation:
- pre-generated assets only
- package validation
- asset budgets
- no live heavyweight generation in headset

## 22. Open Product Decisions
- Should orchestration start with Temporal, LangGraph, or a simpler queue that can later be upgraded?
- Should editing live in a custom admin UI or JSON-first internal tools for V1?
- What is the acceptable human edit rate before a package is considered publishable?
- Should V1 cutscenes be still panels, pan-and-zoom boards, or Timeline-ready image sequences?
- Should player personalization start in V1.5 or wait until V2?

## 23. Final Recommendation
Proceed with a backend-first, reviewed `StoryPackage` platform.

The first milestone should prove one thing only:

Can FarmSim generate one coherent intro-like episode from structured backend data, approved references, and existing minigame templates, then publish it into Unity with fallbacks and QA?

If that succeeds, expansion is controlled. If it fails, adding more models or more agents will only magnify instability.

## 24. Source Register
- [Hidden Door press kit](https://www.hiddendoor.co/press)
- [AI Dungeon Story Cards](https://help.aidungeon.com/faq/story-cards)
- [AI Dungeon Memory System](https://help.aidungeon.com/faq/the-memory-system)
- [AI Dungeon visibility and rating flow](https://help.aidungeon.com/faq/visibility)
- [AI Dungeon moderation flow](https://help.aidungeon.com/faq/how-does-content-moderation-work)
- [Scenario platform](https://www.scenario.com/)
- [Scenario case study: Mighty Bear Games](https://www.scenario.com/case-studies/mighty-bear-games)
- [Scenario guide: cohesive assets from sketches](https://help.scenario.com/en/articles/turning-sketches-into-cohesive-isometric-assets/)
- [Inworld CharacterData](https://docs.inworld.ai/Unity/runtime/runtime-reference/DataClasses/CharacterData)
- [Inworld GoalData](https://docs.inworld.ai/Unity/runtime/runtime-reference/DataClasses/GoalData)
- [Inworld multimodal companion template](https://docs.inworld.ai/node/templates/multimodal-companion)
- [Convai docs](https://docs.convai.com/api-docs)
- [Convai XR Animation Capture App](https://docs.convai.com/api-docs/no-code-experiences/convai-xr-animation-capture-app)
- [PANGeA paper](https://arxiv.org/abs/2404.19721)
- [Language as Reality paper](https://arxiv.org/abs/2308.12915)
- [Gemini 2.5 Flash](https://ai.google.dev/gemini-api/docs/models/gemini-2.5-flash)
- [Gemini 2.5 Flash Image](https://ai.google.dev/gemini-api/docs/models/gemini-2.5-flash-image)
- [Gemini image generation guide](https://ai.google.dev/gemini-api/docs/image-generation)
- [Gemini structured outputs](https://ai.google.dev/gemini-api/docs/structured-output)
- [Gemini function calling](https://ai.google.dev/gemini-api/docs/function-calling)
- [Gemini Files API](https://ai.google.dev/gemini-api/docs/files)
- [ElevenLabs TTS](https://elevenlabs.io/docs/api-reference/text-to-speech/convert)
- [ElevenLabs text-to-dialogue](https://elevenlabs.io/docs/api-reference/text-to-dialogue/convert)
- [ElevenLabs text-to-dialogue overview](https://elevenlabs.io/docs/overview/capabilities/text-to-dialogue)
- [ElevenLabs voice design](https://elevenlabs.io/docs/api-reference/text-to-voice/design)
- [ElevenLabs sound effects](https://elevenlabs.io/docs/overview/capabilities/sound-effects)
- [Temporal docs](https://docs.temporal.io/)
- [LangGraph overview](https://docs.langchain.com/oss/python/langgraph/overview)
- [LangGraph workflow patterns](https://docs.langchain.com/oss/python/langgraph/workflows-agents)
