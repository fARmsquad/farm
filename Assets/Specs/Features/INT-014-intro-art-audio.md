# Feature Spec: Intro Art & Audio Polish — INT-014

## Summary
The final polish pass for the intro cinematic. Replaces all primitive placeholders with real models, adds final audio (music, SFX, voice lines), and tunes timing/volumes. This spec runs AFTER INT-008 (scene assembly) so the full sequence is playable with greybox first, then polished with real assets.

## User Story
As a player, I want the intro to look and sound polished — real models, atmospheric audio, comedic voice lines — so that the experience feels like a finished game, not a prototype.

## Prerequisite
**INT-008 must be Done first.** The entire intro must be playable with placeholders before this spec begins. This ensures the sequence, timing, and gameplay all work before we invest in asset sourcing.

---

## Phase A: Replace Placeholder Models

### Existing Synty assets to wire in (no sourcing needed)

| Placeholder | Replace with | Synty path |
|------------|-------------|------------|
| Bedroom bed (cube) | Farm bed | `Synty/PolygonFarm/Prefabs/Props/SM_Prop_Bed_01.prefab` |
| Bedroom chair (cube) | Rocking chair | `Synty/PolygonFarm/Prefabs/Props/SM_Prop_Chair_Rocking_01.prefab` |
| Bedroom table (cube) | Farm table | `Synty/PolygonFarm/Prefabs/Props/SM_Prop_Table_01.prefab` |
| El Pollo stump (cylinder) | Tree stump | `Synty/PolygonFarm/Prefabs/Environments/SM_Env_Tree_Stump_01.prefab` |
| Pen gate (placeholder) | Wood gate | `Synty/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Wood_Gate_01.prefab` |
| Roof lights (none) | Roof light | `Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Light_Roof_01.prefab` |
| El Pollo Loco | Existing chicken.glb (scaled 1.5x) | `_Project/Art/Models/Source/chicken.glb` |
| Baby chicks | Existing chicken.glb (scaled 0.3x, yellow tint) | `_Project/Art/Models/Source/chicken.glb` |

### Assets to source (USER action)

| Placeholder | Needed model | Suggested path | Notes |
|------------|-------------|---------------|-------|
| Chop (brown cube) | Dog model (.glb) | `_Project/Art/Models/Source/dog.glb` | Sitting/barking poses. Low-poly farm style. |
| Lantern (cylinder+sphere) | Lantern model (.glb) | `_Project/Art/Models/Source/lantern.glb` | Handheld, warm glass. |
| Boot (brown cube) | Boot/shoe model (.glb) | `_Project/Art/Models/Source/boot.glb` | Small, throwable. |
| Cat silhouette (capsule) | Cat model (.glb) | `_Project/Art/Models/Source/cat.glb` | Running pose, small. Optional — capsule works as silhouette. |
| Player character (capsule) | Farmgirl model | `_Project/Art/Models/Source/farmgirl.glb` | Already exists — wire into intro if not already. |
| Moonlight cookie (none) | Light cookie texture | `_Project/Art/Textures/moonlight_cookie.png` | 256×256 soft rectangle gradient. Can be generated. |

---

## Phase B: Audio Assets

### Music tracks (USER to source or create)

| Key | File path | Description | Duration |
|-----|----------|-------------|----------|
| `intro_guitar` | `Sounds/Music/intro_guitar.mp3` | Gentle acoustic fingerpicking. Panels 1-2. | ~90s loop |
| `intro_tension` | `Sounds/Music/intro_tension.mp3` | Minor key acoustic + banjo/harmonica. Panel 4. | ~20s loop |
| `kazoo_standoff` | `Sounds/Music/kazoo_standoff.mp3` | Western standoff whistle on kazoo. Panel 5 comedy beat. | 5-8s |

### Ambient loops (USER to source)

| Key | File path | Description |
|-----|----------|-------------|
| `crickets_loop` | `Sounds/Ambience/crickets_loop.wav` | Night crickets, 10-15s loop |
| `wind_gentle_loop` | `Sounds/Ambience/wind_gentle_loop.wav` | Subtle wind, 10-15s loop |
| `creek_water` | `Sounds/Ambience/creek_water.wav` | Gentle flowing water, 10s loop |
| `morning_birds` | `Sounds/Ambience/morning_birds.wav` | Dawn birdsong, 10s loop |

### SFX one-shots (USER to source)

| Key | File path | Description | Special notes |
|-----|----------|-------------|---------------|
| `rooster_crow_epic` | `Sounds/SFX/rooster_crow_epic.wav` | **Signature sound.** | Pitch down 15%, bass +6dB below 200Hz, 0.3s reverb tail |
| `rooster_clucks` | `Sounds/SFX/rooster_clucks_confident.wav` | Low assertive clucks | 3-4s |
| `guitar_snap` | `Sounds/SFX/guitar_snap.wav` | Single harsh string break | 0.5s |
| `cat_meow` | `Sounds/SFX/cat_meow.wav` | Single meow, panned left | 1s |
| `dogs_barking` | `Sounds/SFX/dogs_barking.wav` | 2-3 dogs staggered | 3-4s |
| `baby_cry_distant` | `Sounds/SFX/baby_cry_distant.wav` | Very faint background | 2s |
| `footsteps_gravel` | `Sounds/SFX/footsteps_gravel.wav` | 4-step walk cycle loop | sync to walk |
| `lantern_creak` | `Sounds/SFX/lantern_creak.wav` | Metal creak on swing | 0.5s |
| `gate_creak` | `Sounds/SFX/gate_creak.wav` | Wooden gate opening | 1.5s |
| `owl_hoot` | `Sounds/SFX/owl_hoot.wav` | Distant single hoot | 2-3s |
| `mission_complete` | `Sounds/SFX/mission_complete.wav` | Short victorious sting | 2-3s |

### Voice lines (USER to source — placeholder TTS acceptable)

| Key | File path | Line | Character |
|-----|----------|------|-----------|
| `vo_not_again` | `Sounds/VO/townsperson_not_again.wav` | "NOT AGAIN!" (muffled) | Townsperson |
| `vo_big_smoke` | `Sounds/VO/big_smoke_line.wav` | "ALL YOU HAD TO DO WAS STAY QUIET, YOU FEATHERED FOOL!" (muffled) | Big Smoke |
| `vo_niko` | `Sounds/VO/niko_line.wav` | "Life is complicated. But this bird... this bird is simple." (muffled) | Niko |
| `vo_tenpenny` | `Sounds/VO/tenpenny_street.wav` | Full Tenpenny monologue (see script Panel 4) | Tenpenny |

### Font

| Asset | Path | Notes |
|-------|------|-------|
| Comic burst font | `Fonts/ComicBurst.ttf` | Bold display font. [Bangers](https://fonts.google.com/specimen/Bangers) or similar. |

---

## Phase C: Tuning Pass

After assets are placed:
- [ ] Adjust all audio volumes relative to each other (VO > SFX > Music)
- [ ] Fine-tune Panel transition timing with real audio cues
- [ ] Verify rooster crow screen shake feels impactful with actual SFX
- [ ] Tune lantern light intensity/range with real model
- [ ] Check baby chick scale against real pen proportions
- [ ] Verify Chop speech bubble positioning with real dog model
- [ ] Full 90-second playthrough with all real assets — adjust comedy timing

---

## Directory Structure

```
Assets/_Project/
├── Sounds/
│   ├── Music/        (intro_guitar, intro_tension, kazoo_standoff)
│   ├── Ambience/     (crickets, wind, creek, morning_birds)
│   ├── SFX/          (crow, snap, meow, barking, footsteps, creak, gate, owl, mission_complete)
│   └── VO/           (not_again, big_smoke, niko, tenpenny)
├── Art/
│   ├── Models/Source/ (dog.glb, lantern.glb, boot.glb, cat.glb — if sourced)
│   └── Textures/     (moonlight_cookie.png)
└── Fonts/            (ComicBurst.ttf)
```

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | asset | Swap bedroom placeholders with Synty bed/chair/table | INT-008 | Real furniture models in bedroom scene |
| 2 | asset | Swap stump placeholder with SM_Env_Tree_Stump_01 | INT-008 | Real stump in pen |
| 3 | asset | Scale chicken.glb to 1.5x for El Pollo Loco | INT-008 | Larger rooster in scene |
| 4 | asset | Scale chicken.glb to 0.3x + yellow tint for baby chicks | INT-008 | Small chicks in pen |
| 5 | **USER** | Source/create music tracks (guitar, tension, kazoo) | — | Files at correct paths |
| 6 | **USER** | Source/create ambient loops (crickets, wind, creek, birds) | — | Files at correct paths |
| 7 | **USER** | Source/create SFX one-shots (see table above) | — | Files at correct paths, crow meets spec |
| 8 | **USER** | Source/create voice lines (TTS placeholder OK) | — | Files at correct paths |
| 9 | **USER** | Source comic burst font | — | TTF at Fonts/ComicBurst.ttf |
| 10 | **USER** | Source/create models: dog, lantern, boot (cat optional) | — | GLB files at correct paths |
| 11 | asset | Update AudioLibrary asset with all new clip keys | 5-8, INT-002 | All keys mapped, no nulls |
| 12 | asset | Wire new models into scene, replace remaining placeholders | 1-4, 10 | Zero primitive placeholders remain |
| 13 | tuning | Volume balancing, timing adjustments, full playtest | 11, 12 | 90-second playthrough feels polished |
