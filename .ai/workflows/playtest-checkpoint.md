# Workflow: VR Playtest Checkpoint

## Purpose
This is the ONLY hard human gate in the pipeline. Code is done, tests pass,
PR is merged. Now the developer puts on the headset and checks that it
FEELS right. Automated tests verify correctness. Playtesting verifies experience.

## Playtest Guide Template

```
## Playtest Guide: [Feature Name]

### Setup
- Scene: Assets/_Project/Scenes/[scene].unity
- Build target: [Quest Link / Quest Build / Editor Play]
- Prerequisites: [any setup needed, e.g. "plant a tomato first"]

### Core Test Sequence
For each interaction in this feature:

#### Test 1: [Interaction Name]
- **Do this**: [exact physical action — "reach down toward the soil plot"]
- **You should see**: [visual result — "seed appears in the dirt, particle burst"]
- **You should feel**: [haptic result — "short buzz in right controller"]
- **You should hear**: [audio result — "soft planting sound"]
- **Timing**: [should it feel instant? delayed? how long?]

#### Test 2: [Next Interaction]
...

### Edge Cases to Try
- [ ] What happens if you do [action] with both hands?
- [ ] What happens if you do [action] from far away?
- [ ] What happens if you do [action] while another action is in progress?
- [ ] Walk away and come back — does the state persist?
- [ ] Look away and look back — do visuals pop in or load smoothly?

### Performance Feel
- [ ] Does it feel smooth (72fps)? Any judder or frame drops?
- [ ] Any visual artifacts when moving your head quickly?
- [ ] Does the interaction respond instantly or feel laggy?

### Comfort Check
- [ ] Any motion discomfort during this feature?
- [ ] Are interactive objects at a comfortable height/distance?
- [ ] Can you reach everything without straining?

### Your Verdict
After testing, tell me what you think:
- What felt good?
- What felt off? (be specific — "the grab felt floaty" is more useful than "bad")
- Any ideas that came to mind while testing?
- Anything you want to try that wasn't in this guide?
```
