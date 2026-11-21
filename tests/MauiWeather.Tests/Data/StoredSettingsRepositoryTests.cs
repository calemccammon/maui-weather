using MauiWeather.Data.Repositories;
using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Tests.Data;

public class StoredSettingsRepositoryTests
{
    private sealed class MemoryStore : IKeyValueStore
    {
        public Dictionary<string, string> Values { get; } = [];

        public bool Broken { get; set; }

        public string? Read(string key)
        {
            return Values.GetValueOrDefault(key);
        }

        public void Write(string key, string value)
        {
            if (Broken)
            {
                throw new InvalidOperationException("The store is full.");
            }

            Values[key] = value;
        }
    }

    private readonly MemoryStore _store = new();

    [Fact]
    public void There_are_no_units_until_some_are_chosen()
    {
        Assert.Null(new StoredSettingsRepository(_store).Units);
    }

    [Fact]
    public void Chosen_units_are_there_for_the_next_launch()
    {
        new StoredSettingsRepository(_store).SaveUnits(Units.Imperial);

        Assert.Equal(Units.Imperial, new StoredSettingsRepository(_store).Units);

        new StoredSettingsRepository(_store).SaveUnits(Units.Metric);

        Assert.Equal(Units.Metric, new StoredSettingsRepository(_store).Units);
    }

    [Fact]
    public void A_stored_word_this_version_does_not_know_counts_as_no_choice()
    {
        _store.Values["units"] = "kelvin";

        Assert.Null(new StoredSettingsRepository(_store).Units);
    }

    [Fact]
    public void A_store_that_fails_is_reported_as_a_setting_not_saved()
    {
        _store.Broken = true;

        Assert.Throws<SettingsException>(() => new StoredSettingsRepository(_store).SaveUnits(Units.Metric));
    }
}