# World Terrain Build-Out Design — Willowbrook

**Date:** 2026-04-07
**Scope:** Full 400×400m world terrain for all 9 zones; Farm + Town populated with Synty assets; remaining zones terrain-only
**Approach:** Hybrid terrain (Unity Terrain for ground + mesh planes for water/paths)
**Replaces:** Existing 20×20m farm greybox (L1-001). Farm layout preserved as reference for plot/barn placement.

---

## 1. World Layout & Coordinates

Total world: **400m × 400m**, origin at center `(0, 0, 0)`. +Z = north, +X = east.

```
         -200         -40    0    40          200
  200  ┌──────────────┬─────────┬──────────────┐
       │              │         │              │
       │  North Field │  (gap)  │ Sandy Shores │
       │  160×100     │         │ 160×80       │
  100  ├──────────────┤ Trail   ├──────────────┤
       │              │ 80×100  │              │
       │              │ creek + │   McTavish   │
       │  Willowbrook │ path   │     Farm     │
       │    Town      │         │   160×120    │
       │  160×180     │         │              │
  -80  ├──────────────┴────┬────┴──────────────┤
       │    Meadow         │  River  │  County │
       │    140×80         │ 180×80  │  Fair   │
       │                   │         │  80×80  │
 -160  ├───────────────────┴─────────┴─────────┤
       │        Wildflower Hills  400×40       │
 -200  └───────────────────────────────────────┘
```

### Zone Coordinates (top-left X,Z → bottom-right X,Z)

| Zone             | Corner 1 (X, Z) | Corner 2 (X, Z) | Size (m)  | Base Y | Ground              |
|------------------|------------------|------------------|-----------|--------|---------------------|
| North Field      | (-200, 200)      | (-40, 100)       | 160 × 100 | 0      | Grass + clover      |
| Sandy Shores     | (40, 200)        | (200, 120)       | 160 × 80  | 0      | Sand                |
| Willowbrook Town | (-200, 100)      | (-40, -80)       | 160 × 180 | 0      | Gravel roads + grass|
| Trail            | (-40, 100)       | (40, 0)          | 80 × 100  | 0→0.5  | Dirt path           |
| McTavish Farm    | (40, 100)        | (200, -20)       | 160 × 120 | 0.5    | Grass + dirt plots  |
| Meadow           | (-200, -80)      | (-60, -160)      | 140 × 80  | 0–2    | Lush grass + flowers|
| River            | (-60, -80)       | (120, -160)      | 180 × 80  | -1     | Water plane channel |
| County Fair      | (120, -80)       | (200, -160)      | 80 × 80   | 0      | Mixed grass/dirt    |
| Wildflower Hills | (-200, -160)     | (200, -200)      | 400 × 40  | 0–4    | Rolling + flowers   |

---

## 2. Terrain Configuration

### Unity Terrain Settings (Quest-optimized)

| Setting               | Value          | Rationale                              |
|-----------------------|----------------|----------------------------------------|
| Heightmap resolution  | 513 × 513      | Good detail for 400m, Quest-friendly   |
| Terrain size          | 400 × 20 × 400 | XYZ — 20m max height range             |
| Texture resolution    | 1024 × 1024    | Balance quality vs. memory             |
| Detail resolution     | 512            | Grass detail patches                   |
| Base texture res      | 1024           | Per-splat tile resolution              |
| Draw instanced        | ON             | GPU instancing for performance         |
| Pixel error           | 8              | Quest LOD tolerance                    |
| Base map distance     | 150            | Beyond this, use base map only         |

### Terrain Layers (from PolygonNature/Textures/Ground_Textures/)

| Layer # | Texture         | Normal            | Use                          |
|---------|-----------------|-------------------|------------------------------|
| 0       | Grass.png       | BaseGrass_normals | Default base — everywhere    |
| 1       | Grass_02.png    | BaseGrass_normals | Meadow/hills variation       |
| 2       | Mud.png         | Mud_Normal        | Farm plots, riverbanks       |
| 3       | Sand.png        | (none)            | Sandy shores zone            |
| 4       | Pebbles.png     | Pebbles_normals   | Paths, road edges            |
| 5       | Flowers.png     | Flowers_normals   | Meadow/clover accents        |
| 6       | Sand_Darker.png | (none)            | Sandy shores wet areas       |

### Elevation Map

| Zone             | Elevation                     | Technique                       |
|------------------|-------------------------------|---------------------------------|
| Town, N. Field   | Flat (y = 0)                  | Default height                  |
| Sandy Shores     | Flat (y = 0)                  | Default height                  |
| Farm             | Gentle plateau (y = 0.5)      | Raised flat brush               |
| Trail            | Slope (0 → 0.5)              | Gradient from town to farm      |
| Meadow           | Rolling (y = 0–2)            | Soft hills brush                |
| River            | Channel (y = -1)             | Carved depression               |
| Wildflower Hills | Rolling hills (y = 0–4)      | Multiple rounded peaks          |
| County Fair      | Flat (y = 0)                  | Default height                  |

---

## 3. Water System

All water uses PolygonNature river mesh prefabs placed in terrain depressions.

| Water Body | Prefab(s)                                      | Length | Placement                        |
|------------|-------------------------------------------------|--------|----------------------------------|
| River      | `River_Plane_01` × ~6 segments chained          | ~180m  | Winding through river zone       |
| Creek      | `River_Plane_01` × ~2 (scaled 0.5)              | ~60m   | Trail zone, crossing path        |
| Farm Pond  | `SM_Env_Pond_01`                                | ~8m    | Near farm pasture                |
| River Edge | `SM_Terrain_RiverSide_01` + `Corner_01/02`      | —      | Along river banks                |
| Reeds      | `SM_Env_Reeds_01/02/03` scattered along banks   | —      | River + pond edges               |
| Lily Pads  | `SM_Env_Lillypads_01/02`                        | —      | Farm pond surface                |

---

## 4. Path & Road Network

Using PolygonFarm dirt/gravel road prefabs snapped together.

### Main Street (Town)
- `SM_Env_Road_Gravel_Straight_01` — core straight segments
- `SM_Env_Road_Gravel_Corner_01/02` — at intersections
- `SM_Env_Road_Gravel_T_Section_01` — side street junctions
- `SM_Env_Road_Gravel_End_01/02` — terminators
- Runs E-W through town center, ~120m

### Trail to Farm
- `SM_Env_Road_Dirt_Straight_01` — main segments
- `SM_Env_Road_Dirt_Swerve_01/02` — winding uphill
- `SM_Env_Road_Dirt_End_01` — terminators
- ~80m, winding NE from town to farm

### Farm Internal Paths
- `SM_Env_Road_Dirt_Straight_01` between buildings and plots
- `SM_Env_Road_Dirt_T_Section_01` at plot junctions
- `SM_Env_Road_GrassEdge_01/02` soft transitions to grass

### Other Zones
- Meadow, Hills: No mesh roads — terrain-painted pebble texture only
- Sandy Shores: Terrain-painted sand, no discrete roads

---

## 5. Farm Zone — Populated (Asset Placement)

Reference: existing L1-001 greybox positions, scaled to new 160×120m zone.

### Buildings

| Asset                        | Position (approx X, Z)   | Rotation | Role                    |
|------------------------------|--------------------------|----------|-------------------------|
| `SM_Bld_Farmhouse_01`       | (100, 60)                | 180°     | Player's farmhouse      |
| `SM_Bld_Barn_01`            | (140, 70)                | 90°      | The barn (paintable)    |
| `SM_Bld_Silo_01`            | (155, 75)                | 0°       | Adjacent to barn        |
| `SM_Bld_Silo_Small_01`      | (150, 65)                | 0°       | Secondary silo          |
| `SM_Bld_Greenhouse_01`      | (60, 30)                 | 0°       | The Truth's basil plot  |

### Circular Pen (El Pollo Loco)
- Ring of `SM_Prop_Fence_Wood_Round_01/02` — radius ~8m
- Center: (120, 40)
- `SM_Prop_Fence_Wood_Gate_01` — single gate opening

### Crop Plots (4 expandable)
- 4× clusters of `SM_Env_Dirt_Rows_Center_01` (8–10 row segments each)
- Bordered by `SM_Env_Dirt_Skirt_01`
- Grid positions: (70,50), (85,50), (70,35), (85,35) — ~12m spacing
- Vegetable overlays: `SM_Env_Vege_Rows_01/02/03` on top

### Pasture (East Side)
- `SM_Prop_Fence_Wire_01/02` perimeter — ~30×20m enclosure
- `SM_Prop_Fence_Wire_Pole_01` at corners and every ~5m
- `SM_Prop_Fence_Wire_Gate_01` — single entry
- Position: centered at (170, 30)

### Props (scattered)
- `SM_Prop_Well_01` — near farmhouse
- `SM_Prop_Scarecrow_01` — in crop plots
- `SM_Prop_Haystack_01/02` — near barn (3–4 instances)
- `SM_Prop_Crate_01/02` — near barn doors
- `SM_Prop_Wheelbarrow_01` — near plots
- `SM_Prop_WateringCan_01` — near well
- `SM_Prop_Barrel_01/02` — near barn and house
- `SM_Prop_Trough_01` — in pasture
- `SM_Prop_Letterbox_01` — at farm entrance

### Trees (Farm)
- `SM_Env_Tree_Apple_Grown_01` × 3 — orchard cluster near house
- `SM_Env_Tree_Large_01` × 2 — shade trees
- `SM_Env_Tree_Cherry_Grown_01` × 2 — near path entrance
- `SM_Generic_Grass_Patch_01/02/03` × ~40 — scattered across grass areas

### FX
- `FX_Pollen_Wind_01` — ambient pollen near crop plots
- `FX_Sprinkler_01` × 2 — in crop plot areas

---

## 6. Town Zone — Populated (Asset Placement)

### Buildings

| Asset                        | Position (approx X, Z)   | Role                           |
|------------------------------|--------------------------|--------------------------------|
| `SM_Bld_Farmhouse_02`       | (-160, 20)               | Player's house (town)          |
| `SM_Bld_ProduceStand_01`    | (-120, 30)               | Cluckin' Bell (Big Smoke's)    |
| `SM_Bld_Garage_01`          | (-70, 30)                | Community center               |
| `SM_Bld_Shelter_01`         | (-160, -10)              | Cafe (OG Loc's open mic)       |
| `SM_Bld_Greenhouse_Large_01`| (-120, -10)              | Library (Lester's destination) |
| `SM_Bld_Farmhouse_01` × 2   | (-80, -20), (-100, -40)  | Town houses                    |

### Main Street Furniture
- `SM_Prop_Fence_Painted_01/02` — along Main Street edges
- `SM_Prop_Fence_Painted_Pole_01/02` — at intervals
- `SM_Prop_Lamp_01` × 6 — along Main Street, ~20m spacing
- `SM_Prop_Sign_01/02` — at intersections
- `SM_Prop_Letterbox_01` × 3 — at houses
- `SM_Prop_Bench_01` × 2 — near community center

### Town Props
- `SM_Prop_Barrel_01/02` × 4 — near buildings
- `SM_Prop_Crate_01/02/03` × 4 — near produce stand
- `SM_Prop_Plant_Bush_01/02` × 8 — decorative along streets
- `SM_Prop_Flower_Pot_01/02` × 4 — at building entrances
- `SM_Prop_Beehive_01` — behind a house (gameplay hook)

### Town Vegetation
- `SM_Generic_Grass_Patch_01/02/03` × ~30 — between buildings
- `SM_Env_Flowers_01/02/03` × ~15 — decorative patches
- `SM_Env_Tree_Large_01` × 4 — shade trees along Main Street
- `SM_Generic_Tree_Patch_01` × 2 — tree clusters at town edges

---

## 7. Unpopulated Zones — Terrain Only

Each zone gets: terrain texture painting, elevation shaping, zone boundary marker (empty GameObject with BoxCollider trigger), and minimal vegetation markers.

### North Field
- Terrain: Grass base + painted Flowers texture in patches
- Scattered: `SM_Env_Flowers_01/02/03` × 8 as visual markers
- `SM_Generic_Grass_Patch_01/02/03` × ~20

### Sandy Shores
- Terrain: Sand/Sand_Darker textures
- Marker: empty GO at trailer position (future Trevor placement)
- Edge: `SM_Env_Pebbles_01/02` × 5 at zone boundary

### Meadow
- Terrain: Grass_02 + Flowers, rolling elevation 0–2m
- Scattered: `SM_Env_Flowers_01/02/03` × 12 (5 species for quest)
- `SM_Generic_Grass_Patch_01/02/03` × ~25
- `SM_Prop_Mushroom_01/02` × 4 (truffle hunting markers)

### River
- Terrain: Carved -1m channel with Mud banks
- Water: `River_Plane_01` × 6 chained segments
- Banks: `SM_Terrain_RiverSide_01` + corners
- Vegetation: `SM_Env_Reeds_01/02/03` × 10 along banks
- Marker: empty GO at The Truth's van location

### County Fair
- Terrain: Flat, mixed grass/dirt (Pebbles texture patches)
- Marker: empty GO for train loop center
- `SM_Prop_Fence_Fancy_01` × ~8 — perimeter hint

### Wildflower Hills
- Terrain: Rolling 0–4m, Grass + Flowers texture
- `SM_Env_Flowers_01/02/03` × 10
- `SM_Generic_Grass_Patch_01/02/03` × ~15
- `SM_Tree_Pine_01` × 3, `SM_Tree_Birch_01` × 3 — scattered

### Trail
- Terrain: Pebbles texture painted path, gentle slope
- Creek: `River_Plane_01` × 2 (scaled), crossing path
- Bridge: `SM_Prop_Bridge_Curved_01` over creek
- `SM_Prop_Lamp_01` × 2 — atmospheric along path

---

## 8. World-Wide Vegetation (Synty-Density Approach)

Following the Synty exemplar's density patterns (~500 grass patches, 300+ flowers, 400+ trees across world), we scatter vegetation globally:

### Trees (~120 total for initial pass)
| Prefab                          | Count | Primary Zones              |
|---------------------------------|-------|----------------------------|
| `SM_Tree_Birch_01/02/03/04`    | 20    | Meadow, Trail, Town edges  |
| `SM_Tree_Pine_01/02/03/04`     | 15    | North Field, Hills         |
| `SM_Tree_Willow_Medium/Large`  | 8     | River banks, Meadow        |
| `SM_Env_Tree_Large_01`         | 10    | Town, Farm, scattered      |
| `SM_Env_Tree_Apple_Grown_01`   | 8     | Farm orchard               |
| `SM_Env_Tree_Cherry_Grown_01`  | 6     | Farm, Town                 |
| `SM_Generic_Tree_Patch_01/02`  | 10    | Zone boundaries, clusters  |
| Other fruit trees               | ~15   | Scattered across zones     |
| Dead trees / stumps            | 8     | River banks, Hills edges   |

### Ground Cover (~200 total)
| Prefab                          | Count | Distribution               |
|---------------------------------|-------|-----------------------------|
| `SM_Generic_Grass_Patch_01`     | 50    | Even across all grassy zones|
| `SM_Generic_Grass_Patch_02`     | 50    | Even across all grassy zones|
| `SM_Generic_Grass_Patch_03`     | 50    | Even across all grassy zones|
| `SM_Env_Flowers_01/02/03`      | 40    | Meadow, Hills, N. Field    |
| `SM_Bush_01/02/03`             | 20    | Zone boundaries, paths     |
| `SM_Fern_01/02/03`             | 15    | Meadow, River banks        |

### Rocks (~40 total)
| Prefab                          | Count | Placement                  |
|---------------------------------|-------|-----------------------------|
| PolygonNature rock prefabs      | 20    | River banks, Hills, N.Field|
| `Rocks and Boulders 2` prefabs | 15    | Zone boundaries            |
| `SM_Generic_Rock_01/02`        | 10    | Scattered accents          |

---

## 9. Scene Hierarchy (Hybrid: Zone → Type)

Informed by Synty exemplar's category grouping, adapted for zone-based world.

```
WorldMain.unity
├── _SceneConfig/
│   ├── Directional Light
│   ├── Global Volume (post-processing)
│   └── ReflectionProbe
├── Main Camera
├── Terrain/
│   └── Willowbrook_Terrain
├── Water/
│   ├── River_Segment_01 ... _06
│   ├── Creek_Segment_01 ... _02
│   ├── FarmPond
│   └── RiverBank/ (RiverSide prefabs, reeds)
├── Paths/
│   ├── MainStreet/ (gravel road prefabs)
│   ├── TrailToFarm/ (dirt road prefabs)
│   └── FarmPaths/ (dirt road prefabs)
├── Farm/
│   ├── Buildings/ (farmhouse, barn, silos, greenhouse)
│   ├── Pen/ (circular fence ring + gate)
│   ├── Plots/ (dirt rows + vege rows)
│   ├── Pasture/ (wire fence enclosure)
│   ├── Props/ (well, scarecrow, haystack, crates, etc.)
│   ├── Trees/ (apple, cherry, shade trees)
│   └── FX/ (pollen, sprinklers)
├── Town/
│   ├── Buildings/ (houses, shops, community center)
│   ├── MainStreet/ (lamps, fences, signs)
│   ├── Props/ (barrels, crates, benches, flower pots)
│   └── Trees/ (shade trees, decorative)
├── NorthField/
│   └── Markers/ (zone trigger, seasonal event position)
├── SandyShores/
│   └── Markers/ (trailer position, zone trigger)
├── Meadow/
│   └── Markers/ (truffle spots, wildflower positions)
├── River/
│   └── Markers/ (van position, zone trigger)
├── CountyFair/
│   └── Markers/ (train loop center, zone trigger)
├── WildflowerHills/
│   └── Markers/ (easel position, flower spawn points)
├── Trail/
│   ├── Bridge/ (creek crossing)
│   └── Markers/ (lamp post positions)
├── Vegetation/
│   ├── Trees/ (world-scattered trees not in Farm/Town)
│   ├── GroundCover/ (grass patches, ferns)
│   ├── Flowers/ (wildflower instances)
│   ├── Bushes/
│   └── Rocks/
├── FX/
│   ├── FX_Pollen_Wind (meadow region)
│   └── FX_Dust_Wind (sandy shores)
└── Markers/
    ├── SpawnPoint
    └── ZoneTriggers/ (one BoxCollider per zone)
```

---

## 10. Lighting & Atmosphere

Following Synty exemplar's warm golden-hour aesthetic + fog boundary technique.

### Directional Light
- Color: `#FFF4E0` (warm afternoon — matches L1-002 spec)
- Intensity: 1.2
- Rotation: (50, -30, 0) — 50° elevation, slight west offset
- Shadows: Soft, distance 80m, 4 cascades

### Fog (Synty-inspired)
- Mode: Linear
- Color: `#FFD49E` (warm golden, Synty uses R:1 G:0.83 B:0.62)
- Start: 80m
- End: 350m (hides world edges at ~400m boundary)

### Ambient
- Mode: Gradient
- Sky color: `#4F4861` (cool purple-blue — Synty exemplar contrast)
- Equator: `#8B7E6A` (warm mid-tone)
- Ground: `#3A3428` (dark warm)

### Reflection Probe
- Position: (0, 5, 0) — world center, elevated
- Size: (400, 20, 400) — covers full world
- Resolution: 256 (Quest budget)
- Type: Baked

---

## 11. Performance Budget (Quest)

| Metric           | Budget    | Our Estimate  | Notes                              |
|------------------|-----------|---------------|------------------------------------|
| Draw calls       | < 200     | ~120–150      | Instanced vegetation, shared mats  |
| Triangles/frame  | < 750K    | ~400–500K     | LODs on trees, low-poly Synty      |
| Terrain layers   | ≤ 8       | 7             | Within limit                       |
| Texture memory   | < 512MB   | ~200–300MB    | Shared Synty atlas materials       |
| Active GOs       | < 2000    | ~600–800      | Sparse non-farm/town zones         |

### Optimization Strategies
- GPU instancing ON for terrain and vegetation
- LOD groups on all PolygonNature trees (LOD prefabs exist)
- Fog hides distant detail (no need for full-distance rendering)
- Baked lighting only (no real-time shadows beyond 80m)
- Zone-based occlusion: only Farm+Town are dense; other zones are sparse

---

## 12. Out of Scope

- NPC placement and AI (INT-006)
- Interactive crop planting/harvesting (L2 specs)
- Day/night cycle (F-040)
- Weather system (F-042)
- Interior scenes / building interiors
- Audio / ambient sounds (INT-002)
- Quest triggers and mission logic (INT-007)
- Save/load of world state (F-006)
- Seasonal variation (F-041)
- Player controller integration (existing F-003 will be migrated separately)
