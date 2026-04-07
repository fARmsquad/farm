# Feature Spec: Environment Particle Systems — INT-010

## Summary
A library of reusable particle effect prefabs for the intro cinematic and general world atmosphere. Covers fireflies, chimney smoke, dew sparkle, creek shimmer/ripples, and interior dust motes. Each is a self-contained prefab that can be placed in any scene.

## User Story
As a cinematic designer, I want drop-in particle effect prefabs so that I can add atmosphere to intro panels (fireflies in the meadow, smoke from chimneys, dew on grass at dawn) without writing custom shaders or VFX graphs.

## Acceptance Criteria
- [ ] Firefly prefab: 8-12 yellow particles, random blink (on/off cycle 0.5-2s), slow drift, configurable bounds volume
- [ ] ChimneySmoke prefab: grey particles rising from a point, dissipating over 3-4 seconds, slight wind drift
- [ ] DewSparkle prefab: small white flash particles on ground plane, brief lifetime (0.3s), random placement within bounds
- [ ] CreekShimmer prefab: blue-white particles on water surface, horizontal drift matching flow direction, subtle scale pulse
- [ ] DustMotes prefab: tiny white particles floating in a beam volume (for moonlit interiors), very slow random movement
- [ ] All prefabs use Unity's built-in Particle System (Shuriken), no VFX Graph dependency
- [ ] All prefabs have a ParticleController.cs wrapper with Play(), Stop(), SetIntensity(float) for sequencer integration
- [ ] Each prefab stays under 50 particles max at any time (Quest performance)
- [ ] All particles use unlit/additive material (no lighting cost)

## Edge Cases
- Particle system disabled then re-enabled: resumes cleanly, no burst of accumulated particles
- SetIntensity(0): emission stops, existing particles fade naturally
- Prefab placed underground or off-screen: particles still calculate but GPU culls — acceptable

## Performance Impact
- Max 50 particles per prefab × 5 prefabs = 250 particles worst case
- Unlit additive material: no lighting calculations
- All CPU-simulated (no GPU particles) for Quest compatibility
- Well within Quest particle budget

## Dependencies
- **Existing:** Unity Particle System (built-in)
- **New:** ParticleController.cs, 5 particle prefabs
- **System refs:** INT-005 (CinematicSequencer for integration)

## Out of Scope
- VFX Graph or Shader Graph custom effects
- Weather particles (rain, snow) — future L3 spec
- Particle collision or interaction with player

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/ParticleController.cs
Assets/_Project/Prefabs/FX/Fireflies.prefab
Assets/_Project/Prefabs/FX/ChimneySmoke.prefab
Assets/_Project/Prefabs/FX/DewSparkle.prefab
Assets/_Project/Prefabs/FX/CreekShimmer.prefab
Assets/_Project/Prefabs/FX/DustMotes.prefab
Assets/_Project/Materials/FX/Particle_Additive.mat
```

### Build Approach
1. Create shared unlit additive particle material
2. Create ParticleController.cs wrapper (Play, Stop, SetIntensity)
3. Build each prefab one at a time, tuning in scene view:
   - Fireflies: cone emission, random lifetime, color over lifetime (yellow→transparent)
   - ChimneySmoke: point emission upward, size over lifetime (grow), color over lifetime (grey→transparent)
   - DewSparkle: box emission on XZ plane, short burst, high start speed downward
   - CreekShimmer: box emission along flow axis, gentle drift, scale pulse
   - DustMotes: box emission in volume, near-zero velocity, very long lifetime
4. Test each in WorldMain scene for visual quality
5. Wire ParticleController for sequencer integration

### Testing Strategy
- EditMode: ParticleController.SetIntensity clamps 0-1, Play/Stop state toggles
- PlayMode: each prefab instantiates without errors, particle count stays under max
- Manual: visual check in scene view, particle count in profiler

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | material | Create Particle_Additive unlit material | — | Renders as additive blend, no lighting |
| 2 | MonoBehaviour | Create ParticleController.cs wrapper | — | Play/Stop/SetIntensity work, serialized reference to ParticleSystem |
| 3 | prefab | Create Fireflies.prefab (yellow blink, drift) | 1,2 | 8-12 particles, random blink, within bounds |
| 4 | prefab | Create ChimneySmoke.prefab (grey, rising) | 1,2 | Particles rise and fade, slight wind offset |
| 5 | prefab | Create DewSparkle.prefab (white flash, ground) | 1,2 | Brief sparkle on ground, dawn-only feel |
| 6 | prefab | Create CreekShimmer.prefab (blue-white, flow) | 1,2 | Horizontal drift, subtle pulse, water feel |
| 7 | prefab | Create DustMotes.prefab (white, floating, interior) | 1,2 | Very slow drift in moonlight beam volume |
| 8 | test | EditMode + PlayMode tests for ParticleController | 2 | State management and bounds verified |

## Asset Requirements — USER ACTION NEEDED

> **No art assets needed** — all particles use Unity's built-in circle/square sprites with the additive material. Tuning is done via Particle System curves in the Inspector.
