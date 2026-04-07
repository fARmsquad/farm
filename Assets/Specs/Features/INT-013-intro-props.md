# Feature Spec: Intro Props & Ambient NPCs — INT-013

## Summary
Scripted props and ambient NPCs specific to the intro cinematic: a handheld lantern with dynamic light and swing animation, baby chicks that peck and scatter, a thrown boot arc, and a cat silhouette crossing a rooftop. These are lightweight, self-contained behaviours that bring the intro panels to life.

## User Story
As a player watching the intro, I want to see small animated details (a swinging lantern, scattering chicks, a thrown boot, a crossing cat) so that the world feels alive and the comedy lands.

## Acceptance Criteria

### Lantern
- [ ] LanternHolder.cs attaches to player, positions lantern in left hand (offset from player transform)
- [ ] Point light child with warm color (FFD699), range 5, intensity 1.2, soft shadows off
- [ ] Pendulum swing animation synced to walk cycle (±15° on Z axis, 0.8s period)
- [ ] Light flicker: subtle random intensity variation (±0.1) via Mathf.PerlinNoise
- [ ] Lantern visual: use existing Synty prop if available, otherwise a small cylinder + sphere

### Baby Chicks
- [ ] BabyChick.cs: small-scale chicken (0.3x), wander within radius (3m from spawn), peck animation (bob head down 0.5s every 2-4s random)
- [ ] Scatter behavior: when player enters 2m radius, chicks flee outward for 1.5s then resume pecking
- [ ] 4-6 chick instances inside the pen in Panel 5
- [ ] Reuses AnimalWander base logic with reduced speed (1.5 m/s) and wander radius

### Thrown Boot
- [ ] BootProjectile.cs: spawns at a window position, follows parabolic arc toward target (near rooster), lands on ground
- [ ] Arc uses simple kinematic trajectory (no Rigidbody): lerp XZ + gravity on Y
- [ ] Duration: 1.2 seconds from throw to land
- [ ] On landing: stays on ground (no bounce, no destroy) — it's a background gag
- [ ] Triggered by CinematicSequencer at the right moment in Panel 3

### Cat Silhouette
- [ ] RooftopRunner.cs: moves a small dark sprite/capsule across a rooftop path (point A to point B) over 1.5 seconds
- [ ] Runs during the aerial pan in Panel 2 — subtle easter egg
- [ ] Simple transform.Translate along local X axis, no animation needed
- [ ] Dark silhouette: black unlit material, flattened capsule (0.3 × 0.15 × 0.6 scale)

## Edge Cases
- Lantern swing while player stationary: swing dampens to idle sway (±3°)
- Baby chicks scatter into fence: chicks have NavMesh or simple raycast to avoid leaving pen bounds
- Boot thrown but rooster already moved: boot lands at original target position regardless
- Cat runner triggered but camera has moved past: cat runs anyway, player may not see it (intentional easter egg)

## Performance Impact
- Lantern: 1 point light (no shadows) — minimal
- Baby chicks: 4-6 transforms with simple wander — under 0.1ms
- Boot: single kinematic object for 1.2s — negligible
- Cat: single transform move — negligible
- Total: well within Quest budget

## Dependencies
- **Existing:** AnimalWander.cs (base for chick wander), PlayerMovement (lantern parent), Synty farm props (lantern model if available)
- **New:** LanternHolder.cs, BabyChick.cs, BootProjectile.cs, RooftopRunner.cs
- **System refs:** INT-005 (CinematicSequencer for triggering boot and cat)

## Out of Scope
- Lantern as equippable/droppable item
- Chick AI beyond scatter (no catching, no pen mechanics for chicks)
- Boot physics or player-throwable objects
- Cat as interactable NPC

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/LanternHolder.cs
Assets/_Project/Scripts/MonoBehaviours/Cinematics/BabyChick.cs
Assets/_Project/Scripts/MonoBehaviours/Cinematics/BootProjectile.cs
Assets/_Project/Scripts/MonoBehaviours/Cinematics/RooftopRunner.cs
```

### Build Approach
1. LanternHolder: child object on player with point light, Mathf.Sin pendulum + PerlinNoise flicker in Update
2. BabyChick: extend AnimalWander pattern with reduced params, add scatter trigger via OnTriggerEnter with player tag
3. BootProjectile: coroutine-based parabolic arc (Vector3.Lerp for XZ, gravity curve for Y), triggered by public Launch()
4. RooftopRunner: coroutine moving transform from start to end over duration, triggered by public Run()
5. Each script has a public method callable from CinematicSequencer

### Testing Strategy
- EditMode: BootProjectile arc math (start, apex, end positions at t=0, 0.5, 1.0)
- PlayMode: LanternHolder light intensity stays within bounds, BabyChick scatter on proximity
- Manual: visual quality of lantern swing, chick scatter feel, boot arc, cat run timing

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | MonoBehaviour | Create LanternHolder.cs with pendulum swing + light flicker | — | Swings with walk, flickers subtly, warm light |
| 2 | MonoBehaviour | Create BabyChick.cs with peck + scatter behaviors | — | Pecks idle, scatters from player, stays in bounds |
| 3 | MonoBehaviour | Create BootProjectile.cs with parabolic arc | — | Launches from window, arcs toward target, lands |
| 4 | MonoBehaviour | Create RooftopRunner.cs with A-to-B movement | — | Dark silhouette crosses rooftop over 1.5s |
| 5 | prefab | Create BabyChick prefab (small chicken, 0.3x scale) | 2 | Prefab works when placed in pen area |
| 6 | test | EditMode arc math, PlayMode scatter + swing | 1-4 | All behaviors verified |

## Asset Strategy

> **Lantern:** No Synty lantern available. Placeholder: small cylinder (handle) + sphere (glass) + point light child. Roof light prefab `SM_Gen_Prop_Light_Roof_01` can substitute if held prop isn't needed.
>
> **Baby chicks:** Existing `chicken.glb` scaled to 0.3x with a yellow-tinted material.
>
> **Boot:** No Synty boot available. Placeholder: small brown cube (0.15 × 0.1 × 0.25 scale).
>
> **Cat silhouette:** No Synty cat available. Placeholder: black unlit flattened capsule (0.3 × 0.15 × 0.6).
>
> All placeholders are replaced with real models in the **INT-014 Art & Audio Polish** pass after scene assembly.
