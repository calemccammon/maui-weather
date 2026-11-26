using System.Net;
using System.Web;

using MauiWeather.Data.Repositories;
using MauiWeather.Domain.Models;
using MauiWeather.Domain.Repositories;

namespace MauiWeather.Tests.Data;

public class OpenMeteoWeatherRepositoryTests
{
    private const string ForecastBody = """
        {
          "latitude": 35.6, "timezone": "America/New_York",
          "current": {
            "time": "2026-11-18T09:00", "interval": 900, "temperature_2m": 35.8,
            "apparent_temperature": 31.3, "weather_code": 3, "wind_speed_10m": 4.4, "is_day": 1
          },
          "daily": {
            "time": ["2026-11-18", "2026-11-19", "2026-11-20"],
            "weather_code": [0, 61, null],
            "temperature_2m_max": [64.3, 58.1, null],
            "temperature_2m_min": [30.2, 41.9, null], "precipitation_probability_max": [0, 46.6, null]
          }
        }
        """;

    /// <summary>Answers every request the same way, and records the URLs.</summary>
    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> answer)
        : HttpMessageHandler
    {
        public List<Uri> Urls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Urls.Add(request.RequestUri!);
            return answer(request, cancellationToken);
        }
    }

    private readonly List<Uri> _urls = [];

    /// <summary>A repository whose every request is answered with <paramref name="body"/>.</summary>
    private OpenMeteoWeatherRepository Answering(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        return Over((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) }));
    }

    private OpenMeteoWeatherRepository Over(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> answer,
        TimeSpan? timeout = null)
    {
        StubHandler handler = new((request, cancellationToken) =>
        {
            _urls.Add(request.RequestUri!);
            return answer(request, cancellationToken);
        });
        return new OpenMeteoWeatherRepository(
            new HttpClient(handler) { Timeout = timeout ?? OpenMeteoWeatherRepository.DefaultTimeout });
    }

    private static Task<Forecast> ForecastSomewhere(OpenMeteoWeatherRepository repository)
    {
        return repository.GetForecastAsync(35.6, -82.55, CancellationToken.None);
    }

    private static async Task<WeatherException> FailureOf(Func<Task> request)
    {
        return await Assert.ThrowsAsync<WeatherException>(request);
    }

    // Searching for places.

    [Fact]
    public async Task A_search_asks_the_geocoding_service_and_reads_its_results()
    {
        OpenMeteoWeatherRepository repository = Answering("""
            {"results": [
              {"id": 1, "name": "Asheville", "latitude": 35.6, "longitude": -82.55,
               "admin1": "North Carolina", "country": "United States"},
              {"name": "Ashford", "latitude": 51.15, "longitude": 0.87}
            ], "generationtime_ms": 0.4}
            """);

        IReadOnlyList<Place> places = await repository.SearchPlacesAsync("ash ford", CancellationToken.None);

        Assert.Equal(
            [
                new Place(35.6, -82.55, "Asheville", "North Carolina", "United States"),
                new Place(51.15, 0.87, "Ashford"),
            ],
            places);
        Uri url = Assert.Single(_urls);
        Assert.Equal("geocoding-api.open-meteo.com", url.Host);
        Assert.Equal("/v1/search", url.AbsolutePath);
        Assert.Equal("ash ford", HttpUtility.ParseQueryString(url.Query)["name"]);
    }

    [Fact]
    public async Task A_search_response_with_no_results_key_is_no_matches()
    {
        Assert.Empty(await Answering("""{"generationtime_ms": 0.4}""").SearchPlacesAsync("zzzz", CancellationToken.None));
    }

    [Theory]
    [InlineData("""{"results": "none"}""")]
    [InlineData("""{"results": ["Asheville"]}""")]
    [InlineData("""{"results": [{"latitude": 1, "longitude": 2}]}""")]
    [InlineData("""{"results": [{"name": "A", "latitude": "north", "longitude": 2}]}""")]
    public async Task Search_results_of_the_wrong_shape_are_a_bad_response(string body)
    {
        WeatherException error = await FailureOf(() => Answering(body).SearchPlacesAsync("ash", CancellationToken.None));

        Assert.Equal(WeatherFailure.BadResponse, error.Failure);
    }

    // Getting a forecast.

    [Fact]
    public async Task A_forecast_has_codes_as_conditions_and_columns_as_days_and_leaves_out_a_day_with_no_temperatures()
    {
        Forecast forecast = await ForecastSomewhere(Answering(ForecastBody));

        Assert.Equal(
            new CurrentConditions(new DateTime(2026, 11, 18, 9, 0, 0), 35.8, 31.3, 4.4, WeatherCondition.Overcast, IsDay: true),
            forecast.Current);
        Assert.Equal(
            [
                new DailyForecast(new DateOnly(2026, 11, 18), 30.2, 64.3, WeatherCondition.Clear, 0),
                new DailyForecast(new DateOnly(2026, 11, 19), 41.9, 58.1, WeatherCondition.Rain, 47),
            ],
            forecast.Days);
    }

    [Fact]
    public async Task A_forecast_is_asked_for_in_metric_units_on_the_places_own_clock()
    {
        await ForecastSomewhere(Answering(ForecastBody));

        Assert.Equal("api.open-meteo.com", _urls[0].Host);
        Assert.Equal("/v1/forecast", _urls[0].AbsolutePath);
        System.Collections.Specialized.NameValueCollection query = HttpUtility.ParseQueryString(_urls[0].Query);
        Assert.Equal("35.6", query["latitude"]);
        Assert.Equal("-82.55", query["longitude"]);
        Assert.Equal("auto", query["timezone"]);
        Assert.Equal("celsius", query["temperature_unit"]);
        Assert.Equal("kmh", query["wind_speed_unit"]);
    }

    [Fact]
    public async Task Coordinates_are_sent_with_a_decimal_point_whatever_the_devices_language()
    {
        System.Globalization.CultureInfo before = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            await ForecastSomewhere(Answering(ForecastBody));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = before;
        }

        Assert.Equal("35.6", HttpUtility.ParseQueryString(_urls[0].Query)["latitude"]);
    }

    [Fact]
    public async Task A_forecast_copes_without_the_column_of_precipitation_chances()
    {
        string body = ForecastBody.Replace(""", "precipitation_probability_max": [0, 46.6, null]""", "", StringComparison.Ordinal);

        Forecast forecast = await ForecastSomewhere(Answering(body));

        Assert.Equal(new DailyForecast(new DateOnly(2026, 11, 18), 30.2, 64.3, WeatherCondition.Clear), forecast.Days[0]);
    }

    [Theory]
    [InlineData("no current conditions", "\"current\"", "\"now\"")]
    [InlineData("no days", "\"daily\"", "\"weekly\"")]
    [InlineData("a temperature that is text", "35.8", "\"warm\"")]
    [InlineData("a time that is not a date", "2026-11-18T09:00", "this morning")]
    [InlineData("a day that is not a date", "\"2026-11-19\"", "\"Thu\"")]
    [InlineData("columns of different lengths", "[0, 61, null]", "[0, 61]")]
    [InlineData("a column that is not a list", "[64.3, 58.1, null]", "64.3")]
    public async Task A_forecast_of_the_wrong_shape_is_a_bad_response(string what, string find, string replace)
    {
        string body = ForecastBody.Replace(find, replace, StringComparison.Ordinal);
        Assert.NotEqual(ForecastBody, body);

        WeatherException error = await FailureOf(() => ForecastSomewhere(Answering(body)));

        Assert.True(error.Failure == WeatherFailure.BadResponse, what);
    }

    // When the service does not answer properly.

    [Fact]
    public async Task The_services_own_refusal_is_reported_with_its_reason_kept_for_the_logs()
    {
        OpenMeteoWeatherRepository repository = Answering(
            """{"error": true, "reason": "Latitude must be in range."}""",
            HttpStatusCode.BadRequest);

        WeatherException error = await FailureOf(() => ForecastSomewhere(repository));

        Assert.Equal(WeatherFailure.Rejected, error.Failure);
        Assert.Equal("Latitude must be in range.", error.Detail);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("<html>Not found</html>")]
    public async Task An_error_status_with_no_reason_is_a_refusal(string body)
    {
        WeatherException error = await FailureOf(
            () => Answering(body, HttpStatusCode.NotFound).SearchPlacesAsync("ash", CancellationToken.None));

        Assert.Equal((WeatherFailure.Rejected, "HTTP 404"), (error.Failure, error.Detail));
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "<html>Service unavailable</html>")]
    [InlineData(HttpStatusCode.BadGateway, """{"error": true, "reason": "Upstream failed."}""")]
    [InlineData(HttpStatusCode.TooManyRequests, """{"error": true, "reason": "Minutely limit exceeded."}""")]
    public async Task A_server_error_or_too_many_requests_is_the_services_own_trouble(HttpStatusCode status, string body)
    {
        WeatherException error = await FailureOf(() => ForecastSomewhere(Answering(body, status)));

        Assert.Equal((WeatherFailure.Unavailable, $"HTTP {(int)status}"), (error.Failure, error.Detail));
    }

    [Theory]
    [InlineData("<html>Bad gateway</html>")]
    [InlineData("[1, 2, 3]")]
    [InlineData("null")]
    public async Task An_answer_that_is_not_a_JSON_object_is_a_bad_response(string body)
    {
        WeatherException error = await FailureOf(() => Answering(body).SearchPlacesAsync("ash", CancellationToken.None));

        Assert.Equal(WeatherFailure.BadResponse, error.Failure);
    }

    [Fact]
    public async Task A_request_that_cannot_be_made_is_no_connection()
    {
        OpenMeteoWeatherRepository repository = Over((_, _) => throw new HttpRequestException("Network is unreachable"));

        WeatherException error = await FailureOf(() => repository.SearchPlacesAsync("ash", CancellationToken.None));

        Assert.Equal(WeatherFailure.NoConnection, error.Failure);
    }

    [Fact]
    public async Task An_answer_that_takes_too_long_is_a_timeout()
    {
        OpenMeteoWeatherRepository repository = Over(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new HttpResponseMessage();
            },
            timeout: TimeSpan.FromMilliseconds(50));

        WeatherException error = await FailureOf(() => repository.SearchPlacesAsync("ash", CancellationToken.None));

        Assert.Equal(WeatherFailure.Timeout, error.Failure);
    }

    [Fact]
    public async Task A_request_the_caller_gives_up_on_is_cancelled_and_not_a_failure()
    {
        using CancellationTokenSource cancellation = new();
        OpenMeteoWeatherRepository repository = Over(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage();
        });

        Task<IReadOnlyList<Place>> search = repository.SearchPlacesAsync("ash", cancellation.Token);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => search);
    }
}