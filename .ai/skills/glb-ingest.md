# GLB Model Ingest Pipeline

## Purpose
Ingest a .glb 3D model into the project. Handles detection, naming,
Quest optimization, material fixup, prefab assembly, and scene
placement. Triggers when developer drops a .glb file, says "add
this model", "import this", or "here's a [object name]".

## Prerequisites
- com.unity.cloud.gltfast package installed
- Unity editor open with MCP running

## Phase 1: Detect & Classify

### Find the file
```
manage_asset → search for recently added .glb files
  OR
find_in_file → search Assets/ for *.glb
  OR
execute_custom_tool → list_unprocessed_glbs
  OR
developer provides path directly
```

### Classify by context
Ask yourself: what IS this thing in the game?

| Category | Tag | Layer | Interaction? |
|----------|-----|-------|-------------|
| Crop | "Crop" | Interactable | XR Grab (harvest) |
| Tool | "Tool" | Interactable | XR Grab (use) |
| Structure | "Structure" | Default | Static, no grab |
| Prop | "Prop" | Default | Static or XR Grab |
| Terrain | "Terrain" | Ground | Static |
| Character | "Character" | Default | Animated |
| Environment | "Environment" | Default | Static |

If unclear, ask the developer: "Is this a tool the player holds,
a prop in the scene, or something else?"

### Rename & Move
```
manage_asset → rename to [Category]_[Name].glb
manage_asset → move to Assets/_Project/Art/Models/[Category]/
refresh_unity → trigger reimport at new location
```

Naming convention:
- `Tool_WateringCan.glb`
- `Crop_Tomato_Mature.glb`
- `Crop_Tomato_Seedling.glb`
- `Struct_Barn.glb`
- `Prop_Fence_Wooden.glb`
- `Env_Tree_Oak_01.glb`

## Phase 2: Inspect

After glTFast imports the .glb, inspect what we got:

```
read_console → check for import warnings/errors
execute_custom_tool → check_model_budget "[asset path]"
  → Mesh count, triangle count, material count, bone count
```

### Budget Check
| Metric | Hero Object | Background Prop | FAIL Threshold |
|--------|------------|-----------------|----------------|
| Triangles | <10,000 | <2,000 | >50,000 |
| Materials | <3 | 1 | >5 |
| Textures | <4 | <2 | >8 |
| Bones | <75 | 0 | >150 |

If over budget:
- Log warning in session-memory
- Ask developer: "This model has [X] triangles, Quest budget is [Y].
  Want me to proceed anyway, or do you have a lower-poly version?"
- If no lower-poly version available, note as tech debt for LOD creation

### Scale Check
glTFast imports at glTF spec scale (meters). Verify:
```
manage_gameobject → instantiate temporarily
manage_gameobject → read bounds/extents
```
- A watering can should be ~0.3m tall
- A barn should be ~3-5m tall
- A tomato should be ~0.08m diameter
- If wildly off: adjust import scale factor

## Phase 3: Optimize for Quest

### Model Import Settings
```
manage_asset → set import settings:
  Model:
    - Scale Factor: 1.0 (adjust if scale check failed)
    - Mesh Compression: Medium
    - Read/Write Enabled: false
    - Optimize Mesh: true
    - Generate Colliders: false (we add specific ones manually)
    - Index Format: Auto (16-bit if <65K verts, 32-bit otherwise)
    - Normals: Import (or Calculate if missing)
    - Tangents: Calculate Mikktspace

  Rig (if animated):
    - Animation Type: Generic (or Humanoid if character)
    - Optimize Game Objects: true

  Animation (if has clips):
    - Anim Compression: Optimal
    - Rotation Error: 0.5
    - Position Error: 0.5
    - Scale Error: 0.5
```

### Texture Import Settings
For each texture that glTFast extracted or referenced:
```
manage_texture → set import settings:
  - Max Size: 1024 (512 for small props, 2048 for terrain only)
  - Compression: ASTC 6x6 (Quest optimal)
  - Generate Mipmaps: true (for 3D objects)
  - sRGB: true (for albedo/diffuse), false (for normal maps)
  - Filter Mode: Bilinear
  - Aniso Level: 1 (save GPU bandwidth)

  For Normal Maps specifically:
  - Texture Type: Normal Map
  - sRGB: false (critical — wrong setting = visual artifacts)
```

Rename extracted textures:
```
TX_[ObjectName]_Albedo_[Size].png
TX_[ObjectName]_Normal_[Size].png
TX_[ObjectName]_MetallicSmoothness_[Size].png
TX_[ObjectName]_Occlusion_[Size].png
TX_[ObjectName]_Emission_[Size].png
```

Move to `Assets/_Project/Art/Textures/[Category]/`

## Phase 4: Material Fixup

glTFast creates materials using its own glTF shaders. For Quest VR,
we may want URP Lit for consistency and performance.

### Decision: Keep glTFast shaders or remap?
- **Keep glTFast shaders** if: the model looks correct in editor, performance
  is fine, and you don't need custom shader features. glTFast shaders are
  PBR-compliant and work on Quest.
- **Remap to URP Lit** if: you need consistency with hand-made materials,
  custom shader features (wind, wetness), or the glTFast shader has issues
  on Quest.

### If remapping to URP Lit:
```
For each material on the model:
  manage_material → create new URP Lit material
  manage_material → copy texture assignments:
    - Base Map ← albedo texture
    - Normal Map ← normal texture (ensure "Normal Map" type)
    - Metallic Map ← metallic texture (if PBR metal/rough workflow)
    - Smoothness source ← Alpha channel of metallic map
    - Occlusion Map ← AO texture
    - Emission ← emission texture (if any)
  manage_material → set properties:
    - Surface Type: Opaque (unless translucent)
    - Render Face: Front (single-sided saves GPU)
    - Alpha Clipping: OFF (unless foliage)
  manage_material → save to Assets/_Project/Materials/[Category]/
  manage_asset → remap material on model to new URP material
```

### Material Naming
```
MAT_[ObjectName]_[Part].mat

Examples:
  MAT_WateringCan_Body.mat
  MAT_WateringCan_Handle.mat
  MAT_Tomato_Skin.mat
  MAT_Barn_Walls.mat
  MAT_Barn_Roof.mat
```

## Phase 5: Prefab Assembly

Turn the imported model into a game-ready prefab.

```
# 1. Instantiate the imported model in a temp scene
manage_gameobject → instantiate the glTFast-generated prefab

# 2. Rename root to game convention
manage_gameobject → rename to [Category]_[Name]

# 3. Add colliders based on category
For Tools (grabbed by player):
  manage_components → add MeshCollider (convex: true) or BoxCollider
  → size to approximate the mesh bounds

For Crops (poked/grabbed for harvest):
  manage_components → add BoxCollider or SphereCollider
  → make trigger for interaction zone

For Structures (static, walkable):
  manage_components → add MeshCollider (convex: false, for collision)
  manage_gameobject → set static: true

For Props (background):
  manage_components → add BoxCollider if needed
  manage_gameobject → set static: true

# 4. Add game components based on category
For Tools:
  manage_components → add XRGrabInteractable
  manage_components → add Rigidbody (useGravity: true, isKinematic: false)
  manage_components → add [ToolBehaviour] MonoBehaviour
  manage_physics → set layer "Interactable"
  manage_gameobject → set tag "Tool"

For Crops:
  manage_components → add CropPlotController (or CropVisualController)
  manage_physics → set layer "Interactable"
  manage_gameobject → set tag "Crop"

For Structures:
  manage_physics → set layer "Default"
  manage_gameobject → set tag "Structure"

# 5. Create the prefab
manage_prefabs → create at Assets/_Project/Prefabs/[Category]/[Name].prefab

# 6. Clean up temp instance
manage_gameobject → delete the temp scene instance
```

### Growth Stage Variant Prefabs (for Crops)
If the model represents one growth stage of a crop, create a
variant prefab structure:

```
Prefabs/Crops/Tomato/
├── Crop_Tomato_Base.prefab          # Base with all shared components
├── Crop_Tomato_Seedling.prefab      # Variant: seedling mesh
├── Crop_Tomato_Growing.prefab       # Variant: mid-growth mesh
├── Crop_Tomato_Mature.prefab        # Variant: harvestable mesh
└── Crop_Tomato_Harvested.prefab     # Variant: empty/withered mesh
```

Each variant overrides only the mesh/materials while inheriting
components from the base prefab.

## Phase 6: Validate

```
# Performance check
execute_custom_tool → quest_perf_check
  → Verify triangle count within budget
  → Verify draw call impact

# Visual check
manage_gameobject → instantiate prefab temporarily
  → Screenshot via manage_editor for visual verify
  → Delete temp instance

# Console check
read_console → any errors or warnings from the import?

# Reference check
manage_prefabs → get info → verify no missing references
find_gameobjects → verify all components resolved
```

### Log to Asset Manifest
Append to `.ai/memory/asset-manifest.md`:
```markdown
## [Category]_[Name]
- Source: [original filename] (from [source])
- Imported: [date]
- Category: [category]
- Triangles: [count]
- Materials: [count] ([names])
- Textures: [count] ([sizes])
- Prefab: Assets/_Project/Prefabs/[Category]/[Name].prefab
- Components: [list]
- Quest budget: [pass/warn/fail]
- Notes: [any issues encountered]
```

## Phase 7: Place in Scene (if applicable)

If this model belongs in the current working scene:

```
# Check if there's a greybox placeholder to replace
find_gameobjects → search for "Greybox_[Name]" or similar

If placeholder exists:
  # Read its transform
  manage_gameobject → get position, rotation, scale of placeholder
  # Delete placeholder
  manage_gameobject → delete placeholder
  # Instantiate real prefab at same transform
  manage_prefabs → instantiate at saved position/rotation/scale

If no placeholder:
  # Ask developer where to put it, or use sensible default
  manage_prefabs → instantiate prefab
  manage_gameobject → position based on context
    (e.g., tool → near tool rack, crop → in a plot)

# Save scene
manage_scene → save
```

Commit: `[asset] ingest [Category]_[Name].glb — prefab + materials + scene placement`

## Phase 8: Report to Developer

```
Model Ingested: [Category]_[Name]

Source: [original filename]
Location: Assets/_Project/Art/Models/[Category]/[Name].glb
Prefab: Assets/_Project/Prefabs/[Category]/[Name].prefab
Materials: [count] ([shader type], ASTC compressed)
Triangles: [count] (budget: [limit] [pass/fail])
Textures: [count] at [size] ASTC

Components added:
- [list of components]

Placed in scene: [scene name] at ([x], [y], [z])
Replaced greybox: [yes/no]

[Any warnings or notes]
[Suggestion for next steps if controller needs TDD implementation]
```
