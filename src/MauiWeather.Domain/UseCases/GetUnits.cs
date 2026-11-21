using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Domain.UseCases;

/// <summary>Decides which units to show a forecast in.</summary>
public sealed class GetUnits(ISettingsRepository repository)
{
    /// <summary>
    /// The user's own choice if they have made one; otherwise the units of
    /// the country the device is set to (<paramref name="deviceCountry"/>,
    /// such as "US").
    /// </summary>
    public Units Invoke(string? deviceCountry)
    {
        return repository.Units ?? UnitsRules.ForCountry(deviceCountry);
    }
}