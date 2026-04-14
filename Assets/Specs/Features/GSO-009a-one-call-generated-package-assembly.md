# Feature Spec: One-Call Generated Package Assembly — GSO-009a

## Summary
This slice removes the current two-call operator burden from the generated
intro path.

Right now, the backend can:
- materialize a generated minigame beat
- generate a linked storyboard cutscene

But it still requires two manually coordinated API calls against the same story
package. That is fragile and easy to drift.

This slice adds one orchestration request that:
1. materializes the selected generated minigame beat
2. links the cutscene request to that beat automatically
3. writes both beats into the same package JSON
4. returns one combined result

The first use case stays narrow: the standing generated intro path that links a
bridge cutscene to a generated minigame beat.

## User Story
As a developer, I want one backend request to assemble the generated minigame
beat and its bridge cutscene together, so the standing package slice is easier
to regenerate without manual sequencing mistakes.

## Acceptance Criteria

### Request Contract
- [ ] A nested package-assembly request model exists.
- [ ] The request contains package metadata, one minigame request, and one
      cutscene request.
- [ ] The cutscene request does not need a manually supplied
      `linked_minigame_beat_id`; the assembly layer injects it.

### Service Behavior
- [ ] The assembly service materializes the minigame beat first.
- [ ] If minigame validation fails, the service returns the structured invalid
      result and does not attempt storyboard generation.
- [ ] If minigame generation succeeds, the service generates the cutscene
      second against the same package file.
- [ ] The combined result returns the final Unity package plus the nested
      minigame result.

### API Surface
- [ ] A new backend endpoint exists for one-call generated package assembly.
- [ ] The endpoint exposes a typed response model, not an unstructured merged
      dictionary.

### Integration Proof
- [ ] One backend test proves a single request can write both:
      - a generated minigame beat
      - a linked generated cutscene beat
- [ ] One backend test proves invalid minigame input short-circuits before
      storyboard generation.

## Non-Negotiable Rules
- Keep the current minigame and storyboard services single-purpose.
- The assembly layer orchestrates; it does not re-implement generator
  validation or storyboard planning logic.
- The minigame beat must be written before the storyboard call so linked
  context resolution remains deterministic.

## Out of Scope
- Job submission and polling workflows
- Multi-beat episode planning
- New Unity runtime behavior
- Live operator review UI
- Generic planner-generated multi-scene package assembly

## Done Definition
- [ ] Spec exists.
- [ ] One API call can assemble a linked minigame beat plus cutscene beat.
- [ ] Invalid minigame input fails cleanly without a partial storyboard write.
- [ ] Backend tests prove both the success and short-circuit paths.
