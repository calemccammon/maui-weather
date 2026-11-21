using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Data.Repositories;

/// <summary>
/// The device's small key-value store, as far as this layer needs it. The
/// app supplies the real one.
/// </summary>
public interface IKeyValueStore
{
    /// <summary>The text stored under <paramref name="key"/>, or null if there is none.</summary>
    string? Read(string key);

    void Write(string key, string value);
}

/// <summary>Keeps settings in the device's key-value store.</summary>
public sealed class StoredSettingsRepository(IKeyValueStore store) : ISettingsRepository
{
    private const string UnitsKey = "units";

    // The stored words are this file's own, so renaming the enum's values
    // cannot orphan a saved choice.
    private const string Metric = "metric";
    private const string Imperial = "imperial";

    public Units? Units => store.Read(UnitsKey) switch
    {
        Metric => Domain.Models.Units.Metric,
        Imperial => Domain.Models.Units.Imperial,
        // Never chosen, or written by a version with a choice this one lacks.
        _ => null,
    };

    public void SaveUnits(Units units)
    {
        try
        {
            store.Write(UnitsKey, units == Domain.Models.Units.Imperial ? Imperial : Metric);
        }
#pragma warning disable CA1031 // Each platform's store fails in its own way.
        catch (Exception e)
#pragma warning restore CA1031
        {
            throw new SettingsException(e);
        }
    }
}