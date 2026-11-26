using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;
using MauiWeather.Domain.UseCases;
using MauiWeather.Presentation.Home.Models;
using MauiWeather.Presentation.Home.ViewModels;
using MauiWeather.Tests.Testing;

namespace MauiWeather.Tests.Presentation;

public sealed class HomeViewModelTests
{
    private readonly FakeWeatherRepository _weather = new();
    private readonly FakeSettingsRepository _settings = new();

    private HomeViewModel ViewModel(string? deviceCountry = "US")
    {
        InlineContinuations.Use();
        return new HomeViewModel(new GetForecast(_weather), new GetUnits(_settings), new SetUnits(_settings), deviceCountry);
    }

    /// <summary>Everything the screen draws from, in one value.</summary>
    private static (string? Place, UnitsUiModel Units, string? Temperature, bool IsLoading, string? Failure) Shown(
        HomeViewModel viewModel)
    {
        return (
            viewModel.Place?.Title,
            viewModel.Units,
            viewModel.Forecast?.Current.Temperature,
            viewModel.IsLoading,
            viewModel.FailureMessage);
    }

    [Fact]
    public void It_starts_with_no_place_in_the_units_of_the_devices_country()
    {
        Assert.Equal((null, UnitsUiModel.Fahrenheit, null, false, null), Shown(ViewModel("US")));
        Assert.Equal((null, UnitsUiModel.Celsius, null, false, null), Shown(ViewModel("DE")));
    }

    [Fact]
    public void It_starts_in_the_units_the_user_chose_before_whatever_the_devices_country()
    {
        _settings.Units = Units.Metric;

        Assert.Equal(UnitsUiModel.Celsius, ViewModel("US").Units);
    }

    [Fact]
    public void Selecting_a_place_shows_it_at_once_and_its_forecast_when_that_arrives()
    {
        using HomeViewModel viewModel = ViewModel();
        _weather.ForecastToReturn = null;

        viewModel.Select(Samples.Asheville);

        Assert.Equal(("Asheville", UnitsUiModel.Fahrenheit, null, true, null), Shown(viewModel));

        _weather.Forecasts[0].SetResult(Samples.Forecast());

        Assert.Equal(("Asheville", UnitsUiModel.Fahrenheit, "36°F", false, null), Shown(viewModel));
        Assert.Equal([new ForecastRequest(35.6, -82.55)], _weather.ForecastRequests);
    }

    [Fact]
    public void A_point_picked_off_the_map_is_selected_by_its_coordinates()
    {
        using HomeViewModel viewModel = ViewModel();

        viewModel.SelectPoint(35.6, 277.45);

        Assert.Equal("35.60°N, 82.55°W", viewModel.Place?.Title);
        ForecastRequest request = Assert.Single(_weather.ForecastRequests);
        Assert.Equal(35.6, request.Latitude);
        Assert.Equal(-82.55, request.Longitude, precision: 9);
    }

    [Fact]
    public void A_failure_is_worded_for_the_user_and_another_try_can_succeed()
    {
        using HomeViewModel viewModel = ViewModel();
        _weather.Failure = new WeatherException(WeatherFailure.Timeout);

        viewModel.Select(Samples.Asheville);

        Assert.Equal(
            ("Asheville", UnitsUiModel.Fahrenheit, null, false, "The weather service took too long to answer."),
            Shown(viewModel));
        Assert.True(viewModel.HasFailed);

        _weather.Failure = null;
        viewModel.RetryForecastCommand.Execute(null);

        Assert.Equal(("Asheville", UnitsUiModel.Fahrenheit, "36°F", false, null), Shown(viewModel));
    }

    [Fact]
    public void The_answer_for_a_place_left_behind_never_replaces_the_newer_places()
    {
        using HomeViewModel viewModel = ViewModel();
        _weather.ForecastToReturn = null;

        viewModel.Select(Samples.Asheville);
        viewModel.Select(Samples.Sydney);
        // The fake answers even though the request was given up.
        _weather.Forecasts[0].TrySetResult(Samples.Forecast(temperature: 1));

        Assert.Equal(("Sydney", UnitsUiModel.Fahrenheit, null, true, null), Shown(viewModel));

        _weather.Forecasts[1].SetResult(Samples.Forecast(temperature: 21));

        Assert.Equal("70°F", viewModel.Forecast?.Current.Temperature);
    }

    [Fact]
    public void A_failure_for_a_place_left_behind_is_not_shown_either()
    {
        using HomeViewModel viewModel = ViewModel();
        _weather.ForecastToReturn = null;

        viewModel.Select(Samples.Asheville);
        viewModel.Select(Samples.Sydney);
        _weather.Forecasts[0].TrySetException(new WeatherException(WeatherFailure.Timeout));

        Assert.Equal(("Sydney", UnitsUiModel.Fahrenheit, null, true, null), Shown(viewModel));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Nor_does_the_answer_of_a_request_that_would_not_stop(bool succeeds)
    {
        using HomeViewModel viewModel = ViewModel();
        _weather.ForecastToReturn = null;
        _weather.IgnoresCancellation = true;

        viewModel.Select(Samples.Asheville);
        viewModel.Select(Samples.Sydney);
        if (succeeds)
        {
            _weather.Forecasts[0].SetResult(Samples.Forecast(temperature: 1));
        }
        else
        {
            _weather.Forecasts[0].SetException(new WeatherException(WeatherFailure.Timeout));
        }

        Assert.Equal(("Sydney", UnitsUiModel.Fahrenheit, null, true, null), Shown(viewModel));
    }

    [Fact]
    public void Clearing_the_place_goes_back_to_the_start_and_drops_the_forecast_on_its_way()
    {
        using HomeViewModel viewModel = ViewModel();
        _weather.ForecastToReturn = null;
        viewModel.Select(Samples.Asheville);

        viewModel.ClearCommand.Execute(null);
        _weather.Forecasts[0].TrySetResult(Samples.Forecast());

        Assert.Equal((null, UnitsUiModel.Fahrenheit, null, false, null), Shown(viewModel));
        Assert.False(viewModel.HasPlace);
    }

    [Fact]
    public void Choosing_other_units_converts_the_forecast_shown_without_asking_again_and_remembers_the_choice()
    {
        using HomeViewModel viewModel = ViewModel();
        viewModel.Select(Samples.Asheville);
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.SetUnitsCommand.Execute(UnitsUiModel.Celsius);

        Assert.Single(_weather.ForecastRequests);
        Assert.Equal(("Asheville", UnitsUiModel.Celsius, "2°C", false, null), Shown(viewModel));
        Assert.Equal("Wind 7 km/h", viewModel.Forecast?.Current.Wind);
        Assert.Equal(Units.Metric, _settings.Units);
        // Nothing is fetched, so nothing loads; and the place is the same one,
        // so the map has no reason to move.
        Assert.DoesNotContain(nameof(HomeViewModel.IsLoading), changed);
        Assert.DoesNotContain(nameof(HomeViewModel.Place), changed);

        viewModel.SetUnits(UnitsUiModel.Fahrenheit);

        Assert.Single(_weather.ForecastRequests);
        Assert.Equal(("Asheville", UnitsUiModel.Fahrenheit, "36°F", false, null), Shown(viewModel));
        Assert.Equal("Wind 4 mph", viewModel.Forecast?.Current.Wind);
    }

    [Fact]
    public void A_forecast_still_on_its_way_when_the_units_change_arrives_in_the_new_ones()
    {
        using HomeViewModel viewModel = ViewModel();
        _weather.ForecastToReturn = null;
        viewModel.Select(Samples.Asheville);

        viewModel.SetUnits(UnitsUiModel.Celsius);

        Assert.Equal(("Asheville", UnitsUiModel.Celsius, null, true, null), Shown(viewModel));

        _weather.Forecasts[0].SetResult(Samples.Forecast());

        Assert.Single(_weather.ForecastRequests);
        Assert.Equal(("Asheville", UnitsUiModel.Celsius, "2°C", false, null), Shown(viewModel));
    }

    [Fact]
    public void Choosing_the_units_already_shown_does_nothing()
    {
        using HomeViewModel viewModel = ViewModel();
        viewModel.Select(Samples.Asheville);

        viewModel.SetUnits(UnitsUiModel.Fahrenheit);

        Assert.Single(_weather.ForecastRequests);
        Assert.Null(_settings.Units);
    }

    [Fact]
    public void Units_can_be_chosen_before_any_place_is()
    {
        using HomeViewModel viewModel = ViewModel();

        viewModel.SetUnits(UnitsUiModel.Celsius);

        Assert.Equal((null, UnitsUiModel.Celsius, null, false, null), Shown(viewModel));
        Assert.Empty(_weather.ForecastRequests);
    }

    [Fact]
    public void Units_that_cannot_be_saved_are_still_used_for_now()
    {
        using HomeViewModel viewModel = ViewModel();
        _settings.RefusesToSave = true;
        viewModel.Select(Samples.Asheville);

        viewModel.SetUnits(UnitsUiModel.Celsius);

        Assert.Equal("2°C", viewModel.Forecast?.Current.Temperature);
        Assert.Null(_settings.Units);
    }

    [Fact]
    public void There_is_nothing_to_try_again_with_no_place_selected()
    {
        using HomeViewModel viewModel = ViewModel();

        viewModel.RetryForecast();

        Assert.Empty(_weather.ForecastRequests);
        Assert.False(viewModel.IsLoading);
    }
}