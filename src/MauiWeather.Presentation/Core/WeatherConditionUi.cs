using MauiWeather.Domain.Models;
using MauiWeather.Presentation.Resources;

namespace MauiWeather.Presentation.Core;

/// <summary>How each weather condition is worded and drawn.</summary>
public static class WeatherConditionUi
{
    public static string Label(this WeatherCondition condition)
    {
        return condition switch
        {
            WeatherCondition.Clear => Strings.ConditionClear,
            WeatherCondition.MostlyClear => Strings.ConditionMostlyClear,
            WeatherCondition.PartlyCloudy => Strings.ConditionPartlyCloudy,
            WeatherCondition.Overcast => Strings.ConditionOvercast,
            WeatherCondition.Fog => Strings.ConditionFog,
            WeatherCondition.Drizzle => Strings.ConditionDrizzle,
            WeatherCondition.FreezingDrizzle => Strings.ConditionFreezingDrizzle,
            WeatherCondition.Rain => Strings.ConditionRain,
            WeatherCondition.FreezingRain => Strings.ConditionFreezingRain,
            WeatherCondition.Snow => Strings.ConditionSnow,
            WeatherCondition.RainShowers => Strings.ConditionRainShowers,
            WeatherCondition.SnowShowers => Strings.ConditionSnowShowers,
            WeatherCondition.Thunderstorm => Strings.ConditionThunderstorm,
            _ => Strings.ConditionUnknown,
        };
    }

    /// <summary>The condition's icon, as a character of the font in <see cref="Glyphs"/>.</summary>
    public static string Icon(this WeatherCondition condition, bool isDay = true)
    {
        return condition switch
        {
            WeatherCondition.Clear or WeatherCondition.MostlyClear => isDay ? Glyphs.Sunny : Glyphs.ClearNight,
            WeatherCondition.PartlyCloudy => isDay ? Glyphs.PartlyCloudy : Glyphs.PartlyCloudyNight,
            WeatherCondition.Overcast => Glyphs.Cloud,
            WeatherCondition.Fog => Glyphs.Fog,
            WeatherCondition.Drizzle or WeatherCondition.FreezingDrizzle => Glyphs.Drizzle,
            WeatherCondition.Rain or WeatherCondition.FreezingRain => Glyphs.Rain,
            WeatherCondition.RainShowers => Glyphs.Showers,
            WeatherCondition.Snow or WeatherCondition.SnowShowers => Glyphs.Snow,
            WeatherCondition.Thunderstorm => Glyphs.Thunderstorm,
            _ => Glyphs.Unknown,
        };
    }
}