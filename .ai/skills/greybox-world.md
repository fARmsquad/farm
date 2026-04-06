# Skill: Greybox World Builder

## Purpose
Build greybox (placeholder geometry) farm environments using
ProBuilder via MCP. Use for rapid spatial prototyping before
importing final art assets.

## Farm Layout Template
Use manage_probuilder to create:

### Ground Plane
- Flat quad, 20x20 meters
- Material: solid green (grass placeholder)
- Position: origin (0, 0, 0)

### Crop Plots
- Raised rectangles, 1m x 1m x 0.1m (slight mound)
- Material: brown (soil placeholder)
- Grid layout with 0.5m spacing
- Each plot gets a BoxCollider (trigger) for interaction zone

### Structures
- Barn: box 6x4x3m, open front face
- Tool rack: box 2x0.2x1.5m, near barn entrance
- Well: cylinder 1m radius x 0.8m height
- Fence: series of thin boxes along perimeter

### Paths
- Flattened boxes 1m wide connecting key areas
- Material: light brown (dirt path placeholder)

## VR Scale Validation
After greyboxing, the developer should put on the headset to verify:
- Can you comfortably reach the crop plots?
- Is the tool rack at a natural hand height?
- Does the barn feel appropriately sized?
- Are paths wide enough to walk comfortably?

## Replacing Greybox with Final Art
When real art assets are ready:
1. Note the transform (position, rotation, scale) of each greybox object
2. Delete the greybox mesh
3. Instantiate the final prefab at the same transform
4. Adjust if needed (final art may have different pivot points)
