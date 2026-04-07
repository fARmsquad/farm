# Polish Backlog — POL-001

## Summary
A shared backlog of polish items across the entire game. These are **not blockers** — the game is fully playable without them. This backlog is for a dedicated polish sprint after all gameplay systems and the intro are feature-complete.

Items are organized by category. Each item has a priority (P1 = high impact, P2 = medium, P3 = nice-to-have) and an estimated scope (S/M/L).

---

## Visual Polish

| # | Item | Priority | Scope | Notes |
|---|------|----------|-------|-------|
| V1 | Replace all primitive placeholders with real 3D models | P1 | L | Chop (dog), lantern, boot, cat, NPCs, furniture |
| V2 | Add proper character animations (walk, idle, sleep, interact) | P1 | L | Currently using transform movement, no skeletal animation |
| V3 | Terrain texture painting (grass, dirt, sand, rock) | P2 | M | Currently using flat colors on terrain |
| V4 | Add skybox transitions (night stars → dawn gradient → day) | P2 | M | Currently discrete presets, no sky texture changes |
| V5 | Water shader for river/creek/pond (reflections, flow) | P2 | M | Currently using flat blue plane or Synty prefab |
| V6 | Add shadow casting to key objects (farmhouse, barn, trees) | P2 | S | Currently no real-time shadows for Quest perf |
| V7 | Crop growth visual stages (seedling → sprout → mature → harvest-ready) | P2 | M | Currently single model with scale change |
| V8 | El Pollo Loco custom model with crow/dodge/tired animations | P1 | M | Currently scaled chicken.glb |
| V9 | NPC models for Tenpenny, Big Smoke, Niko (replace capsules) | P2 | M | Currently colored capsules via NPCController |
| V10 | UI polish: custom HUD icons, styled buttons, health/stamina bars | P3 | M | Currently default Unity UI |
| V11 | Moonlight cookie texture for bedroom window | P3 | S | Currently point light only |
| V12 | Dew/frost on morning grass (shader or particle overlay) | P3 | S | Adds dawn atmosphere |

## Audio Polish

| # | Item | Priority | Scope | Notes |
|---|------|----------|-------|-------|
| A1 | Full intro music suite (guitar, tension, kazoo standoff) | P1 | M | Currently silent or placeholder |
| A2 | El Pollo Loco signature crow SFX (bass-boosted, reverb tail) | P1 | S | The most important single SFX in the game |
| A3 | Ambient sound zones (crickets, birds, water, wind per zone) | P2 | M | Zone-triggered ambient audio layers |
| A4 | Footstep SFX (gravel, grass, wood, water) per terrain type | P2 | M | Currently silent movement |
| A5 | UI sound effects (button clicks, menu transitions, notifications) | P3 | S | Currently silent UI |
| A6 | Animal sounds (clucks, moos, oinks, baas, neighs) per type | P2 | S | Currently silent animals |
| A7 | Day/night ambient music transitions | P3 | M | Morning = upbeat, night = mellow |
| A8 | Voice lines for all NPCs (TTS placeholder → real VO) | P3 | L | Depends on voice talent availability |
| A9 | Farming SFX (plant, water, harvest, tool use) | P2 | S | Per-action feedback sounds |

## Gameplay Polish

| # | Item | Priority | Scope | Notes |
|---|------|----------|-------|-------|
| G1 | Haptic feedback for VR interactions (Quest controllers) | P2 | M | Plant, water, catch, open gate vibrations |
| G2 | Tutorial hints system (contextual prompts for first-time actions) | P2 | M | "Press E to interact" style, fades after first use |
| G3 | Smooth camera transitions between gameplay and cinematic modes | P2 | S | Currently instant swap |
| G4 | Animal pen population visible from distance (LOD or billboards) | P3 | S | Currently pop in at close range |
| G5 | Inventory UI with item icons and descriptions | P2 | M | Currently text-only |
| G6 | Day/night cycle with clock UI and time-based events | P2 | L | Currently static time set by intro |
| G7 | Weather system (rain, sun, overcast) affecting crop growth | P3 | L | Currently using enum but no visual weather |
| G8 | NPC daily schedules (walk routes, sleep, shop) | P3 | L | Currently static positions |

## Performance Polish

| # | Item | Priority | Scope | Notes |
|---|------|----------|-------|-------|
| P1 | LOD setup for all 3D models (3 levels) | P2 | M | Ensure Quest stays under 750K tris |
| P2 | Occlusion culling bake for WorldMain | P2 | S | Reduce draw calls in dense zones |
| P3 | Texture atlas for Synty materials (batch draw calls) | P3 | M | Currently separate materials per object |
| P4 | Object pooling for particle systems and animals | P3 | S | Reduce GC pressure on Quest |
| P5 | Profile full intro sequence on Quest hardware | P1 | S | Ensure 90 FPS throughout |

## Known Tech Debt

| # | Item | Priority | Scope | Notes |
|---|------|----------|-------|-------|
| D1 | Fix FindObjectsSortMode deprecation warnings (SimulationManager, GameStateLogger) | P2 | S | Use new overload without sort param |
| D2 | Remove .bak files from repo (ScreenEffectsSceneSetup.cs.bak etc.) | P1 | S | Cluttering untracked files |
| D3 | Resolve Unity Cloud project name mismatch warning | P3 | S | "Farm Project" vs "My project" |
| D4 | Add missing .meta files or clean up orphaned ones | P2 | S | Occasional meta file warnings in console |
| D5 | Standardize Debug.Log removal (conditional compilation) | P2 | M | Some scripts still have Debug.Log in committed code |

---

## How to Use This Backlog

1. **During feature development:** If you notice a polish item while building, add it here. Don't fix it now — stay focused on the feature.
2. **Polish sprint:** When all gameplay + intro specs are Done, claim items from this backlog by priority.
3. **Scope estimates:** S = under 1 hour, M = 1-4 hours, L = 4+ hours or multi-session.
4. **Priorities:** P1 first (high-impact, visible to players), then P2, then P3.
5. **Spec-specific polish:** INT-014 handles intro-specific polish. This backlog is for everything else.

---

## Sprint Planning Template

When ready for a polish sprint, pick items by priority and create a focused spec:

```
POL-002: Visual Polish Sprint 1 (V1, V2, V8)
POL-003: Audio Polish Sprint 1 (A1, A2, A3, A6)
POL-004: Performance Sprint (P1, P2, P5)
POL-005: Tech Debt Cleanup (D1, D2, D4, D5)
```
