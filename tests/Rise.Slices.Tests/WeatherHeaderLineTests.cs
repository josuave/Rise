using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Weather;
using Xunit;

namespace Rise.Slices.Tests;

public class WeatherHeaderLineTests : BunitContext
{
    private readonly FakeWidgetConfigStore _configStore = new();
    private readonly FakeWeatherClient _weatherClient = new();

    public WeatherHeaderLineTests()
    {
        Services.AddScoped<IWidgetConfigStore>(_ => _configStore);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<WeatherConfig>();
        Services.AddScoped<IWeatherClient>(_ => _weatherClient);
        Services.AddScoped<IPlaceGeocoder>(_ => new FakePlaceGeocoder());
        Services.AddScoped<WeatherService>();
    }

    [Fact]
    public void RendersHeaderLine_WhenCoordsStored()
    {
        _configStore.Stored["Weather"] = """{"Place":"Queensbury, New York, United States","Latitude":43.37729,"Longitude":-73.61317,"ShowForecast":true,"ForecastDays":3}""";

        IRenderedComponent<WeatherHeaderLine> cut = Render<WeatherHeaderLine>();

        cut.WaitForAssertion(() => Assert.Contains("Queensbury · 71° · Clear sky · H 71° / L 40°", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void GeocodesPlace_WhenCoordsMissing()
    {
        _configStore.Stored["Weather"] = """{"Place":"Queensbury"}""";

        IRenderedComponent<WeatherHeaderLine> cut = Render<WeatherHeaderLine>();

        cut.WaitForAssertion(() => Assert.Contains("Queensbury · 71° · Clear sky · H 71° / L 40°", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void RendersNothing_WhenForecastFails()
    {
        _configStore.Stored["Weather"] = """{"Place":"Queensbury, New York, United States","Latitude":43.37729,"Longitude":-73.61317,"ShowForecast":true,"ForecastDays":3}""";
        var failingClient = new FailingWeatherClient();
        Services.AddScoped<IWeatherClient>(_ => failingClient);

        IRenderedComponent<WeatherHeaderLine> cut = Render<WeatherHeaderLine>();

        cut.WaitForAssertion(() => Assert.Equal(1, failingClient.Calls));
        Assert.Empty(cut.FindAll(".calm-weather-header-line"));
    }

    [Fact]
    public void RendersNothing_WhenDailyEmpty()
    {
        _configStore.Stored["Weather"] = """{"Place":"Queensbury, New York, United States","Latitude":43.37729,"Longitude":-73.61317,"ShowForecast":true,"ForecastDays":3}""";
        _weatherClient.Forecast = new WeatherForecast(
            new CurrentConditions(70.6, 0),
            Array.Empty<DailyForecast>());

        IRenderedComponent<WeatherHeaderLine> cut = Render<WeatherHeaderLine>();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".calm-weather-header-line")));
    }

    /// <summary>Weather client that always fails: no network, no forecast.</summary>
    private sealed class FailingWeatherClient : IWeatherClient
    {
        public int Calls { get; private set; }

        public Task<Result<WeatherForecast>> GetForecastAsync(double latitude, double longitude, int forecastDays, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result<WeatherForecast>.Failure("The forecast service is down."));
        }
    }
}
