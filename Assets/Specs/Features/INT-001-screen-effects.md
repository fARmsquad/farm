# Feature Spec: Screen Effects — INT-001

## Summary
Full-screen visual effects overlay that provides fade transitions, screen shake, letterbox bars, objective pop-ups, and a mission-passed banner. These effects are triggered by the cinematic sequencer and mission manager to create polished scene transitions and gameplay feedback.

## User Story
As a player, I want to see cinematic screen effects (fades, shakes, letterboxing, objective text, mission banners) so that the intro feels like a directed experience rather than raw gameplay.

## Acceptance Criteria
- [ ] FadeToBlack(duration) fades screen to opaque black over the given duration using an AnimationCurve for alpha
- [ ] FadeFromBlack(duration) fades screen from opaque black to transparent over the given duration
- [ ] ScreenShake(intensity, duration) applies random camera localPosition offsets that decay over the duration
- [ ] ShowLetterbox(height, duration) animates top and bottom black bars to the specified height
- [ ] HideLetterbox(duration) animates letterbox bars back to zero height
- [ ] ShowObjective(text) slides text in from the left edge, holds for 2 seconds, then slides out to the right
- [ ] ShowMissionPassed(text) displays centered banner text that auto-fades after 3 seconds
- [ ] All public methods invoke completion callbacks or fire UnityEvents when finished
- [ ] ScreenEffects Canvas renders on top of all other UI (sort order 999)
- [ ] All effects can be interrupted or overridden by a new call to the same method

## Edge Cases
- Calling FadeToBlack while already mid-fade cancels the current fade and starts the new one
- ScreenShake with intensity 0 is a no-op
- ShowObjective called while a previous objective is still animating immediately replaces it
- ShowMissionPassed called multiple times queues or replaces (replace for greybox)
- Duration of 0 snaps instantly to the target state

## Performance Impact
- Single Canvas with CanvasGroup — negligible draw call overhead
- Screen shake modifies camera localPosition only, no physics involvement
- All coroutine-based; no Update loop polling when idle

## Dependencies
- **Existing:** None
- **New:** ScreenEffects.cs (MonoBehaviour), Canvas prefab with CanvasGroup, letterbox bar Images, objective TMPro text, mission-passed TMPro text

## Out of Scope
- Post-processing effects (bloom, vignette)
- 3D screen-space effects
- Localization of objective/banner text
- Custom fonts beyond TMPro defaults

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/ScreenEffects.cs
Assets/_Project/Prefabs/UI/ScreenEffectsCanvas.prefab
```

ScreenEffects.cs is a MonoBehaviour placed on a Canvas GameObject with:
- CanvasGroup for full-screen fade (alpha 0..1)
- Two Image components (top/bottom bars) for letterbox
- TMPro text for objective popup (anchored left-center)
- TMPro text for mission-passed banner (anchored center)

Camera shake references Camera.main and applies localPosition offsets via coroutine.

### Build Approach
1. Create ScreenEffects Canvas with CanvasGroup, sort order 999, screen-space overlay
2. Implement FadeToBlack / FadeFromBlack using CanvasGroup.alpha with AnimationCurve evaluation
3. Implement ScreenShake coroutine: random insideUnitCircle offset scaled by intensity, applied to camera localPosition, restore original position on complete
4. Implement Letterbox: two anchored Image bars (top stretch-top, bottom stretch-bottom), animate RectTransform height via coroutine
5. Implement ShowObjective: TMPro text, animate anchoredPosition.x from off-screen-left to center, hold, then animate off-screen-right
6. Implement ShowMissionPassed: TMPro text, set active, wait 3 seconds, fade out via CanvasGroup or text alpha
7. Add System.Action completion callbacks to all methods and corresponding UnityEvents

### Testing Strategy
- EditMode tests for ScreenEffects state transitions (fade state tracking, shake parameters validation)
- PlayMode tests: call FadeToBlack, verify CanvasGroup.alpha reaches 1.0 after duration
- PlayMode tests: call ShowLetterbox, verify bar RectTransform heights reach target
- PlayMode tests: call ShowObjective, verify text content is set correctly
- Manual playtest: visual verification of all effects in a test scene

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | setup | Create ScreenEffectsCanvas prefab with CanvasGroup, letterbox Images, TMPro texts | — | Canvas exists with sort order 999, all child elements present |
| 2 | code | Implement FadeToBlack / FadeFromBlack with AnimationCurve alpha | 1 | Fade completes over specified duration, callback fires |
| 3 | code | Implement ScreenShake with intensity and duration | 1 | Camera offsets applied and restored, no residual offset |
| 4 | code | Implement ShowLetterbox / HideLetterbox bar animation | 1 | Bars animate to target height and back to zero |
| 5 | code | Implement ShowObjective slide-in/hold/slide-out | 1 | Text appears from left, holds 2s, exits right |
| 6 | code | Implement ShowMissionPassed centered banner with 3s auto-fade | 1 | Banner shows centered, fades after 3 seconds |
| 7 | test | Write EditMode + PlayMode tests for all effects | 2-6 | All tests pass, coverage on public API |
