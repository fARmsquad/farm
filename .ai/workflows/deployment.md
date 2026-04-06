# Workflow: Deployment

## Autonomy Level: PARTIAL (build confirmation required)

## Process
1. **Pre-build**: Run full test suite + preflight
2. **Build**: Unity CLI build for target platform
3. **Test build**: Verify APK/build artifact
4. **Deploy**: Upload to target (Quest via SideQuest/Meta Developer Hub)
5. **Verify**: Confirm deployment works on device

## Build Targets
- **Development**: Quest Link (fastest iteration)
- **Testing**: Quest APK (development build with profiler)
- **Release**: Quest APK (release build, optimized)

## Build Command
```bash
# Unity CLI build (example — adjust paths)
/Applications/Unity/Hub/Editor/6000.*/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -executeMethod BuildScript.BuildQuest \
  -logFile build.log
```
