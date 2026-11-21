using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Manipulations;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Maui;

using MauiWeather.Presentation.Home.Models;
using MauiWeather.Presentation.Resources;

using Color = Microsoft.Maui.Graphics.Color;
using Map = Mapsui.Map;

namespace MauiWeather.Views.Components;

/// <summary>Where on the map the user pressed and held.</summary>
public sealed class PointPickedEventArgs(double latitude, double longitude) : EventArgs
{
    public double Latitude { get; } = latitude;

    public double Longitude { get; } = longitude;
}

/// <summary>
/// The map behind everything else. A press-and-hold picks the point under it.
/// </summary>
public sealed partial class WeatherMap : ContentView, IDisposable
{
    public static readonly BindableProperty PlaceProperty = BindableProperty.Create(
        nameof(Place),
        typeof(PlaceUiModel),
        typeof(WeatherMap),
        propertyChanged: (map, _, _) => ((WeatherMap)map).ShowPlace());

    public static readonly BindableProperty CoveredProperty = BindableProperty.Create(
        nameof(Covered),
        typeof(Thickness),
        typeof(WeatherMap),
        default(Thickness),
        // What covers the map changes when the window does, and the map must
        // take its new size before it is moved, or the move is lost.
        propertyChanged: (map, _, _) => map.Dispatcher.Dispatch(((WeatherMap)map).CentreOnPlace));

    public static readonly BindableProperty PinColorProperty = BindableProperty.Create(
        nameof(PinColor),
        typeof(Color),
        typeof(WeatherMap),
        Colors.Black,
        propertyChanged: (map, _, _) => ((WeatherMap)map).ShowPlace());

    private const string UserAgent = "com.calemccammon.weather.maui";
    private const double InitialLatitude = 25;
    private const double InitialZoom = 2;

    /// <summary>How far in the map goes when a place is chosen from further out.</summary>
    private const double PlaceZoom = 10;

    private const long MoveMilliseconds = 600;
    private const double PinSize = 48;

    /// <summary>
    /// Metres across one point of screen at zoom 0, at the equator, in the
    /// projection web maps use. Each zoom level halves it.
    /// </summary>
    private const double ResolutionAtZoomZero = 156543.03392804097;

    private readonly Map _map = new();
    private readonly MemoryLayer _pinLayer = new("Pin");

    public WeatherMap()
    {
        TileLayer tiles = OpenStreetMap.CreateTileLayer(UserAgent);
        // The layer would draw its credit in the map's corner, where the
        // forecast covers it. The page draws the credit where it shows.
        tiles.Attribution.Enabled = false;
        _map.Layers.Add(tiles);
        _map.Layers.Add(_pinLayer);
        // The map's own overlays: a debug log, and zoom buttons.
        _map.Widgets.Clear();
        _map.Navigator.RotationLock = true;
        _map.Navigator.CenterOnAndZoomTo(
            SphericalMercator.FromLonLat(0, InitialLatitude).ToMPoint(),
            ResolutionAt(InitialZoom));
        _map.Tapped += OnMapTapped;

        MapControl control = new() { Map = _map };
        SemanticProperties.SetDescription(control, Strings.MapLabel);
        Content = control;
    }

    /// <summary>The user pressed and held on the map.</summary>
    public event EventHandler<PointPickedEventArgs>? PointPicked;

    /// <summary>The selected place, which gets the pin, or null for no pin.</summary>
    public PlaceUiModel? Place
    {
        get => (PlaceUiModel?)GetValue(PlaceProperty);
        set => SetValue(PlaceProperty, value);
    }

    /// <summary>
    /// How much of each edge of the map the search bar and the forecast
    /// cover. The pin is brought to the middle of what is left.
    /// </summary>
    public Thickness Covered
    {
        get => (Thickness)GetValue(CoveredProperty);
        set => SetValue(CoveredProperty, value);
    }

    public Color PinColor
    {
        get => (Color)GetValue(PinColorProperty);
        set => SetValue(PinColorProperty, value);
    }

    public void Dispose()
    {
        _map.Tapped -= OnMapTapped;
        _map.Dispose();
        _pinLayer.Dispose();
    }

    private static double ResolutionAt(double zoom)
    {
        return ResolutionAtZoomZero / Math.Pow(2, zoom);
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        if (e.GestureType != GestureType.LongPress)
        {
            return;
        }

        (double longitude, double latitude) = SphericalMercator.ToLonLat(e.WorldPosition.X, e.WorldPosition.Y);
        PointPicked?.Invoke(this, new PointPickedEventArgs(latitude, longitude));
        e.Handled = true;
    }

    private void ShowPlace()
    {
        if (Place is PlaceUiModel place)
        {
            (double x, double y) = SphericalMercator.FromLonLat(place.Longitude, place.Latitude);
            _pinLayer.Features = [new PointFeature(x, y)];
            _pinLayer.Style = PinStyle();
        }
        else
        {
            _pinLayer.Features = [];
        }

        _pinLayer.DataHasChanged();
        CentreOnPlace();
    }

    /// <summary>
    /// Moves the map so the selected place sits in the middle of the part of
    /// it that nothing covers.
    /// </summary>
    private void CentreOnPlace()
    {
        if (Place is not PlaceUiModel place)
        {
            return;
        }

        double resolution = Math.Min(_map.Navigator.Viewport.Resolution, ResolutionAt(PlaceZoom));
        (double x, double y) = SphericalMercator.FromLonLat(place.Longitude, place.Latitude);
        // The middle of the clear part is off the middle of the map by half
        // the difference between what covers opposite edges.
        double right = (Covered.Left - Covered.Right) / 2;
        double down = (Covered.Top - Covered.Bottom) / 2;
        MPoint centre = new(x - (right * resolution), y + (down * resolution));

        _map.Navigator.CenterOnAndZoomTo(centre, resolution, MoveMilliseconds, Mapsui.Animations.Easing.CubicOut);
    }

    /// <summary>Material's "location on" pin, in the theme's colour, with its tip on the point.</summary>
    private ImageStyle PinStyle()
    {
        string svg =
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{PinSize}\" height=\"{PinSize}\" viewBox=\"0 0 24 24\">"
            + $"<path fill=\"{PinColor.ToHex()}\" d=\"M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7z"
            + "m0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z\"/></svg>";
        return new ImageStyle
        {
            Image = new Mapsui.Styles.Image { Source = "svg-content://" + svg },
            // Half the image's height up, so the bottom of the pin is on the point.
            RelativeOffset = new RelativeOffset(0, 0.5),
        };
    }
}