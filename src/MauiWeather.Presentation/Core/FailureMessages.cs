using MauiWeather.Domain.Repositories;
using MauiWeather.Presentation.Resources;

namespace MauiWeather.Presentation.Core;

public static class FailureMessages
{
    /// <summary>What to tell the user about each kind of failure.</summary>
    public static string Message(this WeatherFailure failure)
    {
        return failure switch
        {
            WeatherFailure.Timeout => Strings.FailureTimeout,
            WeatherFailure.NoConnection => Strings.FailureNoConnection,
            WeatherFailure.BadResponse => Strings.FailureBadResponse,
            WeatherFailure.Rejected => Strings.FailureRejected,
            WeatherFailure.Unavailable => Strings.FailureUnavailable,
            WeatherFailure.InvalidPlace => Strings.FailureInvalidPlace,
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, "A failure with no message."),
        };
    }
}