using UnityEngine;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours
{
    public class CropPlotController : MonoBehaviour
    {
        public CropPlotState State { get; private set; }

        public void Initialize(CropPlotState state)
        {
            State = state;
            state.OnMilestone += OnMilestone;
        }

        private void OnMilestone(int percent)
        {
            Debug.Log($"[{gameObject.name}] Growth milestone: {percent}%");
        }

        private void OnDestroy()
        {
            if (State != null)
                State.OnMilestone -= OnMilestone;
        }
    }
}
