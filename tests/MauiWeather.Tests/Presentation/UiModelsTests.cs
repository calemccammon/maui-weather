using MauiWeather.Domain.Models;
using MauiWeather.Presentation.Core;
using MauiWeather.Presentation.Home.Models;
using MauiWeather.Tests.Testing;

namespace MauiWeather.Tests.Presentation;

public class UiModelsTests
{
    private readonly ForecastUiModel _imperial = ForecastUiModel.From(Samples.Forecast(), Units.Imperial);

    private static string TitleOf(double latitude, double longitude)
    {
        return PlaceUiModel.From(Place.FromCoordinates(latitude, longitude)).Title;
    }

    // Places.

    [Fact]
    public void A_named_place_shows_its_name_with_its_region_and_country_under_it()
    {
        Assert.Equal(
            new PlaceUiModel("Asheville", "North Carolina, United States", 35.6, -82.55),
            PlaceUiModel.From(Samples.Asheville));
    }

    [Fact]
    public void Whatever_is_missing_is_left_out_of_the_line_under_the_name()
    {
        Assert.Equal("Australia", PlaceUiModel.From(Samples.Sydney).Subtitle);
        Assert.False(PlaceUiModel.From(new Place(0, 0, "Null Island")).HasSubtitle);
    }

    [Theory]
    [InlineData(35.6, -82.55, "35.60°N, 82.55°W")]
    [InlineData(-33.868, 151.209, "33.87°S, 151.21°E")]
    [InlineData(0.05, 9, "0.05°N, 9.00°E")]
    public void A_place_with_no_name_is_called_by_its_coordinates(double latitude, double longitude, string title)
    {
        Assert.Equal(title, TitleOf(latitude, longitude));
        Assert.Equal("", PlaceUiModel.From(Place.FromCoordinates(latitude, longitude)).Subtitle);
    }

    [Theory]
    [InlineData(0, 0, "0.00°N, 0.00°E")]
    [InlineData(-0.001, -0.004, "0.00°N, 0.00°E")]
    [InlineData(-0.006, -0.0051, "0.01°S, 0.01°W")]
    public void A_point_a_hair_off_the_equator_or_Greenwich_is_on_the_same_side_as_the_line_itself(
        double latitude,
        double longitude,
        string title)
    {
        Assert.Equal(title, TitleOf(latitude, longitude));
    }

    // Units.

    [Fact]
    public void Each_units_choice_stands_for_one_system_of_units_and_back()
    {
        Assert.Equal(UnitsUiModel.Celsius, Units.Metric.ToUiModel());
        Assert.Equal(UnitsUiModel.Fahrenheit, Units.Imperial.ToUiModel());
        Assert.All(Enum.GetValues<Units>(), units => Assert.Equal(units, units.ToUiModel().ToDomain()));
    }

    // Forecast.

    [Fact]
    public void Current_conditions_are_rounded_and_worded_in_imperial_units()
    {
        Assert.Equal(
            new CurrentUiModel(
                Icon: Glyphs.Sunny,
                Temperature: "36°F",
                Condition: "Clear",
                FeelsLike: "Feels like 31°",
                Wind: "Wind 4 mph",
                Description: "Now: Clear, 36 degrees Fahrenheit, feels like 31, wind 4 mph"),
            _imperial.Current);
    }

    [Fact]
    public void Current_conditions_are_worded_in_metric_units()
    {
        CurrentUiModel current = ForecastUiModel.From(Samples.Forecast(temperature: 2.5), Units.Metric).Current;

        Assert.Equal("3°C", current.Temperature);
        Assert.Equal("Wind 7 km/h", current.Wind);
        Assert.Equal("Now: Clear, 3 degrees Celsius, feels like 0, wind 7 km/h", current.Description);
    }

    [Fact]
    public void A_forecast_is_converted_before_it_is_rounded()
    {
        // 0.3°C is 32.54°F; rounded first, it would be 0°C and so 32°F.
        CurrentUiModel current = ForecastUiModel.From(Samples.Forecast(temperature: 0.3), Units.Imperial).Current;

        Assert.Equal("33°F", current.Temperature);
    }

    [Fact]
    public void The_day_the_forecast_was_made_on_is_today_and_the_rest_are_named()
    {
        Assert.Equal(["Today", "Thu", "Sun"], _imperial.Days.Select(day => day.DayLabel));
    }

    [Fact]
    public void A_day_shows_its_rounded_low_and_high_and_its_chance_of_precipitation()
    {
        DayUiModel today = _imperial.Days[0];
        DayUiModel thursday = _imperial.Days[1];

        Assert.Equal(("30°", "64°", ""), (today.Low, today.High, today.Chance));
        Assert.Equal("47%", thursday.Chance);
        Assert.Equal(Glyphs.Rain, thursday.Icon);
    }

    [Fact]
    public void A_day_is_one_sentence_for_a_screen_reader()
    {
        Assert.Equal("Today: Clear, low 30, high 64 degrees", _imperial.Days[0].Description);
        Assert.Equal(
            "Thursday: Rain, low 42, high 58 degrees, 47 percent chance of precipitation",
            _imperial.Days[1].Description);
    }

    [Fact]
    public void A_days_bar_shows_where_its_range_sits_in_the_weeks()
    {
        // The week runs from -1.9 to 18°C; converting keeps where each day sits in it.
        Assert.Equal(1, _imperial.Days[0].BarEnd);
        Assert.Equal((-1 - -1.9) / (18 - -1.9), _imperial.Days[0].BarStart, precision: 9);
        Assert.Equal((14.5 - -1.9) / (18 - -1.9), _imperial.Days[1].BarEnd, precision: 9);
        Assert.Equal((0, 1), (_imperial.Days[2].BarStart, _imperial.Days[2].BarEnd));
    }

    [Fact]
    public void A_week_of_one_temperature_fills_every_bar()
    {
        DailyForecast flat = new(new DateOnly(2026, 11, 18), Low: 50, High: 50, WeatherCondition.Fog);

        DayUiModel day = Assert.Single(ForecastUiModel.From(Samples.Forecast(days: [flat]), Units.Imperial).Days);

        Assert.Equal((0, 1), (day.BarStart, day.BarEnd));
    }

    [Fact]
    public void A_forecast_with_no_days_has_none_to_show()
    {
        ForecastUiModel forecast = ForecastUiModel.From(Samples.Forecast(days: []), Units.Metric);

        Assert.Empty(forecast.Days);
        Assert.False(forecast.HasDays);
    }
}