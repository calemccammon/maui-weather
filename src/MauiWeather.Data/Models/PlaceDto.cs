using System.Text.Json.Serialization;

using MauiWeather.Domain.Models;

namespace MauiWeather.Data.Models;

/// <summary>The geocoding response as Open-Meteo sends it.</summary>
/// <param name="Results">With no matches the key is missing altogether.</param>
internal sealed record PlaceSearchDto(
    [property: JsonPropertyName("results")] IReadOnlyList<PlaceDto>? Results);

/// <summary>One entry of the geocoding response's <c>results</c>.</summary>
/// <param name="Admin1">First-level administrative area: a state, province or similar.</param>
internal sealed record PlaceDto(
    [property: JsonPropertyName("name"), JsonRequired] string Name,
    [property: JsonPropertyName("latitude"), JsonRequired] double Latitude,
    [property: JsonPropertyName("longitude"), JsonRequired] double Longitude,
    [property: JsonPropertyName("admin1")] string? Admin1,
    [property: JsonPropertyName("country")] string? Country)
{
    public Place ToDomain()
    {
        return new Place(Latitude, Longitude, Name, Region: Admin1, Country: Country);
    }
}