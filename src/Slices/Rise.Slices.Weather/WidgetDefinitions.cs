using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Weather;

/// <summary>
/// The Weather slice's widget definition: current conditions plus
/// an optional daily forecast from Open-Meteo, hidden by default
/// and shown from the layout editor. The place is typed in
/// Settings, never geolocated.
/// </summary>
public sealed class WeatherWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Weather";

    public string Id => WidgetId;

    public string Title => "Weather";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => false;

    public int Order => 8;

    public Type ComponentType => typeof(WeatherWidget);
}

/// <summary>Shell wiring for the Weather slice's widget.</summary>
public static class WeatherWidgetExtensions
{
    public static IServiceCollection AddWeatherWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, WeatherWidgetDefinition>();
        services.AddScoped<WeatherConfig>();
        return services;
    }
}
