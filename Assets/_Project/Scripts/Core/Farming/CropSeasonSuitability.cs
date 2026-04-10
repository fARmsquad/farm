using System.Collections.Generic;

namespace FarmSimVR.Core.Farming
{
    /// <summary>
    /// Defines which seasons each starter crop is suited to and what
    /// growth multiplier applies in each season.
    ///
    /// Multiplier guide:
    ///   1.0  — ideal season, full growth rate
    ///   0.5  — tolerated, half growth rate
    ///   0.0  — hostile, no growth (cannot plant)
    /// </summary>
    public static class CropSeasonSuitability
    {
        private static readonly Dictionary<string, Dictionary<FarmSeason, float>> Table =
            new()
            {
                // Tomato — loves heat; summer ideal, spring/autumn tolerated, winter hostile
                ["seed_tomato"] = new Dictionary<FarmSeason, float>
                {
                    { FarmSeason.Spring, 0.5f },
                    { FarmSeason.Summer, 1.0f },
                    { FarmSeason.Autumn, 0.5f },
                    { FarmSeason.Winter, 0.0f },
                },

                // Carrot — cool-season crop; spring/autumn ideal, summer tolerated, winter hostile
                ["seed_carrot"] = new Dictionary<FarmSeason, float>
                {
                    { FarmSeason.Spring, 1.0f },
                    { FarmSeason.Summer, 0.5f },
                    { FarmSeason.Autumn, 1.0f },
                    { FarmSeason.Winter, 0.0f },
                },

                // Lettuce — similar to carrot; wilts in summer heat
                ["seed_lettuce"] = new Dictionary<FarmSeason, float>
                {
                    { FarmSeason.Spring, 1.0f },
                    { FarmSeason.Summer, 0.3f },
                    { FarmSeason.Autumn, 0.8f },
                    { FarmSeason.Winter, 0.0f },
                },
            };

        /// <summary>
        /// Returns the growth multiplier for <paramref name="seedId"/> in
        /// <paramref name="season"/>. Returns 1.0 for unknown crops so
        /// future crops degrade gracefully.
        /// </summary>
        public static float GetMultiplier(string seedId, FarmSeason season)
        {
            if (Table.TryGetValue(seedId, out var seasons) &&
                seasons.TryGetValue(season, out float multiplier))
                return multiplier;

            return 1.0f; // unknown crop — don't penalise
        }

        /// <summary>
        /// Returns true when a crop can be planted this season
        /// (multiplier > 0).
        /// </summary>
        public static bool CanPlant(string seedId, FarmSeason season) =>
            GetMultiplier(seedId, season) > 0f;

        /// <summary>Human-readable label for the suitability level.</summary>
        public static string Label(string seedId, FarmSeason season)
        {
            float m = GetMultiplier(seedId, season);
            return m switch
            {
                >= 0.9f => "Ideal",
                >= 0.4f => "Tolerated",
                > 0f    => "Poor",
                _       => "Not plantable",
            };
        }
    }
}
