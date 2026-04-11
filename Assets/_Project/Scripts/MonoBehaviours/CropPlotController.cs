using UnityEngine;
using FarmSimVR.Core.Farming;
using System;

namespace FarmSimVR.MonoBehaviours
{
    public class CropPlotController : MonoBehaviour
    {
        public CropPlotState State { get; private set; }
        public SoilState SoilState { get; private set; }

        public void Initialize(CropPlotState state)
        {
            Initialize(state, null);
        }

        public void Initialize(CropPlotState state, SoilState soilState)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (State != null)
                State.OnMilestone -= OnMilestone;

            State = state;
            SoilState = soilState;
            state.OnMilestone += OnMilestone;
        }

        private void OnMilestone(int percent)
        {
        }

        private void OnDestroy()
        {
            if (State != null)
                State.OnMilestone -= OnMilestone;
        }
    }
}
