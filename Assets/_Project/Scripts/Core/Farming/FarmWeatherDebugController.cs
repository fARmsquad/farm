namespace FarmSimVR.Core.Farming
{
    public enum FarmWeatherDebugCommand
    {
        ForceSunny,
        ForceCloudy,
        ForceRain,
        AutoWeather,
    }

    public sealed class FarmWeatherDebugController
    {
        private readonly FarmWeatherProvider _provider;

        public FarmWeatherDebugController(FarmWeatherProvider provider)
        {
            _provider = provider ?? throw new System.ArgumentNullException(nameof(provider));
        }

        public string Apply(FarmWeatherDebugCommand command)
        {
            switch (command)
            {
                case FarmWeatherDebugCommand.ForceSunny:
                    _provider.Force(WeatherType.Sunny);
                    return "Weather forced: Sunny";
                case FarmWeatherDebugCommand.ForceCloudy:
                    _provider.Force(WeatherType.Cloudy);
                    return "Weather forced: Cloudy";
                case FarmWeatherDebugCommand.ForceRain:
                    _provider.Force(WeatherType.Rain);
                    return "Weather forced: Rain";
                default:
                    _provider.ReleaseForce();
                    return "Weather returned to auto";
            }
        }
    }
}
