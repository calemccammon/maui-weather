using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using MauiWeather.Data.Models;
using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Data.Repositories;

/// <summary>
/// Places and forecasts from Open-Meteo. No API key is needed.
/// </summary>
/// <remarks>
/// Asks the service, reads its answers into the wire-shaped models in
/// <c>Models</c>, and hands back domain models. Every way this can go wrong
/// leaves as a <see cref="WeatherException"/>. How long to wait for an answer
/// is the <see cref="HttpClient.Timeout"/> of the client it is given.
/// <para>
/// Docs: https://open-meteo.com/en/docs and
/// https://open-meteo.com/en/docs/geocoding-api
/// </para>
/// </remarks>
public sealed class OpenMeteoWeatherRepository(HttpClient client) : IWeatherRepository
{
    /// <summary>How long to wait for an answer before giving up.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";
    private const int MaxResults = 8;
    private const int ForecastDays = 7;

    public async Task<IReadOnlyList<Place>> SearchPlacesAsync(string query, CancellationToken cancellationToken)
    {
        string url = $"{GeocodingUrl}?name={Uri.EscapeDataString(query)}&count={MaxResults}&language=en&format=json";
        string body = await GetAsync(url, cancellationToken).ConfigureAwait(false);

        PlaceSearchDto response = Read(body, OpenMeteoJson.Default.PlaceSearchDto);
        return [.. (response.Results ?? []).Select(place => place.ToDomain())];
    }

    public async Task<Forecast> GetForecastAsync(
        double latitude,
        double longitude,
        Units units,
        CancellationToken cancellationToken)
    {
        (string temperature, string wind) = units switch
        {
            Units.Imperial => ("fahrenheit", "mph"),
            _ => ("celsius", "kmh"),
        };
        // Numbers are written the same way whatever the device's language.
        string point = string.Create(CultureInfo.InvariantCulture, $"latitude={latitude}&longitude={longitude}");
        string url = $"{ForecastUrl}?{point}"
            + "&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m,is_day"
            + "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max"
            + $"&forecast_days={ForecastDays}&timezone=auto"
            + $"&temperature_unit={temperature}&wind_speed_unit={wind}";
        string body = await GetAsync(url, cancellationToken).ConfigureAwait(false);

        ForecastDto response = Read(body, OpenMeteoJson.Default.ForecastDto);
        try
        {
            return response.ToDomain();
        }
        catch (FormatException e)
        {
            throw new WeatherException(WeatherFailure.BadResponse, e.Message, e);
        }
    }

    /// <summary>Reads <paramref name="body"/>, reporting one of the wrong shape as a bad response.</summary>
    private static T Read<T>(string body, JsonTypeInfo<T> type)
    {
        try
        {
            return JsonSerializer.Deserialize(body, type)
                ?? throw new WeatherException(WeatherFailure.BadResponse, "the response is null");
        }
        catch (JsonException e)
        {
            throw new WeatherException(WeatherFailure.BadResponse, e.Message, e);
        }
    }

    /// <summary>The body of a successful answer from <paramref name="url"/>.</summary>
    private async Task<string> GetAsync(string url, CancellationToken cancellationToken)
    {
        HttpStatusCode status;
        string body;
        try
        {
            using HttpResponseMessage response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            status = response.StatusCode;
            body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            // Nobody asked for the request to stop, so the client's own
            // timeout stopped it.
            throw new WeatherException(WeatherFailure.Timeout, e.Message, e);
        }
        catch (HttpRequestException e)
        {
            // A dropped socket, a failed TLS handshake, a refused connection.
            throw new WeatherException(WeatherFailure.NoConnection, e.Message, e);
        }

        // The service's own trouble, or too many requests: its answer, often a
        // proxy's error page, is not read at all.
        if (status >= HttpStatusCode.InternalServerError || status == HttpStatusCode.TooManyRequests)
        {
            throw new WeatherException(WeatherFailure.Unavailable, $"HTTP {(int)status}");
        }

        // Bad input comes back as HTTP 400 with {"error": true, "reason": "..."}.
        if (status != HttpStatusCode.OK)
        {
            throw new WeatherException(WeatherFailure.Rejected, ReasonIn(body) ?? $"HTTP {(int)status}");
        }

        return body;
    }

    private static string? ReasonIn(string body)
    {
        try
        {
            return JsonSerializer.Deserialize(body, OpenMeteoJson.Default.ErrorDto)?.Reason;
        }
        catch (JsonException)
        {
            // An error page that is not JSON has no reason to give.
            return null;
        }
    }
}