using System.Globalization;

using MauiWeather.Data;
using MauiWeather.Data.Repositories;
using MauiWeather.Domain.Repositories;
using MauiWeather.Domain.UseCases;
using MauiWeather.Presentation.Core;
using MauiWeather.Presentation.Home.ViewModels;
using MauiWeather.Views;

using Microsoft.Extensions.Logging;

using SkiaSharp.Views.Maui.Controls.Hosting;

namespace MauiWeather;

/// <summary>
/// Starts the app. This is the one place that knows which implementation
/// stands behind each domain interface, and how each use case and view model
/// is built. Everything else asks for the interface.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // The map draws with Skia.
            .UseSkiaSharp()
            .ConfigureFonts(fonts => fonts.AddFont("MaterialIcons-Regular.ttf", Glyphs.FontFamily));

#if ANDROID
        // Android underlines a text field; the search bar is its own outline.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
            "NoUnderline",
            (handler, _) => handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
#endif

        IServiceCollection services = builder.Services;

        // Data.
        services.AddSingleton(_ => new HttpClient { Timeout = OpenMeteoWeatherRepository.DefaultTimeout });
        services.AddSingleton<IWeatherRepository, OpenMeteoWeatherRepository>();
        services.AddSingleton<IKeyValueStore>(_ => new PreferencesStore(Preferences.Default));
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
            deviceCountry: DeviceCountry()));
        services.AddSingleton(provider => new SearchViewModel(
            provider.GetRequiredService<SearchPlaces>(),
            select: provider.GetRequiredService<HomeViewModel>().Select,
            TimeProvider.System));
        services.AddSingleton<HomePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>The country the device is set to, such as "US", if it says.</summary>
    private static string? DeviceCountry()
    {
        // A language with no country, such as plain "en", has no region.
        CultureInfo culture = CultureInfo.CurrentCulture;
        return culture.IsNeutralCulture || culture.Name.Length == 0 ? null : new RegionInfo(culture.Name).TwoLetterISORegionName;
    }
}