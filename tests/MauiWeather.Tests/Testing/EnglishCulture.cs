using System.Globalization;
using System.Runtime.CompilerServices;

namespace MauiWeather.Tests.Testing;

internal static class EnglishCulture
{
    /// <summary>
    /// The tests compare wording and number formats, so they run in English
    /// whatever the machine is set to.
    /// </summary>
    [ModuleInitializer]
    internal static void Use()
    {
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = english;
        CultureInfo.DefaultThreadCurrentUICulture = english;
    }
}