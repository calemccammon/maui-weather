namespace MauiWeather.Domain.Models;

/// <summary>A point on the map.</summary>
/// <param name="Latitude">Degrees north of the equator.</param>
/// <param name="Longitude">Degrees east of Greenwich.</param>
/// <param name="Name">What the place is called. A point picked straight off the map has none.</param>
/// <param name="Region">State, province or similar. Tells two places of one name apart.</param>
/// <param name="Country">The country's name.</param>
public sealed record Place(
    double Latitude,
    double Longitude,
    string? Name = null,
    string? Region = null,
    string? Country = null)
{
    private const double HalfTurn = 180;
    private const double FullTurn = 360;

    /// <summary>
    /// A place picked straight off the map, which has no name.
    /// </summary>
    /// <remarks>
    /// A map that has been dragged round the world more than once reports
    /// longitudes past 180 degrees, so the longitude is brought back into range.
    /// </remarks>
    public static Place FromCoordinates(double latitude, double rawLongitude)
    {
        return new Place(latitude, NormalizeLongitude(rawLongitude));
    }

    /// <summary>The same meridian between -180 (inclusive) and 180.</summary>
    public static double NormalizeLongitude(double longitude)
    {
        // C#'s % keeps the sign of what is divided, so the remainder is made
        // positive before it is shifted back.
        double turned = (longitude + HalfTurn) % FullTurn;
        return (turned < 0 ? turned + FullTurn : turned) - HalfTurn;
    }
}