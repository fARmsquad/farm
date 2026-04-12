# Feature Spec: Linear Tutorial Sequence — TUT-001

## Summary
This spec re-scopes the current onboarding experience into a linear sequence of
small, isolated scenes. The tutorial is no longer delivered through
`WorldMain`; instead it is a clean chain of cutscene -> game -> cutscene ->
game beats that teach the player the minimum needed to begin the game.

The intended tutorial path is:
`Intro Cutscene -> Chicken Hunt Game -> Cutscene -> Placeholder Cutscene ->
Find Tools Game -> Cutscene -> Farm Game`

`WorldMain` remains in the repo exactly as it is, but it becomes a sandbox and
integration scene, not the required first-time-player onboarding route.

## User Story
As a new player, I want the opening tutorial to guide me through a short,
readable sequence of isolated scenes so that I understand the game’s basic
verbs without being dropped into an overbuilt open world too early.

## Acceptance Criteria
- [ ] The tutorial path is linear and scene-based:
      `Intro -> Chicken Hunt -> Cutscene A -> Cutscene B Placeholder ->
      Find Tools -> Cutscene C -> Farm Tutorial`.
- [ ] Each gameplay beat is its own scene and owns only one core teaching goal.
- [ ] `WorldMain` remains available but is not required for the tutorial path.
- [ ] The intro cutscene ends by loading the chicken hunt scene, not the world.
- [ ] The chicken hunt scene ends by loading a dedicated follow-up cutscene.
- [ ] A placeholder cutscene scene exists in the chain and can remain minimal
      until the real content is ready.
- [ ] The find-tools sequence is treated as its own game scene, not as world
      traversal or an open-world quest.
- [ ] The farm tutorial scene remains isolated and teaches only the first farm
      loop: plant, water, wait, harvest.
- [ ] The tutorial can progress using a minimal flow state and direct scene
      transitions. No large quest framework is required.
- [ ] Each scene has one clear completion condition and one next-scene target.
- [ ] Developer shortcuts exist to skip forward, reload the current scene, and
      jump between tutorial scenes during testing.
- [ ] The title screen exposes a development slice launcher for each tutorial
      scene, and that launcher is driven by the same ordered scene catalog as
      the live tutorial flow.
- [ ] Build settings can be reduced to a clean tutorial path plus support
      scenes, without relying on unrelated demo scenes.

## Product Intent
The tutorial is the player’s first hour of trust-building with the game. It
must stay simple, linear, and legible. The player should feel like each scene
teaches one thing clearly:
- The intro establishes tone.
- Chicken hunt teaches a first physical task.
- Cutscenes give narrative glue.
- Find tools teaches “recover what you need to farm.”
- Farm tutorial teaches the core tending loop.

The tutorial is not where advanced progression, open-world systems, or deep
simulation complexity should dominate. It is where the player learns the
rhythm of the game.

## Non-Negotiable Tutorial Rules
- The tutorial uses separate scenes, not a single open-world chain.
- Each gameplay scene teaches one job and ends quickly.
- Cutscenes are bridges, not content dumps.
- Placeholder cutscenes are acceptable if scene flow is correct.
- The tutorial must not depend on the full `WorldMain` integration stack.
- The farm tutorial must stay narrow in scope even if deeper farming systems
  already exist elsewhere.
- A scene should never require the player to wonder “what am I supposed to do
  next?” for more than a few seconds.

## Scene Map
| Step | Tutorial Beat | Scene Role | Current Repo Candidate | Final Intent |
|---|---|---|---|---|
| 1 | Intro Cutscene | Opening cinematic and tone-setter | `Assets/_Project/Scenes/Intro.unity` | Keep and wire into tutorial flow |
| 2 | Chicken Hunt Game | First simple playable task | `Assets/_Project/Scenes/ChickenGame.unity` | Keep as isolated minigame scene |
| 3 | Cutscene A | Post-chicken narrative bridge | New scene | Short follow-up cinematic |
| 4 | Cutscene B Placeholder | Temporary bridge scene | New scene | Minimal placeholder allowed |
| 5 | Find Tools Game | Recover farming tools in isolation | `Assets/_Project/Scenes/PlayerGettingSeeds.unity` as temporary shell or new scene | Dedicated find-tools scene |
| 6 | Cutscene C | Bridge into farming | New scene | Short setup for farm tutorial |
| 7 | Farm Game | First playable farming loop | `Assets/_Project/Scenes/FarmMain.unity` | Tutorial-scoped farm scene |

## Recommended Final Scene Naming
Do not rename existing working scenes until the tutorial flow is wired, but the
target structure should be:

| Recommended Name | Current Candidate |
|---|---|
| `Intro.unity` | Already exists |
| `ChickenGame.unity` | Already exists |
| `Tutorial_PostChickenCutscene.unity` | New |
| `Tutorial_MidpointPlaceholder.unity` | New |
| `FindToolsGame.unity` | New or refactor `PlayerGettingSeeds.unity` |
| `Tutorial_PreFarmCutscene.unity` | New |
| `FarmMain.unity` | Already exists |

## Minimal Flow State
The tutorial should carry only minimal persistent data between scenes.

### Required Tutorial Flags
- `CurrentStep`
- `IntroComplete`
- `ChickenHuntComplete`
- `PostChickenCutsceneComplete`
- `PlaceholderCutsceneVisited`
- `FindToolsComplete`
- `PreFarmCutsceneComplete`
- `FarmTutorialComplete`

### Optional Lightweight Flags
- `ChickenHuntScore`
- `StarterToolsRecovered`
- `FarmTutorialStarted`

### Explicitly Deferred
- Full save/load profile system
- Branching dialogue state
- Open-world mission routing
- Economy state
- Large inventory persistence beyond what the next tutorial scene needs

## Recommended Architecture
Keep the tutorial flow minimal and scene-driven.

### Core
- `TutorialStep`
- `TutorialFlowState`
- `TutorialFlowSnapshot`
- `TutorialSceneResult`

### MonoBehaviour Wrappers
- `TutorialFlowController`
- `TutorialSceneBootstrap`
- `TutorialSceneExit`
- `TutorialDevShortcuts`

### Existing Systems To Reuse
- [`SceneLoader`](/Users/youss/My%20project/Assets/_Project/Scripts/MonoBehaviours/SceneLoader.cs)
- [`TitleScreenManager`](/Users/youss/My%20project/Assets/_Project/Scripts/MonoBehaviours/TitleScreenManager.cs)
- [`MissionManager`](/Users/youss/My%20project/Assets/_Project/Scripts/MonoBehaviours/Cinematics/MissionManager.cs)
- Existing intro, chicken, and farm scene content where appropriate

### Rule
Do not invent a large narrative framework. A tiny tutorial flow controller and
simple scene exits are enough.

## Scene Contracts
### 1. Intro Cutscene
- **Goal**: establish tone, player context, and tutorial start.
- **Scene**: `Intro.unity`
- **Completion condition**: cutscene ends or is skipped.
- **Next scene**: `ChickenGame`
- **What it teaches**: mood, premise, and that the game is guided.
- **Do not add**: open-world exploration, branching choices, complex tutorial UI.

### 2. Chicken Hunt Game
- **Goal**: give the player one simple task with a clear pass condition.
- **Scene**: `ChickenGame.unity`
- **Completion condition**: player completes the hunt objective.
- **Next scene**: `Tutorial_PostChickenCutscene`
- **What it teaches**: first physical interaction loop and success/failure readability.
- **Do not add**: farming, open world systems, secondary progression.

### 3. Post-Chicken Cutscene
- **Goal**: acknowledge success and redirect the player toward farm setup.
- **Scene**: new
- **Completion condition**: scene ends or is skipped.
- **Next scene**: `Tutorial_MidpointPlaceholder`
- **What it teaches**: narrative glue only.
- **Do not add**: gameplay systems.

### 4. Placeholder Cutscene
- **Goal**: preserve tutorial pacing while future story content is pending.
- **Scene**: new placeholder
- **Completion condition**: player advances, auto-advance, or skip.
- **Next scene**: `FindToolsGame`
- **What it teaches**: nothing complex; this is a structural bridge.
- **Do not add**: new mechanics.

### 5. Find Tools Game
- **Goal**: teach that farming starts by recovering what you need.
- **Scene**: dedicated game scene, temporary candidate `PlayerGettingSeeds.unity`
- **Completion condition**: required starter tools are recovered.
- **Next scene**: `Tutorial_PreFarmCutscene`
- **What it teaches**: discovery, tool ownership, and readiness for farming.
- **Do not add**: full homestead simulation, broad open-world searching.

### 6. Pre-Farm Cutscene
- **Goal**: hand the player into the farm loop with context and confidence.
- **Scene**: new
- **Completion condition**: scene ends or is skipped.
- **Next scene**: `FarmMain`
- **What it teaches**: the next stop is farming.
- **Do not add**: heavy exposition or new systems.

### 7. Farm Tutorial
- **Goal**: teach the first real farm loop.
- **Scene**: `FarmMain.unity`
- **Completion condition**: complete a minimal scripted farm objective.
- **Next scene**: end of tutorial or next game flow later
- **What it teaches**: plant, water, wait/grow, harvest.
- **Do not add**: full economy, complex progression trees, broad world integration as a requirement.

## Farm Tutorial Scope
For tutorial purposes, `FarmMain` should teach only:
- focus a plot
- plant a valid seed
- water a planted plot
- see crop growth advance
- harvest a mature crop

If weather is included, it should be readable and supportive:
- scripted or controllable `Sunny`
- scripted or controllable `Rain`
- simple explanation of how rain helps

Seasons may integrate later, but the farm tutorial must not wait on deep season
complexity to be playable.

## Find Tools Game Scope
The find-tools scene should be the smallest possible loop that prepares the
player for farming.

### Required Beats
- enter the scene
- understand the objective
- recover the starter tools needed for the first farm loop
- receive clear completion feedback
- transition out

### Explicitly Deferred
- long-range searching
- full inventory complexity
- deep upgrade progression
- multiple optional tools

## Dev Shortcuts
The tutorial sequence should have first-class developer shortcuts.

### Recommended Global Tutorial Shortcuts
- `Shift+.` → force complete current scene and go next
- `Shift+,` → go back to previous tutorial scene
- `Shift+/` → reload current tutorial scene
- `Shift+1` through `Shift+7` → jump directly to a tutorial step
- `Shift+0` → clear tutorial progress and restart at intro

### Recommended Overlay
One small developer overlay should display:
- current tutorial step
- current scene name
- next scene target
- completion flag state
- current shortcut reference

### Title Screen Slice Launcher
For testing and development, `TitleScreen` should expose one direct launch
button per tutorial beat. Those buttons must be generated from the same ordered
scene catalog that drives the runtime tutorial flow so the launcher never falls
out of date when a step is inserted, renamed, or reordered.

## Build Settings Intent
The final tutorial-facing build order should be reduced and intentional.

### Tutorial Path Priority
1. `TitleScreen.unity` if retained as entry
2. `Intro.unity`
3. `ChickenGame.unity`
4. `Tutorial_PostChickenCutscene.unity`
5. `Tutorial_MidpointPlaceholder.unity`
6. `FindToolsGame.unity` or temporary `PlayerGettingSeeds.unity`
7. `Tutorial_PreFarmCutscene.unity`
8. `FarmMain.unity`

### Support Scenes
- loading screen if needed
- test scenes only if intentionally kept for development, not shipping flow

### Remove From Required Player Path
- `WorldMain.unity`
- autoplay demo scenes
- unrelated package/demo scenes

## Dependencies
- Existing intro/cutscene systems:
  - `INT-001` through `INT-014`
- Existing chicken-hunt gameplay:
  - `L2-007`
- Existing farming foundation:
  - `L2-000`
- Existing scene-loading seam:
  - [`SceneLoader`](/Users/youss/My%20project/Assets/_Project/Scripts/MonoBehaviours/SceneLoader.cs)

## Out of Scope
- Rebuilding `WorldMain`
- Open-world tutorial delivery
- Advanced progression or economy inside the tutorial
- Large save architecture
- Multiplayer, seasons depth, or endgame systems as tutorial requirements
- Branching narrative flow

## Build Sequence
### Block 1: Tutorial Flow Contract
- **Build**: define the tutorial step enum, minimal flow state, and scene order.
- **Connects**: every scene exit and entry point.
- **You test**: can the flow state report current step and intended next step?
- **Feedback gate**: is the scene chain correct before scene work starts?

### Block 2: Intro Exit
- **Build**: wire `Intro.unity` to complete into `ChickenGame.unity`.
- **Connects**: intro cinematic to first gameplay beat.
- **You test**: play or skip intro and confirm the next scene is the chicken game.
- **Feedback gate**: does the opening transition feel correct?

### Block 3: Chicken Hunt Exit
- **Build**: make chicken hunt complete into the dedicated post-hunt cutscene.
- **Connects**: first gameplay beat to its narrative follow-up.
- **You test**: complete the chicken objective and verify the correct next scene.
- **Feedback gate**: does the first game feel self-contained and finished?

### Block 4: Post-Chicken Cutscene
- **Build**: create the short bridge scene after chicken hunt.
- **Connects**: hunt completion to the midpoint bridge.
- **You test**: enter, watch/skip, and exit cleanly.
- **Feedback gate**: is the scene doing enough narrative work without overstaying?

### Block 5: Placeholder Cutscene
- **Build**: add the structural placeholder bridge scene.
- **Connects**: post-chicken scene to find-tools.
- **You test**: confirm the placeholder exists and transitions correctly.
- **Feedback gate**: is the structure correct even before content is finished?

### Block 6: Find Tools Game
- **Build**: isolate the find-tools loop in its own scene.
- **Connects**: midpoint bridge to the farm setup.
- **You test**: recover required tools and confirm scene completion.
- **Feedback gate**: does tool recovery feel like a distinct minigame and not a quest stub?

### Block 7: Pre-Farm Cutscene
- **Build**: create the bridge into farming.
- **Connects**: tool recovery to the first farm loop.
- **You test**: confirm the scene leads clearly into `FarmMain`.
- **Feedback gate**: does the player feel ready to farm after this scene?

### Block 8: Farm Tutorial
- **Build**: constrain `FarmMain` to the first farm loop for tutorial purposes.
- **Connects**: the entire tutorial payoff.
- **You test**: plant, water, grow, harvest, then complete the tutorial.
- **Feedback gate**: does the whole tutorial finally feel coherent?

### Block 9: Dev Shortcuts and Build Order
- **Build**: add tutorial jump shortcuts, overlay, title-screen slice launcher,
  and clean build settings order.
- **Connects**: development/testing speed and final player path.
- **You test**: jump between scenes, restart flow, and verify scene ordering.
- **Feedback gate**: can the team test the tutorial rapidly without manual scene setup?

## Developer Handoff Checkpoints
| After Block | What To Test | Intended Outcome |
|---|---|---|
| 1. Tutorial Flow Contract | Inspect current step, next step, and stored flags in editor/debug view. | The tutorial has one clear spine before scene wiring begins. |
| 2. Intro Exit | Finish or skip intro and verify `ChickenGame` loads. | The tutorial starts correctly and transitions into gameplay. |
| 3. Chicken Hunt Exit | Complete the hunt objective and verify the post-hunt cutscene loads. | The first gameplay beat has a clean end and handoff. |
| 4. Post-Chicken Cutscene | Play or skip the bridge scene and verify it exits correctly. | The narrative bridge is functional and concise. |
| 5. Placeholder Cutscene | Enter and exit the placeholder scene. | The sequence structure is preserved even with missing story content. |
| 6. Find Tools Game | Recover the required tools and verify completion. | Tool recovery feels like its own tutorial beat. |
| 7. Pre-Farm Cutscene | Finish or skip the scene and verify `FarmMain` loads. | The player is correctly handed into farming. |
| 8. Farm Tutorial | Perform the first farm loop end-to-end. | The tutorial lands on the core farming fantasy, not a system dump. |
| 9. Dev Shortcuts and Build Order | Use jump shortcuts, reload the flow, and verify build settings scene order. | The tutorial is fast to test and structurally ready for iteration. |

## Recommended First Implementation Slice
The first vertical slice worth building is:

`Intro -> ChickenGame -> PostChickenCutscene -> PlaceholderCutscene`

That slice proves:
- the tutorial is scene-based
- scene exits work
- cutscenes can bridge gameplay
- `WorldMain` is no longer the mandatory onboarding route

Only after that should `FindToolsGame` and the final farm-tutorial handoff be
layered in.
