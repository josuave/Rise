using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class WeatherTests
{
    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(2, "Partly cloudy")]
    [InlineData(3, "Overcast")]
    [InlineData(61, "Rain")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(999, "Unknown")]
    public void Describe_MapsCodes(int code, string expected) =>
        Assert.Equal(expected, WeatherCode.Describe(code));

    [Fact]
    public async Task Geocode_BlankPlace_FailsWithoutCallingPort()
    {
        var geocoder = new FakePlaceGeocoder();
        var service = new WeatherService(new FakeWeatherClient(), geocoder);

        Result<GeocodedPlace> result = await service.GeocodeAsync("  ");

        Assert.NotEmpty(ResultAssertions.RequireError(result));
        Assert.False(geocoder.Called);
    }

    [Fact]
    public async Task GetForecast_PassesThroughToClient()
    {
        var client = new FakeWeatherClient();
        var service = new WeatherService(client, new FakePlaceGeocoder());

        Result<WeatherForecast> result = await service.GetForecastAsync(43.37, -73.61, 3);

        _ = ResultAssertions.RequireOk(result);
        Assert.Equal(43.37, client.RequestedLatitude);
        Assert.Equal(3, client.RequestedDays);
    }

    private sealed class FakeWeatherClient : IWeatherClient
    {
        public double RequestedLatitude { get; private set; }

        public int RequestedDays { get; private set; }

        public Task<Result<WeatherForecast>> GetForecastAsync(double latitude, double longitude, int forecastDays, CancellationToken cancellationToken = default)
        {
            RequestedLatitude = latitude;
            RequestedDays = forecastDays;
            return Task.FromResult(Result<WeatherForecast>.Success(
                new WeatherForecast(new CurrentConditions(70.6, 0), Array.Empty<DailyForecast>())));
        }
    }

    private sealed class FakePlaceGeocoder : IPlaceGeocoder
    {
        public bool Called { get; private set; }

        public Task<Result<GeocodedPlace>> GeocodeAsync(string place, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(Result<GeocodedPlace>.Success(new GeocodedPlace(place, 0, 0)));
        }
    }
}
