# Feature Spec: Simple Audio Manager — INT-002

## Summary
A singleton audio manager that handles 2D music playback with crossfading and one-shot sound effects. All audio is non-spatial for greybox fidelity, with clips referenced by string key through an AudioLibrary ScriptableObject.

## User Story
As a player, I want to hear background music and sound effects during the intro cinematic so that the experience has audio atmosphere even in greybox form.

## Acceptance Criteria
- [ ] SimpleAudioManager is a singleton MonoBehaviour accessible via SimpleAudioManager.Instance
- [ ] PlayMusic(AudioClip, fadeInDuration) fades in a music clip from volume 0 to 1 over the specified duration
- [ ] StopMusic(fadeOutDuration) fades out the current music clip from current volume to 0
- [ ] CrossfadeMusic(newClip, duration) fades out current music while fading in the new clip simultaneously
- [ ] PlaySFX(AudioClip, volume) plays a one-shot sound effect at the given volume
- [ ] AudioLibrary ScriptableObject maps string keys to AudioClip references
- [ ] PlayMusicByKey(string key) and PlaySFXByKey(string key) look up clips from AudioLibrary
- [ ] Two AudioSources exist: one dedicated to music, one dedicated to SFX
- [ ] All audio is 2D (spatialBlend = 0)
- [ ] Singleton persists across scenes via DontDestroyOnLoad

## Edge Cases
- PlayMusic called while music is already playing stops the current track and starts the new one
- CrossfadeMusic called with the same clip that is already playing is a no-op
- PlaySFXByKey with an unknown key logs a warning and does nothing
- StopMusic called when no music is playing is a no-op
- PlayMusic with fadeInDuration of 0 starts at full volume immediately

## Performance Impact
- Two AudioSources total — negligible CPU/memory overhead
- One-shot SFX via PlayOneShot avoids AudioSource pooling complexity
- No spatial audio calculations (all 2D)

## Dependencies
- **Existing:** None
- **New:** SimpleAudioManager.cs (MonoBehaviour), AudioLibrary.cs (ScriptableObject)

## Out of Scope
- 3D spatial audio
- Audio mixer groups and snapshots
- Audio pooling for overlapping SFX
- Dynamic music layering
- DeepVoice TTS integration (hook only, deferred)

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Audio/SimpleAudioManager.cs
Assets/_Project/Scripts/Core/Audio/AudioLibrary.cs
Assets/_Project/ScriptableObjects/Audio/AudioLibrary.asset
```

SimpleAudioManager.cs lives in FarmSimVR.MonoBehaviours assembly. AudioLibrary.cs is a ScriptableObject in FarmSimVR.Core (no engine references needed — wait, AudioClip is UnityEngine, so this goes in MonoBehaviours or a shared assembly). Place AudioLibrary.cs alongside SimpleAudioManager in MonoBehaviours since it references AudioClip.

### Build Approach
1. Create AudioLibrary ScriptableObject with a serializable list of AudioEntry (string key, AudioClip clip) and a GetClip(string key) lookup method
2. Create SimpleAudioManager MonoBehaviour with singleton pattern (Instance property, DontDestroyOnLoad, duplicate destruction)
3. Add two AudioSource components in Awake: musicSource (loop=true, spatialBlend=0) and sfxSource (spatialBlend=0)
4. Implement PlayMusic / StopMusic with coroutine-based volume fade
5. Implement CrossfadeMusic: requires a temporary third AudioSource for the outgoing track, or swap references
6. Implement PlaySFX using sfxSource.PlayOneShot(clip, volume)
7. Implement key-based lookup methods that delegate to the clip-based methods

### Testing Strategy
- EditMode tests for AudioLibrary: verify GetClip returns correct clip for known keys, returns null for unknown keys
- EditMode tests for SimpleAudioManager: verify singleton pattern (only one instance)
- PlayMode tests: verify PlayMusic sets musicSource.clip and starts playing
- PlayMode tests: verify StopMusic results in musicSource.volume reaching 0 after duration
- PlayMode tests: verify PlaySFX calls PlayOneShot (check isPlaying or use mock)

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | code | Create AudioLibrary ScriptableObject with string-to-AudioClip dictionary and GetClip method | — | GetClip returns correct clip by key, null for missing key |
| 2 | code | Create SimpleAudioManager singleton with DontDestroyOnLoad | — | Only one instance exists, survives scene load |
| 3 | code | Add two AudioSources (music + SFX) with spatialBlend=0 in Awake | 2 | Both sources exist, both set to 2D |
| 4 | code | Implement PlayMusic and StopMusic with coroutine volume fades | 3 | Music fades in/out over specified duration |
| 5 | code | Implement CrossfadeMusic with simultaneous fade-out/fade-in | 4 | Old track fades out while new track fades in over duration |
| 6 | code | Implement PlaySFX one-shot playback | 3 | SFX plays at specified volume without interrupting music |
| 7 | code | Implement key-based lookup methods using AudioLibrary reference | 1,4,6 | PlayMusicByKey and PlaySFXByKey resolve keys to clips |
| 8 | test | Write EditMode + PlayMode tests for AudioLibrary and SimpleAudioManager | 1-7 | All tests pass |
