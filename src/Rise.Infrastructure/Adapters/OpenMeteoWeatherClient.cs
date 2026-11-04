using System.Globalization;
using System.Text.Json;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>Outbound adapter: Open-Meteo's forecast API, no key. A failed fetch or an unparsable reply is a Failure, never an exception to the widget.</summary>
public sealed class OpenMeteoWeatherClient(HttpClient http) : IWeatherClient
{
    public async Task<Result<WeatherForecast>> GetForecastAsync(double latitude, double longitude, int forecastDays, CancellationToken cancellationToken = default)
    {
        string url = string.Create(CultureInfo.InvariantCulture, $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min&temperature_unit=fahrenheit&timezone=auto&forecast_days={forecastDays}");
        string json;
        try
        {
            json = await http.GetStringAsync(new Uri(url), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result<WeatherForecast>.Failure("Couldn't reach the weather service.");
        }

        try
        {
            return Result<WeatherForecast>.Success(Parse(json));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return Result<WeatherForecast>.Failure("The weather service sent an unexpected reply.");
        }
    }

    private static WeatherForecast Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        JsonElement current = root.GetProperty("current");
        var conditions = new CurrentConditions(
            current.GetProperty("temperature_2m").GetDouble(),
            current.GetProperty("weather_code").GetInt32());

        JsonElement daily = root.GetProperty("daily");
        JsonElement[] dateElements = daily.GetProperty("time").EnumerateArray().ToArray();
        JsonElement[] codeElements = daily.GetProperty("weather_code").EnumerateArray().ToArray();
        JsonElement[] maxElements = daily.GetProperty("temperature_2m_max").EnumerateArray().ToArray();
        JsonElement[] minElements = daily.GetProperty("temperature_2m_min").EnumerateArray().ToArray();
        int count = Math.Min(Math.Min(dateElements.Length, codeElements.Length), Math.Min(maxElements.Length, minElements.Length));

        var days = new DailyForecast[count];
        for (int i = 0; i < count; i++)
        {
            string dateText = dateElements[i].GetString() ?? throw new JsonException("Missing daily date.");

            days[i] = new DailyForecast(
                DateOnly.Parse(dateText, CultureInfo.InvariantCulture),
                maxElements[i].GetDouble(),
                minElements[i].GetDouble(),
                codeElements[i].GetInt32());
        }

        return new WeatherForecast(conditions, days);
    }
}
