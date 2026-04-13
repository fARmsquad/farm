## GSO-003 — Title Screen Story Package Sample Slice

### Goal
Expose the current story-package-backed intro flow from the title screen with an explicit, easy-to-find sample entry so the developer can test the new foundation without guessing which existing slice demonstrates it.

### Problem
- The title screen already exposes the tutorial scenes as generic playable slices.
- The new story-package foundation currently reuses the existing `Intro`, `ChickenGame`, and `Tutorial_PostChickenCutscene` scenes.
- That means the package-backed path exists, but it is not obvious from the title screen which slice is meant to validate the new story-package work.

### Player-Facing Outcome
- In editor and development builds, the title screen shows a dedicated story-package sample button in the slice launcher.
- Activating that button launches the package-backed intro flow at `Intro`.

### Constraints
- Do not change the existing tutorial ordering contract in `SceneWorkCatalog`.
- Do not change `StartGame()` behavior; the main play path should still use the first tutorial scene.
- Keep the change scoped to the title-screen launcher UI and its tests.

### Acceptance Criteria
- [ ] The title-screen dev slice launcher renders one additional button for the story-package sample.
- [ ] The new button is clearly labeled as the story-package sample.
- [ ] The existing slice labels from `SceneWorkCatalog.TitleScreenLaunchableScenes` still render in order.
- [ ] `StartGame()` still targets `SceneWorkCatalog.FirstTutorialSceneName`.

