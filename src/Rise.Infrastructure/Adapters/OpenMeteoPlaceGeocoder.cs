using System.Globalization;
using System.Text.Json;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>Outbound adapter: Open-Meteo's geocoding API, no key. First match wins; no match is a Failure.</summary>
public sealed class OpenMeteoPlaceGeocoder(HttpClient http) : IPlaceGeocoder
{
    public async Task<Result<GeocodedPlace>> GeocodeAsync(string place, CancellationToken cancellationToken = default)
    {
        string url = string.Create(CultureInfo.InvariantCulture, $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(place)}&count=1");
        string json;
        try
        {
            json = await http.GetStringAsync(new Uri(url), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result<GeocodedPlace>.Failure("Couldn't reach the place search.");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (!root.TryGetProperty("results", out JsonElement results) || results.GetArrayLength() == 0)
            {
                return Result<GeocodedPlace>.Failure($"No place found for '{place}'.");
            }

            JsonElement first = results[0];
            string display = first.GetProperty("name").GetString() ?? place;
            if (first.TryGetProperty("admin1", out JsonElement admin1) && admin1.GetString() is string region)
            {
                display += ", " + region;
            }

            if (first.TryGetProperty("country", out JsonElement country) && country.GetString() is string countryName)
            {
                display += ", " + countryName;
            }

            return Result<GeocodedPlace>.Success(new GeocodedPlace(
                display,
                first.GetProperty("latitude").GetDouble(),
                first.GetProperty("longitude").GetDouble()));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return Result<GeocodedPlace>.Failure("The place search sent an unexpected reply.");
        }
    }
}
