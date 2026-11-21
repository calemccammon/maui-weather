using System.ComponentModel;

using MauiWeather.Presentation.Home.ViewModels;
using MauiWeather.Presentation.Resources;
using MauiWeather.Views.Components;

namespace MauiWeather.Views;

public partial class HomePage : ContentPage
{
    /// <summary>Material's "expanded" window width: room for a panel beside the map.</summary>
    private const double ExpandedBreakpoint = 840;

    private const double SidePanelWidth = 400;

    /// <summary>Material 3's search bar height.</summary>
    private const double SearchBarHeight = 56;

    /// <summary>The gap round the search bar and the side panel.</summary>
    private const double Margin = 16;

    /// <summary>The gap between the search bar and what is under it.</summary>
    private const double Gap = 12;

    /// <summary>The gap between the map's credit and what it sits beside.</summary>
    private const double CreditMargin = 8;

    /// <summary>How much of the screen's height the bottom sheet takes at rest.</summary>
    private const double SheetRestingFraction = 0.3;

    /// <summary>How much of the screen's height the search results may take.</summary>
    private const double ResultsFraction = 0.5;

    private readonly HomeViewModel _home;
    private readonly SearchViewModel _search;

    public HomePage(HomeViewModel home, SearchViewModel search)
    {
        InitializeComponent();
        _home = home;
        _search = search;
        BindingContext = home;
        SearchBar.BindingContext = search;
        SearchBar.SetBinding(PlaceSearchBar.PlaceTitleProperty, static (HomeViewModel vm) => vm.Place?.Title, source: home);

        SearchBar.PropertyChanged += OnSearchBarChanged;
        home.PropertyChanged += OnHomeChanged;
        search.PropertyChanged += OnSearchChanged;
        // The page runs under the system bars. The map fills the part of it
        // that does not, so the map's size is the room there is.
        Map.SizeChanged += (_, _) => LayOut();
    }

    /// <summary>Puts everything where the size of the screen's usable part says it goes.</summary>
    private void LayOut()
    {
        double width = Map.Width;
        double height = Map.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        bool isWide = width >= ExpandedBreakpoint;
        double underSearchBar = Margin + SearchBarHeight + Gap;

        Sheet.IsVisible = !isWide;
        SidePanel.IsVisible = isWide;
        SearchBar.MaxResultsHeight = height * ResultsFraction;

        if (isWide)
        {
            SearchBar.Margin = new Thickness(Margin, Margin, 0, 0);
            SearchBar.HorizontalOptions = LayoutOptions.Start;
            SearchBar.WidthRequest = SidePanelWidth;
            SidePanel.WidthRequest = SidePanelWidth;
            SidePanel.Margin = new Thickness(Margin, underSearchBar, 0, Margin);
            // Nothing covers the map's bottom corner here.
            Credit.Margin = new Thickness(CreditMargin);
            Map.Covered = new Thickness(SidePanelWidth + (Margin * 2), 0, 0, 0);
        }
        else
        {
            double resting = height * SheetRestingFraction;
            SearchBar.Margin = new Thickness(Margin, Margin, Margin, 0);
            SearchBar.HorizontalOptions = LayoutOptions.Fill;
            SearchBar.WidthRequest = -1;
            Sheet.RestingHeight = resting;
            // Pulled right up, the sheet stops just under the search bar.
            Sheet.ExpandedHeight = height - underSearchBar;
            // Just above the sheet where it rests.
            Credit.Margin = new Thickness(CreditMargin, CreditMargin, CreditMargin, resting + CreditMargin);
            Map.Covered = new Thickness(0, underSearchBar, 0, resting);
        }
    }

    private void OnPointPicked(object? sender, PointPickedEventArgs e)
    {
        _home.SelectPoint(e.Latitude, e.Longitude);
    }

    private void OnClearRequested(object? sender, EventArgs e)
    {
        _home.Clear();
    }

    private void OnScrimTapped(object? sender, TappedEventArgs e)
    {
        SearchBar.IsOpen = false;
    }

    private void OnSearchBarChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == PlaceSearchBar.IsOpenProperty.PropertyName)
        {
            Scrim.IsVisible = SearchBar.IsOpen;
        }
    }

    // What is happening is read out by a screen reader as it changes:
    // loading, then failed; searching, then what came of it.

    private void OnHomeChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(HomeViewModel.IsLoading) when _home.IsLoading:
                SemanticScreenReader.Announce(Strings.ForecastLoading);
                break;
            case nameof(HomeViewModel.FailureMessage) when _home.FailureMessage is string message:
                SemanticScreenReader.Announce(message);
                break;
        }
    }

    private void OnSearchChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!SearchBar.IsOpen)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(SearchViewModel.IsSearching) when _search.IsSearching:
                SemanticScreenReader.Announce(Strings.SearchSearching);
                break;
            case nameof(SearchViewModel.Message) when _search.Message is string message:
                SemanticScreenReader.Announce(message);
                break;
        }
    }
}