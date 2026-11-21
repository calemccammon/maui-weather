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