namespace FarmSimVR.Core.GameState
{
    public readonly struct ZoneEnteredEvent
    {
        public readonly string ZoneName;

        public ZoneEnteredEvent(string zoneName)
        {
            ZoneName = zoneName;
        }
    }

    public readonly struct ZoneExitedEvent
    {
        public readonly string ZoneName;

        public ZoneExitedEvent(string zoneName)
        {
            ZoneName = zoneName;
        }
    }
}
