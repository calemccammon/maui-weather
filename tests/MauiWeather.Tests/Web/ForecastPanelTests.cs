using AngleSharp.Dom;

using Bunit;

using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;
using MauiWeather.Tests.Testing;
using MauiWeather.Web.Views.Components;

namespace MauiWeather.Tests.Web;

public sealed class ForecastPanelTests : WebTest
{
    private IRenderedComponent<ForecastPanel> Panel()
    {
        return Render<ForecastPanel>(parameters => parameters.Add(panel => panel.Home, Home));
    }

    private static string Announced(IRenderedComponent<ForecastPanel> panel)
    {
        return panel.Find("[aria-live=polite]").TextContent;
    }

    [Fact]
    public void With_no_place_it_says_how_to_choose_one()
    {
        IRenderedComponent<ForecastPanel> panel = Panel();

        Assert.Contains("Search for a place, or press and hold on the map", Announced(panel), StringComparison.Ordinal);
        Assert.Empty(panel.FindAll("h1"));
        Assert.Empty(panel.FindAll(".week"));
    }

    [Fact]
    public void While_the_forecast_is_on_its_way_it_shows_the_place_and_progress()
    {
        Weather.ForecastToReturn = null;
        Home.Select(Samples.Asheville);

        IRenderedComponent<ForecastPanel> panel = Panel();

        Assert.Equal("Asheville", panel.Find("h1").TextContent);
        Assert.Equal("Loading the forecast", panel.Find("[aria-live=polite] [role=progressbar]").GetAttribute("aria-label"));
        Assert.DoesNotContain("press and hold", Announced(panel), StringComparison.Ordinal);
    }

    [Fact]
    public void After_a_failure_it_says_what_went_wrong_and_can_try_again()
    {
        Weather.Failure = new WeatherException(WeatherFailure.Rejected);
        Home.Select(Samples.Asheville);
        IRenderedComponent<ForecastPanel> panel = Panel();
        Assert.Contains("The weather service refused the request.", Announced(panel), StringComparison.Ordinal);

        Weather.Failure = null;
        panel.Find(".tonal-button").Click();
        panel.Render();

        Assert.Equal("This week", panel.Find("h2").TextContent);
        Assert.Empty(panel.FindAll("[aria-live=polite]"));
    }

    [Fact]
    public void It_shows_the_place_current_conditions_and_each_day()
    {
        Home.Select(Samples.Asheville);

        IRenderedComponent<ForecastPanel> panel = Panel();

        Assert.Equal("Asheville", panel.Find("h1").TextContent);
        Assert.Equal("North Carolina, United States", panel.Find("header .muted").TextContent);
        Assert.Contains("36°F", panel.Find(".temperature").TextContent, StringComparison.Ordinal);
        IReadOnlyList<IElement> days = panel.FindAll(".week li");
        Assert.Equal(3, days.Count);
        Assert.Contains("Today", days[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("47%", days[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("Weather data by Open-Meteo.com", panel.Find(".credit").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_place_with_no_region_has_no_second_line()
    {
        Home.SelectPoint(10, 20);

        IRenderedComponent<ForecastPanel> panel = Panel();

        Assert.Equal("10.00°N, 20.00°E", panel.Find("h1").TextContent);
        Assert.Empty(panel.FindAll("header .muted"));
    }

    [Fact]
    public void Each_block_is_one_sentence_to_a_screen_reader_and_its_parts_are_hidden_from_it()
    {
        Home.Select(Samples.Asheville);

        IRenderedComponent<ForecastPanel> panel = Panel();

        IElement current = panel.Find(".current");
        Assert.Equal("Now: Clear, 36 degrees Fahrenheit, feels like 31, wind 4 mph", current.GetAttribute("aria-label"));
        Assert.All(current.Children, part => Assert.Equal("true", part.GetAttribute("aria-hidden")));
        IElement thursday = panel.FindAll(".week li")[1];
        Assert.Equal(
            "Thursday: Rain, low 42, high 58 degrees, 47 percent chance of precipitation",
            thursday.GetAttribute("aria-label"));
        Assert.All(thursday.Children, part => Assert.Equal("true", part.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void A_days_bar_is_placed_within_the_weeks_range_in_figures_a_style_sheet_can_read()
    {
        Home.Select(Samples.Asheville);

        IRenderedComponent<ForecastPanel> panel = Panel();

        // The week runs from -1.9 to 18°C; Thursday from 5.5 to 14.5.
        Assert.Equal("--start: 0.3719; --end: 0.8241", panel.FindAll(".week .bar")[1].GetAttribute("style"));
    }

    [Fact]
    public void The_units_in_use_are_marked_and_pressing_the_other_converts_the_forecast_without_asking_again()
    {
        Home.Select(Samples.Asheville);
        IRenderedComponent<ForecastPanel> panel = Panel();
        IElement Celsius() => panel.Find("button[aria-label='Celsius and km/h']");
        IElement Fahrenheit() => panel.Find("button[aria-label='Fahrenheit and mph']");
        Assert.Equal("Units", panel.Find(".units").GetAttribute("aria-label"));
        Assert.Equal("false", Celsius().GetAttribute("aria-pressed"));
        Assert.Equal("true", Fahrenheit().GetAttribute("aria-pressed"));

        Celsius().Click();
        panel.Render();

        Assert.Equal("true", Celsius().GetAttribute("aria-pressed"));
        Assert.Equal("false", Fahrenheit().GetAttribute("aria-pressed"));
        Assert.Single(Weather.ForecastRequests);
        Assert.Equal(Units.Metric, Settings.Units);
        Assert.Contains("2°C", panel.Find(".temperature").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_forecast_with_no_days_says_so()
    {
        Weather.ForecastToReturn = Samples.Forecast(days: []);
        Home.Select(Samples.Asheville);

        IRenderedComponent<ForecastPanel> panel = Panel();

        Assert.Contains("No forecast for the coming days.", panel.Markup, StringComparison.Ordinal);
        Assert.Empty(panel.FindAll(".week"));
    }
}