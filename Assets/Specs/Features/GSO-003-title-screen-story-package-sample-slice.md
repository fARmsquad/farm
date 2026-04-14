## GSO-003 — Title Screen Generative Story Slice

### Goal
Expose the current story-package-backed generated beat from the title screen as
one standing generative slice so the developer always has one stable surface to
test as the Generative Story Orchestrator evolves.

### Problem
- The title screen already exposes the tutorial scenes as generic playable slices.
- The story-package foundation currently reuses the existing `Intro`,
  `ChickenGame`, and `Tutorial_PostChickenCutscene` scenes, but the first
  generated proof lives on the post-chicken bridge beat rather than the intro
  Timeline.
- Without one named standing slice, each new backend increment risks creating a
  new ad hoc testing path instead of improving the same visible sample.

### Player-Facing Outcome
- In editor and development builds, the title screen shows a dedicated
  `Generative Story Slice` button in the slice launcher.
- Activating that button launches the package-backed generated bridge beat at
  `PostChickenCutscene`.
- Future generative-orchestrator progress updates the same underlying sample
  package instead of adding a new separate title-screen slice.

### Constraints
- Do not change the existing tutorial ordering contract in `SceneWorkCatalog`.
- Do not change `StartGame()` behavior; the main play path should still use the first tutorial scene.
- Keep the change scoped to the title-screen launcher UI and its tests.
- Keep the standing slice on the generated cutscene proof, not the authored
  `Intro` Timeline scene.

### Acceptance Criteria
- [x] The title-screen dev slice launcher renders one additional button for the standing generative slice.
- [x] The new button is clearly labeled `Generative Story Slice`.
- [x] The standing slice launches `PostChickenCutscene`, which is the current generated storyboard proof scene.
- [x] The existing slice labels from `SceneWorkCatalog.TitleScreenLaunchableScenes` still render in order.
- [x] `StartGame()` still targets `SceneWorkCatalog.FirstTutorialSceneName`.
- [x] The standing slice is backed by `StoryPackage_IntroChickenSample` and is the default sample to keep updating as GSO work progresses.
