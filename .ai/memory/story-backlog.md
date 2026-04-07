# FarmSim VR — Story Packs (Layered)

## Philosophy

Build the game in the editor first. Wire the headset later.

Every Core/ system, every MonoBehaviour controller, every visual effect,
every sound, every particle — all buildable and testable without putting
on a Quest. The XR input layer is a thin skin on top of completed systems.

Headset stories are extracted to a separate pack: `story-packs-xr.md`.
When the game works in the editor with mouse/keyboard simulated inputs,
THEN we wire real hands.

---

## The Layers

```
BUILDABLE NOW (editor + TDD):
  Layer 1: THE WORLD EXISTS    <- Farm, ground, sky, structures
  Layer 2: THE FARM WORKS      <- Soil, plant, grow, water, harvest (logic + visuals)
  Layer 3: TIME PASSES         <- Day/night, weather, seasons
  Layer 4: I PROGRESS          <- Unlock, upgrade, expand, economy
  Layer 5: IT FEELS ALIVE      <- Sound, particles, atmosphere
  Layer 6: THE WORLD TEACHES   <- Journal, encyclopedia, facts
  Layer 7: THE BUSINESS WORKS  <- Save, analytics

LATER (requires headset — see story-packs-xr.md):
  Layer X0: I EXIST            <- VR rig, hands, locomotion
  Layer X1: I CAN TOUCH THINGS <- Grab, poke, gestures
  Layer X2: I'M NOT ALONE      <- Multiplayer (needs networking + XR)
  Layer X3: MONETIZATION       <- Store, IAP (needs live product)
```

---

## Layer 1: THE WORLD EXISTS

### L1-001: Farm Layout (Greybox)
- Ground plane (20m x 20m, grass material)
- Dirt paths connecting areas
- Perimeter fence
- Spawn/start position marker
- Crop plot positions (6 empty plots in a grid, 1m x 1m each)
- Tool rack position (east side)
- Barn position (north)
- Well position (west)
- Expansion zone boundaries (visible but blocked)
- Basic directional light (sun)
- Procedural skybox
- Layout: Barn(N), Well(W), Plot Grid(center), Rack(E), Spawn(S entrance)
- Expansion zones: east, west, north (beyond fence, fogged)
- Test: Play mode with free camera (WASD + mouse). Fly around. Layout spatial sense, walkable distances.
- Depends on: nothing

### L1-002: Sky & Lighting
- URP procedural skybox (sun disk, gradient horizon, cloud tint)
- Directional light (warm, 45 deg angle, soft shadows)
- Environment lighting from skybox
- Reflection probe in farm center
- Test: Feels like outdoors? Shadows reasonable? Warm and inviting?
- Depends on: L1-001

### L1-003: Farm Art Pass
- Replace greybox with real .glb models via glb-ingest pipeline
- Materials set up for URP (or glTFast PBR)
- Textures optimized for Quest (ASTC, max 1024)
- Static batching on non-interactive objects
- LODs on anything >5K triangles
- Models: Barn, fence (modular), well, tool rack, soil plots, trees, bushes, hay bales, crates, wheelbarrow, path stones
- Depends on: L1-001, glb-ingest pipeline

---

## Layer 2: THE FARM WORKS

### L2-001: Soil System
- SoilManager (Core/) — state machine per plot: empty -> planted -> growing -> harvestable -> depleted
- SoilData (ScriptableObject) — soil type definitions (sandy, loam, clay, rich)
- SoilState (Core/) — moisture (0-1), nutrient level (0-1), current crop ID
- SoilPlotController (MonoBehaviour) — visual state: material swap
- SoilInteractionZone (MonoBehaviour) — trigger collider for proximity
- Depends on: L1-001

### L2-002: Crop Growth System
- CropGrowthCalculator (Core/) — calculates growth per tick
- CropData (ScriptableObject) — per-crop: base rate, stages, max growth, preferred conditions
- GrowthConditions (Core/) — input struct
- GrowthResult (Core/) — output: growth amount, stage, is fully grown
- CropVisualController (MonoBehaviour) — swaps model at stage transitions
- GrowthTicker (MonoBehaviour) — calls calculator each tick
- Depends on: L2-001

### L2-003: Planting System
- PlantingService (Core/) — validate plot, consume seed, create crop record
- SeedInventory (Core/) — track available seeds by type
- PlantingController (MonoBehaviour) — handles input
- SeedlingSpawner (MonoBehaviour) — instantiates seedling visual
- Depends on: L2-001, L2-006

### L2-004: Watering System
- WaterService (Core/) — apply water to plot, manage can capacity
- WateringCanData (ScriptableObject) — capacity, pour rate, refill time
- WateringController (MonoBehaviour) — handles input
- WaterVFXController (MonoBehaviour) — particle system
- SoilWetnessVisual (MonoBehaviour) — lerps soil material
- Depends on: L2-001, L2-002

### L2-005: Harvest System
- HarvestService (Core/) — validate harvestable, calculate yield, award items
- HarvestCalculator (Core/) — yield based on crop type, soil, conditions
- HarvestController (MonoBehaviour) — handles input
- HarvestVFXController (MonoBehaviour) — burst particles
- Depends on: L2-002, L2-006

### L2-006: Inventory System
- InventorySystem (Core/) — add, remove, query, stack items
- ItemDatabase (Core/) — item definitions lookup
- InventorySlot (Core/) — single slot: item type, quantity, max stack
- InventoryUIController (MonoBehaviour) — debug panel
- Depends on: nothing (pure data system)

---

## Layer 3: TIME PASSES

### L3-001: Day/Night Cycle
- TimeService (Core/) — ITimeProvider, current time, time scale
- DayPhase (Core/) — enum: Dawn, Morning, Afternoon, Evening, Night
- TimeConfig (ScriptableObject) — cycle duration, phase thresholds
- DayNightController (MonoBehaviour) — drives light rotation, skybox, ambient
- Depends on: L1-002

### L3-002: Weather System
- WeatherService (Core/) — state machine, API integration
- IWeatherProvider (Interface) — real API vs fake for testing
- FakeWeatherProvider (Core/) — deterministic for tests
- WeatherEffectsController (MonoBehaviour) — particles, skybox, fog
- WeatherAudioController (MonoBehaviour) — rain, thunder, wind
- Depends on: L3-001, L2-002

### L3-003: Seasons
- SeasonManager (Core/) — current season, transition timing
- SeasonData (ScriptableObject) — per-season: crop whitelist, weather weights, visual profile
- SeasonVisualController (MonoBehaviour) — color grading, foliage tint
- Depends on: L3-001, L3-002

---

## Layer 4: I PROGRESS

### L4-001: Economy — depends on L2-005, L2-006
### L4-002: Tool Upgrades — depends on L4-001
### L4-003: Crop Unlocks — depends on L4-001
### L4-004: Farm Expansion — depends on L4-001, L1-001
### L4-005: Skill Trees — depends on L4-001

---

## Layer 5-7: (Future sprints)
### L5-001: Ambient Sound
### L5-002: Particle Polish
### L6-001-004: Educational Content
### L7-001-003: Business Systems

---

## Build Order

Sprint 0: L1-001 Farm Layout, L1-002 Sky & Lighting
Sprint 1: L2-006 Inventory, L2-001 Soil, L2-002 Growth, L2-003 Planting, L2-004 Watering, L2-005 Harvest
Sprint 2: L1-003 Art Pass, L2-* MonoBehaviours, L5-002 Particles
Sprint 3: L3-001 Day/Night, L3-002 Weather, L5-001 Ambient Sound
Sprint 4: L4-001 Economy, L4-002-004 Progression
Sprint 5: L3-003 Seasons, L4-005 Skill Trees, L6-*, L7-001
Sprint 6+: XR Headset (see story-packs-xr.md)
