# Feature Spec: Mission Manager — INT-007

## Summary
An objective tracking and mission state manager that displays active objectives on screen and triggers a mission-passed banner with audio when objectives are completed. It provides the gameplay feedback loop for the intro's tutorial missions.

## User Story
As a player, I want to see my current objective on screen and receive a satisfying completion banner so that I know what to do and feel rewarded when I succeed.

## Acceptance Criteria
- [ ] MissionState enum: None, Active, Complete
- [ ] MissionManager MonoBehaviour tracks current mission state, name, and objective text
- [ ] StartMission(string name, string objectiveText) sets state to Active, stores name and objective, calls ScreenEffects.ShowObjective(objectiveText)
- [ ] UpdateObjective(string newText) changes the displayed objective text mid-mission via ScreenEffects.ShowObjective(newText)
- [ ] CompleteMission() sets state to Complete, calls ScreenEffects.ShowMissionPassed(missionName), plays a completion jingle via SimpleAudioManager.PlaySFXByKey("mission_complete")
- [ ] CurrentMissionName and CurrentMissionState are publicly readable properties
- [ ] OnMissionStarted UnityEvent fires when StartMission is called (passes mission name)
- [ ] OnMissionCompleted UnityEvent fires when CompleteMission is called (passes mission name)
- [ ] Calling StartMission while a mission is already Active completes the current mission silently (no banner) and starts the new one
- [ ] MissionManager holds serialized references to ScreenEffects and SimpleAudioManager

## Edge Cases
- CompleteMission called when state is None logs a warning and does nothing
- UpdateObjective called when state is None logs a warning and does nothing
- StartMission with empty name or objectiveText uses fallback defaults ("Unnamed Mission", "No objective")
- CompleteMission followed immediately by StartMission: completion banner plays, then new objective appears
- Multiple rapid CompleteMission calls: only the first takes effect (state transitions None -> cannot complete again)

## Performance Impact
- Pure state management — no per-frame cost when idle
- Delegates all visual work to ScreenEffects and SimpleAudioManager
- Events use UnityEvent with zero allocation when no listeners

## Dependencies
- **Existing:** None
- **New:** MissionState.cs (enum), MissionManager.cs (MonoBehaviour)
- **System refs:** INT-001 (ScreenEffects for objective popup and mission-passed banner), INT-002 (SimpleAudioManager for completion jingle)

## Out of Scope
- Multiple simultaneous missions
- Mission persistence or save/load
- Sub-objectives or checklists
- Mission failure state
- Mission rewards or scoring
- Quest log UI

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/Core/Cinematics/MissionState.cs               (enum, FarmSimVR.Core)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/MissionManager.cs   (MonoBehaviour)
```

MissionState enum is pure C# (no Unity dependencies) and lives in FarmSimVR.Core. MissionManager is a MonoBehaviour that holds references to ScreenEffects and SimpleAudioManager and delegates all visual/audio work to them.

### Build Approach
1. Define MissionState enum: None, Active, Complete in FarmSimVR.Core assembly
2. Create MissionManager MonoBehaviour with: MissionState currentState, string currentMissionName, string currentObjectiveText, [SerializeField] ScreenEffects screenEffects, [SerializeField] SimpleAudioManager audioManager
3. Implement StartMission(string name, string objectiveText): if currently Active, silently set state to Complete (no banner); set state to Active, store name + objective, call screenEffects.ShowObjective(objectiveText), invoke OnMissionStarted
4. Implement UpdateObjective(string newText): guard against None state, update stored text, call screenEffects.ShowObjective(newText)
5. Implement CompleteMission(): guard against None state, set state to Complete, call screenEffects.ShowMissionPassed(currentMissionName), call audioManager.PlaySFXByKey("mission_complete"), invoke OnMissionCompleted, reset state to None
6. Expose CurrentMissionName and CurrentMissionState as public read-only properties
7. Wire OnMissionStarted and OnMissionCompleted UnityEvents

### Testing Strategy
- EditMode tests: MissionState enum values exist (None, Active, Complete)
- EditMode tests: MissionManager state transitions: None -> Active -> Complete -> None
- PlayMode tests: StartMission sets state to Active, stores name and objective
- PlayMode tests: CompleteMission transitions to Complete, invokes OnMissionCompleted
- PlayMode tests: UpdateObjective changes objective text when Active
- PlayMode tests: CompleteMission when None logs warning, state unchanged
- Integration test: wire mock ScreenEffects and SimpleAudioManager, verify ShowObjective and ShowMissionPassed called

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | code | Define MissionState enum (None, Active, Complete) in FarmSimVR.Core | — | Enum compiles in Core assembly, no Unity dependencies |
| 2 | code | Create MissionManager MonoBehaviour with state, name, objective fields and system references | 1 | Component attaches, all fields editable, references assignable |
| 3 | code | Implement StartMission with ScreenEffects.ShowObjective call and OnMissionStarted event | 2,INT-001 | State set to Active, objective displayed, event fires |
| 4 | code | Implement UpdateObjective with guard and ScreenEffects call | 2,INT-001 | Objective text updated on screen, warning if state is None |
| 5 | code | Implement CompleteMission with banner, jingle, OnMissionCompleted event, state reset | 2,INT-001,INT-002 | Banner shows, jingle plays, event fires, state resets to None |
| 6 | code | Expose CurrentMissionName and CurrentMissionState read-only properties | 2 | Properties return correct values at all states |
| 7 | test | Write EditMode + PlayMode tests for mission state flow | 1-6 | All tests pass, coverage on all state transitions and edge cases |
