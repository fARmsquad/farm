## Playtest Guide: F-011 Crop Plot State Machine

### Setup
- Scene: `Assets/_Project/Scenes/FarmMain.unity`
- Build target: `Editor Play`
- Prerequisites:
  - Let Unity finish recompiling scripts.
  - Make sure a `SimulationManager` exists in the scene.
  - Open the Console if you want to watch the editor-only inventory logs.

### Core Test Sequence

#### Test 1: Empty startup
- **Do this**: Enter Play mode and look at the plot grid immediately.
- **You should see**: The plot cubes exist as interactable spaces, but no crop visuals are rendered on top of them.
- **You should feel**: N/A
- **You should hear**: N/A
- **Timing**: Immediate on scene start.

#### Test 2: Planting from inventory
- **Do this**: Left-click an empty plot.
- **You should see**: A crop visual appears at a small scale and begins growing upward over time.
- **You should feel**: N/A
- **You should hear**: N/A
- **Timing**: Immediate on click.

#### Test 3: Harvesting a ready crop
- **Do this**: Wait for the crop to reach full scale / mature color, then left-click it.
- **You should see**: The crop visual disappears and the plot returns to empty.
- **You should feel**: N/A
- **You should hear**: N/A
- **Timing**: Immediate on click.

#### Test 4: Withering window
- **Do this**: Plant another plot, let it mature, then leave it alone for roughly 3 seconds after it becomes ready.
- **You should see**: The crop stays mature briefly, then changes into the withered brown state.
- **You should feel**: N/A
- **You should hear**: N/A
- **Timing**: Withers after the configured ready-tick budget is consumed.

#### Test 5: Clearing and replanting a withered crop
- **Do this**: Left-click the withered crop to clear it, then left-click the empty plot again.
- **You should see**: The withered crop disappears, then a new planted crop starts from the seedling state.
- **You should feel**: N/A
- **You should hear**: N/A
- **Timing**: Immediate on each click.

### Edge Cases to Try
- [ ] Click a plot repeatedly while it is already `Planted` or `Growing` — nothing should break or double-plant.
- [ ] Plant until seeds run out — empty plots should stop planting and the Console should log that seeds are exhausted.
- [ ] Harvest one plot while another withers — both states should stay independent.
- [ ] Leave a withered plot alone for a while — it should remain stable until cleared.

### Performance Feel
- [ ] No hitch when clicking plots.
- [ ] No visible flicker when an empty plot hides its crop visual.
- [ ] Crop updates still feel smooth while several plots grow together.

### Comfort Check
- [ ] Plot interactions are readable from the normal editor camera angle.
- [ ] Growth / ready / withered states are visually distinct at a glance.

## Handoff Checklist: F-011 Crop Plot State Machine

### Code Complete
- [ ] All tests passing (EditMode + PlayMode)
- [ ] Preflight passed
- [ ] PR merged to main
- [ ] Main branch is green

### Documentation
- [x] Spec acceptance criteria all checked
- [ ] ADR created (not needed unless this lightweight inventory bridge becomes a lasting pattern)
- [ ] SINGLE_SOURCE_OF_TRUTH.md updated

### Playtest Ready
- [x] Playtest guide generated
- [x] Scene identified and loadable
- [x] Build instructions provided

### Developer Actions
- [ ] Read playtest guide
- [ ] Perform editor/VR playtest
- [ ] Provide feedback (approve / suggest / reject)

### Current Blockers
- [ ] Unity CLI test run is still blocked while another Unity instance holds the project open.
