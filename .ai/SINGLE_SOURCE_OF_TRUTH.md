# Single Source of Truth — FarmSim VR

Last updated: 2026-04-15

## Current State

- **Current story**: none
- **Story phase**: idle
- **Recent fix**: The generated-story runtime is now wired for an always-on Railway backend at `https://story-orchestrator-production.up.railway.app`. Unity request execution stays pinned to the readiness-resolved backend instead of re-probing dead localhost fallbacks, remote hosts are no longer treated as candidates for local bootstrap, the deployed service now runs with a persistent `/data` volume plus Railway-managed provider variables, provider request budgets are capped at 30 seconds, the quality gate accepts clean deterministic `local-reference-remix` fallback images while still rejecting placeholder assets, and the standalone runtime `POST /api/runtime/v1/sessions` contract now returns a queued job immediately instead of holding the HTTP request open through first-turn generation. Background workers now own turn execution, recovery replay no longer shares the same worker lane as fresh live jobs, and the current runtime playthrough is constrained to the real `FarmMain` planting game with varied farm parameters across the bounded 3-turn run. Sidecar note: `mccluckin-gtm` X publishing now prefers OAuth 2.0 PKCE user tokens, persists rotated access/refresh tokens for unattended runs, and the current access token was verified live against `GET /2/users/me`; `X_CLIENT_ID` is still required in the env before refresh can run automatically after token expiry.
- **Scene work map**: `.ai/docs/scene-work-map.md`
- **Tests**: EditMode 421 passed / 84 failed / 1 skipped in the current repo-wide baseline (still red outside this slice). Backend Python: `./.venv/bin/python -m unittest tests.test_runtime_api` passed in `backend/story-orchestrator`. Live backend verification: public Railway `/health` returned `200`, live `POST /api/runtime/v1/sessions` now returns immediate `201` with `status: queued`, and the service logs show background turn generation running after the response. Full live Unity-to-Railway turn completion is still constrained by provider-side issues currently visible in Railway logs (`Gemini` image `429` throttling and `ElevenLabs` `401` fallback to OpenAI speech). PlayMode not run for this slice.
- **Main status**: green
- **Last push**: none yet
- **Tech debt items**: 0 (see project-memory.md)

## Architecture Snapshot
- Core/ systems: (none yet)
- MonoBehaviour/ controllers: (none yet)
- Interfaces/: (none yet)
- ScriptableObjects/: (none yet)
- Active third-party packages: (default Unity 6 packages)

## Open Questions
- XR Interaction Toolkit version to target
- Whether to use Unity Input System or XR-specific input

## Next Up
- Bootstrap feature: Crop Growth Calculator (validates harness)
- Core farming systems: CropData, GrowthConditions, GrowthResult
