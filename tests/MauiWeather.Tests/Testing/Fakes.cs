using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Tests.Testing;

internal static class Samples
{
    public static readonly Place Asheville = new(35.6, -82.55, "Asheville", "North Carolina", "United States");

    public static readonly Place Sydney = new(-33.87, 151.21, "Sydney", Country: "Australia");

    /// <summary>
    /// A Wednesday morning, then a week whose first day is that Wednesday, in
    /// metric units as fetched. In imperial units it is 36°F, feels like 31°,
    /// wind 4 mph, and the days run from 30° to 64°, 42° to 58° and 29° to 64°.
    /// </summary>
    public static Forecast Forecast(double temperature = 2.1, IReadOnlyList<DailyForecast>? days = null)
    {
        return new Forecast(
            new CurrentConditions(
                ObservedAt: new DateTime(2026, 11, 18, 9, 0, 0),
                Temperature: temperature,
                FeelsLike: -0.4,
                WindSpeed: 7,
                Condition: WeatherCondition.Clear,
                IsDay: true),
            days ??
            [
                new DailyForecast(new DateOnly(2026, 11, 18), Low: -1, High: 18, WeatherCondition.Clear),
                new DailyForecast(new DateOnly(2026, 11, 19), Low: 5.5, High: 14.5, WeatherCondition.Rain, 47),
                new DailyForecast(new DateOnly(2026, 11, 22), Low: -1.9, High: 18, WeatherCondition.Rain, 80),
            ]);
    }
}

internal sealed record ForecastRequest(double Latitude, double Longitude);

/// <summary>
/// A repository that answers from memory and records what it was asked.
/// </summary>
/// <remarks>
/// It answers at once with <see cref="PlacesToReturn"/> and
/// <see cref="ForecastToReturn"/>. Set either to null and each request waits
/// in <see cref="Searches"/> or <see cref="Forecasts"/> for the test to
/// complete it by hand.
/// </remarks>
internal sealed class FakeWeatherRepository : IWeatherRepository
{
    public IReadOnlyList<Place>? PlacesToReturn { get; set; } = [];

    public Forecast? ForecastToReturn { get; set; } = Samples.Forecast();

    /// <summary>When set, every request fails with it.</summary>
    public WeatherException? Failure { get; set; }

    /// <summary>
    /// When true, a request that is told to stop carries on regardless, and
    /// can still be answered by hand. A real one may do the same if its
    /// answer and the order to stop cross.
    /// </summary>
    public bool IgnoresCancellation { get; set; }

    public List<string> SearchQueries { get; } = [];

    public List<ForecastRequest> ForecastRequests { get; } = [];

    public List<TaskCompletionSource<IReadOnlyList<Place>>> Searches { get; } = [];

    public List<TaskCompletionSource<Forecast>> Forecasts { get; } = [];

    public Task<IReadOnlyList<Place>> SearchPlacesAsync(string query, CancellationToken cancellationToken)
    {
        SearchQueries.Add(query);
        return Answer(PlacesToReturn, Searches, cancellationToken);
    }

    public Task<Forecast> GetForecastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        ForecastRequests.Add(new ForecastRequest(latitude, longitude));
        return Answer(ForecastToReturn, Forecasts, cancellationToken);
    }

    private Task<T> Answer<T>(T? ready, List<TaskCompletionSource<T>> waiting, CancellationToken cancellationToken)
        where T : class
    {
        if (Failure is not null)
        {
            return Task.FromException<T>(Failure);
        }

        if (ready is not null)
        {
            return Task.FromResult(ready);
        }

        // Continuations run inline, so completing a request by hand has
        // taken effect by the time the test's next line runs.
        TaskCompletionSource<T> pending = new();
        if (!IgnoresCancellation)
        {
            cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
        }

        waiting.Add(pending);
        return pending.Task;
    }
}

internal sealed class FakeSettingsRepository(Units? units = null) : ISettingsRepository
{
    public Units? Units { get; set; } = units;

    /// <summary>When true, saving fails as a full or forbidden store would.</summary>
    public bool RefusesToSave { get; set; }

    public void SaveUnits(Units units)
    {
        if (RefusesToSave)
        {
            throw new SettingsException();
        }

        Units = units;
    }
}