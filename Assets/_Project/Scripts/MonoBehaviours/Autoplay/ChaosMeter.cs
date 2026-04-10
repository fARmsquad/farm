using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class ChaosMeter : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float currentFill;

        public float CurrentFill => currentFill;

        public void AddFill(float amount)
        {
            currentFill = Mathf.Clamp01(currentFill + amount);
        }
    }
}
