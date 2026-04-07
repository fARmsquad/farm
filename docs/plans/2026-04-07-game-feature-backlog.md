# fARm — Comprehensive Feature Backlog (3-6 Month Horizon)

> Single-player, gameplay-first scope. AR/XR integration deferred to a later phase.
> Generated 2026-04-07. Covers all systems needed for a playable vertical slice.

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done (merged to main) |
| 🔲 | Not started |
| ⚙️ | Partially built / needs iteration |

**Priority tiers:**
- **P0 — Foundation:** Must exist before any other gameplay works
- **P1 — Core Loop:** The minimum playable farming experience
- **P2 — Depth:** Systems that make the game interesting beyond 10 minutes
- **P3 — Polish:** Quality-of-life, juice, and feel

**Size estimates:** S (1-2 days), M (3-5 days), L (1-2 weeks), XL (2+ weeks)

---

## P0 — Foundation (Environment & Infrastructure)

These are the bedrock. Nothing else works without them.

### ✅ F-001: Farm Layout (Greybox)
> *L1-001 — Done*
- Ground plane with UV grid
- Placeholder farm structures (barn, house, fences)
- Walkable area defined

### ✅ F-002: Sky & Lighting
> *L1-002 — Done*
- Directional light + skybox
- URP rendering pipeline configured
- Basic ambient lighting

### ✅ F-003: Player Controller
> *Implemented in HuntingTest scene*
- WASD movement with CharacterController
- Gravity + ground snapping
- Rotation toward movement direction
- Third-person camera (fixed angle, Stardew-style offset)

### 🔲 F-004: Scene Unification | Size: M
Merge FarmMain and HuntingTest into a single playable scene.
- **Stories:**
  - [ ] Single scene with farm layout + player controller + camera
  - [ ] Farming plots placed in the greybox farm area
  - [ ] Hunting spawner integrated into same scene
  - [ ] Barn drop-off positioned at greybox barn
  - [ ] Animal pen placed near barn
  - [ ] HUD shows both farming and hunting info
  - [ ] SimulationManager and HuntingManager coexist
- **Acceptance:** Player can walk around one scene, see crops growing, catch animals, and deposit them — all in one session.

### 🔲 F-005: Game State Manager | Size: M
Central state machine for the game session.
- **Stories:**
  - [ ] GameManager singleton (or ScriptableObject-based) tracks current game state
  - [ ] States: Loading → Playing → Paused → GameOver (placeholder)
  - [ ] Pause/resume functionality (Time.timeScale or tick-based)
  - [ ] Clean scene initialization sequence (spawn player, init systems)
  - [ ] Event bus or simple event system for cross-system communication
- **Acceptance:** Game boots into a consistent state. Pause stops all simulation. Systems initialize in deterministic order.

### 🔲 F-006: Save/Load System (Core) | Size: L
Persistent game state across sessions.
- **Stories:**
  - [ ] ISaveData interface in Core/ (no Unity deps)
  - [ ] FarmSaveData: plot states, growth percentages, planted crop types
  - [ ] AnimalSaveData: pen animals (type, deposit time), wild animal state
  - [ ] InventorySaveData: items held, quantities
  - [ ] PlayerSaveData: position, current day, total playtime
  - [ ] JSON serialization to Application.persistentDataPath
  - [ ] Save on key events (harvest, deposit, day end) + manual save
  - [ ] Load on game start, handle missing/corrupt save gracefully
  - [ ] Save versioning (schema version field for future migration)
- **Acceptance:** Quit and relaunch — farm state, inventory, and animals persist exactly.

---

## P1 — Core Farming Loop

The minimum viable farming experience: plant → water → wait → harvest → sell.

### ✅ F-010: Crop Growth Calculator
> *Core — Done*
- Growth formula: `baseRate × weather × temp × soil × deltaTime`
- Weather/temp/soil multipliers
- Milestone events at 25/50/75/100%

### ⚙️ F-011: Crop Plot State Machine
> *Core — Done, needs iteration*
- PlotPhase: Empty → Planted → Growing → Ready
- Current: auto-plants tomatoes on start (hardcoded)
- **Remaining stories:**
  - [ ] Plot starts Empty (not auto-planted)
  - [ ] Player-initiated planting (from inventory)
  - [ ] Harvest returns crop to inventory (not just log)
  - [ ] Plot returns to Empty after harvest (re-plantable)
  - [ ] Withering phase if crop not harvested within X ticks

### 🔲 F-012: Soil System | Size: M
> *L2-001*
- **Stories:**
  - [ ] SoilState in Core/: quality (Poor/Normal/Rich), moisture (0-100%), tilled flag
  - [ ] Tilling action: converts raw ground → tilled soil
  - [ ] Soil degrades over time (Rich → Normal → Poor without fertilizer)
  - [ ] Moisture decreases each tick (evaporation rate based on weather)
  - [ ] Soil visuals: color tint for quality, darkening for moisture
  - [ ] SoilPlot MonoBehaviour wraps SoilState, updates visuals
- **Acceptance:** Soil quality and moisture visibly affect crop growth rate. Tilling is required before planting.

### 🔲 F-013: Seed & Planting System | Size: M
> *L2-002*
- **Stories:**
  - [ ] SeedType enum in Core/: Tomato, Corn, Pumpkin, Wheat, Carrot (starter set)
  - [ ] SeedData ScriptableObject: growth rate, max growth, seasons, sell price, seed cost
  - [ ] Planting action: player selects seed from inventory → targets tilled plot → plants
  - [ ] Planting validation: plot must be tilled + empty, player must have seed in inventory
  - [ ] Seed consumed from inventory on plant
  - [ ] Visual: small sprout appears on plant
  - [ ] Each crop type has unique growth parameters
- **Acceptance:** Player can choose between multiple seed types, plant them on tilled soil, and each grows at its own rate.

### 🔲 F-014: Watering System | Size: M
> *L2-003*
- **Stories:**
  - [ ] WateringCan tool (equippable item)
  - [ ] Water action: targets planted/growing plot → sets moisture to 100%
  - [ ] Watering can has capacity (e.g., 20 uses), refills at water source
  - [ ] Water source object (well, pond) placed in farm
  - [ ] Unwatered plots stop growing (moisture = 0 → growth paused)
  - [ ] Visual: water splash particle on pour, wet soil darkens
  - [ ] Moisture HUD indicator per plot (or visual-only)
- **Acceptance:** Crops require watering to grow. Watering can depletes and must be refilled.

### 🔲 F-015: Harvesting System | Size: S
> *L2-004*
- **Stories:**
  - [ ] Harvest action: player targets Ready plot → collects crop
  - [ ] Crop item added to inventory (type + quantity)
  - [ ] Plot resets to Empty (tilled state preserved or lost — design choice)
  - [ ] Harvest visual: crop disappears with small VFX
  - [ ] Quality system (optional v1): harvest quality based on soil + water consistency
  - [ ] Multi-harvest crops (e.g., tomato gives 3 harvests before dying)
- **Acceptance:** Player can harvest mature crops, receive items in inventory, and re-plant the plot.

### 🔲 F-016: Crop Visual System | Size: M
> *Replaces current cube-scaling placeholder*
- **Stories:**
  - [ ] Per-crop-type growth stage visuals (seed → sprout → mid → mature → harvestable)
  - [ ] 3-4 visual stages per crop (swap mesh or sprite)
  - [ ] Color/scale interpolation between stages
  - [ ] Withered visual state (brown, droopy) if neglected
  - [ ] Harvestable state visual cue (glow, bounce, particle)
- **Acceptance:** Each crop type is visually distinct and shows clear growth progression. Player can tell crop state at a glance.

---

## P1 — Core Hunting Loop

### ✅ F-020: Animal Spawning & Wandering
> *L2-007 — Done*
- Wild animals spawn at farm perimeter on timer
- AnimalWander: idle/walk state machine with circular bounds
- Configurable via HuntingConfig ScriptableObject

### ✅ F-021: Animal Flee Behavior
> *L2-007 — Done*
- Detection radius triggers flee
- Flee direction = away from player
- Cooldown before returning to wander

### ✅ F-022: Catch Mechanic
> *L2-007 — Done*
- CatchZone per animal, IPlayerInput abstraction
- Press E within catch radius → animal caught
- Carried count tracked in CaughtAnimalTracker

### ✅ F-023: Barn Drop-Off & Animal Pen
> *L2-007 — Done*
- BarnDropOff trigger deposits all carried animals
- AnimalPen: procedural fence, spawns pen animals
- Pen animals wander within pen bounds (no flee)

### 🔲 F-024: Animal Products | Size: M
- **Stories:**
  - [ ] AnimalProductType enum: Egg, Milk, Wool, Truffle
  - [ ] Product generation: pen animals produce items on a timer
  - [ ] Production rates vary by animal type (chicken→eggs daily, cow→milk every 2 days)
  - [ ] Collection action: player picks up product from pen area
  - [ ] Product added to inventory
  - [ ] Animal happiness/health affects production rate (future hook)
  - [ ] Visual: product item appears near animal when ready
- **Acceptance:** Pen animals passively generate collectible products over time.

### 🔲 F-025: Animal Feeding | Size: S
- **Stories:**
  - [ ] Feed item types: AnimalFeed (generic), or crop-based (corn, wheat)
  - [ ] Feeding action: player uses feed item on pen → animals eat
  - [ ] Fed animals have boosted product output for X ticks
  - [ ] Unfed animals reduce production (not die — cozy game, no punishment)
  - [ ] Feed trough object in pen (optional visual)
- **Acceptance:** Feeding animals improves their production rate. Missing a feeding day reduces output but never kills animals.

---

## P1 — Inventory & Economy

### 🔲 F-030: Inventory System | Size: L
- **Stories:**
  - [ ] InventoryData in Core/: list of (ItemType, quantity) slots
  - [ ] ItemType enum: Seeds (per crop), Crops (per crop), Tools, AnimalProducts, Misc
  - [ ] ItemData ScriptableObject: name, icon, stackSize, sellPrice, description
  - [ ] Add/remove/check quantity operations
  - [ ] Inventory capacity limit (e.g., 20 slots)
  - [ ] Stack limit per item type (e.g., seeds stack to 99, tools don't stack)
  - [ ] InventoryManager MonoBehaviour: wraps InventoryData, exposes to UI
  - [ ] Events: OnItemAdded, OnItemRemoved, OnInventoryFull
- **Acceptance:** Player can carry items. Adding items beyond capacity is rejected. Items stack correctly.

### 🔲 F-031: Tool System | Size: M
- **Stories:**
  - [ ] ToolType enum: Hoe (tilling), WateringCan, Basket (harvesting), Net (catching)
  - [ ] Tool equip/swap: player has one active tool
  - [ ] Tool action: context-sensitive (hoe on dirt → till, watering can on plot → water)
  - [ ] Tool durability (optional v1): tools don't break in cozy mode
  - [ ] Tool hotbar: quick-select between equipped tools (1-4 keys)
  - [ ] Current tool shown in HUD
  - [ ] Tool determines available player action on interact
- **Acceptance:** Player can switch tools and each tool performs its specific action on the correct target.

### 🔲 F-032: Shop / Selling System | Size: M
- **Stories:**
  - [ ] ShopData in Core/: buy/sell price lists
  - [ ] Sell action: player interacts with shipping bin → sells items from inventory
  - [ ] Gold/currency system: player earns gold from sales
  - [ ] Buy action: interact with shop NPC/sign → buy seeds and tools
  - [ ] Shop inventory: available seeds, tools, upgrades (static for v1)
  - [ ] Price display UI when browsing shop
  - [ ] Shipping bin object in farm (drop items to sell at end of day)
  - [ ] End-of-day sales tally (items in bin → gold earned)
- **Acceptance:** Player can sell harvested crops for gold and buy new seeds to plant. Economy loop is closed.

### 🔲 F-033: Currency & Wallet | Size: S
- **Stories:**
  - [ ] WalletData in Core/: gold balance (int)
  - [ ] Earn gold: from selling crops, animal products
  - [ ] Spend gold: buying seeds, tools, upgrades
  - [ ] Gold display in HUD (always visible)
  - [ ] Transaction validation: can't buy if insufficient gold
  - [ ] Event: OnGoldChanged
- **Acceptance:** Gold earned from sales, spent at shop. Balance always visible and accurate.

---

## P1 — Time & Day Cycle

### 🔲 F-040: Day/Night Cycle (Gameplay) | Size: M
- **Stories:**
  - [ ] GameClock in Core/: current day number, current time of day (0.0–1.0)
  - [ ] Configurable day length in real seconds (e.g., 15 min = 1 game day)
  - [ ] Time periods: Morning, Afternoon, Evening, Night
  - [ ] Day-end trigger: auto-save, tally sales, advance crop growth
  - [ ] Day-start trigger: reset daily flags, spawn new animals
  - [ ] Sleep action: player interacts with bed → skip to next morning
  - [ ] Day counter displayed in HUD
  - [ ] Events: OnNewDay, OnTimePeriodChanged, OnDayEnd
- **Acceptance:** Days pass. End of day tallies sales and advances simulation. Player can sleep to skip night.

### 🔲 F-041: Seasonal System | Size: M
- **Stories:**
  - [ ] Season enum: Spring, Summer, Fall, Winter
  - [ ] Season length: configurable days per season (e.g., 28)
  - [ ] Year = 4 seasons (tracked in save)
  - [ ] Crop seasonality: each crop grows only in certain seasons
  - [ ] Planting out-of-season blocked (or crop withers immediately)
  - [ ] Season affects weather probabilities
  - [ ] Season transition event: crops that don't survive die on season change
  - [ ] Visual hint: lighting color shift per season (subtle)
- **Acceptance:** Seasons rotate. Crops are season-locked. Planning what to plant when is a real decision.

### 🔲 F-042: Weather System | Size: S
- **Stories:**
  - [ ] WeatherState in Core/: current weather (Sunny/Cloudy/Rain/Storm)
  - [ ] Daily weather roll: random with seasonal bias (spring = more rain)
  - [ ] Weather affects crop growth (already in CropGrowthCalculator)
  - [ ] Rain auto-waters all outdoor plots (skip manual watering on rain days)
  - [ ] Weather forecast: next-day prediction shown in HUD (80% accuracy)
  - [ ] Event: OnWeatherChanged
- **Acceptance:** Weather changes daily, affects farming decisions. Rain days save watering effort.

---

## P2 — Depth & Progression

### 🔲 F-050: Farm Upgrades | Size: L
- **Stories:**
  - [ ] Upgrade tiers: Small Farm → Medium Farm → Large Farm
  - [ ] Each tier unlocks: more plots, bigger pen, new structures
  - [ ] Upgrade costs gold + materials
  - [ ] Upgrade menu accessible from farmhouse
  - [ ] Unlockable structures: greenhouse (season-immune), silo (feed storage), coop, stable
  - [ ] Progressive plot count: 4 → 9 → 16 → 25
  - [ ] Pen capacity increases with upgrades
- **Acceptance:** Player can invest gold to expand farm. Upgrades visibly change the farm layout.

### 🔲 F-051: Crop Encyclopedia / Catalog | Size: S
- **Stories:**
  - [ ] Catalog UI: list of all discoverable crops
  - [ ] Entries unlock on first harvest
  - [ ] Entry shows: name, season, growth time, sell price, tips
  - [ ] Completion tracker (X/Y crops discovered)
  - [ ] Catalog accessible from pause menu or bookshelf object
- **Acceptance:** Player can track which crops they've grown and learn about new ones.

### 🔲 F-052: Animal Encyclopedia / Catalog | Size: S
- **Stories:**
  - [ ] Same pattern as crop catalog but for animals
  - [ ] Entries unlock on first catch
  - [ ] Entry shows: animal type, products, rarity, behavior notes
  - [ ] Completion tracker
- **Acceptance:** Player can track which animals they've caught.

### 🔲 F-053: Crafting System (Basic) | Size: M
- **Stories:**
  - [ ] RecipeData in Core/: inputs (item+qty list) → output (item+qty)
  - [ ] Recipe list: fertilizer (crops→fertilizer), animal feed (wheat→feed), fencing, tools
  - [ ] Crafting station object in farm
  - [ ] Crafting UI: select recipe, show required materials, craft button
  - [ ] Material check: verify inventory has required inputs
  - [ ] Craft action: consume inputs, produce output
- **Acceptance:** Player can craft useful items from harvested materials. Adds value chain beyond sell-only.

### 🔲 F-054: Fertilizer & Soil Enrichment | Size: S
- **Stories:**
  - [ ] Fertilizer item: craftable from crops or purchasable
  - [ ] Apply fertilizer to plot: upgrades soil quality (Normal → Rich)
  - [ ] Effect duration: lasts N growth cycles then soil reverts
  - [ ] Stacking: can't apply to already-Rich soil
  - [ ] Speed fertilizer variant: boosts growth rate temporarily
- **Acceptance:** Fertilizer meaningfully speeds up farming. Strategic choice between selling crops vs. crafting fertilizer.

### 🔲 F-055: Fishing Mini-Game | Size: L
- **Stories:**
  - [ ] Fishing rod tool
  - [ ] Water tiles/zones on farm map (pond, river edge)
  - [ ] Cast action: aim at water → start fishing
  - [ ] Bite mechanic: random wait time → fish bites → timing window to reel
  - [ ] Reel mini-game: simple bar/timing mechanic (keep indicator in zone)
  - [ ] Fish types: 5-8 varieties with rarity tiers
  - [ ] Fish sellable or usable in crafting
  - [ ] FishData ScriptableObject: type, rarity, season, sell price
  - [ ] Seasonal + time-of-day fish availability
- **Acceptance:** Fishing is a relaxing alternative activity. Fish are sellable and some are rare/seasonal.

### 🔲 F-056: Quest / Task Board | Size: M
- **Stories:**
  - [ ] QuestData in Core/: description, objectives (item+qty), reward (gold+item)
  - [ ] Task board object in farm (or near shop)
  - [ ] Daily quests: 2-3 randomly generated (deliver X crops, catch Y animals)
  - [ ] Weekly quests: 1 larger goal (harvest 50 crops, earn 500 gold)
  - [ ] Quest tracking in HUD (active quest + progress)
  - [ ] Quest completion: auto-detect when objectives met → reward
  - [ ] Quest log UI in pause menu
- **Acceptance:** Quests give direction and bonus rewards. Keeps daily play sessions focused.

### 🔲 F-057: Tool Upgrades | Size: S
- **Stories:**
  - [ ] Upgrade tiers per tool: Basic → Iron → Gold → Iridium (4 tiers)
  - [ ] Upgrades cost gold + materials
  - [ ] Better tools: larger area (hoe tills 3 plots), more capacity (bigger watering can)
  - [ ] Upgrade at shop or blacksmith object
  - [ ] Visual change per tier (color tint or model swap)
- **Acceptance:** Upgraded tools reduce tedium. Clear progression path for tool investment.

### 🔲 F-058: Rare / Special Animals | Size: S
- **Stories:**
  - [ ] Rarity tiers for wild animals: Common, Uncommon, Rare
  - [ ] Rare animals: faster flee speed, smaller catch radius
  - [ ] Rare animal products are worth more gold
  - [ ] Spawn weights: rare animals appear less frequently
  - [ ] Visual distinction: size, color variant, particle effect
  - [ ] Seasonal rare spawns (e.g., golden chicken in fall)
- **Acceptance:** Catching rare animals feels rewarding and valuable. Adds depth to hunting loop.

---

## P2 — World & Exploration

### 🔲 F-060: Farm Map / Zones | Size: M
- **Stories:**
  - [ ] Farm divided into named zones: Crop Field, Pasture, Orchard, Pond, Homestead
  - [ ] Zone boundaries defined by colliders or tilemap regions
  - [ ] Each zone has specific functionality (crops only in field, animals in pasture)
  - [ ] Mini-map or zone indicator in HUD
  - [ ] Unlockable zones (start with Crop Field + Pasture, unlock others via upgrades)
- **Acceptance:** Farm feels like a place with distinct areas, not a flat grid.

### 🔲 F-061: Foraging / Wild Items | Size: S
- **Stories:**
  - [ ] Forageable items spawn in farm borders / wilderness area
  - [ ] Types: berries, mushrooms, flowers, sticks, stones
  - [ ] Respawn daily (random positions)
  - [ ] Pickup action: walk over or interact → add to inventory
  - [ ] Used in crafting or sold for small gold
  - [ ] Seasonal forageables (different items per season)
- **Acceptance:** Walking around the farm edges rewards exploration with free resources.

---

## P3 — UI & HUD

### ⚙️ F-070: Basic HUD
> *HuntingHUD exists (OnGUI) — needs full replacement*
- **Stories:**
  - [ ] Replace OnGUI with Unity UI Toolkit or uGUI Canvas
  - [ ] Top bar: gold, day number, time of day, season, weather icon
  - [ ] Bottom bar: tool hotbar (selected tool highlighted)
  - [ ] Context prompt: "Press E to [action]" near interactive objects
  - [ ] Carried items indicator (during hunting)
  - [ ] Notification toasts: "Crop harvested!", "New day!", "+50 gold"
- **Acceptance:** All critical game state visible at a glance. No OnGUI remnants.

### 🔲 F-071: Inventory UI | Size: M
- **Stories:**
  - [ ] Grid-based inventory panel (open with Tab or I)
  - [ ] Item icons with stack count
  - [ ] Tooltip on hover: item name, description, sell price
  - [ ] Drag-and-drop reordering (optional v1)
  - [ ] Sell button / trash button per item
  - [ ] Category tabs: All, Seeds, Crops, Tools, Animal Products
- **Acceptance:** Player can view, manage, and understand their inventory easily.

### 🔲 F-072: Pause Menu | Size: S
- **Stories:**
  - [ ] Escape key opens pause menu
  - [ ] Options: Resume, Save, Settings, Quit
  - [ ] Settings sub-menu: volume, camera sensitivity, controls reference
  - [ ] Pauses game time (not just UI overlay)
  - [ ] Catalog / quest log accessible from pause
- **Acceptance:** Player can pause, save, adjust settings, and quit cleanly.

### 🔲 F-073: Main Menu | Size: S
- **Stories:**
  - [ ] Title screen with game logo
  - [ ] New Game / Continue / Settings / Quit
  - [ ] Continue loads most recent save
  - [ ] New Game initializes fresh state
  - [ ] Settings: audio, controls
- **Acceptance:** Clean entry point. New/Continue work correctly with save system.

### 🔲 F-074: Dialog / Interaction UI | Size: S
- **Stories:**
  - [ ] Simple text box for object interactions ("This is your shipping bin")
  - [ ] Shop UI: item grid with buy/sell prices
  - [ ] Confirmation dialogs ("Sell 5 tomatoes for 50g?")
  - [ ] Tutorial text boxes (first-time hints)
- **Acceptance:** Player receives clear feedback from all interactions.

---

## P3 — Audio & Juice

### 🔲 F-080: Sound Effects | Size: M
- **Stories:**
  - [ ] Farming SFX: till, water, plant, harvest, crop milestone pops
  - [ ] Animal SFX: per-type ambient sounds, catch, flee, pen ambient
  - [ ] UI SFX: menu open/close, buy, sell, notification ding
  - [ ] Player SFX: footsteps (surface-dependent), tool swing
  - [ ] AudioManager: pool-based, prevents overlapping, distance falloff
- **Acceptance:** Every player action has audio feedback. World feels alive.

### 🔲 F-081: Background Music | Size: S
- **Stories:**
  - [ ] Ambient music track per time of day (morning calm, afternoon upbeat, night gentle)
  - [ ] Crossfade between tracks on time period change
  - [ ] Volume control in settings
  - [ ] Music dips during important events (harvest fanfare)
- **Acceptance:** Cozy background music plays continuously, shifts with time of day.

### 🔲 F-082: Visual Effects (Particles) | Size: M
- **Stories:**
  - [ ] Watering: water splash particles
  - [ ] Harvesting: crop pop + sparkle
  - [ ] Planting: dirt puff
  - [ ] Animal catch: net swoosh + star burst
  - [ ] Gold earned: floating "+50g" text
  - [ ] Growth milestone: small sparkle on crop
  - [ ] Day transition: screen fade or wipe
- **Acceptance:** Core actions have satisfying visual feedback. Game feels responsive.

### 🔲 F-083: Camera Polish | Size: S
- **Stories:**
  - [ ] Smooth follow with configurable damping
  - [ ] Slight zoom when entering buildings/menus
  - [ ] Screen shake on big events (rare animal catch, upgrade)
  - [ ] Camera bounds: prevent seeing beyond farm edges
  - [ ] Adjustable zoom level (scroll wheel or pinch)
- **Acceptance:** Camera feels smooth and intentional, never jarring.

---

## P3 — Tutorials & Onboarding

### 🔲 F-090: First-Time Tutorial | Size: M
- **Stories:**
  - [ ] Triggered on New Game
  - [ ] Step 1: Movement (WASD)
  - [ ] Step 2: Till soil (equip hoe, use on plot)
  - [ ] Step 3: Plant seed (select seed, use on tilled plot)
  - [ ] Step 4: Water crop (equip can, use on planted plot)
  - [ ] Step 5: Wait + harvest (time skip or fast-forward for tutorial)
  - [ ] Step 6: Sell at shipping bin
  - [ ] Step 7: Buy new seeds at shop
  - [ ] Step 8: Catch an animal (bonus)
  - [ ] Each step has highlight/arrow pointing at target
  - [ ] Tutorial flag in save data (don't repeat)
- **Acceptance:** New player understands all core mechanics within first 5 minutes. Tutorial never replays.

### 🔲 F-091: Contextual Hints | Size: S
- **Stories:**
  - [ ] "Tip" popups triggered by situation (first time near water source, first rain day)
  - [ ] Hint database: keyed by trigger condition
  - [ ] Each hint shown once per save file
  - [ ] Dismissible with any key
  - [ ] Option to disable hints in settings
- **Acceptance:** Player learns advanced mechanics organically through play.

---

## Summary: Build Order Recommendation

```
Phase 1 (Weeks 1-4): Core Playable Loop
├── F-004  Scene Unification
├── F-005  Game State Manager
├── F-012  Soil System
├── F-013  Seed & Planting
├── F-014  Watering
├── F-015  Harvesting
├── F-030  Inventory System
├── F-031  Tool System
└── F-033  Currency & Wallet

Phase 2 (Weeks 5-8): Economy & Time
├── F-032  Shop / Selling
├── F-040  Day/Night Cycle
├── F-041  Seasonal System
├── F-042  Weather System
├── F-016  Crop Visuals
├── F-024  Animal Products
└── F-070  Basic HUD (replace OnGUI)

Phase 3 (Weeks 9-12): Depth
├── F-006  Save/Load System
├── F-050  Farm Upgrades
├── F-053  Crafting
├── F-054  Fertilizer
├── F-025  Animal Feeding
├── F-056  Quest Board
├── F-071  Inventory UI
└── F-072  Pause Menu

Phase 4 (Weeks 13-16): Polish & Content
├── F-055  Fishing Mini-Game
├── F-057  Tool Upgrades
├── F-058  Rare Animals
├── F-060  Farm Zones
├── F-061  Foraging
├── F-051  Crop Encyclopedia
├── F-052  Animal Encyclopedia
├── F-073  Main Menu
├── F-074  Dialog UI
└── F-090  Tutorial

Phase 5 (Weeks 17-20): Juice
├── F-080  Sound Effects
├── F-081  Background Music
├── F-082  Visual Effects
├── F-083  Camera Polish
└── F-091  Contextual Hints
```

---

## Feature Count

| Priority | Count | Status |
|----------|-------|--------|
| P0 Foundation | 6 | 3 done, 3 remaining |
| P1 Core Loop | 14 | 5 done, 9 remaining |
| P2 Depth | 11 | 0 done |
| P3 Polish | 10 | 0 done (1 partial) |
| **Total** | **41** | **8 done, 33 remaining** |

---

## Dependencies Graph (Critical Path)

```
F-004 (Scene Unification)
  └─► F-005 (Game State Manager)
       └─► F-006 (Save/Load)

F-012 (Soil) ─► F-013 (Planting) ─► F-014 (Watering) ─► F-015 (Harvesting)
                     │                                         │
                     └─► F-030 (Inventory) ◄─────────────────┘
                              │
                              ├─► F-031 (Tools)
                              ├─► F-032 (Shop) ─► F-033 (Currency)
                              └─► F-053 (Crafting)

F-040 (Day/Night) ─► F-041 (Seasons) ─► F-042 (Weather)

F-024 (Animal Products) ─► F-025 (Feeding)

F-070 (HUD) ─► F-071 (Inventory UI) ─► F-072 (Pause Menu) ─► F-073 (Main Menu)
```
