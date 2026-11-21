using MauiWeather.Data.Models;
using MauiWeather.Domain.Models;

namespace MauiWeather.Tests.Data;

public class WmoCodeTests
{
    [Theory]
    [InlineData(0, WeatherCondition.Clear)]
    [InlineData(1, WeatherCondition.MostlyClear)]
    [InlineData(2, WeatherCondition.PartlyCloudy)]
    [InlineData(3, WeatherCondition.Overcast)]
    [InlineData(48, WeatherCondition.Fog)]
    [InlineData(53, WeatherCondition.Drizzle)]
    [InlineData(57, WeatherCondition.FreezingDrizzle)]
    [InlineData(65, WeatherCondition.Rain)]
    [InlineData(66, WeatherCondition.FreezingRain)]
    [InlineData(77, WeatherCondition.Snow)]
    [InlineData(81, WeatherCondition.RainShowers)]
    [InlineData(86, WeatherCondition.SnowShowers)]
    [InlineData(99, WeatherCondition.Thunderstorm)]
    public void Each_group_of_codes_is_its_condition(int code, WeatherCondition expected)
    {
        Assert.Equal(expected, WmoCode.ToCondition(code));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(null)]
    public void A_code_outside_the_table_or_a_missing_one_is_unknown(int? code)
    {
        Assert.Equal(WeatherCondition.Unknown, WmoCode.ToCondition(code));
    }
}