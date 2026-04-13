## GSO-004 — Generated Storyboard Cutscene Slice

### Goal
Replace one of the placeholder bridge cutscenes with a generated storyboard
package that is assembled from backend data, Gemini-generated still images, and
ElevenLabs narration audio based on the same subtitle lines.

### Problem
- The current story-package sample proves routing, but it still leans on legacy
  authored cinematics and text-only placeholders.
- That does not prove the foundation we actually need: generated shots with
  consistent prompt styling, synchronized narration, and a package Unity can
  play as a cutscene beat.
- The first useful slice should stay narrow enough to test from the title
  screen without migrating the full intro Timeline.

### First Slice
- Target the `PostChickenCutscene` bridge scene.
- Generate a storyboard package beat for that scene.
- Save the package into Unity `Resources` so the existing runtime catalog can
  resolve it.
- Save the generated image and audio assets into Unity `Resources` so the scene
  can render them immediately.

### Player-Facing Outcome
- Launching the post-chicken bridge scene plays a storyboard-style cutscene
  instead of a text card.
- The cutscene advances shot-by-shot using generated images and subtitle-backed
  narration audio.
- When the final shot completes, the scene still hands off into the next
  tutorial beat.

### Constraints
- Keep secrets in the backend `.env.local`; Unity must not need provider keys.
- Preserve the existing story-package contract for non-storyboard beats.
- Extend the shared package schema instead of inventing a Unity-only data path.
- Keep the first live slice on a tutorial bridge scene, not the `Intro`
  Timeline scene.
- Store provider-specific details behind backend abstractions so tests can run
  with fakes.

### Acceptance Criteria
- [ ] Backend exposes a cutscene-generation path that produces a Unity story
      package beat with storyboard shots.
- [ ] Each generated storyboard shot carries subtitle text, image resource path,
      audio resource path, and duration.
- [ ] The backend writes generated assets into Unity `Resources` using stable
      resource-relative paths.
- [ ] The story package contract rejects storyboard shots missing required media
      fields.
- [ ] `TutorialSceneInstaller` uses storyboard data when a cutscene beat
      provides it, and falls back to the old text card when it does not.
- [ ] The post-chicken bridge scene can be launched from the title screen and
      visibly render the generated storyboard slice.

### Out of Scope
- Multi-beat orchestration across the entire tutorial
- LLM-driven minigame parameterization
- Provider retries, queues, moderation, or review tools
- Replacing the authored `Intro` Timeline cutscene
