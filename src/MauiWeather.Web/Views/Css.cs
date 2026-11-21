using System.Globalization;

namespace MauiWeather.Web.Views;

/// <summary>Numbers written the way a style sheet reads them, whatever the browser's language.</summary>
internal static class Css
{
    /// <summary>"12.5px".</summary>
    public static string Pixels(double value)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{value:0.##}px");
    }

    /// <summary>"0.75".</summary>
    public static string Number(double value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}