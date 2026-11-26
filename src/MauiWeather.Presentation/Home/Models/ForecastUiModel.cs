using System.Globalization;

using MauiWeather.Domain.Models;
using MauiWeather.Presentation.Core;
using MauiWeather.Presentation.Resources;

namespace MauiWeather.Presentation.Home.Models;

/// <summary>
/// The forecast as the screen shows it: every value already rounded, worded
/// and laid out, so the views only place it.
/// </summary>
public sealed record ForecastUiModel(CurrentUiModel Current, IReadOnlyList<DayUiModel> Days)
{
    public bool HasDays => Days.Count > 0;

    /// <summary>
    /// Translates a metric domain <see cref="Forecast"/> for display in
    /// <paramref name="units"/>, converting it before anything is rounded.
    /// </summary>
    public static ForecastUiModel From(Forecast metric, Units units)
    {
        ArgumentNullException.ThrowIfNull(metric);
        Forecast forecast = metric.InUnits(units);

        // The whole week's range, so each day's bar shows where it sits in it.
        double weekLow = forecast.Days.Count == 0 ? 0 : forecast.Days.Min(day => day.Low);
        double weekHigh = forecast.Days.Count == 0 ? 0 : forecast.Days.Max(day => day.High);

        return new ForecastUiModel(
            CurrentUiModel.From(forecast.Current, units),
            [.. forecast.Days.Select(day => DayUiModel.From(day, forecast.IsToday(day), weekLow, weekHigh))]);
    }

    /// <summary>A whole number of degrees or of speed: halves round away from zero.</summary>
    internal static int Whole(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}

/// <summary>The "right now" block at the top of the forecast.</summary>
/// <param name="Icon">A character of the icon font.</param>
/// <param name="Temperature">"34°F". The one place the scale is spelled out; the rest of the forecast is in the same one.</param>
/// <param name="Condition">"Overcast".</param>
/// <param name="FeelsLike">"Feels like 28°".</param>
/// <param name="Wind">"Wind 2 mph".</param>
/// <param name="Description">The whole block as one sentence for a screen reader.</param>
public sealed record CurrentUiModel(
    string Icon,
    string Temperature,
    string Condition,
    string FeelsLike,
    string Wind,
    string Description)
{
    public static CurrentUiModel From(CurrentConditions current, Units units)
    {
        int temperature = ForecastUiModel.Whole(current.Temperature);
        int feelsLike = ForecastUiModel.Whole(current.FeelsLike);
        int wind = ForecastUiModel.Whole(current.WindSpeed);
        (string shown, string spoken, string speed) = units switch
        {
            Units.Imperial => (
                Strings.Format(nameof(Strings.TemperatureFahrenheit), temperature),
                Strings.Format(nameof(Strings.SpokenFahrenheit), temperature),
                Strings.Format(nameof(Strings.SpeedMph), wind)),
            _ => (
                Strings.Format(nameof(Strings.TemperatureCelsius), temperature),
                Strings.Format(nameof(Strings.SpokenCelsius), temperature),
                Strings.Format(nameof(Strings.SpeedKmh), wind)),
        };
        string condition = current.Condition.Label();

        return new CurrentUiModel(
            Icon: current.Condition.Icon(current.IsDay),
            Temperature: shown,
            Condition: condition,
            FeelsLike: Strings.Format(nameof(Strings.FeelsLike), feelsLike),
            Wind: Strings.Format(nameof(Strings.Wind), speed),
            Description: Strings.Format(nameof(Strings.CurrentDescription), condition, spoken, feelsLike, speed));
    }
}

/// <summary>One row of the week.</summary>
/// <param name="DayLabel">"Today" for the day the forecast was made on, otherwise a short weekday: "Sat".</param>
/// <param name="Icon">A character of the icon font.</param>
/// <param name="Chance">"47%", or empty when there is no chance worth showing.</param>
/// <param name="Low">"30°".</param>
/// <param name="High">"68°".</param>
/// <param name="BarStart">Where this day's low falls within the week's range, from 0 (the week's lowest) to 1.</param>
/// <param name="BarEnd">Where this day's high falls within the week's range, from 0 to 1 (the week's highest).</param>
/// <param name="Description">The whole row as one sentence for a screen reader.</param>
public sealed record DayUiModel(
    string DayLabel,
    string Icon,
    string Chance,
    string Low,
    string High,
    double BarStart,
    double BarEnd,
    string Description)
{
    public static DayUiModel From(DailyForecast day, bool isToday, double weekLow, double weekHigh)
    {
        int low = ForecastUiModel.Whole(day.Low);
        int high = ForecastUiModel.Whole(day.High);
        int chance = day.PrecipitationChance ?? 0;
        double span = weekHigh - weekLow;
        CultureInfo culture = CultureInfo.CurrentUICulture;
        string dayName = isToday ? Strings.Today : day.Date.ToString("dddd", culture);
        string condition = day.Condition.Label();

        return new DayUiModel(
            DayLabel: isToday ? Strings.Today : day.Date.ToString("ddd", culture),
            Icon: day.Condition.Icon(),
            Chance: chance > 0 ? Strings.Format(nameof(Strings.PrecipitationChance), chance) : "",
            Low: Strings.Format(nameof(Strings.Temperature), low),
            High: Strings.Format(nameof(Strings.Temperature), high),
            // A week with one temperature throughout fills the whole bar.
            BarStart: span <= 0 ? 0 : (day.Low - weekLow) / span,
            BarEnd: span <= 0 ? 1 : (day.High - weekLow) / span,
            Description: chance > 0
                ? Strings.Format(nameof(Strings.DayDescriptionWithChance), dayName, condition, low, high, chance)
                : Strings.Format(nameof(Strings.DayDescription), dayName, condition, low, high));
    }
}