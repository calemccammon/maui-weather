using MauiWeather.Domain.Models;

namespace MauiWeather.Presentation.Home.Models;

/// <summary>The choices the units control offers.</summary>
public enum UnitsUiModel
{
    Celsius,
    Fahrenheit,
}

public static class UnitsUiModelMapping
{
    public static UnitsUiModel ToUiModel(this Units units)
    {
        return units == Units.Imperial ? UnitsUiModel.Fahrenheit : UnitsUiModel.Celsius;
    }

    public static Units ToDomain(this UnitsUiModel units)
    {
        return units == UnitsUiModel.Fahrenheit ? Units.Imperial : Units.Metric;
    }
}