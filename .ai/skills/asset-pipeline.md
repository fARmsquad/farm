# Skill: Asset Pipeline

## Purpose
Validate 3D model, texture, and audio imports against project conventions.

## Naming Convention
```
[Category]_[Name]_[Variant]_[Size].[ext]

Examples:
  Crop_Tomato_Mature_1K.fbx
  TX_Soil_Wet_512.png
  SFX_Harvest_Crunch_01.wav
  UI_Button_Plant_Normal.png
```

## Texture Budget (Quest)
- Max size: 1024x1024 (most), 2048x2048 (terrain/skybox)
- Format: ASTC 6x6
- Mipmaps: ON for 3D, OFF for UI
- Total GPU memory: <200MB (Quest 2), <300MB (Quest 3)

## Model Budget (Quest)
- Max triangles: 10K (hero), 2K (background)
- LOD required for >5K triangles
- Max bones: 75 per skinned mesh
- Scale: 1 unit = 1 meter

## Validation Script
```bash
.ai/scripts/asset_import_check.sh
```

## Import Checklist
- [ ] File follows naming convention
- [ ] Texture size within budget
- [ ] Model triangle count within budget
- [ ] LODs present if required
- [ ] Import scale correct (1 unit = 1m)
- [ ] Materials use URP/Mobile shaders
