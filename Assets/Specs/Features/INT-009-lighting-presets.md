# Feature Spec: Lighting Presets & Transitions — INT-009

## Summary
A reusable lighting preset system that can smoothly transition between time-of-day states (Night, Pre-Dawn, Dawn, Day). Includes a window cascade effect where house windows light up in sequence radiating outward from a trigger point, simulating a shockwave of disturbance.

## User Story
As a cinematic designer, I want to define lighting states as data assets and transition between them smoothly so that the intro sequence can move from moonlit night through pre-dawn to golden dawn without hardcoded values.

## Acceptance Criteria
- [ ] LightingPreset ScriptableObject stores: ambient color, ambient intensity, directional light color, directional light intensity, directional light rotation, fog color, fog density, skybox tint
- [ ] LightingTransition.cs lerps between two LightingPreset assets over a configurable duration
- [ ] Transitions use AnimationCurve for easing (default: ease-in-out)
- [ ] At least 3 presets created: Night (cool blue, 0.1 ambient), PreDawn (purple-navy, 0.25 ambient), Dawn (warm gold, 0.6 ambient)
- [ ] WindowCascade.cs accepts a list of Light components (or Renderer emissive swaps) and enables them in sequence with configurable delay (default 0.2s)
- [ ] Cascade radiates outward from a world-space origin point (e.g., El Pollo Loco's position)
- [ ] Cascade sorts lights by distance from origin before activating
- [ ] LightingTransition integrates with CinematicSequencer via a public Play() method
- [ ] WindowCascade integrates with CinematicSequencer via a public Trigger(Vector3 origin) method
- [ ] All transitions cancellable via Stop()

## Edge Cases
- Transition interrupted mid-lerp: snaps to current interpolated value, begins new transition from there
- No directional light in scene: logs warning, skips directional properties
- WindowCascade with zero lights: no-op, no error
- Multiple transitions queued: latest wins, previous cancelled

## Performance Impact
- Single directional light property changes per frame during transition — negligible
- Window cascade: one SetActive/material swap per light per interval — under 10 calls total
- No baked lighting changes, all runtime

## Dependencies
- **Existing:** RenderSettings API, directional light in scene
- **New:** LightingPreset.cs (ScriptableObject), LightingTransition.cs (MonoBehaviour), WindowCascade.cs (MonoBehaviour)
- **System refs:** INT-005 (CinematicSequencer for integration)

## Out of Scope
- Full day/night cycle (continuous clock-driven) — this is discrete presets only
- Baked lightmaps or light probes
- Weather-dependent lighting

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/LightingPreset.cs      (ScriptableObject)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/LightingTransition.cs  (MonoBehaviour)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/WindowCascade.cs       (MonoBehaviour)
Assets/_Project/ScriptableObjects/Cinematics/Lighting/NightPreset.asset
Assets/_Project/ScriptableObjects/Cinematics/Lighting/PreDawnPreset.asset
Assets/_Project/ScriptableObjects/Cinematics/Lighting/DawnPreset.asset
```

### Build Approach
1. Create LightingPreset ScriptableObject with all lighting properties
2. Create LightingTransition MonoBehaviour with coroutine-based lerp and AnimationCurve
3. Create WindowCascade MonoBehaviour with sorted distance activation
4. Create 3 preset assets (Night, PreDawn, Dawn) with values from the intro script
5. Test transitions in isolation, then integrate with CinematicSequencer

### Testing Strategy
- EditMode: LightingPreset serialization (values round-trip correctly)
- PlayMode: LightingTransition lerps ambient color over duration
- PlayMode: WindowCascade activates lights in distance order with correct delay
- Manual: visual check of Night→PreDawn→Dawn transitions in WorldMain

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | ScriptableObject | Create LightingPreset.cs with all lighting properties | — | Asset can be created in editor, all fields serialized |
| 2 | MonoBehaviour | Create LightingTransition.cs with coroutine lerp + AnimationCurve | 1 | Lerps between two presets over duration, easing works |
| 3 | MonoBehaviour | Create WindowCascade.cs with distance-sorted sequential activation | — | Lights activate in order from origin, configurable delay |
| 4 | asset | Create NightPreset, PreDawnPreset, DawnPreset assets | 1 | Values match intro script lighting descriptions |
| 5 | test | EditMode + PlayMode tests for transitions and cascade | 2,3 | Phase transitions and timing verified |
| 6 | integration | Wire LightingTransition and WindowCascade for sequencer use | 2,3,INT-005 | Sequencer SetLighting step can trigger transitions |
