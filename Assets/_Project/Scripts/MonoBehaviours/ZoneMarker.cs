using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Marks a GameObject as a world zone trigger.
    /// Attach to zone root GameObjects that have a trigger collider.
    /// ZoneTracker reads the ZoneName when the player enters the trigger.
    /// </summary>
    public class ZoneMarker : MonoBehaviour
    {
        [SerializeField] private string zoneName;

        public string ZoneName => zoneName;

        public void SetZoneName(string value)
        {
            zoneName = value ?? string.Empty;
        }
    }
}
