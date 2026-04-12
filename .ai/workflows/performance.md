# Workflow: Performance Optimization

## Autonomy Level: FULL (with Quest budget as hard constraint)

## Process
1. **Profile**: Identify the bottleneck (CPU, GPU, memory, draw calls)
   - Load global `unity-profiler` for measurement workflow and evidence capture
2. **Benchmark**: Write a performance test that captures current metrics
3. **Target**: Set specific improvement goal (e.g., "reduce draw calls from 200 to 100")
4. **Optimize**: Implement changes, run benchmark after each
   - Load global `unity-developer` for general Unity optimization guidance
   - Load global `unity-ecs-patterns` if the optimization path involves DOTS,
     Jobs, Burst, ECS, or large-entity architectural changes
5. **Verify**: Full test suite passes, benchmark meets target
6. **Document**: Record optimization in project-memory.md

## Quest Performance Budget (Hard Constraints)
- Frame rate: 72fps minimum (13.9ms frame budget)
- Draw calls: <100 per frame
- Triangles: <750K visible per frame
- Texture memory: <200MB (Quest 2), <300MB (Quest 3)
- RAM: <2GB total application memory
- Thermal: must not trigger thermal throttling in 30min session

## Common Optimizations
- Object pooling for frequently instantiated objects
- LOD groups for 3D assets
- Texture atlasing for UI
- Occlusion culling for farm plots
- Batching static geometry
- Reducing shader complexity (mobile URP shaders only)

## Skill Wiring
- `unity-profiler` is the default profiling aid for this workflow
- `unity-ecs-patterns` is the default architecture aid when optimization
  requires ECS-scale changes
- `unity-developer` is the default Unity implementation aid for non-ECS
  optimization changes
