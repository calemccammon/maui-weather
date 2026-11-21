using System.Globalization;
using System.Resources;

namespace MauiWeather.Presentation.Resources;

/// <summary>
/// The app's wording, from <c>Strings.resx</c>, in the device's language.
/// </summary>
/// <remarks>
/// One property per entry of the file, so a misspelt name does not compile.
/// A test checks that the two lists match.
/// </remarks>
public static class Strings
{
    private static readonly ResourceManager Manager = new(typeof(Strings).FullName!, typeof(Strings).Assembly);

    /// <summary>Weather</summary>
    public static string AppTitle => Get(nameof(AppTitle));

    /// <summary>Search for a place</summary>
    public static string SearchHint => Get(nameof(SearchHint));

    /// <summary>Type a city, town or postcode</summary>
    public static string SearchPrompt => Get(nameof(SearchPrompt));

    /// <summary>Nothing found for "{0}"</summary>
    public static string SearchNothingFound => Get(nameof(SearchNothingFound));

    /// <summary>Searching</summary>
    public static string SearchSearching => Get(nameof(SearchSearching));

    /// <summary>Clear place</summary>
    public static string ClearPlace => Get(nameof(ClearPlace));

    /// <summary>Try again</summary>
    public static string TryAgain => Get(nameof(TryAgain));

    /// <summary>Search for a place, or press and hold on the map, to see its forecast.</summary>
    public static string ForecastHint => Get(nameof(ForecastHint));

    /// <summary>Loading the forecast</summary>
    public static string ForecastLoading => Get(nameof(ForecastLoading));

    /// <summary>This week</summary>
    public static string ForecastThisWeek => Get(nameof(ForecastThisWeek));

    /// <summary>No forecast for the coming days.</summary>
    public static string ForecastNoDays => Get(nameof(ForecastNoDays));

    /// <summary>Weather data by Open-Meteo.com</summary>
    public static string ForecastCredit => Get(nameof(ForecastCredit));

    /// <summary>Show more of the forecast</summary>
    public static string ForecastExpand => Get(nameof(ForecastExpand));

    /// <summary>Show more of the map</summary>
    public static string ForecastCollapse => Get(nameof(ForecastCollapse));

    /// <summary>Units</summary>
    public static string UnitsLabel => Get(nameof(UnitsLabel));

    /// <summary>°C</summary>
    public static string UnitsCelsius => Get(nameof(UnitsCelsius));

    /// <summary>°F</summary>
    public static string UnitsFahrenheit => Get(nameof(UnitsFahrenheit));

    /// <summary>Celsius and km/h</summary>
    public static string UnitsCelsiusName => Get(nameof(UnitsCelsiusName));

    /// <summary>Fahrenheit and mph</summary>
    public static string UnitsFahrenheitName => Get(nameof(UnitsFahrenheitName));

    /// <summary>Map. Press and hold to pick a point.</summary>
    public static string MapLabel => Get(nameof(MapLabel));

    /// <summary>© OpenStreetMap contributors</summary>
    public static string MapCredit => Get(nameof(MapCredit));

    /// <summary>{0}, {1}</summary>
    public static string PlaceCoordinates => Get(nameof(PlaceCoordinates));

    /// <summary>{0}°N</summary>
    public static string PlaceNorth => Get(nameof(PlaceNorth));

    /// <summary>{0}°S</summary>
    public static string PlaceSouth => Get(nameof(PlaceSouth));

    /// <summary>{0}°E</summary>
    public static string PlaceEast => Get(nameof(PlaceEast));

    /// <summary>{0}°W</summary>
    public static string PlaceWest => Get(nameof(PlaceWest));

    /// <summary>Today</summary>
    public static string Today => Get(nameof(Today));

    /// <summary>{0}°</summary>
    public static string Temperature => Get(nameof(Temperature));

    /// <summary>{0}°C</summary>
    public static string TemperatureCelsius => Get(nameof(TemperatureCelsius));

    /// <summary>{0}°F</summary>
    public static string TemperatureFahrenheit => Get(nameof(TemperatureFahrenheit));

    /// <summary>Feels like {0}°</summary>
    public static string FeelsLike => Get(nameof(FeelsLike));

    /// <summary>Wind {0}</summary>
    public static string Wind => Get(nameof(Wind));

    /// <summary>{0} km/h</summary>
    public static string SpeedKmh => Get(nameof(SpeedKmh));

    /// <summary>{0} mph</summary>
    public static string SpeedMph => Get(nameof(SpeedMph));

    /// <summary>{0}%</summary>
    public static string PrecipitationChance => Get(nameof(PrecipitationChance));

    /// <summary>{0} degrees Celsius</summary>
    public static string SpokenCelsius => Get(nameof(SpokenCelsius));

    /// <summary>{0} degrees Fahrenheit</summary>
    public static string SpokenFahrenheit => Get(nameof(SpokenFahrenheit));

    /// <summary>Now: {0}, {1}, feels like {2}, wind {3}</summary>
    public static string CurrentDescription => Get(nameof(CurrentDescription));

    /// <summary>{0}: {1}, low {2}, high {3} degrees</summary>
    public static string DayDescription => Get(nameof(DayDescription));

    /// <summary>{0}: {1}, low {2}, high {3} degrees, {4} percent chance of precipitation</summary>
    public static string DayDescriptionWithChance => Get(nameof(DayDescriptionWithChance));

    /// <summary>Clear</summary>
    public static string ConditionClear => Get(nameof(ConditionClear));

    /// <summary>Mostly clear</summary>
    public static string ConditionMostlyClear => Get(nameof(ConditionMostlyClear));

    /// <summary>Partly cloudy</summary>
    public static string ConditionPartlyCloudy => Get(nameof(ConditionPartlyCloudy));

    /// <summary>Overcast</summary>
    public static string ConditionOvercast => Get(nameof(ConditionOvercast));

    /// <summary>Fog</summary>
    public static string ConditionFog => Get(nameof(ConditionFog));

    /// <summary>Drizzle</summary>
    public static string ConditionDrizzle => Get(nameof(ConditionDrizzle));

    /// <summary>Freezing drizzle</summary>
    public static string ConditionFreezingDrizzle => Get(nameof(ConditionFreezingDrizzle));

    /// <summary>Rain</summary>
    public static string ConditionRain => Get(nameof(ConditionRain));

    /// <summary>Freezing rain</summary>
    public static string ConditionFreezingRain => Get(nameof(ConditionFreezingRain));

    /// <summary>Snow</summary>
    public static string ConditionSnow => Get(nameof(ConditionSnow));

    /// <summary>Rain showers</summary>
    public static string ConditionRainShowers => Get(nameof(ConditionRainShowers));

    /// <summary>Snow showers</summary>
    public static string ConditionSnowShowers => Get(nameof(ConditionSnowShowers));

    /// <summary>Thunderstorm</summary>
    public static string ConditionThunderstorm => Get(nameof(ConditionThunderstorm));

    /// <summary>Unknown</summary>
    public static string ConditionUnknown => Get(nameof(ConditionUnknown));

    /// <summary>The weather service took too long to answer.</summary>
    public static string FailureTimeout => Get(nameof(FailureTimeout));

    /// <summary>Could not reach the weather service. Check your connection.</summary>
    public static string FailureNoConnection => Get(nameof(FailureNoConnection));

    /// <summary>The weather service sent an answer this app could not read.</summary>
    public static string FailureBadResponse => Get(nameof(FailureBadResponse));

    /// <summary>The weather service refused the request.</summary>
    public static string FailureRejected => Get(nameof(FailureRejected));

    /// <summary>The weather service is having trouble. Try again in a moment.</summary>
    public static string FailureUnavailable => Get(nameof(FailureUnavailable));

    /// <summary>That point is not on the map.</summary>
    public static string FailureInvalidPlace => Get(nameof(FailureInvalidPlace));

    /// <summary>The string called <paramref name="name"/>, with <paramref name="arguments"/> in its gaps.</summary>
    public static string Format(string name, params object[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(name), arguments);
    }

    private static string Get(string name)
    {
        return Manager.GetString(name, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"Strings.resx has no entry named {name}.");
    }
}