using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;

using AndroidX.Core.View;

namespace MauiWeather;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        MatchSystemBarsToTheme(Resources?.Configuration);
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        MatchSystemBarsToTheme(newConfig);
    }

    /// <summary>
    /// The page's background shows through the status and navigation bars, so
    /// their icons are made dark over the light theme and light over the dark.
    /// </summary>
    private void MatchSystemBarsToTheme(Configuration? configuration)
    {
        if (Window is not { } window || configuration is null)
        {
            return;
        }

        bool isLight = (configuration.UiMode & UiMode.NightMask) != UiMode.NightYes;
        if (WindowCompat.GetInsetsController(window, window.DecorView) is { } bars)
        {
            bars.AppearanceLightStatusBars = isLight;
            bars.AppearanceLightNavigationBars = isLight;
        }
    }
}