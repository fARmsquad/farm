namespace FarmSimVR.Core.HorseTaming
{
    /// <summary>
    /// Pure trust tick rules for the comfort-radius loop (no Unity dependencies).
    /// </summary>
    public static class HorseTamingTrustMath
    {
        public struct TrustTickResult
        {
            public float Trust;
            public bool Spooked;
        }

        /// <summary>
        /// Applies one frame of trust change while the player is near the horse.
        /// </summary>
        /// <param name="trust">Current trust 0–100.</param>
        /// <param name="inComfortRadius">True if the player is inside the green comfort zone.</param>
        /// <param name="tooClose">True if closer than the crowding radius (still inside comfort). Drains trust while below 100%.</param>
        /// <param name="hasMoveInput">True if the player is trying to move horizontally (WASD).</param>
        /// <param name="sprintHeld">True if sprint is held (unsafe inside comfort radius).</param>
        /// <param name="deltaTime">Frame dt.</param>
        /// <param name="walkTrustPerSecond">Trust gained per second while slow-walking inside the zone.</param>
        /// <param name="standTrustPerSecond">Trust gained per second while standing still inside the zone (smaller).</param>
        /// <param name="spookPenalty">Trust lost when spooked (positive number; subtracted from trust).</param>
        /// <param name="crowdingLossPerSecond">Trust lost per second when too close (before 100%).</param>
        public static TrustTickResult ProcessComfortTrust(
            float trust,
            bool inComfortRadius,
            bool tooClose,
            bool hasMoveInput,
            bool sprintHeld,
            float deltaTime,
            float walkTrustPerSecond,
            float standTrustPerSecond,
            float spookPenalty,
            float crowdingLossPerSecond)
        {
            trust = ClampTrust(trust);

            if (!inComfortRadius)
                return new TrustTickResult { Trust = trust, Spooked = false };

            if (sprintHeld)
            {
                trust = ClampTrust(trust - spookPenalty);
                return new TrustTickResult { Trust = trust, Spooked = true };
            }

            if (tooClose)
            {
                trust = ClampTrust(trust - crowdingLossPerSecond * deltaTime);
                return new TrustTickResult { Trust = trust, Spooked = false };
            }

            if (hasMoveInput)
                trust = ClampTrust(trust + walkTrustPerSecond * deltaTime);
            else
                trust = ClampTrust(trust + standTrustPerSecond * deltaTime);

            return new TrustTickResult { Trust = trust, Spooked = false };
        }

        public static float ApplyCarrotBonus(float trust, float bonus) =>
            ClampTrust(trust + bonus);

        public static float ClampTrust(float trust)
        {
            if (trust < 0f) return 0f;
            if (trust > 100f) return 100f;
            return trust;
        }
    }
}
