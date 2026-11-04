using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Weather;
using Xunit;

namespace Rise.Slices.Tests;

public class WeatherSettingsTests : BunitContext
{
    private readonly FakeWidgetConfigStore _configStore = new();

    public WeatherSettingsTests()
    {
        Services.AddScoped<IWidgetConfigStore>(_ => _configStore);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<WeatherConfig>();
        Services.AddScoped<IPlaceGeocoder>(_ => new FakePlaceGeocoder());
        Services.AddScoped<IWeatherClient>(_ => new FakeWeatherClient());
        Services.AddScoped<WeatherService>();
    }

    [Fact]
    public void SavePlace_ResolvesAndPersistsCoords()
    {
        IRenderedComponent<WeatherSettings> cut = Render<WeatherSettings>();
        cut.Find("[aria-label=\"Weather place\"]").Change("Queensbury");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Save place").Click();

        cut.WaitForAssertion(() => Assert.True(
            _configStore.Stored.TryGetValue("Weather", out string? json)
                && json.Contains("Latitude\":43.37729", StringComparison.Ordinal)));
        Assert.Contains("Queensbury, New York, United States", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ForecastToggle_SwitchesToCurrentOnly_AndPersistsIt()
    {
        IRenderedComponent<WeatherSettings> cut = Render<WeatherSettings>();

        cut.Find("[aria-label=\"Show current conditions only\"]").Click();

        cut.WaitForAssertion(() => Assert.True(
            _configStore.Stored.TryGetValue("Weather", out string? json)
                && json.Contains("ShowForecast\":false", StringComparison.Ordinal)));
    }
}
