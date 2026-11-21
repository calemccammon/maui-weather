namespace MauiWeather.Domain.Models;

/// <summary>The weather right now at a place.</summary>
/// <param name="ObservedAt">
/// When these conditions were measured, on the place's own clock. It says
/// which calendar day is "today" there, whatever the device's clock says.
/// </param>
/// <param name="Temperature">In the units the forecast was asked for.</param>
/// <param name="FeelsLike">The apparent temperature.</param>
/// <param name="WindSpeed">In the units the forecast was asked for.</param>
/// <param name="Condition">The kind of weather.</param>
/// <param name="IsDay">Whether the sun is up there.</param>
public sealed record CurrentConditions(
    DateTime ObservedAt,
    double Temperature,
    double FeelsLike,
    double WindSpeed,
    WeatherCondition Condition,
    bool IsDay);

/// <summary>One day of the weekly forecast.</summary>
/// <param name="Date">The calendar day at the place.</param>
/// <param name="Low">The day's lowest temperature.</param>
/// <param name="High">The day's highest temperature.</param>
/// <param name="Condition">The kind of weather.</param>
/// <param name="PrecipitationChance">Highest chance of precipitation during the day, in percent.</param>
public sealed record DailyForecast(
    DateOnly Date,
    double Low,
    double High,
    WeatherCondition Condition,
    int? PrecipitationChance = null);

/// <summary>Current conditions and the days ahead for one place.</summary>
public sealed record Forecast(CurrentConditions Current, IReadOnlyList<DailyForecast> Days)
{
    /// <summary>Whether <paramref name="day"/> is the day the current conditions were measured on.</summary>
    public bool IsToday(DailyForecast day)
    {
        return day.Date == DateOnly.FromDateTime(Current.ObservedAt);
    }
}