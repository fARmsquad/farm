namespace FarmSimVR.Core.Farming
{
    public static class FarmGrowthConditionResolver
    {
        public static GrowthConditions Build(
            WeatherType weather,
            DayPhase? dayPhase,
            SoilQuality soilQuality,
            string cropSeedId,
            FarmSeason? season,
            float temperature)
        {
            var effectiveWeather = ResolveWeather(weather, dayPhase);
            var seasonMultiplier = ResolveSeasonMultiplier(cropSeedId, season);
            return new GrowthConditions(effectiveWeather, temperature, soilQuality, seasonMultiplier);
        }

        private static WeatherType ResolveWeather(WeatherType weather, DayPhase? dayPhase)
        {
            if (weather != WeatherType.Sunny || dayPhase == null)
                return weather;

            return dayPhase.Value is DayPhase.Night or DayPhase.Dawn or DayPhase.Dusk
                ? WeatherType.Cloudy
                : weather;
        }

        private static float ResolveSeasonMultiplier(string cropSeedId, FarmSeason? season)
        {
            if (season == null || string.IsNullOrWhiteSpace(cropSeedId))
                return 1f;

            return CropSeasonSuitability.GetMultiplier(cropSeedId, season.Value);
        }
    }
}
