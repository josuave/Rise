namespace Rise.Domain;

/// <summary>A day's forecast: high, low, and the WMO weather code.</summary>
public sealed record DailyForecast(DateOnly Date, double MaxF, double MinF, int Code);

/// <summary>The current conditions: temperature and the WMO weather code.</summary>
public sealed record CurrentConditions(double TempF, int Code);

/// <summary>A weather report: current conditions plus the coming days (first entry is today).</summary>
public sealed record WeatherForecast(CurrentConditions Current, IReadOnlyList<DailyForecast> Daily);

/// <summary>A place resolved to coordinates by the geocoder.</summary>
public sealed record GeocodedPlace(string Name, double Latitude, double Longitude);

/// <summary>Human words for Open-Meteo's WMO weather codes. Pure mapping, no I/O.</summary>
public static class WeatherCode
{
    public static string Describe(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing drizzle",
        61 or 63 or 65 => "Rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 => "Snow",
        77 => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Unknown",
    };
}
