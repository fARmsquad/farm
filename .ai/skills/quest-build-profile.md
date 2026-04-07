# Skill: Quest Build & Profile

## Purpose
Build for Quest and profile performance using MCP tools.
Use for performance workflow or pre-release verification.

## Build for Quest
1. manage_build → set build target Android
2. manage_build → set texture compression ASTC
3. manage_build → set build settings:
   - Minimum API level: 29
   - Target architecture: ARM64
   - Scripting backend: IL2CPP
   - Single Pass Instanced rendering
4. manage_build → trigger build
5. read_console → check for build errors

## Profile in Editor (Quick Check)
For fast performance assessment without building:

1. manage_profiler → start profiling (CPU, GPU, Memory)
2. Enter play mode via manage_editor
3. Wait 5-10 seconds of representative gameplay
4. manage_profiler → stop
5. manage_profiler → read frame timing
   - Target: <14ms per frame (72fps)
   - WARN if any frame >14ms
   - FAIL if average >14ms
6. manage_profiler → read memory counters
   - Total texture memory < 200MB
   - Total mesh memory < 50MB
   - GC allocations per frame < 1KB (ideally zero)
7. manage_profiler → take memory snapshot for comparison

## Performance Regression Check
Before merging a perf-sensitive feature:
1. Profile on main branch → save baseline
2. Profile after changes → save comparison
3. manage_profiler → compare snapshots
4. FAIL if frame time regressed >2ms
5. WARN if memory increased >10MB
