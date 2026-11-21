using System.Globalization;

using MauiWeather.Data.Repositories;
using MauiWeather.Domain.Repositories;
using MauiWeather.Domain.UseCases;
using MauiWeather.Presentation.Home.ViewModels;
using MauiWeather.Web;
using MauiWeather.Web.Data;
using MauiWeather.Web.Views;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

// Starts the app in the browser. Like MauiProgram in the MAUI app, this is
// the one place that knows which implementation stands behind each domain
// interface, and how each use case and view model is built.

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

IServiceCollection services = builder.Services;

// Data.
services.AddSingleton(_ => new HttpClient { Timeout = OpenMeteoWeatherRepository.DefaultTimeout });
services.AddSingleton<IWeatherRepository, OpenMeteoWeatherRepository>();
// In the browser the JavaScript runtime can be called without waiting.
services.AddSingleton<IKeyValueStore>(provider =>
    new BrowserStore((IJSInProcessRuntime)provider.GetRequiredService<IJSRuntime>()));
services.AddSingleton<ISettingsRepository, StoredSettingsRepository>();

// Domain.
services.AddSingleton<SearchPlaces>();
services.AddSingleton<GetForecast>();
services.AddSingleton<GetUnits>();
services.AddSingleton<SetUnits>();

// Presentation. The app has one screen, so one of each view model.
services.AddSingleton(provider => new HomeViewModel(
    provider.GetRequiredService<GetForecast>(),
    provider.GetRequiredService<GetUnits>(),
    provider.GetRequiredService<SetUnits>(),
    deviceCountry: BrowserCountry()));
services.AddSingleton(provider => new SearchViewModel(
    provider.GetRequiredService<SearchPlaces>(),
    select: provider.GetRequiredService<HomeViewModel>().Select,
    TimeProvider.System));

// The views' own way to the browser.
services.AddSingleton<Browser>();

await builder.Build().RunAsync();

// The country in the browser's language setting, such as "US", if it has one.
static string? BrowserCountry()
{
    // A language with no country, such as plain "en", has no region.
    CultureInfo culture = CultureInfo.CurrentCulture;
    return culture.IsNeutralCulture || culture.Name.Length == 0 ? null : new RegionInfo(culture.Name).TwoLetterISORegionName;
}