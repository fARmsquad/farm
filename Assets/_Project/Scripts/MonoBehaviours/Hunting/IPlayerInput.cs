using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    /// <summary>
    /// Abstraction for player input. Keyboard now, VR later.
    /// </summary>
    public interface IPlayerInput
    {
        bool CatchPressed { get; }
        Vector3 Position { get; }
    }
}
