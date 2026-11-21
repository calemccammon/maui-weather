using System.Globalization;

using MauiWeather.Domain.Models;
using MauiWeather.Presentation.Resources;

namespace MauiWeather.Presentation.Home.Models;

/// <summary>
/// A place as the screen shows it: in a search result, in the search bar, as
/// the forecast's heading and as the pin on the map.
/// </summary>
/// <param name="Title">"Asheville", or "35.60°N, 82.55°W" for a place with no name.</param>
/// <param name="Subtitle">"North Carolina, United States", or empty when nothing more is known.</param>
/// <param name="Latitude">Where the pin goes.</param>
/// <param name="Longitude">Where the pin goes.</param>
public sealed record PlaceUiModel(string Title, string Subtitle, double Latitude, double Longitude)
{
    /// <summary>Coordinates are shown to two decimal places of a degree: about a kilometre.</summary>
    private const int CoordinateDecimals = 2;

    public bool HasSubtitle => Subtitle.Length > 0;

    public static PlaceUiModel From(Place place)
    {
        string?[] parts = [place.Region, place.Country];
        return new PlaceUiModel(
            Title: place.Name ?? Coordinates(place.Latitude, place.Longitude),
            Subtitle: string.Join(", ", parts.Where(part => part is not null)),
            place.Latitude,
            place.Longitude);
    }

    /// <summary>"35.60°N, 82.55°W": what a place with no name is called.</summary>
    private static string Coordinates(double latitude, double longitude)
    {
        // The side is read off the rounded figure, so a point a hair south of
        // the equator is "0.00°N" like the equator itself.
        double north = Math.Round(latitude, CoordinateDecimals, MidpointRounding.AwayFromZero);
        double east = Math.Round(longitude, CoordinateDecimals, MidpointRounding.AwayFromZero);

        return Strings.Format(
            nameof(Strings.PlaceCoordinates),
            Strings.Format(north >= 0 ? nameof(Strings.PlaceNorth) : nameof(Strings.PlaceSouth), Degrees(north)),
            Strings.Format(east >= 0 ? nameof(Strings.PlaceEast) : nameof(Strings.PlaceWest), Degrees(east)));
    }

    private static string Degrees(double rounded)
    {
        return Math.Abs(rounded).ToString("F" + CoordinateDecimals, CultureInfo.CurrentCulture);
    }
}