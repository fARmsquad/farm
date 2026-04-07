using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public class KeyboardPlayerInput : MonoBehaviour, IPlayerInput
    {
        public bool CatchPressed
        {
            get
            {
                var kb = Keyboard.current;
                return kb != null && kb.eKey.wasPressedThisFrame;
            }
        }

        public Vector3 Position => transform.position;
    }
}
