# URP + XR Gotchas

## Known Issues
1. **Single Pass Instanced**: Required for Quest. Some shaders break in SPI mode.
   Fix: Use URP/Mobile shaders, avoid custom vertex shaders that don't handle SPI.

2. **Post-processing**: Many effects too expensive for Quest.
   Safe: Color grading, vignette. Avoid: bloom, SSAO, motion blur.

3. **UI Rendering**: World-space canvases in VR need careful setup.
   - Canvas render mode: World Space
   - Event camera: main XR camera
   - Sorting order matters for overlapping UI

4. **Occlusion Culling**: Unity's built-in occlusion can be unreliable in VR.
   Consider manual occlusion for known geometry (farm plots behind walls).

5. **Texture Compression**: ASTC is Quest-native. ETC2 works but ASTC is better.
   Set platform override in texture import settings.

6. **Lightmapping**: Bake lighting for static objects. Real-time lights are expensive.
   Max 1 real-time directional light (sun). Everything else baked or probes.

## XR Interaction Toolkit Tips
- Use XR Direct Interactor for hand grab
- Use XR Ray Interactor for distant interaction
- Haptic feedback: XRBaseControllerInteractor.SendHapticImpulse()
- Tracking: always handle tracking loss gracefully
