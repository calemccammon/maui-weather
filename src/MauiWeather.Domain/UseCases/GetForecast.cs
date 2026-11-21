using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Domain.UseCases;

/// <summary>Gets the forecast for a place.</summary>
public sealed class GetForecast(IWeatherRepository repository)
{
    private const double MaxLatitude = 90;

    /// <summary>
    /// The forecast at <paramref name="place"/> in <paramref name="units"/>.
    /// </summary>
    /// <remarks>
    /// The longitude is brought into range first (see
    /// <see cref="Place.NormalizeLongitude"/>). A point off the globe fails
    /// with <see cref="WeatherFailure.InvalidPlace"/> without asking the
    /// repository.
    /// </remarks>
    public async Task<Forecast> InvokeAsync(Place place, Units units, CancellationToken cancellationToken)
    {
        // Written so that a NaN fails the check too.
        bool onGlobe = Math.Abs(place.Latitude) <= MaxLatitude && double.IsFinite(place.Longitude);
        if (!onGlobe)
        {
            throw new WeatherException(WeatherFailure.InvalidPlace);
        }

        return await repository
            .GetForecastAsync(place.Latitude, Place.NormalizeLongitude(place.Longitude), units, cancellationToken)
            .ConfigureAwait(false);
    }
}