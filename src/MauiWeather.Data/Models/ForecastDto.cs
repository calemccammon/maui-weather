using System.Globalization;
using System.Text.Json.Serialization;

using MauiWeather.Domain.Models;

namespace MauiWeather.Data.Models;

/// <summary>
/// The forecast response as Open-Meteo sends it: one <c>current</c> object
/// and a <c>daily</c> object of parallel columns.
/// </summary>
internal sealed record ForecastDto(
    [property: JsonPropertyName("current"), JsonRequired] CurrentDto Current,
    [property: JsonPropertyName("daily"), JsonRequired] DailyDto Daily)
{
    /// <summary>
    /// Translates the wire shape into what the app works with: codes become
    /// conditions, columns become one entry per day, and a day the weather
    /// model has no temperatures for is left out.
    /// </summary>
    /// <exception cref="FormatException">
    /// A date or time is not ISO 8601, or the columns differ in length.
    /// </exception>
    public Forecast ToDomain()
    {
        int count = Daily.Time.Count;
        IReadOnlyList<double?>? chances = Daily.PrecipitationProbabilityMax;
        if (Daily.WeatherCode.Count != count
            || Daily.TemperatureMax.Count != count
            || Daily.TemperatureMin.Count != count
            || (chances is not null && chances.Count != count))
        {
            throw new FormatException("daily columns differ in length");
        }

        List<DailyForecast> days = [];
        for (int i = 0; i < count; i++)
        {
            if (Daily.TemperatureMax[i] is not double high || Daily.TemperatureMin[i] is not double low)
            {
                continue;
            }

            days.Add(new DailyForecast(
                Date: DateOnly.ParseExact(Daily.Time[i], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Low: low,
                High: high,
                Condition: WmoCode.ToCondition(Daily.WeatherCode[i]),
                PrecipitationChance: chances?[i] is double chance
                    ? (int)Math.Round(chance, MidpointRounding.AwayFromZero)
                    : null));
        }

        return new Forecast(
            new CurrentConditions(
                ObservedAt: DateTime.ParseExact(Current.Time, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture),
                Temperature: Current.Temperature,
                FeelsLike: Current.ApparentTemperature,
                WindSpeed: Current.WindSpeed,
                Condition: WmoCode.ToCondition(Current.WeatherCode),
                IsDay: Current.IsDay == 1),
            days);
    }
}

/// <summary>The response's <c>current</c> object.</summary>
/// <param name="Time">ISO date and time, local to the place: <c>2026-11-18T09:00</c>.</param>
/// <param name="IsDay">1 in daylight, 0 at night.</param>
internal sealed record CurrentDto(
    [property: JsonPropertyName("time"), JsonRequired] string Time,
    [property: JsonPropertyName("temperature_2m"), JsonRequired] double Temperature,
    [property: JsonPropertyName("apparent_temperature"), JsonRequired] double ApparentTemperature,
    [property: JsonPropertyName("weather_code"), JsonRequired] int WeatherCode,
    [property: JsonPropertyName("wind_speed_10m"), JsonRequired] double WindSpeed,
    [property: JsonPropertyName("is_day"), JsonRequired] int IsDay);

/// <summary>
/// The response's <c>daily</c> object: a list of dates and one list per
/// variable, all the same length. A value the weather model lacks is null.
/// </summary>
/// <param name="Time">ISO dates, local to the place: <c>2026-11-18</c>.</param>
internal sealed record DailyDto(
    [property: JsonPropertyName("time"), JsonRequired] IReadOnlyList<string> Time,
    [property: JsonPropertyName("weather_code"), JsonRequired] IReadOnlyList<int?> WeatherCode,
    [property: JsonPropertyName("temperature_2m_max"), JsonRequired] IReadOnlyList<double?> TemperatureMax,
    [property: JsonPropertyName("temperature_2m_min"), JsonRequired] IReadOnlyList<double?> TemperatureMin,
    [property: JsonPropertyName("precipitation_probability_max")] IReadOnlyList<double?>? PrecipitationProbabilityMax);

/// <summary>What the service sends with an error status.</summary>
internal sealed record ErrorDto([property: JsonPropertyName("reason")] string? Reason);

/// <summary>
/// Reads the models above without reflection, so the app can be trimmed for
/// release.
/// </summary>
[JsonSerializable(typeof(PlaceSearchDto))]
[JsonSerializable(typeof(ForecastDto))]
[JsonSerializable(typeof(ErrorDto))]
internal sealed partial class OpenMeteoJson : JsonSerializerContext;