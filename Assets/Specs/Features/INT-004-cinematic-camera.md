# Feature Spec: Cinematic Camera — INT-004

## Summary
A waypoint-driven cinematic camera that lerps between predefined positions, rotations, and FOV values during cutscenes. It operates separately from the gameplay ThirdPersonCamera and supports follow-target and hold modes for flexible shot composition.

## User Story
As a player, I want the camera to move smoothly between cinematic angles during the intro so that cutscenes feel directed and visually engaging.

## Acceptance Criteria
- [ ] CameraWaypoint struct contains: Vector3 position, Quaternion rotation, float fov, float duration, AnimationCurve easing
- [ ] CameraPath ScriptableObject holds an ordered array of CameraWaypoint
- [ ] CinematicCamera MonoBehaviour exists on a separate Camera GameObject from ThirdPersonCamera
- [ ] MoveToWaypoint(int index) lerps position, rotation, and FOV to the target waypoint over its duration using its easing curve
- [ ] FollowTarget(Transform target, Vector3 offset) switches to follow mode, updating position each frame to target.position + offset
- [ ] HoldPosition() freezes the camera at its current position, rotation, and FOV
- [ ] OnWaypointReached UnityEvent fires when a MoveToWaypoint completes
- [ ] PlayPath() plays all waypoints in sequence from index 0 to end, firing OnWaypointReached for each
- [ ] ThirdPersonCamera is disabled when CinematicCamera is active, and vice versa
- [ ] Camera swap is handled by EnableCinematicCamera() and EnableGameplayCamera() public methods

## Edge Cases
- MoveToWaypoint called with an out-of-range index logs a warning and does nothing
- MoveToWaypoint called while already moving cancels the current move and starts the new one
- FollowTarget with a null target logs a warning and switches to HoldPosition
- PlayPath on an empty CameraPath fires OnWaypointReached immediately
- CameraPath with a single waypoint snaps to that waypoint (duration still respected)

## Performance Impact
- Single Camera component — no additional render passes
- Lerp calculations in coroutine, not Update, when idle
- Follow mode uses LateUpdate for smooth tracking (same as ThirdPersonCamera)

## Dependencies
- **Existing:** ThirdPersonCamera.cs (needs enable/disable support, already has this via MonoBehaviour.enabled)
- **New:** CameraWaypoint.cs (struct), CameraPath.cs (ScriptableObject), CinematicCamera.cs (MonoBehaviour)

## Out of Scope
- Cinemachine integration
- Dolly tracks or spline paths
- Camera collision/clipping avoidance
- Depth of field or post-processing per waypoint
- Timeline track integration (sequencer calls directly)

---

## Technical Plan

### Architecture
```
Assets/_Project/Scripts/MonoBehaviours/Cinematics/CinematicCamera.cs
Assets/_Project/Scripts/MonoBehaviours/Cinematics/CameraWaypoint.cs  (serializable struct)
Assets/_Project/Scripts/MonoBehaviours/Cinematics/CameraPath.cs      (ScriptableObject)
```

CinematicCamera is a MonoBehaviour on a dedicated Camera GameObject. It references the ThirdPersonCamera for swap logic. CameraPath is a ScriptableObject created via [CreateAssetMenu].

All waypoint structs use UnityEngine types (Vector3, Quaternion, AnimationCurve), so they live in the MonoBehaviours assembly.

### Build Approach
1. Define CameraWaypoint as a [System.Serializable] struct: position (Vector3), rotation (Quaternion), fov (float), duration (float), easing (AnimationCurve)
2. Create CameraPath ScriptableObject with CameraWaypoint[] waypoints and [CreateAssetMenu] attribute
3. Create CinematicCamera MonoBehaviour with Camera reference, mode enum (Idle, Moving, Following, Holding)
4. Implement MoveToWaypoint: coroutine that lerps transform.position (Vector3.Lerp), transform.rotation (Quaternion.Slerp), and Camera.fieldOfView (Mathf.Lerp) using easing curve evaluation over duration
5. Implement FollowTarget: store target + offset, in LateUpdate set position to target.position + offset
6. Implement HoldPosition: clear any active coroutine, freeze in current state
7. Implement PlayPath: coroutine that iterates waypoints, calling MoveToWaypoint and awaiting completion via OnWaypointReached
8. Implement EnableCinematicCamera / EnableGameplayCamera: toggle .enabled and .gameObject camera component on both cameras

### Testing Strategy
- EditMode tests: CameraPath creation, verify waypoint array is serializable
- EditMode tests: CameraWaypoint default values and easing curve evaluation
- PlayMode tests: MoveToWaypoint moves camera to target position/rotation/fov over time
- PlayMode tests: OnWaypointReached fires after MoveToWaypoint completes
- PlayMode tests: EnableCinematicCamera disables ThirdPersonCamera, EnableGameplayCamera reverses
- PlayMode tests: FollowTarget tracks a moving transform each frame

---

## Task Breakdown

| # | Type | Action | Depends | Acceptance |
|---|------|--------|---------|------------|
| 1 | code | Define CameraWaypoint struct with position, rotation, fov, duration, easing | — | Struct is serializable, editable in Inspector |
| 2 | code | Create CameraPath ScriptableObject with CameraWaypoint array | 1 | Asset creatable via Create menu, waypoints editable |
| 3 | code | Create CinematicCamera MonoBehaviour with mode state tracking | — | Component attaches to Camera, tracks Idle/Moving/Following/Holding |
| 4 | code | Implement MoveToWaypoint lerp with easing curve | 1,3 | Camera reaches target pos/rot/fov over duration using curve |
| 5 | code | Implement FollowTarget mode in LateUpdate | 3 | Camera follows target position + offset each frame |
| 6 | code | Implement HoldPosition freeze | 3 | Camera stays locked at current transform after call |
| 7 | code | Implement PlayPath sequential waypoint playback | 2,4 | All waypoints played in order, OnWaypointReached fires for each |
| 8 | code | Implement camera swap (EnableCinematicCamera / EnableGameplayCamera) | 3 | ThirdPersonCamera disables when cinematic enables, and vice versa |
| 9 | test | Write EditMode + PlayMode tests for waypoint movement and camera swap | 1-8 | All tests pass |
