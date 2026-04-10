using FarmSimVR.Core.Farming;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmGrowthConditionResolverTests
    {
        [Test]
        public void Build_DimSunlightHours_DowngradesSunnyToCloudy()
        {
            var conditions = FarmGrowthConditionResolver.Build(
                WeatherType.Sunny,
                DayPhase.Dusk,
                SoilQuality.Normal,
                "seed_tomato",
                FarmSeason.Summer,
                22f);

            Assert.That(conditions.Weather, Is.EqualTo(WeatherType.Cloudy));
            Assert.That(conditions.SeasonMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Build_UsesPlantedCropSeasonSuitability()
        {
            var conditions = FarmGrowthConditionResolver.Build(
                WeatherType.Rain,
                DayPhase.Morning,
                SoilQuality.Rich,
                "seed_tomato",
                FarmSeason.Winter,
                22f);

            Assert.That(conditions.Weather, Is.EqualTo(WeatherType.Rain));
            Assert.That(conditions.SeasonMultiplier, Is.EqualTo(0f));
        }

        [Test]
        public void Build_WithoutSeasonOrCrop_DefaultsToNeutralMultiplier()
        {
            var conditions = FarmGrowthConditionResolver.Build(
                WeatherType.Cloudy,
                DayPhase.Noon,
                SoilQuality.Poor,
                null,
                null,
                22f);

            Assert.That(conditions.SeasonMultiplier, Is.EqualTo(1f));
        }
    }
}
