using UnityEngine;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    /// <summary>World pickup dropped by the player; horse walks to this transform to eat.</summary>
    public sealed class HorseTamingCarrot : MonoBehaviour
    {
        public Vector3 Position => transform.position;
    }
}
