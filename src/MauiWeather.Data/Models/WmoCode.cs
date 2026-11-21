using MauiWeather.Domain.Models;

namespace MauiWeather.Data.Models;

internal static class WmoCode
{
    /// <summary>
    /// What a WMO weather code, as Open-Meteo reports it, means.
    /// </summary>
    /// <remarks>
    /// The table is on https://open-meteo.com/en/docs under "WMO Weather
    /// interpretation codes". A code outside it is <see cref="WeatherCondition.Unknown"/>.
    /// </remarks>
    public static WeatherCondition ToCondition(int? code)
    {
        return code switch
        {
            0 => WeatherCondition.Clear,
            1 => WeatherCondition.MostlyClear,
            2 => WeatherCondition.PartlyCloudy,
            3 => WeatherCondition.Overcast,
            45 or 48 => WeatherCondition.Fog,
            51 or 53 or 55 => WeatherCondition.Drizzle,
            56 or 57 => WeatherCondition.FreezingDrizzle,
            61 or 63 or 65 => WeatherCondition.Rain,
            66 or 67 => WeatherCondition.FreezingRain,
            71 or 73 or 75 or 77 => WeatherCondition.Snow,
            80 or 81 or 82 => WeatherCondition.RainShowers,
            85 or 86 => WeatherCondition.SnowShowers,
            95 or 96 or 99 => WeatherCondition.Thunderstorm,
            _ => WeatherCondition.Unknown,
        };
    }
}