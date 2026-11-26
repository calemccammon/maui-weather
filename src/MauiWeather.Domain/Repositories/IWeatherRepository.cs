using MauiWeather.Domain.Models;

namespace MauiWeather.Domain.Repositories;

/// <summary>
/// Where the app gets places and forecasts from.
/// </summary>
/// <remarks>
/// The domain only says what it needs. Which service answers, and what that
/// service's responses look like, is the data layer's business. Both calls
/// fail with a <see cref="WeatherException"/>, or are cancelled, and nothing
/// else.
/// </remarks>
public interface IWeatherRepository
{
    /// <summary>Places whose name matches <paramref name="query"/>, best match first.</summary>
    Task<IReadOnlyList<Place>> SearchPlacesAsync(string query, CancellationToken cancellationToken);

    /// <summary>Current conditions and the days ahead at a point, in metric units.</summary>
    Task<Forecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken);
}