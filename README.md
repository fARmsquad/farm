# 🌱 fARm

**An AR-enabled farm simulation game inspired by Stardew Valley and Animal Crossing**

---

## Concept

Harvest Horizon blends the cozy life-sim loop of Stardew Valley and Animal Crossing with augmented reality — letting players grow, tend, and inhabit a living farm layered over the real world. Your backyard becomes your field. Your kitchen table becomes your crafting bench. The seasons in-game mirror real-world seasons and time of day.

---

## Core Pillars

| Pillar | Description |
|---|---|
| **Real-world Anchoring** | Farms are planted in physical spaces using AR anchors. A raised bed in your yard can become your in-game crop plot. |
| **Persistent World** | The farm persists and evolves even when the app is closed, using server-side simulation. Crops grow on real time. |
| **Social & Shared** | Friends can visit your AR farm in real space or remotely. Villagers (NPCs) appear in your physical environment. |
| **Cozy Progression** | No fail states. Slow, rewarding loops around planting, crafting, relationships, and exploration. |

---

## Gameplay Loop

```
Wake up → Check farm (AR or map view) → Water/harvest crops
→ Craft goods → Visit town / neighbor farms → Talk to villagers
→ Complete requests / seasonal events → Sleep (end day)
```

---

## Key Features

### Farm & Crops
- Plant seeds in real-world AR plots anchored to physical locations
- Crops grow on a real-time schedule (hours/days, not just game days)
- Seasonal crops tied to real-world date and location climate data
- Soil quality, weather effects (rain helps, frost hurts)

### AR Layer
- Place farm buildings, decorations, and characters in physical space
- Walk around your actual environment to explore your farm
- Remote "map view" for when you're away from home base
- Shared AR spaces — friends see your farm when they visit in-person

### Villagers & Relationships
- Procedurally generated villagers with schedules, preferences, and dialogue
- Villagers appear at real-world locations (coffee shop NPC at a coffee shop, etc.)
- Gift-giving, friendship meters, and story arcs à la Animal Crossing
- Special villager requests tied to seasonal events

### Crafting & Economy
- Harvest crops → craft goods → sell at market or gift to villagers
- Recipes unlock through relationships and exploration
- Player-to-player trading marketplace

### Seasonal Events
- Real-world calendar drives in-game festivals (harvest festival in fall, etc.)
- Limited-time items, crops, and villager dialogue
- Community goals — server-wide crop totals unlock town upgrades

### Customization
- Farm layout, building aesthetics, character appearance
- Furniture and decoration for both AR-placed home and in-game cottage
- "Pocket camp"-style home base for remote play

---

## Platform

| Target | Notes |
|---|---|
| **Meta Quest (primary)** | Full AR/MR experience via passthrough, spatial anchors, hand tracking |
| **Web** | Farm management, market, social features, and remote play without a headset |

---

## Technical Sketch

```
Client (AR)                    Backend
─────────────────              ─────────────────────────────
ARKit / ARCore                 Farm simulation engine (tick-based)
  │                            Persistent world state (Postgres)
  ├── Anchor management        Real-time updates (WebSockets / Redis pub-sub)
  ├── Crop/building rendering  Villager AI scheduler
  └── Social overlay           Seasonal event manager
                               Player economy & trading
```

- **AR Anchors**: Shared spatial anchors via Meta's Scene API so co-located players see the same farm
- **Simulation**: Server ticks every ~15 min, advancing crop growth, NPC schedules, weather
- **Offline grace**: Client predicts state locally; reconciles on reconnect

---

## Inspiration & Differentiation

| Game | What we borrow | What we do differently |
|---|---|---|
| Stardew Valley | Deep farming loop, relationship system, crafting | Real-time (not turn-based days), AR-first |
| Animal Crossing | Slow cozy pace, villager charm, seasonal events | Active farming gameplay, no Nintendo lock-in |
| Minecraft Earth | AR building in real world | Cozy tone, social NPCs, farming focus |

---

## Open Questions

- How do players without large spaces participate? (Tabletop mode on any flat surface)
- Multiplayer farm ownership — shared farms or visiting only?
- How do web players interact with a Quest-primary world? (map view, async actions)
- Quest-specific UX: hand tracking vs. controller for farming actions

---

## Vibe

> *"What if your garden was also a video game, and your neighbors were characters you actually wanted to talk to?"*
