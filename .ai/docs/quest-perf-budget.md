# Quest Performance Budget

## Hard Constraints
| Metric | Quest 2 | Quest 3 |
|--------|---------|---------|
| Frame rate | 72fps (13.9ms) | 72-90fps |
| Draw calls | <100/frame | <150/frame |
| Triangles | <750K visible | <1M visible |
| Texture memory | <200MB | <300MB |
| Total RAM | <2GB | <3GB |
| Thermal | 30min session | 30min session |

## Shader Limits
- Mobile URP shaders only
- Max 4 texture samples per shader
- No real-time shadows on Quest 2 (baked only)
- Single pass instanced rendering

## Physics
- Max 50 active rigidbodies
- Simplified colliders (box, sphere — avoid mesh colliders)
- Fixed timestep: 0.02s (50Hz)

## Audio
- Max 16 simultaneous audio sources
- Spatial audio for world objects
- Compressed formats (Vorbis for music, ADPCM for SFX)
