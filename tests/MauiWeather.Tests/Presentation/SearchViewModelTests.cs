using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;
using MauiWeather.Domain.UseCases;
using MauiWeather.Presentation.Core;
using MauiWeather.Presentation.Home.ViewModels;
using MauiWeather.Tests.Testing;

using Microsoft.Extensions.Time.Testing;

namespace MauiWeather.Tests.Presentation;

public sealed class SearchViewModelTests : IDisposable
{
    private readonly FakeWeatherRepository _weather = new() { PlacesToReturn = [Samples.Asheville, Samples.Sydney] };
    private readonly FakeTimeProvider _clock = new();
    private readonly List<Place> _chosen = [];
    private SearchViewModel? _made;

    /// <summary>The view model under test, made when a test first reaches for it.</summary>
    private SearchViewModel ViewModel
    {
        get
        {
            InlineContinuations.Use();
            return _made ??= new SearchViewModel(new SearchPlaces(_weather), _chosen.Add, _clock);
        }
    }

    public void Dispose()
    {
        _made?.Dispose();
    }

    /// <summary>Types <paramref name="text"/> and waits out the pause before a search.</summary>
    private void Search(string text)
    {
        ViewModel.Query = text;
        _clock.Advance(SearchViewModel.Debounce);
    }

    private IEnumerable<string> Titles => ViewModel.Results.Select(place => place.Title);

    [Fact]
    public void It_starts_by_asking_for_something_to_type()
    {
        Assert.Equal("Type a city, town or postcode", ViewModel.Message);
        Assert.False(ViewModel.IsSearching);
        Assert.False(ViewModel.HasResults);
    }

    [Fact]
    public void Too_little_to_search_for_is_not_searched_for()
    {
        Search(" a ");

        Assert.Equal("Type a city, town or postcode", ViewModel.Message);
        Assert.False(ViewModel.IsSearching);
        Assert.Empty(_weather.SearchQueries);
    }

    [Fact]
    public void It_waits_for_a_pause_in_typing_before_it_searches()
    {
        ViewModel.Query = "ashev";

        Assert.True(ViewModel.IsSearching);
        Assert.Null(ViewModel.Message);
        _clock.Advance(SearchViewModel.Debounce - TimeSpan.FromMilliseconds(1));
        Assert.Empty(_weather.SearchQueries);

        _clock.Advance(TimeSpan.FromMilliseconds(1));

        Assert.Equal(["ashev"], _weather.SearchQueries);
    }

    [Fact]
    public void Typing_on_starts_the_wait_again_so_only_the_last_text_is_searched_for()
    {
        ViewModel.Query = "as";
        _clock.Advance(TimeSpan.FromMilliseconds(300));
        ViewModel.Query = "ashev";
        _clock.Advance(TimeSpan.FromMilliseconds(300));

        Assert.Empty(_weather.SearchQueries);

        _clock.Advance(TimeSpan.FromMilliseconds(50));

        Assert.Equal(["ashev"], _weather.SearchQueries);
    }

    [Fact]
    public void The_places_found_are_shown_as_rows_and_choosing_a_row_selects_its_place()
    {
        Search("a place");

        Assert.Equal(["Asheville", "Sydney"], Titles);
        Assert.Equal("North Carolina, United States", ViewModel.Results[0].Subtitle);
        Assert.False(ViewModel.IsSearching);
        Assert.False(ViewModel.HasMessage);

        ViewModel.ChooseCommand.Execute(ViewModel.Results[1]);

        Assert.Equal([Samples.Sydney], _chosen);
    }

    [Fact]
    public void Finding_nothing_is_said_as_such()
    {
        _weather.PlacesToReturn = [];

        Search("zzzz");

        Assert.Equal("Nothing found for \"zzzz\"", ViewModel.Message);
        Assert.Equal(Glyphs.SearchOff, ViewModel.MessageIcon);
        Assert.False(ViewModel.HasFailed);
        Assert.False(ViewModel.IsSearching);
    }

    [Fact]
    public void A_failure_is_worded_for_the_user_and_another_try_searches_at_once()
    {
        _weather.Failure = new WeatherException(WeatherFailure.NoConnection);

        Search("ashev");

        Assert.Equal("Could not reach the weather service. Check your connection.", ViewModel.Message);
        Assert.True(ViewModel.HasFailed);

        _weather.Failure = null;
        ViewModel.RetryCommand.Execute(null);

        Assert.Equal(["Asheville", "Sydney"], Titles);
        Assert.False(ViewModel.HasFailed);
        Assert.Equal(["ashev", "ashev"], _weather.SearchQueries);
    }

    [Fact]
    public void The_answer_to_an_older_search_never_replaces_a_newer_one()
    {
        _weather.PlacesToReturn = null;

        Search("ashev");
        Search("sydney");
        // The fake answers even though the search was given up.
        _weather.Searches[0].TrySetResult([Samples.Asheville]);

        Assert.True(ViewModel.IsSearching);
        Assert.Empty(ViewModel.Results);

        _weather.Searches[1].SetResult([Samples.Sydney]);

        Assert.Equal(["Sydney"], Titles);
        ViewModel.Choose(ViewModel.Results[0]);
        Assert.Equal([Samples.Sydney], _chosen);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Nor_does_the_answer_of_a_search_that_would_not_stop(bool succeeds)
    {
        _weather.PlacesToReturn = null;
        _weather.IgnoresCancellation = true;

        Search("ashev");
        Search("sydney");
        if (succeeds)
        {
            _weather.Searches[0].SetResult([Samples.Asheville]);
        }
        else
        {
            _weather.Searches[0].SetException(new WeatherException(WeatherFailure.Timeout));
        }

        Assert.True(ViewModel.IsSearching);
        Assert.Empty(ViewModel.Results);
        Assert.False(ViewModel.HasFailed);
    }

    [Fact]
    public void The_same_text_typed_again_is_not_searched_for_twice()
    {
        Search("ashev");
        Search(" ashev ");

        Assert.Equal(["ashev"], _weather.SearchQueries);
    }

    [Fact]
    public void Resetting_forgets_the_text_and_the_results_and_drops_a_search_on_its_way()
    {
        Search("ashev");
        _weather.PlacesToReturn = null;
        Search("sydney");

        ViewModel.Reset();
        _weather.Searches[0].TrySetResult([Samples.Sydney]);

        Assert.Equal("", ViewModel.Query);
        Assert.Equal("Type a city, town or postcode", ViewModel.Message);
        Assert.Empty(ViewModel.Results);
        Assert.False(ViewModel.IsSearching);

        // The text that was there before can be searched for again.
        _weather.PlacesToReturn = [Samples.Sydney];
        Search("sydney");

        Assert.Equal(["Sydney"], Titles);
    }

    [Fact]
    public void A_row_that_is_no_longer_shown_selects_nothing()
    {
        Search("ashev");
        MauiWeather.Presentation.Home.Models.PlaceUiModel stale = ViewModel.Results[0];
        ViewModel.Reset();

        ViewModel.Choose(stale);

        Assert.Empty(_chosen);
    }

    [Fact]
    public void There_is_nothing_to_try_again_before_anything_is_typed()
    {
        ViewModel.Retry();

        Assert.Empty(_weather.SearchQueries);
        Assert.False(ViewModel.IsSearching);
    }
}