using MauiWeather.Domain.Models;
using MauiWeather.Tests.Testing;

namespace MauiWeather.Tests.Domain;

public class ModelsTests
{
    [Fact]
    public void A_place_picked_off_the_map_has_coordinates_and_no_name()
    {
        Place place = Place.FromCoordinates(35.6, -82.55);

        Assert.Equal(new Place(35.6, -82.55), place);
        Assert.Null(place.Name);
    }

    [Theory]
    [InlineData(190, -170)]
    [InlineData(-190, 170)]
    [InlineData(730, 10)]
    [InlineData(180, -180)]
    [InlineData(-180, -180)]
    [InlineData(-82.55, -82.55)]
    public void A_longitude_from_a_map_dragged_round_the_world_is_brought_back_into_range(double raw, double expected)
    {
        Assert.Equal(expected, Place.NormalizeLongitude(raw), precision: 9);
    }

    [Fact]
    public void Today_is_the_day_the_current_conditions_were_measured_on()
    {
        Forecast forecast = Samples.Forecast();

        Assert.True(forecast.IsToday(forecast.Days[0]));
        Assert.False(forecast.IsToday(forecast.Days[1]));
    }

    [Theory]
    [InlineData(10, 50)]
    [InlineData(0, 32)]
    [InlineData(-40, -40)]
    [InlineData(37.5, 99.5)]
    public void A_forecast_in_imperial_units_has_every_temperature_in_Fahrenheit(double celsius, double fahrenheit)
    {
        DailyForecast day = new(new DateOnly(2026, 11, 18), Low: celsius, High: celsius + 10, WeatherCondition.Clear);
        Forecast metric = new(Samples.Forecast().Current with { Temperature = celsius, FeelsLike = celsius - 10 }, [day]);

        Forecast imperial = metric.InUnits(Units.Imperial);

        Assert.Equal(fahrenheit, imperial.Current.Temperature, precision: 9);
        Assert.Equal(fahrenheit - 18, imperial.Current.FeelsLike, precision: 9);
        Assert.Equal(fahrenheit, imperial.Days[0].Low, precision: 9);
        Assert.Equal(fahrenheit + 18, imperial.Days[0].High, precision: 9);
    }

    [Fact]
    public void A_forecast_in_imperial_units_has_its_wind_in_mph_and_the_rest_unchanged()
    {
        Forecast metric = Samples.Forecast() with { Current = Samples.Forecast().Current with { WindSpeed = 20 } };

        Forecast imperial = metric.InUnits(Units.Imperial);

        Assert.Equal(12.42742, imperial.Current.WindSpeed, precision: 9);
        Assert.Equal(
            metric.Days.Select(day => (day.Date, day.Condition, day.PrecipitationChance)),
            imperial.Days.Select(day => (day.Date, day.Condition, day.PrecipitationChance)));
        Assert.Equal(
            (metric.Current.ObservedAt, metric.Current.Condition, metric.Current.IsDay),
            (imperial.Current.ObservedAt, imperial.Current.Condition, imperial.Current.IsDay));
    }

    [Fact]
    public void A_forecast_in_metric_units_is_the_one_fetched()
    {
        Forecast metric = Samples.Forecast();

        Assert.Same(metric, metric.InUnits(Units.Metric));
    }

    [Theory]
    [InlineData("US", Units.Imperial)]
    [InlineData("LR", Units.Imperial)]
    [InlineData("MM", Units.Imperial)]
    [InlineData("GB", Units.Metric)]
    [InlineData(null, Units.Metric)]
    public void Only_the_countries_that_use_Fahrenheit_start_in_imperial_units(string? country, Units expected)
    {
        Assert.Equal(expected, UnitsRules.ForCountry(country));
    }
}