using MauiWeather.Data.Repositories;

namespace MauiWeather.Data;

/// <summary>The device's key-value store, through MAUI's <see cref="IPreferences"/>.</summary>
internal sealed class PreferencesStore(IPreferences preferences) : IKeyValueStore
{
    public string? Read(string key)
    {
        return preferences.Get<string?>(key, null);
    }

    public void Write(string key, string value)
    {
        preferences.Set(key, value);
    }
}