using System.ComponentModel;

using MauiWeather.Presentation.Home.ViewModels;
using MauiWeather.Web.Views.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MauiWeather.Web.Views;

public sealed partial class HomePage : ComponentBase, IAsyncDisposable
{
    /// <summary>Material's "expanded" window width: room for a panel beside the map.</summary>
    private const double ExpandedBreakpoint = 840;

    // Sizes the map is told about. The same ones are in HomePage.razor.css.
    private const double SidePanelWidth = 400;
    private const double SearchBarHeight = 56;
    private const double Margin = 16;
    private const double Gap = 12;

    /// <summary>Where the top of the sheet stops, and the side panel starts: just under the search bar.</summary>
    private const double UnderSearchBar = Margin + SearchBarHeight + Gap;

    /// <summary>How much of the screen's height the bottom sheet takes at rest.</summary>
    private const double SheetRestingFraction = 0.3;

    private DotNetObjectReference<HomePage>? _listener;
    private IJSObjectReference? _watch;

    /// <summary>The window's size and theme. Null until the browser has said.</summary>
    private WindowSize? _window;

    private bool _isSearchOpen;

    [Inject]
    public required HomeViewModel Home { get; set; }

    [Inject]
    public required SearchViewModel Search { get; set; }

    [Inject]
    public required Browser Browser { get; set; }

    /// <summary>Called by the browser, with the window's size and whether the system theme is dark.</summary>
    [JSInvokable]
    public void OnWindowChanged(double width, double height, bool isDark)
    {
        _window = new WindowSize(width, height, isDark);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        Home.PropertyChanged -= OnViewModelChanged;
        Search.PropertyChanged -= OnViewModelChanged;

        if (_watch is not null)
        {
            await _watch.InvokeVoidAsync("stop");
            await _watch.DisposeAsync();
        }

        _listener?.Dispose();
    }

    protected override void OnInitialized()
    {
        // The view models say when what they hold has changed; the screen
        // is then drawn again from them.
        Home.PropertyChanged += OnViewModelChanged;
        Search.PropertyChanged += OnViewModelChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _listener = DotNetObjectReference.Create(this);
            _watch = await Browser.WatchWindowAsync(_listener);
        }
    }

    private static bool IsWide(WindowSize window)
    {
        return window.Width >= ExpandedBreakpoint;
    }

    private static double SheetRestingHeight(WindowSize window)
    {
        return window.Height * SheetRestingFraction;
    }

    /// <summary>Pulled right up, the sheet stops just under the search bar.</summary>
    private static double SheetExpandedHeight(WindowSize window)
    {
        return Math.Max(0, window.Height - UnderSearchBar);
    }

    /// <summary>
    /// What covers the map, so the selected place can be shown in the clear.
    /// It changes when the window crosses between the two layouts (a rotated
    /// phone, a resized window), and the map then moves the place again.
    /// </summary>
    private static MapInsets Covered(WindowSize window)
    {
        return IsWide(window)
            ? new MapInsets(Top: 0, Right: 0, Bottom: 0, Left: SidePanelWidth + (Margin * 2))
            : new MapInsets(Top: UnderSearchBar, Right: 0, Bottom: SheetRestingHeight(window), Left: 0);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private readonly record struct WindowSize(double Width, double Height, bool IsDark);
}