# Feature Spec: Generated Slice Launch And Storyboard Quality Gates — GSO-017

## Summary
The standing `Generative Story Slice` is currently too forgiving in the wrong
places. When the local story-orchestrator is unavailable, the Unity title flow
silently falls back into stale packaged content. When live Gemini image
generation succeeds but produces low-quality frames with embedded caption boxes
or obvious placeholder output, those frames are still accepted and published
into the shared Resources package.

This slice hardens both edges. Unity should stay on the title screen and report
that the live generated slice is unavailable instead of launching stale fallback
content. The backend should reject placeholder or caption-box storyboard frames,
retry generation, and only publish accepted assets into the standing slice.

## User Story
As a developer, I want the standing `Generative Story Slice` to either launch a
real live-generated beat or clearly stay put with an error, so I never mistake
stale fallback content for the current orchestrated output.

## Acceptance Criteria

### Generated Slice Launch Gating
- [ ] When the title-screen generated-slice bootstrap call fails, Unity does
      not load the fallback scene.
- [ ] The title-screen launcher remains the active surface after a bootstrap
      failure.
- [ ] The title-screen launcher shows a readable generated-slice status/error
      message after the failure.
- [ ] Successful generated-slice bootstrap still loads the returned entry scene
      through the runtime package override path.

### Storyboard Quality Gates
- [ ] Storyboard image prompts explicitly forbid subtitle boxes, caption panels,
      readable text, speech bubbles, UI overlays, labels, and prompt leakage
      inside the generated image.
- [ ] Placeholder fallback image output is rejected by the storyboard quality
      gate and is not published into the generated package.
- [ ] Live image output with a large dark caption-style bottom panel is rejected
      by the storyboard quality gate and is not published into the generated
      package.
- [ ] Rejected image files are deleted before a retry or final failure.
- [ ] The backend retries rejected image generations at least once before
      failing the package generation request.

### Testing
- [ ] An EditMode test proves generated-slice bootstrap failure does not load
      the fallback scene.
- [ ] A backend test proves placeholder fallback image assets are rejected by
      the quality gate.
- [ ] A backend test proves a caption-panel image is rejected, deleted, and
      retried before an accepted frame is published.
- [ ] A backend test proves the generated image prompt now carries explicit
      negative constraints against embedded text and subtitle boxes.

## Non-Negotiable Rules
- Do not silently route the developer into old packaged content when the live
  generated path is unavailable.
- Do not publish placeholder or visibly degraded fallback art into the standing
  generated slice.
- Do not accept generated frames with subtitle boxes or caption panels baked
  into the image.
- Do not add provider-side image understanding calls in this slice; keep the
  quality gate local and cheap.

## Out Of Scope
- Automatic operator approval for rejected storyboard retries
- Semantic visual scoring or learned image-ranking models
- Video generation or lip-sync
- Replacing the standing slice package ID or title-screen entry point

## Done Definition
- [ ] Spec exists.
- [ ] Title-screen generated bootstrap failure stays on the launcher and
      reports status instead of loading stale fallback content.
- [ ] Storyboard image prompt and quality gate reject the current trash frame
      classes.
- [ ] Focused Unity/backend tests lock the new behavior.
