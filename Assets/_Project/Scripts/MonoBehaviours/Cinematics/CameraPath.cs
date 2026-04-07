using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// A single waypoint along a camera path, defining position, rotation, FOV,
    /// duration, and easing curve for the transition.
    /// </summary>
    [System.Serializable]
    public struct CameraWaypoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fov;
        public float duration;
        public AnimationCurve easing;
    }

    /// <summary>
    /// ScriptableObject that stores an ordered sequence of camera waypoints
    /// for cinematic camera paths.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraPath", menuName = "FarmSimVR/Camera Path")]
    public class CameraPath : ScriptableObject
    {
        public CameraWaypoint[] waypoints;
    }
}
