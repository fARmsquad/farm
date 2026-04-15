## GSO-024 — Runtime Cutscene Shot Contract

### Goal
Make every generated runtime cutscene a real storyboard sequence instead of a
single bridge card.

### Problem
- The generated runtime flow is currently allowed to behave like a one-shot
  handoff into gameplay.
- That breaks the intended experience: each generated cutscene should feel like
  a short cinematic sequence with multiple beats, not one still plus one line.
- Subtitle text and spoken audio also need to stay aligned exactly so the
  playback feels intentional instead of loosely synchronized.

### Required Runtime Contract
- Every generated runtime cutscene must contain **3 to 6 shots**.
- Every shot must provide:
  - one generated image
  - one subtitle line
  - one narration audio asset
  - one alignment artifact
- The spoken audio for a shot must be generated from the exact subtitle line for
  that shot.
- The runtime contract must fail closed if a cutscene has fewer than 3 shots,
  more than 6 shots, or narration text that does not match the subtitle text.

### Player-Facing Outcome
- Starting a generated runtime turn plays a sequence of 3 to 6 storyboard
  images before the minigame loads.
- Each image has a matching subtitle line and narration audio.
- The cutscene only hands off into gameplay after the full sequence finishes.

### Constraints
- Keep the current bounded cutscene scene and minigame adapters.
- Enforce the contract in backend generation and validation, not only in prompt
  wording.
- Keep Unity playback generic so it can render any valid 3 to 6 shot runtime
  cutscene.

### Acceptance Criteria
- [ ] The storyboard planner requests 3 to 6 shots and rejects mismatched
      subtitle/narration text.
- [ ] The generated storyboard service uses the subtitle line as the speech text
      for each shot.
- [ ] The runtime envelope model rejects cutscenes outside the 3 to 6 shot
      range.
- [ ] Unity runtime cutscene playback advances across all runtime shots before
      completing the scene.

### Out of Scope
- Dynamic camera moves
- Lip sync
- Longer-form cinematics beyond the 3 to 6 shot runtime contract
