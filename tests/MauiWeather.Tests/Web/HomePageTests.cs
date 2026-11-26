using Bunit;

using MauiWeather.Tests.Testing;
using MauiWeather.Web.Views;
using MauiWeather.Web.Views.Components;

namespace MauiWeather.Tests.Web;

public sealed class HomePageTests : WebTest
{
    private readonly BunitJSModuleInterop _mapModule;

    public HomePageTests()
    {
        _mapModule = JSInterop.SetupModule("./js/weatherMap.js");
        _mapModule.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>The screen, once the browser has said how big its window is.</summary>
    private IRenderedComponent<HomePage> Page(double width, double height, bool isDark = false)
    {
        IRenderedComponent<HomePage> page = Render<HomePage>();
        Resize(page, width, height, isDark);
        return page;
    }

    private static void Resize(IRenderedComponent<HomePage> page, double width, double height, bool isDark = false)
    {
        page.InvokeAsync(() => page.Instance.OnWindowChanged(width, height, isDark));
    }

    private static WeatherMap Map(IRenderedComponent<HomePage> page)
    {
        return page.FindComponent<WeatherMap>().Instance;
    }

    [Fact]
    public void It_asks_the_browser_for_the_windows_size_and_draws_nothing_until_it_is_known()
    {
        IRenderedComponent<HomePage> page = Render<HomePage>();

        BrowserModule.VerifyInvoke("watchWindow");
        Assert.Empty(page.FindAll(".map"));
        Assert.Empty(page.FindAll("input"));
    }

    [Fact]
    public void On_a_wide_window_the_forecast_is_in_a_panel_at_the_side()
    {
        IRenderedComponent<HomePage> page = Page(1280, 800);

        Assert.Contains("press and hold", page.Find(".side-panel").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll(".sheet"));
        Assert.Contains("wide", page.Find(".search").ClassList);
        // The panel, and the margin on each side of it.
        Assert.Equal(new MapInsets(Top: 0, Right: 0, Bottom: 0, Left: 432), Map(page).Covered);
    }

    [Fact]
    public void A_window_exactly_at_the_breakpoint_counts_as_wide()
    {
        Assert.NotEmpty(Page(840, 800).FindAll(".side-panel"));
        Assert.Empty(Page(839, 800).FindAll(".side-panel"));
    }

    [Fact]
    public void On_a_phone_the_forecast_is_in_a_sheet_that_rests_over_the_bottom_of_the_map()
    {
        IRenderedComponent<HomePage> page = Page(400, 800);

        Assert.Contains("press and hold", page.Find(".sheet").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll(".side-panel"));
        Assert.DoesNotContain("wide", page.Find(".search").ClassList);
        BottomSheet sheet = page.FindComponent<BottomSheet>().Instance;
        // Three tenths of the window at rest; pulled up, to just under the search bar.
        Assert.Equal(240, sheet.RestingHeight);
        Assert.Equal(716, sheet.ExpandedHeight);
        Assert.Equal(new MapInsets(Top: 84, Right: 0, Bottom: 240, Left: 0), Map(page).Covered);
    }

    [Fact]
    public void Turning_a_phone_on_its_side_moves_the_forecast_to_the_side_panel()
    {
        IRenderedComponent<HomePage> page = Page(400, 900);

        Resize(page, 900, 400);

        Assert.NotEmpty(page.FindAll(".side-panel"));
        Assert.Empty(page.FindAll(".sheet"));
        Assert.Equal(432, Map(page).Covered.Left);
    }

    [Fact]
    public void The_map_follows_the_systems_theme()
    {
        IRenderedComponent<HomePage> page = Page(1280, 800, isDark: true);

        Assert.True(Map(page).IsDark);

        Resize(page, 1280, 800, isDark: false);

        Assert.False(Map(page).IsDark);
    }

    [Fact]
    public void A_point_held_on_the_map_is_selected_and_its_forecast_shown()
    {
        IRenderedComponent<HomePage> page = Page(1280, 800);

        page.InvokeAsync(() => Map(page).OnLongPress(35.6, -82.55));

        // The selection renders asynchronously: wait for it rather than look at once.
        page.WaitForAssertion(() =>
        {
            Assert.Equal("35.60°N, 82.55°W", page.Find("h1").TextContent);
            Assert.Equal("35.60°N, 82.55°W", page.Find("input").GetAttribute("value"));
            Assert.Equal("This week", page.Find("h2").TextContent);
            Assert.Equal(35.6, Map(page).Place?.Latitude);
        });
    }

    [Fact]
    public void A_place_found_by_searching_is_shown_everywhere_and_the_search_closes()
    {
        IRenderedComponent<HomePage> page = Page(1280, 800);

        page.Find("input").Focus();
        page.Find("input").Input("ashev");
        Clock.Advance(MauiWeather.Presentation.Home.ViewModels.SearchViewModel.Debounce);
        page.WaitForElement(".result").Click();

        // Choosing a result renders asynchronously: wait for it rather than look at once.
        page.WaitForAssertion(() =>
        {
            Assert.Equal("Asheville", page.Find("h1").TextContent);
            Assert.Equal("Asheville", page.Find("input").GetAttribute("value"));
            Assert.Equal(Samples.Asheville.Longitude, Map(page).Place?.Longitude);
            Assert.Empty(page.FindAll(".results"));
            Assert.Empty(page.FindAll(".scrim"));
        });
    }

    [Fact]
    public void A_press_outside_the_open_search_closes_it_and_puts_the_places_name_back()
    {
        Home.Select(Samples.Asheville);
        IRenderedComponent<HomePage> page = Page(1280, 800);

        page.Find("input").Focus();
        page.Find("input").Input("zz");
        Assert.Equal("zz", page.Find("input").GetAttribute("value"));

        page.Find(".scrim").Click();

        Assert.Equal("Asheville", page.Find("input").GetAttribute("value"));
        Assert.Empty(page.FindAll(".results"));
        Assert.Empty(page.FindAll(".scrim"));
    }

    [Fact]
    public void Clearing_the_place_brings_the_hint_back()
    {
        Home.Select(Samples.Asheville);
        IRenderedComponent<HomePage> page = Page(1280, 800);

        page.Find("button[aria-label='Clear place']").Click();

        Assert.Equal("", page.Find("input").GetAttribute("value"));
        Assert.Contains("press and hold", page.Find(".side-panel").TextContent, StringComparison.Ordinal);
        Assert.Null(Map(page).Place);
    }

    [Fact]
    public void It_tells_the_map_what_to_show_only_when_that_changes()
    {
        IRenderedComponent<HomePage> page = Page(1280, 800);
        Assert.Single(_mapModule.Invocations["create"]);

        Home.Select(Samples.Asheville);
        page.WaitForElement("h1");

        // Once with no pin, once with Asheville's; not once per redraw.
        Assert.Equal(2, JSInterop.Invocations["show"].Count);
        object?[] shown = [.. JSInterop.Invocations["show"][^1].Arguments];
        Assert.Equal(new MapPoint(35.6, -82.55), shown[0]);
        Assert.Equal(new MapInsets(0, 0, 0, 432), shown[1]);
        Assert.Equal(false, shown[2]);
    }

    [Fact]
    public async Task Leaving_the_screen_stops_watching_the_window_and_takes_the_map_down()
    {
        Page(1280, 800);

        await DisposeComponentsAsync();

        JSInterop.VerifyInvoke("stop");
        JSInterop.VerifyInvoke("dispose");
    }
}