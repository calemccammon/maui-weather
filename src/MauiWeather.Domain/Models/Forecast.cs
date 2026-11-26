namespace MauiWeather.Domain.Models;

/// <summary>The weather right now at a place.</summary>
/// <param name="ObservedAt">
/// When these conditions were measured, on the place's own clock. It says
/// which calendar day is "today" there, whatever the device's clock says.
/// </param>
/// <param name="Temperature">In degrees Celsius, as fetched; see <see cref="Forecast.InUnits"/>.</param>
/// <param name="FeelsLike">The apparent temperature.</param>
/// <param name="WindSpeed">In kilometres an hour, as fetched.</param>
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
/// <remarks>
/// A forecast is fetched and kept in metric units: degrees Celsius and
/// kilometres an hour. <see cref="InUnits"/> gives the same forecast in
/// another system, so changing units never needs the network.
/// </remarks>
public sealed record Forecast(CurrentConditions Current, IReadOnlyList<DailyForecast> Days)
{
    private const double MilesPerKilometre = 0.621371;
    private const double FahrenheitPerCelsius = 1.8;
    private const double FreezingFahrenheit = 32;

    /// <summary>This metric forecast with its temperatures and wind speed in <paramref name="units"/>.</summary>
    /// <remarks>The chance of precipitation is a percentage, the same in any units.</remarks>
    public Forecast InUnits(Units units)
    {
        return units switch
        {
            Units.Imperial => new Forecast(
                Current with
                {
                    Temperature = Fahrenheit(Current.Temperature),
                    FeelsLike = Fahrenheit(Current.FeelsLike),
                    WindSpeed = Current.WindSpeed * MilesPerKilometre,
                },
                [.. Days.Select(day => day with { Low = Fahrenheit(day.Low), High = Fahrenheit(day.High) })]),
            _ => this,
        };
    }

    /// <summary>Whether <paramref name="day"/> is the day the current conditions were measured on.</summary>
    public bool IsToday(DailyForecast day)
    {
        return day.Date == DateOnly.FromDateTime(Current.ObservedAt);
    }

    private static double Fahrenheit(double celsius)
    {
        return (celsius * FahrenheitPerCelsius) + FreezingFahrenheit;
    }
}