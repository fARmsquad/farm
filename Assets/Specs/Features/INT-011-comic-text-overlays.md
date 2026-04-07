# Feature Spec: Comic Text & Speech Bubbles — INT-011

## Summary
A comic-style text overlay system for cinematic panels. Supports three modes: full-screen panel text (bottom, fade in/out), comic burst text (large impact font with rotation and scale-in animation), and world-space speech bubbles with tails pointing to speakers. Used for the intro's narration text, El Pollo Loco's "COCK-A-DOODLE-DOO!" burst, and Chop's subtitle translations.

## User Story
As a cinematic designer, I want comic-book-style text effects so that the intro panels feel like an animated graphic novel with personality, not just plain subtitles.

## Acceptance Criteria
- [ ] PanelText: bottom-screen TMPro text, fades in over 0.5s, holds for configurable duration, fades out over 0.5s
- [ ] PanelText supports configurable font size, color, and italic/bold
- [ ] ComicBurst: large center-screen text with impact-style font, scale-in from 0→1 with overshoot bounce, slight random rotation (±5°), holds then fades
- [ ] ComicBurst supports configurable hold duration, font color, outline color, outline thickness
- [ ] SpeechBubble: world-space Canvas above a target Transform, rounded rectangle background with triangular tail pointing down toward speaker
- [ ] SpeechBubble text uses typewriter effect (reuses DialogueManager's timing of ~30 chars/sec)
- [ ] SpeechBubble has optional "translation" sub-line in smaller italic text below the main text (for Chop's subtitles)
- [ ] ComicTextManager.cs provides: ShowPanelText(), ShowComicBurst(), ShowSpeechBubble(), HideAll()
- [ ] All methods accept an optional onComplete callback
- [ ] All overlays render above letterbox bars (sortingOrder > ScreenEffects)
- [ ] Integration point for CinematicSequencer via public methods

## Edge Cases
- Multiple PanelTexts queued: latest replaces previous (fade out old, fade in new)
- ComicBurst while dialogue is playing: both visible (different screen regions)
- SpeechBubble target destroyed: bubble auto-hides
- SpeechBubble off-screen: clamps to screen edge with arrow pointing off-screen

## Performance Impact
- 1-3 TMPro text objects active at any time — negligible
- World-space Canvas for speech bubble: 1 draw call per bubble
- No texture atlas needed — TMPro SDF rendering

## Dependencies
- **Existing:** TextMeshPro, ScreenEffects (for sortingOrder reference)
- **New:** ComicTextManager.cs, SpeechBubble.cs (prefab), comic burst animation
- **System refs:** INT-003 (DialogueManager typewriter timing), INT-005 (CinematicSequencer)

## Out of Scope
- Full comic panel borders or page layouts
- Animated text effects (wavy, rainbow) — plain text only
- Localization or multi-language support

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/ComicTextManager.cs
Assets/_Project/Scripts/MonoBehaviours/Cinematics/SpeechBubble.cs
Assets/_Project/Prefabs/UI/SpeechBubble.prefab
Assets/_Project/Fonts/ImpactStyle.ttf (or TMPro asset)
```

### Build Approach
1. Create ComicTextManager.cs as singleton MonoBehaviour on a dedicated Canvas (sortingOrder 1000+)
2. Implement PanelText: TMPro text at bottom, coroutine fade in/hold/fade out
3. Implement ComicBurst: TMPro text at center, scale + rotation animation via coroutine, outline shader
4. Create SpeechBubble.cs: world-space Canvas prefab with background Image (9-slice rounded rect), tail Image, TMPro text, optional sub-text
5. Wire all methods with onComplete callbacks
6. Test each mode independently, then through sequencer

### Testing Strategy
- EditMode: ComicTextManager method signatures, callback invocation
- PlayMode: PanelText fade timing, ComicBurst scale animation, SpeechBubble follows target
- Manual: visual quality of comic burst, speech bubble tail alignment

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | MonoBehaviour | Create ComicTextManager.cs with Canvas + ShowPanelText() | — | Panel text fades in/out at bottom screen |
| 2 | MonoBehaviour | Add ShowComicBurst() with scale-in + rotation animation | 1 | Impact text bounces in at center, holds, fades |
| 3 | prefab | Create SpeechBubble.prefab (world-space Canvas, rounded rect, tail) | — | Bubble renders above target, tail points down |
| 4 | MonoBehaviour | Create SpeechBubble.cs with typewriter + translation sub-line | 3 | Text types in, optional italic translation below |
| 5 | MonoBehaviour | Add ShowSpeechBubble() / HideAll() to ComicTextManager | 1,4 | Manager can spawn/hide bubbles on any Transform |
| 6 | test | PlayMode tests for fade timing, scale animation, bubble tracking | 1-5 | All overlay modes work, callbacks fire |

## Asset Strategy

> **Font:** Use TMPro's default bold font (LiberationSans SDF Bold) as placeholder. Real comic font (Bangers/Bungee) swapped in during **INT-014 Art & Audio Polish**.
>
> **Speech bubble background:** Generated at build time as a Unity UI rounded-rect sprite — no external asset needed.
