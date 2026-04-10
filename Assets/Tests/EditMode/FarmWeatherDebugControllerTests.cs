using FarmSimVR.Core.Farming;
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class FarmWeatherDebugControllerTests
    {
        [Test]
        public void Apply_ForceRain_UpdatesProviderAndReturnsFeedback()
        {
            var provider = new FarmWeatherProvider(WeatherType.Sunny, seed: 1);
            var controller = new FarmWeatherDebugController(provider);

            var message = controller.Apply(FarmWeatherDebugCommand.ForceRain);

            Assert.That(provider.Current, Is.EqualTo(WeatherType.Rain));
            Assert.That(provider.IsForced, Is.True);
            Assert.That(message, Is.EqualTo("Weather forced: Rain"));
        }

        [Test]
        public void Apply_Auto_ReleasesForcedWeather()
        {
            var provider = new FarmWeatherProvider(WeatherType.Sunny, seed: 1);
            var controller = new FarmWeatherDebugController(provider);
            controller.Apply(FarmWeatherDebugCommand.ForceCloudy);

            var message = controller.Apply(FarmWeatherDebugCommand.AutoWeather);

            Assert.That(provider.IsForced, Is.False);
            Assert.That(message, Is.EqualTo("Weather returned to auto"));
        }
    }
}
