using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>Weather lookups for the widget and its settings: forecast by coordinates, geocode by place name.</summary>
public sealed class WeatherService(IWeatherClient client, IPlaceGeocoder geocoder)
{
    public Task<Result<WeatherForecast>> GetForecastAsync(double latitude, double longitude, int forecastDays, CancellationToken cancellationToken = default) =>
        client.GetForecastAsync(latitude, longitude, forecastDays, cancellationToken);

    public Task<Result<GeocodedPlace>> GeocodeAsync(string place, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(place)
            ? Task.FromResult(Result<GeocodedPlace>.Failure("Enter a place name."))
            : geocoder.GeocodeAsync(place, cancellationToken);
}
