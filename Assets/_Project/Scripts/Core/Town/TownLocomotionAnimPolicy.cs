using System;

namespace FarmSimVR.Core.Town
{
    /// <summary>
    /// Maps steering-style locomotion (forward/back axis only) to animator Speed for
    /// controllers that only have forward walk clips (no strafe).
    /// </summary>
    public static class TownLocomotionAnimPolicy
    {
        public const float DefaultDeadZone = 0.01f;

        /// <summary>
        /// Returns 1 when |forwardAxis| exceeds the dead zone (walk), else 0 (idle).
        /// </summary>
        public static float GetWalkSpeedParameter(float forwardAxis, float deadZone = DefaultDeadZone)
        {
            return Math.Abs(forwardAxis) > deadZone ? 1f : 0f;
        }
    }
}
