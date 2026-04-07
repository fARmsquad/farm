using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Controls a cinematic camera that can move along waypoints, follow targets,
    /// and switch between cinematic and gameplay cameras. Uses unscaled time so
    /// cinematic moves work even when the game is paused.
    /// </summary>
    public class CinematicCamera : MonoBehaviour
    {
        #region Enums

        public enum CameraMode
        {
            Idle,
            Moving,
            Following,
            Holding
        }

        #endregion

        #region Serialized Fields

        [Header("Camera References")]
        [SerializeField] private Camera cinematicCam;
        [SerializeField] private Camera gameplayCam;

        [Header("Path")]
        [SerializeField] private CameraPath currentPath;

        [Header("Events")]
        public UnityEvent OnWaypointReached;

        #endregion

        #region Internal State

        private Coroutine moveCoroutine;
        private Coroutine pathCoroutine;
        private Transform followTarget;
        private Vector3 followOffset;
        private int currentWaypointIndex;
        private CameraMode currentMode = CameraMode.Idle;

        private static readonly AnimationCurve defaultEasing = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion

        #region Public State

        /// <summary>
        /// The current operating mode of the cinematic camera.
        /// </summary>
        public CameraMode CurrentMode => currentMode;

        /// <summary>
        /// Whether the camera is currently moving to a waypoint.
        /// </summary>
        public bool IsMoving => currentMode == CameraMode.Moving;

        /// <summary>
        /// Index of the current or most recently reached waypoint.
        /// </summary>
        public int CurrentWaypointIndex => currentWaypointIndex;

        #endregion

        #region Unity Callbacks

        private void LateUpdate()
        {
            if (currentMode == CameraMode.Following)
            {
                if (followTarget == null)
                {
                    Debug.LogWarning("[CinematicCamera] Follow target is null. Switching to HoldPosition.");
                    HoldPosition();
                    return;
                }

                transform.position = followTarget.position + followOffset;
            }
        }

        #endregion

        #region Camera Switching

        /// <summary>
        /// Enables the cinematic camera and disables the gameplay camera.
        /// </summary>
        public void EnableCinematicCamera()
        {
            if (cinematicCam != null)
                cinematicCam.enabled = true;
            if (gameplayCam != null)
                gameplayCam.enabled = false;
        }

        /// <summary>
        /// Enables the gameplay camera and disables the cinematic camera.
        /// </summary>
        public void EnableGameplayCamera()
        {
            if (cinematicCam != null)
                cinematicCam.enabled = false;
            if (gameplayCam != null)
                gameplayCam.enabled = true;
        }

        #endregion

        #region Waypoint Movement

        /// <summary>
        /// Moves the camera to the specified waypoint using coroutine-based lerp.
        /// Cancels any existing move before starting.
        /// </summary>
        public void MoveToWaypoint(CameraWaypoint waypoint)
        {
            CancelActiveMove();
            moveCoroutine = StartCoroutine(MoveToWaypointCoroutine(waypoint));
        }

        /// <summary>
        /// Moves the camera to the waypoint at the given index in the current CameraPath.
        /// Logs a warning if the index is out of range.
        /// </summary>
        public void MoveToWaypoint(int index)
        {
            if (currentPath == null || currentPath.waypoints == null)
            {
                Debug.LogWarning("[CinematicCamera] No CameraPath assigned or waypoints array is null.");
                return;
            }

            if (index < 0 || index >= currentPath.waypoints.Length)
            {
                Debug.LogWarning($"[CinematicCamera] Waypoint index {index} is out of range (0-{currentPath.waypoints.Length - 1}).");
                return;
            }

            currentWaypointIndex = index;
            MoveToWaypoint(currentPath.waypoints[index]);
        }

        private IEnumerator MoveToWaypointCoroutine(CameraWaypoint waypoint)
        {
            currentMode = CameraMode.Moving;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startFov = cinematicCam != null ? cinematicCam.fieldOfView : 60f;

            AnimationCurve easing = waypoint.easing ?? defaultEasing;
            float duration = Mathf.Max(waypoint.duration, 0.001f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));

                transform.position = Vector3.Lerp(startPos, waypoint.position, t);
                transform.rotation = Quaternion.Slerp(startRot, waypoint.rotation, t);

                if (cinematicCam != null)
                    cinematicCam.fieldOfView = Mathf.Lerp(startFov, waypoint.fov, t);

                yield return null;
            }

            // Snap to final values
            transform.position = waypoint.position;
            transform.rotation = waypoint.rotation;
            if (cinematicCam != null)
                cinematicCam.fieldOfView = waypoint.fov;

            moveCoroutine = null;
            currentMode = CameraMode.Idle;
        }

        #endregion

        #region Follow Target

        /// <summary>
        /// Sets the camera to follow a target transform with the given offset.
        /// Position is updated every LateUpdate. If the target is null, logs a
        /// warning and switches to HoldPosition.
        /// </summary>
        public void FollowTarget(Transform target, Vector3 offset)
        {
            if (target == null)
            {
                Debug.LogWarning("[CinematicCamera] Cannot follow a null target. Switching to HoldPosition.");
                HoldPosition();
                return;
            }

            CancelActiveMove();
            followTarget = target;
            followOffset = offset;
            currentMode = CameraMode.Following;
        }

        #endregion

        #region Hold Position

        /// <summary>
        /// Stops any active coroutine and holds the camera at its current position.
        /// </summary>
        public void HoldPosition()
        {
            CancelActiveMove();
            followTarget = null;
            currentMode = CameraMode.Holding;
        }

        #endregion

        #region Path Playback

        /// <summary>
        /// Plays through all waypoints in the given CameraPath sequentially.
        /// Fires OnWaypointReached after each waypoint is reached.
        /// </summary>
        public void PlayPath(CameraPath path)
        {
            if (path == null || path.waypoints == null || path.waypoints.Length == 0)
            {
                Debug.LogWarning("[CinematicCamera] Cannot play a null or empty CameraPath.");
                return;
            }

            CancelActiveMove();
            if (pathCoroutine != null)
                StopCoroutine(pathCoroutine);

            currentPath = path;
            pathCoroutine = StartCoroutine(PlayPathCoroutine(path));
        }

        private IEnumerator PlayPathCoroutine(CameraPath path)
        {
            currentMode = CameraMode.Moving;

            for (int i = 0; i < path.waypoints.Length; i++)
            {
                currentWaypointIndex = i;
                CameraWaypoint waypoint = path.waypoints[i];

                Vector3 startPos = transform.position;
                Quaternion startRot = transform.rotation;
                float startFov = cinematicCam != null ? cinematicCam.fieldOfView : 60f;

                AnimationCurve easing = waypoint.easing ?? defaultEasing;
                float duration = Mathf.Max(waypoint.duration, 0.001f);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));

                    transform.position = Vector3.Lerp(startPos, waypoint.position, t);
                    transform.rotation = Quaternion.Slerp(startRot, waypoint.rotation, t);

                    if (cinematicCam != null)
                        cinematicCam.fieldOfView = Mathf.Lerp(startFov, waypoint.fov, t);

                    yield return null;
                }

                // Snap to final values
                transform.position = waypoint.position;
                transform.rotation = waypoint.rotation;
                if (cinematicCam != null)
                    cinematicCam.fieldOfView = waypoint.fov;

                OnWaypointReached?.Invoke();
            }

            pathCoroutine = null;
            currentMode = CameraMode.Idle;
        }

        #endregion

        #region Utility

        private void CancelActiveMove()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            if (pathCoroutine != null)
            {
                StopCoroutine(pathCoroutine);
                pathCoroutine = null;
            }
        }

        #endregion
    }
}
