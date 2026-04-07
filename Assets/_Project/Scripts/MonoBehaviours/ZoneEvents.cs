namespace FarmSimVR.MonoBehaviours
{
    public readonly struct ZoneEnteredEvent
    {
        public ZoneEnteredEvent(string zoneName)
        {
            ZoneName = zoneName;
        }

        public string ZoneName { get; }
    }

    public readonly struct ZoneExitedEvent
    {
        public ZoneExitedEvent(string zoneName)
        {
            ZoneName = zoneName;
        }

        public string ZoneName { get; }
    }
}
