# Feature Spec: Standing Slice Regeneration Orchestrator — GSO-009c

## Summary
The standing `Generative Story Slice` already has the core pieces needed to
refresh individual beats:
- generated minigame materialization
- generated storyboard cutscene assembly
- one-call minigame + cutscene package assembly

What is still missing is the operator-friendly refresh path for the whole
standing slice. Right now the sample package has to be rebuilt in separate
calls, which is slow to test and leaves the package half-updated if a later
generation leg fails.

This slice adds one orchestration layer above `GeneratedPackageAssemblyService`
that refreshes the current standing package path in one request:
1. post-chicken bridge -> plant rows intro
2. find-tools intro -> pre-farm bridge

If the second leg fails validation or assembly, the service restores the
original package manifest so the standing slice never gets stranded in a
partially rewritten state.

## User Story
As a developer, I want one backend call that refreshes the standing
`Generative Story Slice` package end-to-end, so I can iterate on the current
title-screen slice without manually sequencing multiple assembly calls or
repairing half-written package JSON when one leg fails.

## Acceptance Criteria

### Request Contract
- [ ] A typed request model exists for standing-slice regeneration.
- [ ] The request owns package metadata and two assembly legs:
      - `post_chicken_to_farm`
      - `find_tools_to_pre_farm`
- [ ] Each leg contains one minigame input and one cutscene input using the
      existing package-assembly contract shapes.

### Orchestration Behavior
- [ ] The standing-slice service snapshots the current package manifest before
      mutation when the package file already exists.
- [ ] The service runs `post_chicken_to_farm` first through the existing
      `GeneratedPackageAssemblyService`.
- [ ] The service runs `find_tools_to_pre_farm` second through the existing
      `GeneratedPackageAssemblyService`.
- [ ] If the first leg fails, the second leg is not attempted.
- [ ] If the second leg fails, the original package manifest is restored.
- [ ] The response reports whether rollback happened.

### Standing Package Outcome
- [ ] A successful regeneration returns the updated standing package payload.
- [ ] The updated package still targets the standing title-screen slice backed
      by `StoryPackage_IntroChickenSample`.
- [ ] A failed second leg does not leave the package manifest partially
      updated.

### Integration Proof
- [ ] One backend test proves both assembly legs can be refreshed in one call.
- [ ] One backend test proves a second-leg validation failure restores the
      original manifest bytes.
- [ ] One API test proves the new regeneration endpoint returns the typed
      composed result.

## Non-Negotiable Rules
- Do not duplicate minigame or storyboard generation logic inside the new
  orchestration layer.
- Do not introduce a new sample slice; keep refreshing the standing
  `Generative Story Slice`.
- Do not leave the package manifest half-mutated after a second-leg failure.

## Out of Scope
- Asset rollback or cleanup for already-written generated images/audio
- Background jobs or queue-based execution
- Operator review UI
- Full episode generation beyond the standing intro sample path

## Done Definition
- [ ] Spec exists.
- [ ] One request can refresh the standing generated slice package.
- [ ] Failed later-leg assembly restores the prior package manifest.
- [ ] Focused backend tests cover success, rollback, and endpoint shape.
