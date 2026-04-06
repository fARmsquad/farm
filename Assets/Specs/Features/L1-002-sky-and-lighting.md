# Feature Spec: Sky & Lighting — L1-002

## Summary
Procedural skybox, directional light as sun, ambient lighting. Foundation for
day/night cycle later. Fixed "pleasant afternoon" for now.

## User Story
As a player, I want to see a beautiful outdoor sky and warm lighting so the farm
feels like a real outdoor space.

## Acceptance Criteria
- [ ] URP procedural skybox with sun disk, gradient horizon, cloud tint
- [ ] Directional light: warm color (slightly orange), 45 degree elevation angle, soft shadows enabled
- [ ] Environment lighting set to source from skybox
- [ ] Reflection probe placed at farm center capturing skybox
- [ ] Ambient intensity comfortable (not washed out, not too dark)
- [ ] Shadow distance appropriate for farm scale (~30m)

## VR Interaction Model
Not applicable — environment setup.

## Edge Cases
- Shadows must not flicker at distance (shadow cascade settings)
- Skybox must look good from all farm positions
- Reflection probe bounds must cover full farm area

## Performance Impact
- Single directional light — no per-pixel overhead
- Reflection probe: baked, not real-time
- Quest compliant (single light, baked reflections)

## Dependencies
- L1-001 (ground + structures to receive shadows)

## Out of Scope
- Day/night cycle animation (L3-001)
- Weather effects (L3-002)
- Dynamic lighting changes

---

## Technical Plan

### Architecture
- **No Core/ classes** — scene and settings configuration only
- **MCP tools**: manage_gameobject (reflection probe), manage_components (light settings)
- **Scene**: Assets/_Project/Scenes/FarmMain.unity
- **Settings**: Lighting Settings asset, URP pipeline asset

### Configuration Targets
```
Directional Light → rotation (45, -30, 0), color #FFF4E0, soft shadows
Lighting Settings → Environment Source = Skybox, ambient intensity ~1.0
ReflectionProbe → position (0, 2, 0), bounds 30x10x30, baked
Shadow Settings → distance 30m, 4 cascades, stable cascades on
```

### Testing Strategy
- **Editor**: Scene view visual check from multiple angles
- **Verify**: Shadows crisp at near range, no flicker at 30m, skybox continuous horizon
- **No automated tests** (visual/perceptual verification)

---

## Task Breakdown

### Task 1: Skybox
- **Type**: Material + Lighting Settings
- **Action**: Create URP procedural skybox material, assign to Lighting Settings skybox slot
- **Depends on**: nothing
- **Acceptance**: Sky visible in Scene view with sun disk and horizon gradient

### Task 2: Sun Light
- **Type**: Scene component configuration
- **Action**: Configure directional light — color #FFF4E0, rotation (45, -30, 0), soft shadows on
- **Depends on**: nothing
- **Acceptance**: Warm directional shadows cast across ground plane at correct angle

### Task 3: Ambient
- **Type**: Lighting Settings configuration
- **Action**: Set environment lighting source to skybox, ambient intensity ~1.0
- **Depends on**: Task 1
- **Acceptance**: Scene ambient matches skybox tone, no flat dark areas in shadow

### Task 4: Reflection Probe
- **Type**: Scene GameObject
- **Action**: Place ReflectionProbe at (0, 2, 0), set bounds 30x10x30, bake
- **Depends on**: Task 1, L1-001
- **Acceptance**: Reflective surfaces in farm area show skybox, probe bake completes without error

### Task 5: Shadow Settings
- **Type**: URP pipeline asset configuration
- **Action**: Shadow distance 30m, 4 cascades, stable fit mode on
- **Depends on**: Task 2
- **Acceptance**: No shadow flicker across farm at any viewing angle, shadows crisp within 10m
