# Scene Work Map

Use this document when splitting scene-scoped work across teammates or AI
agents. The numbering here is the stable shorthand for the current playable
beats.

## Handoff Rule

When assigning work, lead with the scene number:

- `Scene 01` = Intro cutscene
- `Scene 02` = Chicken chase gameplay
- `Scene 03` = Post-chicken bridge cutscene
- `Scene 04` = Tool-bridge placeholder cutscene
- `Scene 05` = Find tools gameplay
- `Scene 06` = Pre-farm bridge cutscene
- `Scene 07` = Farm tutorial gameplay
- `Scene 08` = World sandbox / integration slice

Recommended handoff prompt:

`Work on Scene 05. Read .ai/docs/scene-work-map.md and the listed spec/doc files first. Keep edits scoped to the files named for Scene 05 unless the task clearly needs a shared system change.`

## Numbered Scene Index

| Scene | Type | Unity scene | Purpose | Primary ownership |
|---|---|---|---|---|
| Scene 01 | Cutscene | `Assets/_Project/Scenes/Intro.unity` | Opening image sequence and transition into the first playable beat | Intro pacing, timeline, subtitles, transition to Scene 02 |
| Scene 02 | Gameplay | `Assets/_Project/Scenes/ChickenGame.unity` | Catch the chicken and complete the first quick win | Chicken rules, chase feel, win condition, transition to Scene 03 |
| Scene 03 | Cutscene | `Assets/_Project/Scenes/Tutorial_PostChickenCutscene.unity` | Short narrative bridge after the chase | Narrative glue, concise pacing, transition to Scene 04 |
| Scene 04 | Cutscene | `Assets/_Project/Scenes/Tutorial_MidpointPlaceholder.unity` | Placeholder bridge that preserves structure until final story content exists | Placeholder copy, timing, transition to Scene 05 |
| Scene 05 | Gameplay | `Assets/_Project/Scenes/FindToolsGame.unity` | Recover tools in an isolated tutorial beat | Tool-recovery interaction, waypoint flow, transition to Scene 06 |
| Scene 06 | Cutscene | `Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity` | Final bridge into the farming loop | Final framing, concise pacing, transition to Scene 07 |
| Scene 07 | Gameplay | `Assets/_Project/Scenes/FarmMain.unity` | First complete plant-water-harvest onboarding loop | Farm tutorial prompts, first-loop clarity, completion state |
| Scene 08 | Sandbox | `Assets/_Project/Scenes/WorldMain.unity` | Open-world integration slice outside the required onboarding path | Farming slice, pen-game integration, sandbox iteration |

## Scene 01 — Intro Cutscene

- Goal: make the opening readable, short, and decisive.
- Completion: the timeline ends or is skipped, then hands off to Scene 02.
- Primary files:
  - `Assets/_Project/Scenes/Intro.unity`
  - `Assets/_Project/Scripts/MonoBehaviours/Cinematics/IntroCinematicAutoPlay.cs`
  - `Assets/_Project/Editor/CreateIntroScene.cs`
- Read before editing:
  - `Assets/Specs/Features/TUT-001-linear-tutorial-sequence.md`
  - `Assets/Specs/Features/Tutorial-Sequence-Handbook.md`
- Keep out of scope:
  - chicken chase mechanics
  - farm tutorial logic

## Scene 02 — Chicken Chase Game

- Goal: one clear chase, one clear win, immediate handoff.
- Completion: player wins the chicken beat and advances to Scene 03.
- Primary files:
  - `Assets/_Project/Scenes/ChickenGame.unity`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialChickenSceneController.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/ChickenGame/`
- Read before editing:
  - `Assets/Specs/Features/TUT-001-linear-tutorial-sequence.md`
  - `Assets/Specs/Features/Tutorial-Sequence-Handbook.md`
- Keep out of scope:
  - intro timeline content
  - farming systems

## Scene 03 — Post-Chicken Cutscene

- Goal: acknowledge the chicken win and point forward cleanly.
- Completion: auto-advance or skip into Scene 04.
- Primary files:
  - `Assets/_Project/Scenes/Tutorial_PostChickenCutscene.unity`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialCutsceneSceneController.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialSceneInstaller.cs`
- Keep out of scope:
  - chicken chase rules
  - tool gameplay logic

## Scene 04 — Tool-Bridge Placeholder

- Goal: preserve the sequence shape while final story content is pending.
- Completion: auto-advance or skip into Scene 05.
- Primary files:
  - `Assets/_Project/Scenes/Tutorial_MidpointPlaceholder.unity`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialCutsceneSceneController.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialSceneInstaller.cs`
- Keep out of scope:
  - replacing the entire tutorial flow contract

## Scene 05 — Find Tools Game

- Goal: isolate the tool-recovery beat so it is easy to tune and replace later.
- Completion: player reaches the recovery goal and advances to Scene 06.
- Primary files:
  - `Assets/_Project/Scenes/FindToolsGame.unity`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialFindToolsSceneController.cs`
  - `Assets/_Project/Scripts/Core/Tutorial/TutorialSceneCatalog.cs`
- Keep out of scope:
  - farm tutorial systems
  - open-world sandbox systems

## Scene 06 — Pre-Farm Cutscene

- Goal: frame the switch into the first farm loop without adding story drag.
- Completion: auto-advance or skip into Scene 07.
- Primary files:
  - `Assets/_Project/Scenes/Tutorial_PreFarmCutscene.unity`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialCutsceneSceneController.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialSceneInstaller.cs`
- Keep out of scope:
  - crop simulation internals

## Scene 07 — Farm Tutorial Game

- Goal: teach the smallest complete farm loop only.
- Completion: tutorial onboarding loop feels readable and finished.
- Primary files:
  - `Assets/_Project/Scenes/FarmMain.unity`
  - `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialFarmSceneController.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Farming/`
- Read before editing:
  - `Assets/Specs/Features/L2-000-farming-foundation-and-sequence.md`
  - `Assets/Specs/Features/Tutorial-Sequence-Handbook.md`
- Keep out of scope:
  - broad `WorldMain` sandbox integration unless a shared system fix is required

## Scene 08 — World Sandbox

- Goal: preserve a separate integration/sandbox slice for world-native farming and pen-game work.
- Completion: not part of the required onboarding path.
- Primary files:
  - `Assets/_Project/Scenes/WorldMain.unity`
  - `Assets/_Project/Scripts/MonoBehaviours/WorldSceneBootstrap.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Farming/WorldFarmBootstrap.cs`
  - `Assets/_Project/Scripts/MonoBehaviours/Hunting/WorldPenBootstrap.cs`
- Read before editing:
  - `Assets/Specs/Features/L2-011-world-farming-playable-slice.md`
  - `Assets/Specs/Features/L2-012-world-pen-game-integration.md`
- Keep out of scope:
  - tutorial sequence pacing unless the task explicitly spans both tracks

## Shared Files

These files affect more than one numbered scene:

- `Assets/_Project/Scripts/Core/Tutorial/TutorialSceneCatalog.cs`
- `Assets/_Project/Scripts/Core/Tutorial/SceneWorkCatalog.cs`
- `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialFlowController.cs`
- `Assets/_Project/Scripts/MonoBehaviours/Tutorial/TutorialDevShortcuts.cs`

If a task needs one of these files, say that clearly in the handoff so the next
agent knows it is a shared-system change instead of a scene-local change.
