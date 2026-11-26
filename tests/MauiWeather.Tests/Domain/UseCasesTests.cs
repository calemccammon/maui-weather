using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;
using MauiWeather.Domain.UseCases;
using MauiWeather.Tests.Testing;

namespace MauiWeather.Tests.Domain;

public class UseCasesTests
{
    private readonly FakeWeatherRepository _weather = new();
    private readonly FakeSettingsRepository _settings = new();

    [Fact]
    public async Task A_search_asks_for_the_query_without_the_spaces_around_it()
    {
        _weather.PlacesToReturn = [Samples.Asheville];

        IReadOnlyList<Place> places = await new SearchPlaces(_weather).InvokeAsync("  ashev ", CancellationToken.None);

        Assert.Equal([Samples.Asheville], places);
        Assert.Equal(["ashev"], _weather.SearchQueries);
    }

    [Fact]
    public async Task A_query_of_one_letter_finds_nothing_and_asks_nobody()
    {
        Assert.Empty(await new SearchPlaces(_weather).InvokeAsync(" a ", CancellationToken.None));
        Assert.Empty(_weather.SearchQueries);
    }

    [Fact]
    public async Task A_forecast_is_asked_for_at_the_place()
    {
        await new GetForecast(_weather).InvokeAsync(Samples.Asheville, CancellationToken.None);

        Assert.Equal([new ForecastRequest(35.6, -82.55)], _weather.ForecastRequests);
    }

    [Fact]
    public async Task A_forecast_is_asked_for_with_the_longitude_in_range()
    {
        await new GetForecast(_weather).InvokeAsync(new Place(10, 190), CancellationToken.None);

        Assert.Equal(-170, Assert.Single(_weather.ForecastRequests).Longitude);
    }

    [Theory]
    [InlineData(90.5, 0)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    [InlineData(0, double.NaN)]
    public async Task A_point_off_the_globe_is_refused_without_asking_the_repository(double latitude, double longitude)
    {
        WeatherException error = await Assert.ThrowsAsync<WeatherException>(
            () => new GetForecast(_weather).InvokeAsync(new Place(latitude, longitude), CancellationToken.None));

        Assert.Equal(WeatherFailure.InvalidPlace, error.Failure);
        Assert.Empty(_weather.ForecastRequests);
    }

    [Fact]
    public void Units_follow_the_devices_country_until_the_user_chooses()
    {
        Assert.Equal(Units.Imperial, new GetUnits(_settings).Invoke("US"));
        Assert.Equal(Units.Metric, new GetUnits(_settings).Invoke("FR"));

        new SetUnits(_settings).Invoke(Units.Metric);

        Assert.Equal(Units.Metric, new GetUnits(_settings).Invoke("US"));
    }
}