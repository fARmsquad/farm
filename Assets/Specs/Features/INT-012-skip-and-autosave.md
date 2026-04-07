# Feature Spec: Cutscene Skip & Auto-Save — INT-012

## Summary
A hold-to-skip UI prompt that appears during cutscenes and an auto-save system that triggers when gameplay begins. Skipping the intro jumps directly to Panel 5's gameplay transition (mission card + HUD fade-in). Auto-save writes an intro-complete flag so the player doesn't replay the cutscene on restart.

## User Story
As a player, I want to skip the intro cutscene if I've seen it before, and I want my progress saved automatically when gameplay starts so I don't lose my place.

## Acceptance Criteria
- [ ] After 3 seconds of cutscene playback, a subtle "Hold [Space] to skip" prompt appears in the bottom-right corner
- [ ] Prompt uses low-opacity text (40% alpha) that brightens to 100% while Space is held
- [ ] Hold duration to skip: 1.5 seconds (circular radial fill indicator around the prompt)
- [ ] Skipping calls CinematicSequencer.SkipToStep(panelFiveStepIndex) — jumps to Panel 5 gameplay transition
- [ ] Skip triggers: hide letterbox, show mission card, fade in HUD, enable player control
- [ ] SkipPrompt.cs is a self-contained MonoBehaviour that attaches to any cutscene Canvas
- [ ] AutoSave.cs writes a JSON file to Application.persistentDataPath on gameplay start
- [ ] Save data includes: introComplete (bool), gameClockTime (6:15 AM after intro), timestamp
- [ ] On next launch, if introComplete is true, skip directly to gameplay (load WorldMain, set clock)
- [ ] Save file location: {persistentDataPath}/farm_save.json
- [ ] AutoSave.HasCompletedIntro() static method for other systems to query

## Edge Cases
- Player releases Space before 1.5s: radial fill resets, prompt goes back to 40% opacity
- Skip during a dialogue line: DialogueManager.Hide() called first, then skip
- Skip during a lighting transition: transition snaps to Dawn preset immediately
- Save file corrupted or missing: treated as fresh start, intro plays
- Save file from different version: version field checked, reset if mismatched

## Performance Impact
- SkipPrompt: one Image (radial fill) + one TMPro text — negligible
- AutoSave: single JSON write (~200 bytes) — negligible, happens once
- JSON parsing on startup: < 1ms

## Dependencies
- **Existing:** CinematicSequencer (for SkipToStep), ScreenEffects (for letterbox/fade), Application.persistentDataPath
- **New:** SkipPrompt.cs, AutoSave.cs
- **System refs:** INT-005 (CinematicSequencer), INT-001 (ScreenEffects)

## Out of Scope
- Full save/load system (inventory, farm state, etc.) — this is intro-only
- Multiple save slots
- Cloud save sync
- Settings persistence (audio volume, controls)

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/SkipPrompt.cs
Assets/_Project/Scripts/Core/GameState/AutoSave.cs
```

**SkipPrompt.cs**: MonoBehaviour on cutscene Canvas. Watches Keyboard.current.spaceKey.isPressed, fills radial Image, fires onSkip UnityEvent when full. Self-hides when cutscene ends.

**AutoSave.cs**: Pure C# (Core/) static class. SaveIntroComplete(), HasCompletedIntro(), GetSaveData(), ClearSave(). Uses JsonUtility for serialization. No UnityEngine dependency beyond JsonUtility and Application.persistentDataPath (accessed via injection or wrapper).

### Build Approach
1. Create SkipPrompt.cs with hold-to-skip logic (radial fill, opacity lerp, Input System)
2. Create AutoSave.cs with JSON read/write and version checking
3. Wire SkipPrompt.onSkip to CinematicSequencer.SkipToStep
4. Wire AutoSave.SaveIntroComplete() to gameplay-start event
5. Wire startup check: if AutoSave.HasCompletedIntro(), skip intro scene load

### Testing Strategy
- EditMode: AutoSave round-trip (save, load, verify fields), version mismatch handling, corrupted file handling
- PlayMode: SkipPrompt radial fill progresses while held, resets on release, fires event at 1.5s
- Manual: visual check of prompt opacity, skip flow from Panel 2 to Panel 5

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | MonoBehaviour | Create SkipPrompt.cs with hold-to-skip radial fill | — | Space held fills circle, releases resets, 1.5s triggers skip |
| 2 | Core | Create AutoSave.cs with JSON save/load | — | SaveIntroComplete writes JSON, HasCompletedIntro reads it, version check works |
| 3 | integration | Wire SkipPrompt to CinematicSequencer.SkipToStep | 1, INT-005 | Skip jumps to Panel 5 transition |
| 4 | integration | Wire AutoSave to gameplay-start event | 2 | Save written when player control activates |
| 5 | integration | Wire startup intro-skip check | 2 | If intro complete, loads WorldMain directly |
| 6 | test | EditMode tests for AutoSave, PlayMode tests for SkipPrompt | 1,2 | Round-trip save, hold timing, event firing |
