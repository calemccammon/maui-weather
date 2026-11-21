using MauiWeather.Presentation.Home.Models;
using MauiWeather.Presentation.Home.ViewModels;

namespace MauiWeather.Views.Components;

public partial class PlaceSearchBar : ContentView
{
    public static readonly BindableProperty PlaceTitleProperty = BindableProperty.Create(
        nameof(PlaceTitle),
        typeof(string),
        typeof(PlaceSearchBar),
        propertyChanged: (bar, _, _) => ((PlaceSearchBar)bar).ShowPlace());

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen),
        typeof(bool),
        typeof(PlaceSearchBar),
        defaultValue: false,
        propertyChanged: (bar, _, _) => ((PlaceSearchBar)bar).ShowOpenOrClosed());

    public static readonly BindableProperty MaxResultsHeightProperty = BindableProperty.Create(
        nameof(MaxResultsHeight),
        typeof(double),
        typeof(PlaceSearchBar),
        double.PositiveInfinity,
        propertyChanged: (bar, _, height) => ((PlaceSearchBar)bar).ResultsScroll.MaximumHeightRequest = (double)height);

    public PlaceSearchBar()
    {
        InitializeComponent();
        ShowPlace();
    }

    /// <summary>The user pressed the button that deselects the place.</summary>
    public event EventHandler? ClearRequested;

    /// <summary>The selected place's name, shown while the bar is closed. Null when no place is selected.</summary>
    public string? PlaceTitle
    {
        get => (string?)GetValue(PlaceTitleProperty);
        set => SetValue(PlaceTitleProperty, value);
    }

    /// <summary>Whether the bar is open for typing, with its results under it.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>How tall the list of results may grow before it scrolls.</summary>
    public double MaxResultsHeight
    {
        get => (double)GetValue(MaxResultsHeightProperty);
        set => SetValue(MaxResultsHeightProperty, value);
    }

    private SearchViewModel? ViewModel => BindingContext as SearchViewModel;

    private void ShowPlace()
    {
        PlaceLabel.Text = PlaceTitle;
        PlaceLabel.IsVisible = PlaceTitle is not null;
        HintLabel.IsVisible = PlaceTitle is null;
        ClearButton.IsVisible = PlaceTitle is not null;
    }

    private void ShowOpenOrClosed()
    {
        ClosedBar.IsVisible = !IsOpen;
        OpenBar.IsVisible = IsOpen;
        ResultsArea.IsVisible = IsOpen;

        if (IsOpen)
        {
            // Opening starts from an empty search: the place's name is a
            // label, not something to search for.
            ViewModel?.Reset();
            // The entry cannot take the keyboard until it has been shown.
            Dispatcher.Dispatch(() => QueryEntry.Focus());
        }
        else
        {
            QueryEntry.Unfocus();
        }
    }

    private void OnBarTapped(object? sender, TappedEventArgs e)
    {
        IsOpen = true;
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        ClearRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnPlaceTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: PlaceUiModel row })
        {
            ViewModel?.Choose(row);
            IsOpen = false;
        }
    }
}