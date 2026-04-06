# fARm

**An AR farming sim inspired by Stardew Valley and Animal Crossing — built in Unity 6 for Meta Quest.**

---

## Quick Start

```bash
# 1. Clone (requires Git LFS)
brew install git-lfs    # if not installed
git lfs install
git clone https://github.com/fARmsquad/farm.git
cd farm

# 2. Open in Unity
# Unity Hub > Open > select this folder
# Required: Unity 6000.4.1f1

# 3. Play
# Open Assets/_Project/Scenes/FarmMain.unity
# Hit Play — WASD + mouse to fly around the greyboxed farm
```

---

## Project Structure

```
farm/
├── README.md               <-- you are here
├── SPECS.md                <-- spec index, status tracker, workflow guide
├── CONTRIBUTING.md         <-- how-to guides for devs
│
├── Assets/
│   ├── _Project/           <-- all our code and content
│   │   ├── Scripts/
│   │   │   ├── Core/       <-- pure C# logic (no Unity dependencies where possible)
│   │   │   ├── MonoBehaviours/  <-- Unity components that go on GameObjects
│   │   │   ├── Interfaces/      <-- shared interfaces between assemblies
│   │   │   └── ScriptableObjects/  <-- data containers (crop types, etc.)
│   │   │
│   │   ├── Editor/         <-- editor-only tools (scene builders, MCP tools)
│   │   ├── Scenes/         <-- Unity scenes (FarmMain.unity)
│   │   ├── Materials/      <-- materials and shaders
│   │   ├── Prefabs/        <-- reusable prefab assets
│   │   └── Art/            <-- models, textures, audio, animations
│   │       ├── Animals/    <-- animal GLB models
│   │       ├── Models/     <-- environment/prop models
│   │       ├── Textures/
│   │       └── Audio/
│   │
│   ├── Specs/              <-- feature specifications
│   │   ├── Features/       <-- all spec files live here (L1-001, L2-002, etc.)
│   │   ├── Assignments/    <-- per-person folders (Jack/, Olivia/, Youssef/)
│   │   ├── AcceptanceCriteria/  <-- (future) detailed AC docs
│   │   └── ArchitectureDecisions/  <-- (future) ADRs
│   │
│   └── Tests/
│       ├── EditMode/       <-- unit tests (no Play mode needed)
│       └── PlayMode/       <-- integration tests (runs in Play mode)
│
├── Packages/               <-- Unity package dependencies (don't edit manually)
├── ProjectSettings/        <-- Unity engine settings (auto-managed)
└── .gitattributes          <-- Git LFS rules for binary files
```

**Rule of thumb:** All our work goes in `Assets/_Project/`. Everything else is Unity infrastructure.

---

## Concept

fARm blends the cozy life-sim loop of Stardew Valley and Animal Crossing with augmented reality. Your backyard becomes your field. Your kitchen table becomes your crafting bench. Seasons in-game mirror real-world seasons and time of day.

### Core Pillars

| Pillar | Description |
|---|---|
| **Real-world Anchoring** | Farms are planted in physical spaces using AR anchors |
| **Persistent World** | The farm persists and evolves even when the app is closed |
| **Social & Shared** | Friends can visit your AR farm in real space or remotely |
| **Cozy Progression** | No fail states. Slow, rewarding loops |

### Gameplay Loop

```
Wake up > Check farm (AR or map view) > Water/harvest crops
> Craft goods > Visit town / neighbor farms > Talk to villagers
> Complete requests / seasonal events > Sleep (end day)
```

---

## Tech Stack

| Component | Technology |
|---|---|
| Engine | Unity 6 (6000.4.1f1) |
| Rendering | Universal Render Pipeline (URP) |
| Language | C# |
| Target | Meta Quest (AR/MR passthrough) |
| Version Control | Git + Git LFS for binaries |
| Testing | Unity Test Framework (EditMode + PlayMode) |
| 3D Assets | glTF/GLB via glTFast |

---

## Team

| Name | Role |
|---|---|
| **Youssef** | Project lead, systems architecture |
| **Jack** | Gameplay systems, farming mechanics |
| **Olivia** | Environment, art pipeline, UX |

Each team member has an assignment folder at `Assets/Specs/Assignments/<Name>/`.

---

## Key Docs

| Doc | What it covers |
|---|---|
| [SPECS.md](SPECS.md) | All feature specs, their status, who's working on what, and how to claim/build/merge specs |
| [CONTRIBUTING.md](CONTRIBUTING.md) | How-to guides: branching, testing, code organization, scene builder, Git LFS |
| [Assets/Specs/Features/](Assets/Specs/Features/) | The actual spec files — read these before building anything |

---

## Platform Targets

| Target | Notes |
|---|---|
| **Meta Quest** (primary) | Full AR/MR via passthrough, spatial anchors, hand tracking |
| **Web** (secondary) | Farm management, market, social features without a headset |

---

## Inspiration

| Game | What we borrow | What we do differently |
|---|---|---|
| Stardew Valley | Deep farming loop, relationships, crafting | Real-time growth, AR-first |
| Animal Crossing | Cozy pace, villager charm, seasonal events | Active farming, no platform lock-in |
| Minecraft Earth | AR building in real world | Cozy tone, social NPCs, farming focus |
