using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Domain.UseCases;

/// <summary>Records the units the user chose, for this launch and later ones.</summary>
public sealed class SetUnits(ISettingsRepository repository)
{
    public void Invoke(Units units)
    {
        repository.SaveUnits(units);
    }
}