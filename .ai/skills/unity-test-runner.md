# Skill: Unity Test Runner

## Purpose
Execute Unity tests via MCP (preferred) or CLI (fallback) and parse results.

## Primary: MCP run_tests (when editor is open)
Use the MCP `run_tests` tool for all test execution when the editor is available.
Benefits:
- No batchmode boot (~10-30s saved per run)
- Non-blocking (editor stays usable)
- Real-time results via get_test_job
- Console errors visible via read_console

### Execution Flow
1. Check editor_state — not in play mode, not compiling
2. refresh_unity — ensure latest scripts compiled
3. read_console — verify no compilation errors
4. run_tests with platform=EditMode (or PlayMode)
5. get_test_job to poll for completion
6. Parse results
7. read_console — check for runtime errors during tests

### Filtered Runs (fast iteration)
run_tests with testFilter="CropGrowthCalculatorTests" for single-class runs
during tight TDD loops.

## Fallback: CLI batchmode (when editor is closed)
Use ./run-tests.sh only when:
- Unity editor is not open
- MCP connection fails
- CI/CD pipeline (no editor available)
- Codex agent (no MCP access)

### CLI Commands
```bash
# EditMode tests only (fast)
./run-tests.sh editmode

# PlayMode tests only (slow, needs display)
./run-tests.sh playmode

# All tests
./run-tests.sh all
```

The harness auto-detects: try MCP first, fall back to CLI if unavailable.

## Output Parsing
CLI tests produce NUnit XML results in TestResults/.
Parse with: `.ai/scripts/parse-test-results.py TestResults/*.xml`

## Result Interpretation
- PASS: all tests green, proceed
- FAIL: specific tests failed, read XML for details
- ERROR: test runner itself failed (build error, missing assembly)
- TIMEOUT: test took too long (usually PlayMode + XR issue)

## Troubleshooting
- "Assembly not found": check .asmdef references
- "No tests found": check test assembly includes correct folders
- "Compilation error": fix C# errors before running tests
- EditMode tests in Core/ should never touch UnityEngine
