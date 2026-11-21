using System.Reflection;
using System.Xml.Linq;

using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;
using MauiWeather.Presentation.Core;
using MauiWeather.Presentation.Resources;

namespace MauiWeather.Tests.Presentation;

public class WordingTests
{
    [Fact]
    public void Every_string_in_the_file_has_a_property_and_every_property_a_string()
    {
        string resx = Path.Combine(
            RepositoryRoot(), "src", "MauiWeather.Presentation", "Resources", "Strings.resx");
        IEnumerable<string> entries = XDocument.Load(resx).Root!.Elements("data").Select(data => data.Attribute("name")!.Value);
        PropertyInfo[] properties = typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Static);

        Assert.Equal(entries.Order(), properties.Select(property => property.Name).Order());
        Assert.All(properties, property => Assert.False(string.IsNullOrEmpty((string?)property.GetValue(null))));
    }

    [Fact]
    public void Every_failure_has_its_own_message()
    {
        WeatherFailure[] failures = Enum.GetValues<WeatherFailure>();

        Assert.Equal(failures.Length, failures.Select(failure => failure.Message()).Distinct().Count());
        Assert.Equal("The weather service took too long to answer.", WeatherFailure.Timeout.Message());
        Assert.Equal("That point is not on the map.", WeatherFailure.InvalidPlace.Message());
    }

    [Fact]
    public void Every_condition_has_its_own_name()
    {
        WeatherCondition[] conditions = Enum.GetValues<WeatherCondition>();

        Assert.Equal(conditions.Length, conditions.Select(condition => condition.Label()).Distinct().Count());
        Assert.Equal("Partly cloudy", WeatherCondition.PartlyCloudy.Label());
        Assert.Equal("Freezing drizzle", WeatherCondition.FreezingDrizzle.Label());
    }

    [Fact]
    public void Clear_and_partly_cloudy_skies_are_drawn_differently_at_night()
    {
        Assert.NotEqual(WeatherCondition.Clear.Icon(isDay: true), WeatherCondition.Clear.Icon(isDay: false));
        Assert.NotEqual(WeatherCondition.PartlyCloudy.Icon(isDay: true), WeatherCondition.PartlyCloudy.Icon(isDay: false));
        Assert.Equal(WeatherCondition.Rain.Icon(isDay: true), WeatherCondition.Rain.Icon(isDay: false));
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MauiWeather.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("MauiWeather.slnx was not found above the tests.");
    }
}