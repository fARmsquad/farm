namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public enum CinematicStepType
    {
        CameraMove,
        OrbitMove,   // floatParam = radius, duration field = total seconds, intParam = total degrees
        Dialogue,
        Wait,
        PlaySFX,
        PlayMusic,
        StopMusic,
        Fade,
        Shake,
        Letterbox,
        ObjectivePopup,
        MissionStart,
        MissionComplete,
        EnablePlayerControl,
        DisablePlayerControl,
        ActivateNPC,
        DeactivateNPC,
        SetLighting
    }
}
