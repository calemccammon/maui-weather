using MauiWeather.Domain.Models;

namespace MauiWeather.Domain.Repositories;

/// <summary>What the user has chosen for the app, kept between launches.</summary>
public interface ISettingsRepository
{
    /// <summary>The units the user chose, or null if they have never chosen.</summary>
    Units? Units { get; }

    /// <summary>Fails with a <see cref="SettingsException"/> if the choice could not be kept.</summary>
    void SaveUnits(Units units);
}

/// <summary>Thrown when the device would not store a setting.</summary>
public sealed class SettingsException(Exception? innerException = null)
    : Exception("The setting was not saved", innerException);