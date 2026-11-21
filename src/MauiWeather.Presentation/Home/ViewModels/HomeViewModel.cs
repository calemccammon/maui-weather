using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;
using MauiWeather.Domain.UseCases;
using MauiWeather.Presentation.Core;
using MauiWeather.Presentation.Home.Models;

namespace MauiWeather.Presentation.Home.ViewModels;

/// <summary>
/// Holds the selected place and the chosen units, and loads the forecast.
/// </summary>
/// <remarks>
/// Selecting a place while an earlier forecast is still on its way gives the
/// earlier request up, so its answer never replaces the newer one.
/// </remarks>
public sealed partial class HomeViewModel : ObservableObject, IDisposable
{
    private readonly GetForecast _getForecast;
    private readonly SetUnits _setUnits;

    private Place? _place;
    private Units _units;

    /// <summary>Stops the forecast request under way, if any.</summary>
    private CancellationTokenSource? _loading;

    /// <param name="deviceCountry">The country the device is set to, such as "US", if it says.</param>
    public HomeViewModel(GetForecast getForecast, GetUnits getUnits, SetUnits setUnits, string? deviceCountry)
    {
        ArgumentNullException.ThrowIfNull(getUnits);
        _getForecast = getForecast;
        _setUnits = setUnits;
        _units = getUnits.Invoke(deviceCountry);
        Units = _units.ToUiModel();
    }

    /// <summary>The selected place, or null when there is none.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlace))]
    public partial PlaceUiModel? Place { get; private set; }

    [ObservableProperty]
    public partial UnitsUiModel Units { get; private set; }

    /// <summary>The selected place's forecast, once it has arrived.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasForecast))]
    public partial ForecastUiModel? Forecast { get; private set; }

    /// <summary>Whether the selected place's forecast is on its way.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    /// <summary>What went wrong getting the forecast, worded for the user, or null.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFailed))]
    public partial string? FailureMessage { get; private set; }

    public bool HasPlace => Place is not null;

    public bool HasForecast => Forecast is not null;

    public bool HasFailed => FailureMessage is not null;

    /// <summary>Makes <paramref name="place"/> the selected place and loads its forecast.</summary>
    public void Select(Place place)
    {
        _place = place;
        _ = LoadAsync();
    }

    /// <summary>Selects a point picked straight off the map.</summary>
    public void SelectPoint(double latitude, double longitude)
    {
        Select(Domain.Models.Place.FromCoordinates(latitude, longitude));
    }

    [RelayCommand]
    public void Clear()
    {
        StopLoading();
        _place = null;
        Show(place: null, forecast: null, isLoading: false, failureMessage: null);
    }

    /// <summary>
    /// Shows forecasts in <paramref name="chosen"/> from now on, and remembers
    /// the choice. The forecast on screen is asked for again in them.
    /// </summary>
    [RelayCommand]
    public void SetUnits(UnitsUiModel chosen)
    {
        Units units = chosen.ToDomain();
        if (units == _units)
        {
            return;
        }

        _units = units;
        Units = chosen;
        try
        {
            _setUnits.Invoke(units);
        }
        catch (SettingsException)
        {
            // Not saved, so the next launch starts from the old choice; this
            // one carries on with the new.
        }

        _ = LoadAsync();
    }

    /// <summary>Asks for the selected place's forecast again, after a failure.</summary>
    [RelayCommand]
    public void RetryForecast()
    {
        _ = LoadAsync();
    }

    public void Dispose()
    {
        StopLoading();
    }

    private async Task LoadAsync()
    {
        if (_place is not Place place)
        {
            return;
        }

        Units units = _units;
        StopLoading();
        CancellationTokenSource loading = new();
        _loading = loading;
        Show(PlaceUiModel.From(place), forecast: null, isLoading: true, failureMessage: null);

        try
        {
            Forecast forecast = await _getForecast.InvokeAsync(place, units, loading.Token);
            // A repository may answer although it was told to stop.
            if (!loading.IsCancellationRequested)
            {
                Show(Place, ForecastUiModel.From(forecast, units), isLoading: false, failureMessage: null);
            }
        }
        catch (OperationCanceledException)
        {
            // Given up for a newer request, which shows its own progress.
        }
        catch (WeatherException e) when (!loading.IsCancellationRequested)
        {
            Show(Place, forecast: null, isLoading: false, e.Failure.Message());
        }
    }

    private void StopLoading()
    {
        _loading?.Cancel();
        _loading?.Dispose();
        _loading = null;
    }

    private void Show(PlaceUiModel? place, ForecastUiModel? forecast, bool isLoading, string? failureMessage)
    {
        Place = place;
        Forecast = forecast;
        IsLoading = isLoading;
        FailureMessage = failureMessage;
    }
}