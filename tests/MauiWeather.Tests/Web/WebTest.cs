using Bunit;

using MauiWeather.Domain.Models;
using MauiWeather.Domain.UseCases;
using MauiWeather.Presentation.Home.ViewModels;
using MauiWeather.Tests.Testing;
using MauiWeather.Web.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace MauiWeather.Tests.Web;

/// <summary>
/// What every test of the web app's views starts from: the real view models
/// over a weather service that answers from memory, and a stand-in for the
/// browser's JavaScript that records what it was asked to do.
/// </summary>
public abstract class WebTest : BunitContext
{
    protected WebTest()
    {
        InlineContinuations.Use();
        JSInterop.Mode = JSRuntimeMode.Loose;
        BrowserModule = JSInterop.SetupModule("./js/browser.js");
        BrowserModule.Mode = JSRuntimeMode.Loose;

        Home = new HomeViewModel(new GetForecast(Weather), new GetUnits(Settings), new SetUnits(Settings), "US");
        Search = new SearchViewModel(new SearchPlaces(Weather), Home.Select, Clock);
        Services.AddSingleton(Home);
        Services.AddSingleton(Search);
        Services.AddSingleton<Browser>();
    }

    internal FakeWeatherRepository Weather { get; } = new() { PlacesToReturn = [Samples.Asheville, Samples.Sydney] };

    internal FakeSettingsRepository Settings { get; } = new();

    protected FakeTimeProvider Clock { get; } = new();

    protected HomeViewModel Home { get; }

    protected SearchViewModel Search { get; }

    /// <summary>What the views asked of <c>browser.js</c>.</summary>
    protected BunitJSModuleInterop BrowserModule { get; }

    /// <summary>Types <paramref name="text"/> into the search and waits out the pause before it is sent.</summary>
    protected void SearchFor(string text)
    {
        Search.Query = text;
        Clock.Advance(SearchViewModel.Debounce);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Search.Dispose();
            Home.Dispose();
        }

        base.Dispose(disposing);
    }
}