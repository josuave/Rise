using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Weather;
using Xunit;

namespace Rise.Slices.Tests;

public class WeatherWidgetTests : BunitContext
{
    private readonly FakeWidgetConfigStore _configStore = new();

    public WeatherWidgetTests()
    {
        Services.AddScoped<IWidgetConfigStore>(_ => _configStore);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<WeatherConfig>();
        Services.AddScoped<IWeatherClient>(_ => new FakeWeatherClient());
        Services.AddScoped<IPlaceGeocoder>(_ => new FakePlaceGeocoder());
        Services.AddScoped<WeatherService>();
    }

    [Fact]
    public void RendersCurrentAndForecast_WhenCoordsStored()
    {
        _configStore.Stored["Weather"] = """{"Place":"Queensbury, New York, United States","Latitude":43.37729,"Longitude":-73.61317,"ShowForecast":true,"ForecastDays":3}""";

        IRenderedComponent<WeatherWidget> cut = Render<WeatherWidget>();

        cut.WaitForAssertion(() => Assert.Contains("71°F", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Clear sky", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Rain", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Overcast", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GeocodesPlace_WhenCoordsMissing()
    {
        _configStore.Stored["Weather"] = """{"Place":"Queensbury"}""";

        IRenderedComponent<WeatherWidget> cut = Render<WeatherWidget>();

        cut.WaitForAssertion(() => Assert.Contains("71°F", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Queensbury, New York, United States", cut.Markup, StringComparison.Ordinal);
    }
}
