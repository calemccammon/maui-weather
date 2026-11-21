using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;
using MauiWeather.Domain.UseCases;
using MauiWeather.Presentation.Core;
using MauiWeather.Presentation.Home.Models;
using MauiWeather.Presentation.Resources;

namespace MauiWeather.Presentation.Home.ViewModels;

/// <summary>
/// Turns what the user types into search results.
/// </summary>
/// <remarks>
/// It waits for a pause in typing before searching, and a search is given up
/// when a newer one replaces it, so an old answer never shows over a new one.
/// </remarks>
/// <param name="select">What to do with the place in the row the user chooses.</param>
/// <param name="timeProvider">The clock the pause is measured on.</param>
public sealed partial class SearchViewModel(SearchPlaces searchPlaces, Action<Place> select, TimeProvider timeProvider)
    : ObservableObject, IDisposable
{
    /// <summary>How long typing must pause before a search is sent.</summary>
    public static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(350);

    /// <summary>The text last asked about; what <see cref="Retry"/> asks about again.</summary>
    private string _latestQuery = "";

    /// <summary>The places behind the rows of <see cref="Results"/>, in order.</summary>
    private IReadOnlyList<Place> _places = [];

    /// <summary>Stops the wait for a pause in typing, and the search after it.</summary>
    private CancellationTokenSource? _searching;

    /// <summary>What is typed in the search bar.</summary>
    [ObservableProperty]
    public partial string Query { get; set; } = "";

    /// <summary>The places found, one per row. Empty unless a search has found some.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    public partial IReadOnlyList<PlaceUiModel> Results { get; private set; } = [];

    /// <summary>Whether it is waiting, either for the user to stop typing or for the answer.</summary>
    [ObservableProperty]
    public partial bool IsSearching { get; private set; }

    /// <summary>
    /// What to say when there are no rows to show: a prompt, that nothing was
    /// found, or what went wrong. Null while searching or showing results.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string? Message { get; private set; } = Strings.SearchPrompt;

    /// <summary>The icon beside <see cref="Message"/>.</summary>
    [ObservableProperty]
    public partial string MessageIcon { get; private set; } = Glyphs.TravelExplore;

    /// <summary>Whether <see cref="Message"/> reports a failure, which can be tried again.</summary>
    [ObservableProperty]
    public partial bool HasFailed { get; private set; }

    public bool HasResults => Results.Count > 0;

    public bool HasMessage => Message is not null;

    /// <summary>Forgets the current query and results, as when the search bar opens.</summary>
    public void Reset()
    {
        // Emptying the text gives up any search and brings the prompt back.
        Query = "";
    }

    /// <summary>Makes the place in <paramref name="row"/> the selected one.</summary>
    [RelayCommand]
    public void Choose(PlaceUiModel row)
    {
        for (int i = 0; i < Results.Count; i++)
        {
            if (ReferenceEquals(Results[i], row))
            {
                select(_places[i]);
                return;
            }
        }
    }

    /// <summary>Runs the last search again after a failure.</summary>
    [RelayCommand]
    public void Retry()
    {
        string query = _latestQuery;
        if (!SearchPlaces.IsSearchable(query))
        {
            return;
        }

        _ = SearchAsync(query, pause: TimeSpan.Zero);
    }

    public void Dispose()
    {
        StopSearching();
    }

    partial void OnQueryChanged(string value)
    {
        string query = value.Trim();
        if (query == _latestQuery)
        {
            return;
        }

        _latestQuery = query;
        if (!SearchPlaces.IsSearchable(query))
        {
            StopSearching();
            ShowIdle();
            return;
        }

        _ = SearchAsync(query, Debounce);
    }

    private async Task SearchAsync(string query, TimeSpan pause)
    {
        StopSearching();
        CancellationTokenSource searching = new();
        _searching = searching;
        Show(places: [], isSearching: true, message: null, Glyphs.TravelExplore, hasFailed: false);

        try
        {
            if (pause > TimeSpan.Zero)
            {
                await Task.Delay(pause, timeProvider, searching.Token);
            }

            IReadOnlyList<Place> places = await searchPlaces.InvokeAsync(query, searching.Token);
            // A repository may answer although it was told to stop.
            if (searching.IsCancellationRequested)
            {
                return;
            }

            if (places.Count == 0)
            {
                string nothing = Strings.Format(nameof(Strings.SearchNothingFound), query);
                Show(places: [], isSearching: false, nothing, Glyphs.SearchOff, hasFailed: false);
            }
            else
            {
                Show(places, isSearching: false, message: null, Glyphs.TravelExplore, hasFailed: false);
            }
        }
        catch (OperationCanceledException)
        {
            // Given up for a newer search, which shows its own progress.
        }
        catch (WeatherException e) when (!searching.IsCancellationRequested)
        {
            Show(places: [], isSearching: false, e.Failure.Message(), Glyphs.CloudOff, hasFailed: true);
        }
    }

    private void StopSearching()
    {
        _searching?.Cancel();
        _searching?.Dispose();
        _searching = null;
    }

    private void ShowIdle()
    {
        Show(places: [], isSearching: false, Strings.SearchPrompt, Glyphs.TravelExplore, hasFailed: false);
    }

    private void Show(IReadOnlyList<Place> places, bool isSearching, string? message, string messageIcon, bool hasFailed)
    {
        _places = places;
        Results = [.. places.Select(PlaceUiModel.From)];
        IsSearching = isSearching;
        Message = message;
        MessageIcon = messageIcon;
        HasFailed = hasFailed;
    }
}