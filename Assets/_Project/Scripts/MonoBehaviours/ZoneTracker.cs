using System.Collections.Generic;
using FarmSimVR.Core.GameState;
using FarmSimVR.MonoBehaviours.Diagnostics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours
{
    /// <summary>
    /// Tracks which world zone the player is currently in.
    /// Attaches to the player GameObject with a CharacterController.
    /// Reads ZoneMarker components on trigger colliders to identify zones.
    /// Publishes ZoneEnteredEvent / ZoneExitedEvent via the GameEventBus.
    /// </summary>
    public class ZoneTracker : MonoBehaviour
    {
        public string CurrentZone { get; private set; } = "";

        private readonly HashSet<string> _visitedZones = new();
        public IReadOnlyCollection<string> VisitedZones => _visitedZones;

        private void OnTriggerEnter(Collider other)
        {
            var marker = other.GetComponent<ZoneMarker>();
            if (marker == null || string.IsNullOrEmpty(marker.ZoneName))
                return;

            CurrentZone = marker.ZoneName;
            _visitedZones.Add(marker.ZoneName);

            GameStateLogger.Instance?.LogEvent($"Entered zone: {marker.ZoneName}");
            GameManager.Instance?.EventBus.Publish(new ZoneEnteredEvent(marker.ZoneName));
        }

        private void OnTriggerExit(Collider other)
        {
            var marker = other.GetComponent<ZoneMarker>();
            if (marker == null || string.IsNullOrEmpty(marker.ZoneName))
                return;

            if (marker.ZoneName != CurrentZone)
                return;

            GameStateLogger.Instance?.LogEvent($"Exited zone: {marker.ZoneName}");
            GameManager.Instance?.EventBus.Publish(new ZoneExitedEvent(marker.ZoneName));
            CurrentZone = "";
        }
    }
}
