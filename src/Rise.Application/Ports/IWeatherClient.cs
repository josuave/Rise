using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: fetch a forecast for coordinates.</summary>
public interface IWeatherClient
{
    Task<Result<WeatherForecast>> GetForecastAsync(double latitude, double longitude, int forecastDays, CancellationToken cancellationToken = default);
}
