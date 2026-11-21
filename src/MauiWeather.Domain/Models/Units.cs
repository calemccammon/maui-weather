namespace MauiWeather.Domain.Models;

/// <summary>The two systems of units the app can show a forecast in.</summary>
public enum Units
{
    Metric,
    Imperial,
}

public static class UnitsRules
{
    /// <summary>The United States, Liberia and Myanmar use Fahrenheit and miles.</summary>
    private static readonly HashSet<string> ImperialCountries = ["US", "LR", "MM"];

    /// <summary>The units of the country with this code, such as "US".</summary>
    public static Units ForCountry(string? countryCode)
    {
        return countryCode is not null && ImperialCountries.Contains(countryCode) ? Units.Imperial : Units.Metric;
    }
}