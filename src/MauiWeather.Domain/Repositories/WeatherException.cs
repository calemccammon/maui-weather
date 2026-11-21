namespace MauiWeather.Domain.Repositories;

/// <summary>The ways getting weather can fail, as far as the app cares.</summary>
public enum WeatherFailure
{
    /// <summary>No answer in time.</summary>
    Timeout,

    /// <summary>The service could not be reached at all.</summary>
    NoConnection,

    /// <summary>An answer came, but not one the app understands.</summary>
    BadResponse,

    /// <summary>The service refused the request.</summary>
    Rejected,

    /// <summary>The service is in trouble or too busy; trying later may work.</summary>
    Unavailable,

    /// <summary>The place asked about is not a point on the globe.</summary>
    InvalidPlace,
}

/// <summary>
/// Thrown when a place search or a forecast cannot be had.
/// </summary>
/// <remarks>
/// It says what kind of failure it was; wording that for the user is the
/// presentation layer's job.
/// </remarks>
public sealed class WeatherException(WeatherFailure failure, string? detail = null, Exception? innerException = null)
    : Exception(detail is null ? failure.ToString() : $"{failure}: {detail}", innerException)
{
    public WeatherFailure Failure { get; } = failure;

    /// <summary>Anything more the source said, for logs. Not for showing to the user.</summary>
    public string? Detail { get; } = detail;
}