using AngleSharp.Dom;

using Bunit;

using MauiWeather.Domain.Repositories;
using MauiWeather.Presentation.Home.ViewModels;
using MauiWeather.Tests.Testing;
using MauiWeather.Web.Views.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MauiWeather.Tests.Web;

public sealed class PlaceSearchBarTests : WebTest
{
    private bool _isOpen;
    private int _clears;

    private IRenderedComponent<PlaceSearchBar> Bar(string? placeTitle = null, bool isOpen = false)
    {
        _isOpen = isOpen;
        return Render<PlaceSearchBar>(parameters => parameters
            .Add(bar => bar.PlaceTitle, placeTitle)
            .Add(bar => bar.Search, Search)
            .Add(bar => bar.IsOpen, isOpen)
            .Add(bar => bar.IsOpenChanged, EventCallback.Factory.Create<bool>(this, open => _isOpen = open))
            .Add(bar => bar.OnClearPlace, EventCallback.Factory.Create(this, () => _clears++)));
    }

    private static IElement Input(IRenderedComponent<PlaceSearchBar> bar)
    {
        return bar.Find("input");
    }

    [Fact]
    public void Closed_with_no_place_it_shows_only_the_hint()
    {
        IRenderedComponent<PlaceSearchBar> bar = Bar();

        Assert.Equal("", Input(bar).GetAttribute("value"));
        Assert.Equal("Search for a place", Input(bar).GetAttribute("placeholder"));
        Assert.Empty(bar.FindAll("button"));
        Assert.Empty(bar.FindAll(".results"));
    }

    [Fact]
    public void Closed_it_shows_the_places_name_and_a_button_that_clears_it()
    {
        IRenderedComponent<PlaceSearchBar> bar = Bar("Asheville");

        Assert.Equal("Asheville", Input(bar).GetAttribute("value"));
        bar.Find("button[aria-label='Clear place']").Click();

        Assert.Equal(1, _clears);
        // Clearing is not searching.
        Assert.False(_isOpen);
    }

    [Fact]
    public void Taking_the_keyboard_opens_it_on_an_empty_search()
    {
        SearchFor("sydney");
        IRenderedComponent<PlaceSearchBar> bar = Bar("Asheville");

        Input(bar).Focus();

        Assert.True(_isOpen);
        Assert.Equal("", Search.Query);
        Assert.Empty(Search.Results);
    }

    [Fact]
    public void Open_it_holds_what_is_typed_and_not_the_places_name()
    {
        IRenderedComponent<PlaceSearchBar> bar = Bar("Asheville", isOpen: true);

        Assert.Equal("", Input(bar).GetAttribute("value"));
        Assert.Empty(bar.FindAll("button[aria-label='Clear place']"));
        Assert.Contains("Type a city, town or postcode", bar.Find("[aria-live=polite]").TextContent, StringComparison.Ordinal);

        Input(bar).Input("ashev");

        Assert.Equal("ashev", Search.Query);
        Assert.Equal("ashev", Input(bar).GetAttribute("value"));
    }

    [Fact]
    public void While_searching_it_shows_progress_a_screen_reader_can_name()
    {
        IRenderedComponent<PlaceSearchBar> bar = Bar(isOpen: true);

        Input(bar).Input("ashev");

        Assert.Equal("Searching", bar.Find("[aria-live=polite] [role=progressbar]").GetAttribute("aria-label"));
    }

    [Fact]
    public void It_lists_what_was_found_and_choosing_a_row_selects_its_place_and_closes()
    {
        IRenderedComponent<PlaceSearchBar> bar = Bar(isOpen: true);
        Input(bar).Input("ashev");
        Clock.Advance(SearchViewModel.Debounce);
        bar.Render();

        IReadOnlyList<IElement> rows = bar.FindAll(".result");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Asheville", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("North Carolina, United States", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Australia", rows[1].TextContent, StringComparison.Ordinal);

        rows[1].Click();

        Assert.Equal("Sydney", Home.Place?.Title);
        Assert.False(_isOpen);
    }

    [Fact]
    public void It_says_when_nothing_was_found()
    {
        Weather.PlacesToReturn = [];
        IRenderedComponent<PlaceSearchBar> bar = Bar(isOpen: true);

        SearchFor("zzzz");
        bar.Render();

        Assert.Contains("Nothing found for \"zzzz\"", bar.Find("[aria-live=polite]").TextContent, StringComparison.Ordinal);
        Assert.Empty(bar.FindAll(".text-button"));
    }

    [Fact]
    public void After_a_failure_it_says_what_went_wrong_and_can_try_again()
    {
        Weather.Failure = new WeatherException(WeatherFailure.NoConnection);
        IRenderedComponent<PlaceSearchBar> bar = Bar(isOpen: true);
        SearchFor("ashev");
        bar.Render();
        Assert.Contains(
            "Could not reach the weather service. Check your connection.",
            bar.Find("[aria-live=polite]").TextContent,
            StringComparison.Ordinal);

        Assert.Equal("Try again", bar.Find(".text-button").TextContent);

        Weather.Failure = null;
        bar.Find(".text-button").Click();

        Assert.Equal(["ashev", "ashev"], Weather.SearchQueries);
        Assert.Equal(2, bar.FindAll(".result").Count);
    }

    [Fact]
    public void Escape_closes_it_and_gives_up_the_keyboard()
    {
        IRenderedComponent<PlaceSearchBar> bar = Bar("Asheville", isOpen: true);

        Input(bar).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(_isOpen);
        BrowserModule.VerifyInvoke("blur");
    }

    [Fact]
    public void Any_other_key_leaves_it_open()
    {
        IRenderedComponent<PlaceSearchBar> bar = Bar(isOpen: true);

        Input(bar).KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.True(_isOpen);
        BrowserModule.VerifyNotInvoke("blur");
    }
}