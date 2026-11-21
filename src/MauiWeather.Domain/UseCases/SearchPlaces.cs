using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Domain.UseCases;

/// <summary>Finds places by name.</summary>
public sealed class SearchPlaces(IWeatherRepository repository)
{
    /// <summary>A query shorter than this is not worth sending: one letter matches nothing useful.</summary>
    public const int MinQueryLength = 2;

    /// <summary>Whether <paramref name="query"/> is long enough to search for, spaces around it aside.</summary>
    public static bool IsSearchable(string query)
    {
        return query.Trim().Length >= MinQueryLength;
    }

    /// <summary>
    /// Places matching <paramref name="query"/>, best match first. Empty,
    /// without asking the repository, when the query is too short.
    /// </summary>
    public async Task<IReadOnlyList<Place>> InvokeAsync(string query, CancellationToken cancellationToken)
    {
        if (!IsSearchable(query))
        {
            return [];
        }

        return await repository.SearchPlacesAsync(query.Trim(), cancellationToken).ConfigureAwait(false);
    }
}