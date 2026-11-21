namespace MauiWeather.Web.Views.Components;

/// <summary>How much of each edge of the map something else is covering, in pixels.</summary>
public readonly record struct MapInsets(double Top, double Right, double Bottom, double Left);

/// <summary>A point on the map.</summary>
public readonly record struct MapPoint(double Latitude, double Longitude);