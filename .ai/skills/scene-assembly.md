# Skill: Scene Assembly

## Purpose
Assembles Unity scenes from code artifacts using MCP tools.
Triggers after TDD REFACTOR phase when MonoBehaviours or scene
objects were created. Handles GameObjects, components, prefabs,
physics, materials, and XR interaction wiring.

## When to Use
After writing MonoBehaviours or any code that needs to exist in a Unity scene.
Pure Core/ code does NOT trigger this — only code that has a visual/interactive
presence in the game.

## Assembly Sequence

### 0. Memory Check (BEFORE touching the scene)
```
READ .ai/memory/project-memory.md
  → Check "Antipatterns > Asset Paths" — never hardcode paths
  → Check "Antipatterns > MCP" — domain reload rules
  → Check "Established Patterns > MCP Workflow"
  → Check "Lessons Learned" for any scene assembly gotchas
```
**Critical rule**: Before referencing ANY prefab, model, or asset in assembly:
```
FindProjectAssets(query: "descriptive name")  // or glob
→ Use the EXACT path returned
→ DO NOT construct paths from assumed naming conventions
```

### 1. Pre-check
```
editor_state → verify editor ready
refresh_unity → compile new scripts
read_console → check for errors
```

### 2. Build Hierarchy
For each new scene object, use manage_gameobject:
- Create with meaningful name: "CropPlot_Tomato_01"
- Set position, rotation, scale
- Parent under logical hierarchy: "Farm/Plots/CropPlot_Tomato_01"
- Set tag if applicable (e.g., "Crop", "Tool", "Interactable")
- Set layer for physics

### 3. Wire Components
For each MonoBehaviour and Unity component:
- manage_components → add the MonoBehaviour
- manage_components → set SerializeField values
- For XR: add XRGrabInteractable, configure grab type
- For physics: add Collider, Rigidbody, set constraints
- For visuals: add MeshRenderer, assign material

### 4. Create ScriptableObject Assets
For data-driven objects (CropData, ToolData):
- manage_scriptable_object → create asset
- manage_scriptable_object → set field values
- Save to Assets/_Project/ScriptableObjects/[Category]/

### 5. Prefab Everything
After assembling a complete GameObject:
- manage_prefabs → create prefab
- Save to Assets/_Project/Prefabs/[Category]/
- Verify prefab saved: manage_prefabs → get info

### 6. Batch Patterns
Use batch_execute for repetitive setup:
```
# Create 6 crop plots in a row
batch_execute([
  { manage_gameobject: create "CropPlot_01" at (0,0,0) },
  { manage_gameobject: create "CropPlot_02" at (1.5,0,0) },
  { manage_gameobject: create "CropPlot_03" at (3,0,0) },
  { manage_gameobject: create "CropPlot_04" at (0,0,1.5) },
  { manage_gameobject: create "CropPlot_05" at (1.5,0,1.5) },
  { manage_gameobject: create "CropPlot_06" at (3,0,1.5) },
])
# Then batch-add components to all 6
```

### 7. Verify
- find_gameobjects → confirm all objects exist
- read_console → no errors
- Optionally: screenshot for visual check

### 8. Save Scene
- manage_scene → save current scene
- Commit: `[scene] assemble [feature] objects and prefabs`

### 9. Learn
If anything unexpected happened during assembly:
- Asset path didn't match expected name → WRITE to project-memory.md "Lessons Learned"
- New MCP pattern discovered → WRITE to project-memory.md "Established Patterns"
- MCP gotcha hit (disconnect, timeout, etc.) → WRITE to project-memory.md "Antipatterns"
